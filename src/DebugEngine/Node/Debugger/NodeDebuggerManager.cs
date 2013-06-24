using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DebugEngine.Node.Debugger.Communication;
using DebugEngine.Node.Debugger.Serialization;

namespace DebugEngine.Node.Debugger
{
    /// <summary>
    ///     Node debugger manager.
    /// </summary>
    internal sealed class NodeDebuggerManager : IDebuggerManager
    {
        /// <summary>
        ///     Current breakpoints collection.
        /// </summary>
        private readonly Dictionary<int, NodeBreakpoint> _breakpoints = new Dictionary<int, NodeBreakpoint>();

        /// <summary>
        ///     Node debugger client.
        /// </summary>
        private readonly NodeDebuggerClient _client;

        /// <summary>
        ///     Collection of loaded scripts.
        /// </summary>
        private readonly Dictionary<int, NodeScript> _scripts = new Dictionary<int, NodeScript>();

        private FrameMessage _initialFrame;
        private Version _v8EngineVersion;
        private bool _wasContinued;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="client">Node debugger client.</param>
        public NodeDebuggerManager(NodeDebuggerClient client)
        {
            _client = client;
            _client.BreakpointEvent += OnBreakpointEvent;
            _client.CompileEvent += OnCompileEvent;
            _client.ExceptionEvent += OnExceptionEvent;
        }

        /// <summary>
        ///     Gets or sets a node thread.
        /// </summary>
        public NodeThread Thread { get; set; }

        public bool IsRunning { get; private set; }

        public bool IsException { get; private set; }

        public event EventHandler<ThreadEventArgs> StepComplete;
        public event EventHandler<ThreadEventArgs> AsyncBreakComplete;
        public event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;
        public event EventHandler<ExceptionRaisedEventArgs> ExceptionRaised;
        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;
        public event EventHandler<BreakpointEventArgs> BreakpointBindSucceeded;
        public event EventHandler<BreakpointEventArgs> BreakpointBindFailed;

        /// <summary>
        ///     Removes a break point.
        /// </summary>
        /// <param name="breakpoint">Break point.</param>
        public Task RemoveBreakpointAsync(NodeBreakpoint breakpoint)
        {
            _breakpoints.Remove(breakpoint.Id);
            return _client.SendMessage("clearbreakpoint", new
                {
                    breakpoint = breakpoint.Id
                });
        }

        public Task SetExceptionHandlingAsync(bool throwOnAllExceptions)
        {
            var type = throwOnAllExceptions ? "all" : "uncaught";
            return _client.SendMessage("setexceptionbreak", new {type });
        }

        /// <summary>
        ///     Changes a break point.
        /// </summary>
        /// <param name="breakpoint">Break point.</param>
        public Task ChangeBreakpointAsync(NodeBreakpoint breakpoint)
        {
            return _client.SendMessage("changebreakpoint", new
                {
                    breakpoint = breakpoint.Id,
                    enabled = breakpoint.Enabled,
                    condition = breakpoint.Condition
                });
        }

        public async Task InitializeAsync()
        {
            // Retrieve v8 version
            IResponseMessage response = await _client.SendMessage("version").ConfigureAwait(false);

            var versionMessage = response as VersionMessage;
            if (versionMessage == null || !versionMessage.IsSuccessful)
            {
                string message = string.Format("Invalid version response: {0}", response);
                throw new InvalidOperationException(message);
            }

            IsRunning = versionMessage.IsRunning;
            _v8EngineVersion = versionMessage.Version;

            response = await _client.SendMessage("frame", new {inlineRefs = true}).ConfigureAwait(false);
            var frameMessage = response as FrameMessage;
            if (frameMessage == null || !frameMessage.IsSuccessful)
            {
                string message = string.Format("Invalid listbreakpoints response: {0}", response);
                throw new InvalidOperationException(message);
            }

            foreach (NodeScript script in frameMessage.Scripts)
            {
                AddModuleIfNotExist(script.Id, script.Filename);
            }

            _initialFrame = frameMessage;
        }

        /// <summary>
        ///     Adds a breakpoint.
        /// </summary>
        /// <param name="breakpoint">Break point.</param>
        public async Task AddBreakpointAsync(NodeBreakpoint breakpoint)
        {
            IResponseMessage response = await _client.SendMessage("setbreakpoint", new
                {
                    type = "script",
                    target = breakpoint.Filename,
                    line = breakpoint.Line,
                    column = breakpoint.Column,
                    condition = breakpoint.Condition
                }).ConfigureAwait(false);

            var setBreakpointMessage = response as SetBreakpointMessage;
            if (setBreakpointMessage == null || !setBreakpointMessage.IsSuccessful)
            {
                string message = string.Format("Invalid breakpoint response: {0}", response);
                throw new InvalidOperationException(message);
            }

            IsRunning = response.IsRunning;
            breakpoint.Id = setBreakpointMessage.Id;
            breakpoint.Line = setBreakpointMessage.Line;
            breakpoint.Column = setBreakpointMessage.Column;

            if (!_wasContinued)
            {
                CheckWhetherFirstLineBreakpoint(breakpoint);
            }

            _breakpoints.Add(breakpoint.Id, breakpoint);
        }

