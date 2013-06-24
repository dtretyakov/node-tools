using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Common;
using DebugEngine.Management;
using DebugEngine.Node;
using DebugEngine.Node.Debugger;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace DebugEngine.Engine
{
    // AD7Engine is the primary entrypoint object for the debugging engine. 
    //
    // It implements:
    //
    // IDebugEngine2: This interface represents a debug engine (DE). It is used to manage various aspects of a debugging session, 
    // from creating breakpoints to setting and clearing exceptions.
    //
    // IDebugEngineLaunch2: Used by a debug engine (DE) to launch and terminate programs.
    //
    // IDebugProgram3: This interface represents a program that is running in a process. Since this engine only debugs one process at a time and each 
    // process only contains one program, it is implemented on the engine.

    [ComVisible(true)]
    [Guid("8355452D-6D2F-41b0-89B8-BB2AA2529E94")]
    public sealed class AD7Engine : IDebugEngine2, IDebugEngineLaunch2, IDebugProgram3, IDebugSymbolSettings100
    {
        /// <summary>
        ///     Specifies the version of the language which is being debugged.  One of
        ///     V24, V25, V26, V27, V30, V31 or V32.
        /// </summary>
        public const string VersionSetting = "VERSION";

        /// <summary>
        ///     Specifies whether the process should prompt for input before exiting on an abnormal exit.
        /// </summary>
        public const string WaitOnAbnormalExitSetting = "WAIT_ON_ABNORMAL_EXIT";

        /// <summary>
        ///     Specifies whether the process should prompt for input before exiting on a normal exit.
        /// </summary>
        public const string WaitOnNormalExitSetting = "WAIT_ON_NORMAL_EXIT";

        /// <summary>
        ///     Specifies if the output should be redirected to the visual studio output window.
        /// </summary>
        public const string RedirectOutputSetting = "REDIRECT_OUTPUT";

        /// <summary>
        ///     Specifies if the debugger should break on SystemExit exceptions with an exit code of zero.
        /// </summary>
        public const string BreakSystemExitZero = "BREAK_SYSTEMEXIT_ZERO";

        /// <summary>
        ///     Specifies if the debugger should step/break into std lib code.
        /// </summary>
        public const string DebugStdLib = "DEBUG_STDLIB";

        /// <summary>
        ///     Specifies options which should be passed to the node.js interpreter before the script.  If
        ///     the interpreter options should include a semicolon then it should be escaped as a double
        ///     semi-colon.
        /// </summary>
        public const string InterpreterOptions = "INTERPRETER_OPTIONS";

        public const string AttachRunning = "ATTACH_RUNNING";

        /// <summary>
        ///     True if Django debugging is enabled.
        /// </summary>
        public const string EnableDjangoDebugging = "DJANGO_DEBUG";

        /// <summary>
        ///     Specifies a directory mapping in the form of:
        ///     OldDir|NewDir
        ///     for mapping between the files on the local machine and the files deployed on the
        ///     running machine.
        /// </summary>
        public const string DirMappingSetting = "DIR_MAPPING";

        private static readonly HashSet<WeakReference> Engines = new HashSet<WeakReference>();
        private readonly BreakpointManager _breakpointManager;
        private readonly Dictionary<NodeScript, AD7Module> _modules = new Dictionary<NodeScript, AD7Module>();
        private readonly object _syncLock = new object();
        private Guid _ad7ProgramId; // A unique identifier for the program being debugged.
        private bool _attached;
        private bool _breakOnAllExceptions;
        private IDebugEventCallback2 _events;
        private NodeProcess _process;
        private AD7Thread _processLoadedThread;
        private bool _programCreated;
        private bool _pseudoAttach;
        private AD7Module _startModule;
        private AD7Thread _startThread;
        private Tuple<NodeThread, AD7Thread> _threads;

        public AD7Engine()
        {
            _breakpointManager = new BreakpointManager(this);
            Debug.WriteLine("Node Engine Created " + GetHashCode());
            Engines.Add(new WeakReference(this));
        }

        public bool BreakOnAllExceptions
        {
            get { return _breakOnAllExceptions; }
            set
            {
                if (_breakOnAllExceptions == value)
                {
                    return;
                }

                _breakOnAllExceptions = value;

                if (_process != null)
                {
                    _process.BreakOnAllExceptions = _breakOnAllExceptions;
                }
            }
        }

        internal NodeProcess Process
        {
            get { return _process; }
        }

        internal BreakpointManager BreakpointManager
        {
            get { return _breakpointManager; }
        }

        internal static event EventHandler<AD7EngineEventArgs> EngineBreakpointHit;
        internal static event EventHandler<AD7EngineEventArgs> EngineAttached;
        internal static event EventHandler<AD7EngineEventArgs> EngineDetaching;

        ~AD7Engine()
        {
            Debug.WriteLine("Node Engine Finalized " + GetHashCode());
            if (!_attached && _process != null)
            {
                // detach the process exited event, we don't need to send the exited event
                // which could happen when we terminate the process and check if it's still
                // running.
                try
                {
                    _process.ProcessExited -= OnProcessExited;

                    // we launched the process, go ahead and kill it now that
                    // VS has released us
                    _process.Terminate();
                }
                catch (InvalidOperationException)
                {
                }
            }

            foreach (WeakReference engine in Engines)
            {
                if (engine.Target == this)
                {
                    Engines.Remove(engine);
                    break;
                }
            }
        }

        internal static IList<AD7Engine> GetEngines()
        {
            return Engines.Select(engine => (AD7Engine) engine.Target).Where(target => target != null).ToList();
        }

        /// <summary>
        ///     Returns information about the given stack frame for the given process and thread ID.
        ///     If the process, thread, or frame are unknown the null is returned.
        ///     New in 1.5.
        /// </summary>
        public static IDebugDocumentContext2 GetCodeMappingDocument(int processId, int threadId, int frame)
        {
            if (frame >= 0)
            {
                foreach (WeakReference engineRef in Engines)
                {
                    var engine = engineRef.Target as AD7Engine;
                    if (engine != null)
                    {
                        if (engine._process.Id == processId)
                        {
                            NodeThread thread = engine._threads.Item1;

                            if (thread.Id == threadId)
                            {
                                IList<NodeStackFrame> frames = thread.Frames;

                                if (frame < frames.Count)
                                {
                                    NodeStackFrame curFrame = thread.Frames[frame];
                                    return null;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        #region IDebugEngine2 Members

        // Attach the debug engine to a program.
        int IDebugEngine2.Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint celtPrograms, IDebugEventCallback2 ad7Callback, enum_ATTACH_REASON dwReason)
        {
            Debug.Assert(_ad7ProgramId == Guid.Empty);

            if (celtPrograms != 1)
            {
                Debug.Fail("Node debugging only supports one program in a process");
                throw new ArgumentException();
            }

            IDebugProgram2 program = rgpPrograms[0];
            int processId = EngineUtils.GetProcessId(program);
            if (processId == 0)
            {
                // engine only supports system processes
                Debug.WriteLine("NodeEngine failed to get process id during attach");
                return VSConstants.E_NOTIMPL;
            }

            EngineUtils.RequireOk(program.GetProgramId(out _ad7ProgramId));

            // Attach can either be called to attach to a new process, or to complete an attach
            // to a launched process
            if (_process == null)
            {
                // TODO: Where do we get the language version from?
                _events = ad7Callback;

                // Check if we're attaching remotely using the node remote debugging transport
                if (!NodeProcess.TryAttach(processId, out _process))
                {
                    MessageBox.Show("Failed to attach debugger:\n", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return VSConstants.E_FAIL;
                }

                AttachProcessEvents(_process);
                _attached = true;
            }
            else
            {
                if (processId != _process.Id)
                {
                    Debug.Fail("Asked to attach to a process while we are debugging");
                    return VSConstants.E_FAIL;
                }
            }

            AD7EngineCreateEvent.Send(this);

            lock (_syncLock)
            {
                _programCreated = true;

                if (_processLoadedThread != null)
                {
                    SendLoadComplete(_processLoadedThread);
                }
            }

            Debug.WriteLine("NodeEngine Attach returning S_OK");
            return VSConstants.S_OK;
        }

        // Requests that all programs being debugged by this DE stop execution the next time one of their threads attempts to run.
        // This is normally called in response to the user clicking on the pause button in the debugger.
        // When the break is complete, an AsyncBreakComplete event will be sent back to the debugger.
        int IDebugEngine2.CauseBreak()
        {
            return (this).CauseBreak();
        }

        // Called by the SDM to indicate that a synchronous debug event, previously sent by the DE to the SDM,
        // was received and processed. The only event we send in this fashion is Program Destroy.
        // It responds to that event by shutting down the engine.
        int IDebugEngine2.ContinueFromSynchronousEvent(IDebugEvent2 eventObject)
        {
            if (!(eventObject is AD7ProgramDestroyEvent))
            {
                Debug.Fail("Unknown syncronious event");
                return VSConstants.E_FAIL;
            }

            _ad7ProgramId = Guid.Empty;

            _modules.Clear();
            _process.Close();

            _threads = null;
            _events = null;
            _process = null;

            return VSConstants.S_OK;
        }

        // Creates a pending breakpoint in the engine. A pending breakpoint is contains all the information needed to bind a breakpoint to 
        // a location in the debuggee.
        int IDebugEngine2.CreatePendingBreakpoint(IDebugBreakpointRequest2 pBpRequest, out IDebugPendingBreakpoint2 ppPendingBp)
        {
            Debug.WriteLine("Creating pending break point");
            Debug.Assert(_breakpointManager != null);

            _breakpointManager.CreatePendingBreakpoint(pBpRequest, out ppPendingBp);

            return VSConstants.S_OK;
        }

        // Informs a DE that the program specified has been atypically terminated and that the DE should 
        // clean up all references to the program and send a program destroy event.
        int IDebugEngine2.DestroyProgram(IDebugProgram2 pProgram)
        {
            Debug.WriteLine("NodeEngine DestroyProgram");
            // Tell the SDM that the engine knows that the program is exiting, and that the
            // engine will send a program destroy. We do this because the Win32 debug api will always
            // tell us that the process exited, and otherwise we have a race condition.

            return (DebuggerConstants.E_PROGRAM_DESTROY_PENDING);
        }

        // Gets the GUID of the DE.
        int IDebugEngine2.GetEngineId(out Guid guidEngine)
        {
            guidEngine = Guids.DebugEngine;
            return VSConstants.S_OK;
        }

        int IDebugEngine2.RemoveAllSetExceptions(ref Guid guidType)
        {
            BreakOnAllExceptions = false;
            return VSConstants.S_OK;
        }

        int IDebugEngine2.RemoveSetException(EXCEPTION_INFO[] pException)
        {
            BreakOnAllExceptions = false;
            return VSConstants.S_OK;
        }

        int IDebugEngine2.SetException(EXCEPTION_INFO[] pException)
        {
            for (int i = 0; i < pException.Length; i++)
            {
                if (pException[i].guidType == Guids.DebugEngine)
                {
                    if (pException[i].bstrExceptionName == DebugConstants.Exceptions)
                    {
                        BreakOnAllExceptions = pException[i].dwState.HasFlag(enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE);
                    }
                }
            }

            return VSConstants.S_OK;
        }

        // Sets the locale of the DE.
        // This method is called by the session debug manager (SDM) to propagate the locale settings of the IDE so that
        // strings returned by the DE are properly localized. The engine is not localized so this is not implemented.
        int IDebugEngine2.SetLocale(ushort wLangID)
        {
            return VSConstants.S_OK;
        }

        // A metric is a registry value used to change a debug engine's behavior or to advertise supported functionality. 
        // This method can forward the call to the appropriate form of the Debugging SDK Helpers function, SetMetric.
        int IDebugEngine2.SetMetric(string pszMetric, object varValue)
        {
            return VSConstants.S_OK;
        }

        // Sets the registry root currently in use by the DE. Different installations of Visual Studio can change where their registry information is stored
        // This allows the debugger to tell the engine where that location is.
        int IDebugEngine2.SetRegistryRoot(string pszRegistryRoot)
        {
            return VSConstants.S_OK;
        }

        private void SendLoadComplete(AD7Thread thread)
        {
            Debug.WriteLine("Sending load complete" + GetHashCode());
            AD7ProgramCreateEvent.Send(this);

            Send(new AD7LoadCompleteEvent(), AD7LoadCompleteEvent.IID, thread);

            if (_startModule != null)
            {
                SendModuleLoaded(_startModule);
                _startModule = null;
            }

            if (_startThread != null)
            {
                SendThreadStart(_startThread);
                _startThread = null;
            }

            _processLoadedThread = null;

            EventHandler<AD7EngineEventArgs> attached = EngineAttached;
            if (attached != null)
            {
                attached(this, new AD7EngineEventArgs(this));
            }
        }

        private void SendThreadStart(AD7Thread ad7Thread)
        {
            Send(new AD7ThreadCreateEvent(), AD7ThreadCreateEvent.IID, ad7Thread);
        }

        private void SendModuleLoaded(AD7Module ad7Module)
        {
            var eventObject = new AD7ModuleLoadEvent(ad7Module, true /* this is a module load */);

            // TODO: Bind breakpoints when the module loads

            Send(eventObject, AD7ModuleLoadEvent.IID, null);
        }

        #endregion

        #region IDebugEngineLaunch2 Members

        // Determines if a process can be terminated.
        int IDebugEngineLaunch2.CanTerminateProcess(IDebugProcess2 process)
        {
            Debug.Assert(_events != null);
            Debug.Assert(_process != null);

            int processId = EngineUtils.GetProcessId(process);
            if (processId == _process.Id)
            {
                return VSConstants.S_OK;
            }

            return VSConstants.S_FALSE;
        }

        // Launches a process by means of the debug engine.
        // Normally, Visual Studio launches a program using the IDebugPortEx2::LaunchSuspended method and then attaches the debugger 
        // to the suspended program. However, there are circumstances in which the debug engine may need to launch a program 
        // (for example, if the debug engine is part of an interpreter and the program being debugged is an interpreted language), 
        // in which case Visual Studio uses the IDebugEngineLaunch2::LaunchSuspended method
        // The IDebugEngineLaunch2::ResumeProcess method is called to start the process after the process has been successfully launched in a suspended state.
        int IDebugEngineLaunch2.LaunchSuspended(string pszServer, IDebugPort2 port, string exe, string args, string dir, string env, string options, enum_LAUNCH_FLAGS launchFlags, uint hStdInput,
                                                uint hStdOutput, uint hStdError, IDebugEventCallback2 ad7Callback, out IDebugProcess2 process)
        {
            Debug.Assert(_events == null);
            Debug.Assert(_process == null);
            Debug.Assert(_ad7ProgramId == Guid.Empty);

            _events = ad7Callback;

            List<string[]> dirMapping = null;

            Guid processId;
            if (Guid.TryParse(exe, out processId))
            {
                _process = DebugConnectionListener.GetProcess(processId);
                _attached = true;
                _pseudoAttach = true;
            }
            else
            {
                _process = new NodeProcess(exe, args, dir, BreakOnAllExceptions, dirMapping);
            }

            _programCreated = false;

            AttachProcessEvents(_process);

            _process.Start();

            var adProcessId = new AD_PROCESS_ID
                {
                    ProcessIdType = (uint) enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM,
                    dwProcessId = (uint) _process.Id
                };

            EngineUtils.RequireOk(port.GetProcess(adProcessId, out process));
            Debug.WriteLine("NodeEngine LaunchSuspended returning S_OK");
            Debug.Assert(process != null);
            Debug.Assert(!_process.HasExited);

            return VSConstants.S_OK;
        }

        // Resume a process launched by IDebugEngineLaunch2.LaunchSuspended
        int IDebugEngineLaunch2.ResumeProcess(IDebugProcess2 process)
        {
            if (_events == null)
            {
                // process failed to start
                return VSConstants.E_FAIL;
            }

            Debug.Assert(_events != null);
            Debug.Assert(_process != null);
            Debug.Assert(_process != null);
            Debug.Assert(_ad7ProgramId == Guid.Empty);

            int processId = EngineUtils.GetProcessId(process);

            if (processId != _process.Id)
            {
                Debug.WriteLine("ResumeProcess fails, wrong process");
                return VSConstants.S_FALSE;
            }

            // Send a program node to the SDM. This will cause the SDM to turn around and call IDebugEngine2.Attach
            // which will complete the hookup with AD7
            IDebugPort2 port;
            EngineUtils.RequireOk(process.GetPort(out port));

            var defaultPort = (IDebugDefaultPort2) port;

            IDebugPortNotify2 portNotify;
            EngineUtils.RequireOk(defaultPort.GetPortNotify(out portNotify));

            EngineUtils.RequireOk(portNotify.AddProgramNode(new AD7ProgramNode(_process.Id)));

            if (_ad7ProgramId == Guid.Empty)
            {
                Debug.WriteLine("ResumeProcess fails, empty program guid");
                Debug.Fail("Unexpected problem -- IDebugEngine2.Attach wasn't called");
                return VSConstants.E_FAIL;
            }

            Debug.WriteLine("ResumeProcess return S_OK");
            return VSConstants.S_OK;
        }

        // This function is used to terminate a process that the engine launched
        // The debugger will call IDebugEngineLaunch2::CanTerminateProcess before calling this method.
        int IDebugEngineLaunch2.TerminateProcess(IDebugProcess2 process)
        {
            Debug.WriteLine("NodeEngine TerminateProcess");

            Debug.Assert(_events != null);
            Debug.Assert(_process != null);

            int processId = EngineUtils.GetProcessId(process);
            if (processId != _process.Id)
            {
                return VSConstants.S_FALSE;
            }

            EventHandler<AD7EngineEventArgs> detaching = EngineDetaching;
            if (detaching != null)
            {
                detaching(this, new AD7EngineEventArgs(this));
            }

            if (!_pseudoAttach)
            {
                _process.Terminate();
            }
            else
            {
                _process.Detach();
            }

            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugProgram2 Members

        // Determines if a debug engine (DE) can detach from the program.
        public int CanDetach()
        {
            if (_attached)
            {
                return VSConstants.S_OK;
            }
            return VSConstants.S_FALSE;
        }

        // The debugger calls CauseBreak when the user clicks on the pause button in VS. The debugger should respond by entering
        // breakmode. 
        public int CauseBreak()
        {
            _process.Debugger.Break();

            return VSConstants.S_OK;
        }

        // Continue is called from the SDM when it wants execution to continue in the debugee
        // but have stepping state remain. An example is when a tracepoint is executed, 
        // and the debugger does not want to actually enter break mode.
        public int Continue(IDebugThread2 pThread)
        {
            _process.Debugger.Continue();
            return VSConstants.S_OK;
        }

        // Detach is called when debugging is stopped and the process was attached to (as opposed to launched)
        // or when one of the Detach commands are executed in the UI.
        public int Detach()
        {
            _breakpointManager.ClearBoundBreakpoints();

            EventHandler<AD7EngineEventArgs> detaching = EngineDetaching;
            if (detaching != null)
            {
                detaching(this, new AD7EngineEventArgs(this));
            }

            _process.Detach();
            _ad7ProgramId = Guid.Empty;

            return VSConstants.S_OK;
        }

        // Enumerates the code contexts for a given position in a source file.
        public int EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
        {
            string filename;
            pDocPos.GetFileName(out filename);
            TEXT_POSITION[] beginning = new TEXT_POSITION[1], end = new TEXT_POSITION[1];

            pDocPos.GetRange(beginning, end);

            ppEnum = new AD7CodeContextEnum(new IDebugCodeContext2[]
                {
                    new AD7MemoryAddress(this, filename, beginning[0].dwLine)
                });
            return VSConstants.S_OK;
        }

        // EnumCodePaths is used for the step-into specific feature -- right click on the current statment and decide which
        // function to step into. This is not something that we support.
        public int EnumCodePaths(string hint, IDebugCodeContext2 start, IDebugStackFrame2 frame, int fSource, out IEnumCodePaths2 pathEnum, out IDebugCodeContext2 safetyContext)
        {
            pathEnum = null;
            safetyContext = null;
            return VSConstants.E_NOTIMPL;
        }

        // EnumModules is called by the debugger when it needs to enumerate the modules in the program.
        public int EnumModules(out IEnumDebugModules2 ppEnum)
        {
            var moduleObjects = new AD7Module[_modules.Count];
            int i = 0;
            foreach (var keyValue in _modules)
            {
                AD7Module adModule = keyValue.Value;

                moduleObjects[i++] = adModule;
            }

            ppEnum = new AD7ModuleEnum(moduleObjects);

            return VSConstants.S_OK;
        }

        // EnumThreads is called by the debugger when it needs to enumerate the threads in the program.
        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            var threadObjects = new AD7Thread[1];
            AD7Thread adThread = _threads.Item2;

            Debug.Assert(adThread != null);
            threadObjects[0] = adThread;

            ppEnum = new AD7ThreadEnum(threadObjects);

            return VSConstants.S_OK;
        }

        // The properties returned by this method are specific to the program. If the program needs to return more than one property, 
        // then the IDebugProperty2 object returned by this method is a container of additional properties and calling the 
        // IDebugProperty2::EnumChildren method returns a list of all properties.
        // A program may expose any number and type of additional properties that can be described through the IDebugProperty2 interface. 
        // An IDE might display the additional program properties through a generic property browser user interface.
        public int GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger calls this when it needs to obtain the IDebugDisassemblyStream2 for a particular code-context.
        public int GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 codeContext, out IDebugDisassemblyStream2 disassemblyStream)
        {
            disassemblyStream = null;
            return VSConstants.E_NOTIMPL;
        }

        // This method gets the Edit and Continue (ENC) update for this program. A custom debug engine always returns E_NOTIMPL
        public int GetENCUpdate(out object update)
        {
            update = null;
            return VSConstants.S_OK;
        }

        // Gets the name and identifier of the debug engine (DE) running this program.
        public int GetEngineInfo(out string engineName, out Guid engineGuid)
        {
            engineName = Resources.EngineName;
            engineGuid = Guids.DebugEngine;
            return VSConstants.S_OK;
        }

        // The memory bytes as represented by the IDebugMemoryBytes2 object is for the program's image in memory and not any memory 
        // that was allocated when the program was executed.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Gets the name of the program.
        // The name returned by this method is always a friendly, user-displayable name that describes the program.
        public int GetName(out string programName)
        {
            // The engine uses default transport and doesn't need to customize the name of the program,
            // so return NULL.
            programName = null;
            return VSConstants.S_OK;
        }

        // Gets a GUID for this program. A debug engine (DE) must return the program identifier originally passed to the IDebugProgramNodeAttach2::OnAttach
        // or IDebugEngine2::Attach methods. This allows identification of the program across debugger components.
        public int GetProgramId(out Guid guidProgramId)
        {
            guidProgramId = _ad7ProgramId;
            return guidProgramId == Guid.Empty ? VSConstants.E_FAIL : VSConstants.S_OK;
        }

        // This method is deprecated. Use the IDebugProcess3::Step method instead.

        /// <summary>
        ///     Performs a step.
        ///     In case there is any thread synchronization or communication between threads, other threads in the program should run when a particular thread is stepping.
        /// </summary>
        public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT step)
        {
            switch (sk)
            {
                case enum_STEPKIND.STEP_INTO:
                    _process.Debugger.StepInto();
                    break;

                case enum_STEPKIND.STEP_OUT:
                    _process.Debugger.StepOut();
                    break;

                case enum_STEPKIND.STEP_OVER:
                    Process.Debugger.StepOver();
                    break;
            }
            return VSConstants.S_OK;
        }

        // Terminates the program.
        public int Terminate()
        {
            Debug.WriteLine("NodeEngine Terminate");
            // Because we implement IDebugEngineLaunch2 we will terminate
            // the process in IDebugEngineLaunch2.TerminateProcess

            Process.Terminate();

            return VSConstants.S_OK;
        }

        // Writes a dump to a file.
        public int WriteDump(enum_DUMPTYPE dumptype, string pszDumpUrl)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion

        #region IDebugProgram3 Members

        // ExecuteOnThread is called when the SDM wants execution to continue and have 
        // stepping state cleared.  See http://msdn.microsoft.com/en-us/library/bb145596.aspx for a
        // description of different ways we can resume.
        public int ExecuteOnThread(IDebugThread2 pThread)
        {
            // clear stepping state on the thread the user was currently on
            var thread = (AD7Thread) pThread;
            thread.Thread.ClearSteppingState();
            _process.Debugger.Continue();

            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugSymbolSettings100 members

        public int SetSymbolLoadState(int bIsManual, int bLoadAdjacent, string strIncludeList, string strExcludeList)
        {
            // The SDM will call this method on the debug engine when it is created, to notify it of the user's
            // symbol settings in Tools->Options->Debugging->Symbols.
            //
            // Params:
            // bIsManual: true if 'Automatically load symbols: Only for specified modules' is checked
            // bLoadAdjacent: true if 'Specify modules'->'Always load symbols next to the modules' is checked
            // strIncludeList: semicolon-delimited list of modules when automatically loading 'Only specified modules'
            // strExcludeList: semicolon-delimited list of modules when automatically loading 'All modules, unless excluded'

            return VSConstants.S_OK;
        }

        #endregion

        #region Deprecated interface methods

        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugEngine2.EnumPrograms(out IEnumDebugPrograms2 programs)
        {
            Debug.Fail("This function is not called by the debugger");

            programs = null;
            return VSConstants.E_NOTIMPL;
        }

        public int Attach(IDebugEventCallback2 pCallback)
        {
            Debug.Fail("This function is not called by the debugger");

            return VSConstants.E_NOTIMPL;
        }

        public int GetProcess(out IDebugProcess2 process)
        {
            Debug.Fail("This function is not called by the debugger");

            process = null;
            return VSConstants.E_NOTIMPL;
        }

        public int Execute()
        {
            Debug.Fail("This function is not called by the debugger.");
            return VSConstants.E_NOTIMPL;
        }

        #endregion

        #region Events

        internal void Send(IDebugEvent2 eventObject, string iidEvent, IDebugProgram2 program, IDebugThread2 thread)
        {
            uint attributes;
            var riidEvent = new Guid(iidEvent);

            EngineUtils.RequireOk(eventObject.GetAttributes(out attributes));

            Debug.WriteLine(string.Format("Sending Event: {0} {1}", eventObject.GetType(), iidEvent));
            try
            {
                EngineUtils.RequireOk(_events.Event(this, null, program, thread, eventObject, ref riidEvent, attributes));
            }
            catch (InvalidCastException)
            {
                // COM object has gone away
            }
        }

        internal void Send(IDebugEvent2 eventObject, string iidEvent, IDebugThread2 thread)
        {
            Send(eventObject, iidEvent, this, thread);
        }

        private void AttachProcessEvents(NodeProcess process)
        {
            process.ProcessLoaded += OnProcessLoaded;
            process.ProcessExited += OnProcessExited;
            process.ProcessOutput += OnProcessOutput;
            process.ThreadCreated += OnThreadCreated;
            process.ThreadExited += OnThreadExited;
        }

        private void AttachThreadEvents(IDebuggerManager debugger)
        {
            debugger.ModuleLoaded += OnModuleLoaded;
            debugger.BreakpointBindFailed += OnBreakpointBindFailed;
            debugger.BreakpointBindSucceeded += OnBreakpointBindSucceeded;
            debugger.BreakpointHit += OnBreakpointHit;
            debugger.AsyncBreakComplete += OnAsyncBreakComplete;
            debugger.ExceptionRaised += OnExceptionRaised;
            debugger.StepComplete += OnStepComplete;
        }

        private void OnThreadCreated(object sender, EventArgs e)
        {
            var nodeThread = new NodeThread(_process, _process.Id);
            var newThread = new AD7Thread(this, nodeThread);
            _threads = new Tuple<NodeThread, AD7Thread>(nodeThread, newThread);

            lock (_syncLock)
            {
                if (_programCreated)
                {
                    SendThreadStart(newThread);
                }
                else
                {
                    _startThread = newThread;
                }
            }
        }

        private void OnThreadExited(object sender, EventArgs e)
        {
            // TODO: Thread exit code
            if (_threads == null)
            {
                return;
            }

            Send(new AD7ThreadDestroyEvent(0), AD7ThreadDestroyEvent.IID, _threads.Item2);
            _threads = null;
        }

        private void OnStepComplete(object sender, ThreadEventArgs e)
        {
            Send(new AD7SteppingCompleteEvent(), AD7SteppingCompleteEvent.IID, _threads.Item2);
        }

        private void OnProcessLoaded(object sender, EventArgs e)
        {
            lock (_syncLock)
            {
                if (_pseudoAttach)
                {
                    _process.Unregister();
                }

                if (_programCreated)
                {
                    // we've delviered the program created event, deliver the load complete event
                    SendLoadComplete(_threads.Item2);
                }
                else
                {
                    // we haven't delivered the program created event, wait until we do to deliver the process loaded event.
                    _processLoadedThread = _threads.Item2;
                }
            }

            AttachThreadEvents(_process.Debugger);
        }

        private void OnProcessExited(object sender, ProcessExitedEventArgs e)
        {
            try
            {
                Send(new AD7ProgramDestroyEvent((uint) e.ExitCode), AD7ProgramDestroyEvent.IID, null);
            }
            catch (InvalidOperationException)
            {
                // we can race at shutdown and deliver the event after the debugger is shutting down.
            }
        }

        private void OnModuleLoaded(object sender, ModuleLoadedEventArgs e)
        {
            lock (_syncLock)
            {
                AD7Module adModule = _modules[e.Script] = new AD7Module(e.Script);
                if (_programCreated)
                {
                    SendModuleLoaded(adModule);
                }
                else
                {
                    _startModule = adModule;
                }
            }
        }

        private void OnExceptionRaised(object sender, ExceptionRaisedEventArgs e)
        {
            // Exception events are sent when an exception occurs in the debuggee that the debugger was not expecting.

            Send(
                new AD7DebugExceptionEvent(e.Exception.TypeName, e.Exception.Description, e.IsUnhandled),
                AD7DebugExceptionEvent.IID,
                _threads.Item2);
        }

        private void OnBreakpointHit(object sender, BreakpointHitEventArgs e)
        {
            var boundBreakpoints = new[] {_breakpointManager.GetBreakpoint(e.Breakpoint)};

            // An engine that supports more advanced breakpoint features such as hit counts, conditions and filters
            // should notify each bound breakpoint that it has been hit and evaluate conditions here.

            Send(new AD7BreakpointEvent(new AD7BoundBreakpointsEnum(boundBreakpoints)), AD7BreakpointEvent.IID, _threads.Item2);
        }

        private void OnBreakpointBindSucceeded(object sender, BreakpointEventArgs e)
        {
            IDebugPendingBreakpoint2 pendingBreakpoint;
            AD7BoundBreakpoint boundBreakpoint = _breakpointManager.GetBreakpoint(e.Breakpoint);
            ((IDebugBoundBreakpoint2) boundBreakpoint).GetPendingBreakpoint(out pendingBreakpoint);

            Send(
                new AD7BreakpointBoundEvent((AD7PendingBreakpoint) pendingBreakpoint, boundBreakpoint),
                AD7BreakpointBoundEvent.IID,
                null
                );

            EventHandler<AD7EngineEventArgs> breakpointHit = EngineBreakpointHit;
            if (breakpointHit != null)
            {
                breakpointHit(this, new AD7EngineEventArgs(this));
            }
        }

        private void OnBreakpointBindFailed(object sender, BreakpointEventArgs e)
        {
        }

        private void OnAsyncBreakComplete(object sender, ThreadEventArgs e)
        {
            Send(new AD7AsyncBreakCompleteEvent(), AD7AsyncBreakCompleteEvent.IID, _threads.Item2);
        }

        private void OnProcessOutput(object sender, OutputEventArgs e)
        {
            if (_threads == null)
            {
                return;
            }

            Send(new AD7DebugOutputStringEvent2(e.Output), AD7DebugOutputStringEvent2.IID, _threads.Item2);
        }

        #endregion
    }
}