        /// <summary>
        ///     Gets a collection of descentants for a variable.
        /// </summary>
        /// <param name="variable">Variable.</param>
        /// <param name="stackFrame">Stack frame.</param>
        /// <returns>Collection of descendants.</returns>
        public async Task<IList<NodeEvaluationResult>> GetChildrenAsync(NodeEvaluationResult variable, NodeStackFrame stackFrame)
        {
            IResponseMessage response = await _client.SendMessage("lookup", new
                {
                    handles = new[] {variable.Id},
                    includeSource = false
                }, variable).ConfigureAwait(false);

            var lookupMessage = response as LookupMessage;
            if (lookupMessage == null || !lookupMessage.IsSuccessful)
            {
                string message = string.Format("Invalid lookup response: {0}", response);
                throw new InvalidOperationException(message);
            }

            IsRunning = response.IsRunning;
            return lookupMessage.Children;
        }

        public async Task<IList<NodeStackFrame>> GetStackFramesAsync()
        {
            IResponseMessage response = await _client.SendMessage("backtrace", new
                {
                    inlineRefs = true
                }, this).ConfigureAwait(false);

            var backtraceMessage = response as BacktraceMessage;
            if (backtraceMessage == null || !backtraceMessage.IsSuccessful)
            {
                string message = string.Format("Invalid backtrace response: {0}", response);
                throw new InvalidOperationException(message);
            }

            foreach (NodeScript script in backtraceMessage.Scripts)
            {
                AddModuleIfNotExist(script.Id, script.Filename);
            }

            IsRunning = response.IsRunning;
            return backtraceMessage.StackFrames;
        }

        public async Task<NodeEvaluationResult> EvaluateVariableAsync(NodeEvaluationResult variable)
        {
            IResponseMessage response = await _client.SendMessage("evaluate", new
                {
                    expression = variable.FullName,
                    frame = variable.StackFrame.Id,
                    maxStringLength = -1
                }, this, variable.StackFrame, variable.Name).ConfigureAwait(false);

            var evaluateMessage = response as EvaluateMessage;
            if (evaluateMessage == null || !evaluateMessage.IsSuccessful)
            {
                return null;
            }

            IsRunning = response.IsRunning;
            return evaluateMessage.Result;
        }

        public async Task<NodeEvaluationResult> EvaluateExpressionAsync(string expression, NodeStackFrame stackFrame)
        {
            IResponseMessage response = await _client.SendMessage("evaluate", new
                {
                    expression,
                    frame = stackFrame.Id
                }, this, stackFrame, expression).ConfigureAwait(false);

            var evaluateMessage = response as EvaluateMessage;
            if (evaluateMessage == null || !evaluateMessage.IsSuccessful)
            {
                return null;
            }

            IsRunning = response.IsRunning;
            return evaluateMessage.Result;
        }

        public async Task<NodeEvaluationResult> SetVariableValueAsync(NodeEvaluationResult variable, string value)
        {
            if (_v8EngineVersion < new Version(3, 15, 8))
            {
                throw new NotSupportedException("Changing variable values available in node.js v0.11.0 and higher.");
            }

            IResponseMessage response = await _client.SendMessage("setVariableValue", new
                {
                    name = variable.FullName,
                    newValue = new RawValue(value),
                    scope = new {number = 0, frameNumber = variable.StackFrame.Id}
                }, this, variable.StackFrame, variable.Name).ConfigureAwait(false);

            var setVariableValueMessage = response as SetVariableValueMessage;
            if (setVariableValueMessage == null || !setVariableValueMessage.IsSuccessful)
            {
                return null;
            }

            IsRunning = response.IsRunning;
            return setVariableValueMessage.Result;
        }

        public Task StepInto()
        {
            return _client.SendMessage("continue", new {stepaction = "in"});
        }

        public Task StepOver()
        {
            return _client.SendMessage("continue", new {stepaction = "next"});
        }

        public Task StepOut()
        {
            return _client.SendMessage("continue", new {stepaction = "out"});
        }

        public async Task Break()
        {
            var response = await _client.SendMessage("suspend").ConfigureAwait(false);
            var suspendMessage = response as SuspendMessage;
            if (suspendMessage == null || !suspendMessage.IsSuccessful)
            {
                return;
            }

            EventHandler<ThreadEventArgs> asyncBreakpoint = AsyncBreakComplete;
            if (asyncBreakpoint != null)
            {
                asyncBreakpoint(this, new ThreadEventArgs(Thread));
            }
        }

        public Task Terminate()
        {
            return _client.SendMessage("evaluate", new {expression = "process.exit(1)", global = true});
        }

        public Task Continue()
        {
            _wasContinued = true;
            return _client.SendMessage("continue");
        }

        private void CheckWhetherFirstLineBreakpoint(NodeBreakpoint breakpoint)
        {
            if (_initialFrame == null)
            {
                return;
            }

            int scriptId = _initialFrame.ScriptId;
            string filename = _scripts[scriptId].Filename;

            if (breakpoint.Line == _initialFrame.Line &&
                string.Equals(filename, breakpoint.Filename, StringComparison.InvariantCultureIgnoreCase))
            {
                var eventMessage = new BreakpointMessage(scriptId, filename, breakpoint.Line, breakpoint.Column);
                OnBreakpointEvent(this, new BreakpointMessageEventArgs(eventMessage));
            }
        }

        /// <summary>
        ///     Adds a script into the modules list.
        /// </summary>
        /// <param name="id">Script identifier.</param>
        /// <param name="filename">Script file name.</param>
        private void AddModuleIfNotExist(int id, string filename)
        {
            if (_scripts.ContainsKey(id))
            {
                return;
            }

            var module = new NodeScript(id, filename);
            _scripts.Add(id, module);

            EventHandler<ModuleLoadedEventArgs> moduleLoaded = ModuleLoaded;
            if (moduleLoaded == null)
            {
                return;
            }

            moduleLoaded(this, new ModuleLoadedEventArgs(module));
        }

        /// <summary>
        ///     Handles break point event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnBreakpointEvent(object sender, BreakpointMessageEventArgs e)
        {
            IsRunning = e.Message.IsRunning;
            int scriptId = e.Message.ScriptId;
            string filename = e.Message.Filename;
            int lineNo = e.Message.Line;

            AddModuleIfNotExist(scriptId, filename);

            EventHandler<BreakpointHitEventArgs> breakpointHit = BreakpointHit;
            if (breakpointHit != null)
            {
                // Try to find break point on a line
                NodeBreakpoint breakpoint = _breakpoints.Values.FirstOrDefault(
                    p => p.Line == lineNo && String.Compare(p.Filename, filename, StringComparison.OrdinalIgnoreCase) == 0);

                if (breakpoint != null)
                {
                    breakpointHit(this, new BreakpointHitEventArgs(breakpoint, Thread));
                    return;
                }
            }

            EventHandler<ThreadEventArgs> asyncBreakpoint = AsyncBreakComplete;
            if (asyncBreakpoint != null)
            {
                asyncBreakpoint(this, new ThreadEventArgs(Thread));
            }
        }

        /// <summary>
        ///     Handles compile script event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCompileEvent(object sender, CompileScriptMessageEventArgs e)
        {
            IsRunning = e.Message.IsRunning;
            int scriptId = e.Message.ScriptId;
            string filename = e.Message.Filename;

            AddModuleIfNotExist(scriptId, filename);
        }

        /// <summary>
        ///     Handles exception event.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private async void OnExceptionEvent(object sender, ExceptionMessageEventArgs e)
        {
            IsRunning = e.Message.IsRunning;
            IsException = true;

            int scriptId = e.Message.ScriptId;
            string filename = e.Message.Filename;
            string description = e.Message.Description;

            AddModuleIfNotExist(scriptId, filename);

            // Try to serialize exception
            string objectName = string.Format("object{0}", DateTime.UtcNow.Ticks);
            IResponseMessage response = await _client.SendMessage("evaluate", new
                {
                    expression = objectName + ".toString()",
                    additional_context = new[]
                        {
                            new {name = objectName, handle = e.Message.ExceptionId}
                        },
                    maxStringLength = -1
                }, this, null, objectName).ConfigureAwait(false);

            var evaluateMessage = response as EvaluateMessage;
            if (evaluateMessage != null && evaluateMessage.IsSuccessful)
            {
                description = evaluateMessage.Result.StringValue;
            }

            // Fire async breakpoint event
            EventHandler<ExceptionRaisedEventArgs> exceptionRaised = ExceptionRaised;
            if (exceptionRaised != null)
            {
                var nodeException = new NodeException(e.Message.TypeName, description);
                exceptionRaised(this, new ExceptionRaisedEventArgs(Thread, nodeException, e.Message.IsUnhandled));
            }
        }
    }
}