using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DebugEngine.Node.Debugger;
using DebugEngine.Node.Debugger.Communication;

namespace DebugEngine.Node
{
    /// <summary>
    ///     Handles all interactions with a node.js process which is being debugged.
    /// </summary>
    internal class NodeProcess
    {
        private readonly Regex _debuggerPort = new Regex(@"debugger listening on port (\d+)", RegexOptions.Compiled);
        private readonly List<string[]> _dirMapping;
        private readonly string _directory;
        private readonly Process _process;
        private readonly Guid _processGuid = Guid.NewGuid();
        private bool _breakOnAllExceptions;
        private NodeDebuggerManager _debugger;

        private bool _sentExited;

        public NodeProcess(string exe, string args, string directory, bool breakOnAllExceptions, List<string[]> dirMapping = null)
        {
            DebugConnectionListener.RegisterProcess(_processGuid, this);

            if (directory.EndsWith("\\"))
            {
                directory = directory.Substring(0, directory.Length - 1);
            }

            _directory = directory;
            _breakOnAllExceptions = breakOnAllExceptions;
            _dirMapping = dirMapping;

            var processInfo = new ProcessStartInfo(exe)
                {
                    UseShellExecute = false,
                    Arguments = args,
                    WorkingDirectory = directory,
                    RedirectStandardError = true
                };

            Debug.WriteLine("Launching: {0} {1}", processInfo.FileName, processInfo.Arguments);

            _process = new Process
                {
                    StartInfo = processInfo,
                    EnableRaisingEvents = true
                };

            _process.Exited += OnProcessExited;
            _process.ErrorDataReceived += OnProcessOutputData;
        }

        internal string Directory
        {
            get { return _directory; }
        }

        public int Id
        {
            get { return _process != null ? _process.Id : 0; }
        }

        public Guid ProcessGuid
        {
            get { return _processGuid; }
        }

        public bool HasExited
        {
            get { return _process != null && _process.HasExited; }
        }

        public IDebuggerManager Debugger
        {
            get { return _debugger; }
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

                if (_debugger != null)
                {
                    _debugger.SetExceptionHandlingAsync(_breakOnAllExceptions);
                }
            }
        }

        internal string GetRelativeFilePath(string path)
        {
            var uri1 = new Uri(path);
            var uri2 = new Uri(_directory + "\\");

            return uri2.MakeRelativeUri(uri1).ToString();
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            if (_sentExited)
            {
                return;
            }

            _sentExited = true;
            _debugger = null;

            EventHandler<EventArgs> threadExited = ThreadExited;
            if (threadExited != null)
            {
                threadExited(this, EventArgs.Empty);
            }

            EventHandler<ProcessExitedEventArgs> processExited = ProcessExited;
            if (processExited == null)
            {
                return;
            }

            int exitCode;
            try
            {
                exitCode = (_process != null && _process.HasExited) ? _process.ExitCode : -1;
            }
            catch (InvalidOperationException)
            {
                // debug attach, we didn't start the process...
                exitCode = -1;
            }

            processExited(this, new ProcessExitedEventArgs(exitCode));
        }

        private void OnProcessOutputData(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            EventHandler<OutputEventArgs> processOutput = ProcessOutput;
            if (processOutput != null)
            {
                string message = string.Format("{0}{1}", e.Data, Environment.NewLine);
                processOutput(this, new OutputEventArgs(this, message));
            }

            if (_debugger != null)
            {
                return;
            }

            Match match = _debuggerPort.Match(e.Data);
            if (match.Success)
            {
                string portValue = match.Groups[1].Value;
                int port = int.Parse(portValue);

                var client = new NodeDebuggerClient(new NodeDebuggerConnection("localhost", port));
                _debugger = new NodeDebuggerManager(client);

                var tasks = new List<Task>
                    {
                        _debugger.InitializeAsync()
                    };

                if (BreakOnAllExceptions)
                {
                    tasks.Add(_debugger.SetExceptionHandlingAsync(BreakOnAllExceptions));
                }

                try
                {
                    Task.WhenAll(tasks).Wait();
                }
                catch (Exception)
                {
                    _debugger.Terminate();
                }

                EventHandler<EventArgs> processLoaded = ProcessLoaded;
                if (processLoaded != null)
                {
                    processLoaded(this, EventArgs.Empty);
                }
            }
        }

        public static bool TryAttach(int pid, out NodeProcess process)
        {
            throw new NotImplementedException();
        }

        internal void Close()
        {
        }

        /// <summary>
        ///     Fired when the process received initial debugger output.
        /// </summary>
        public event EventHandler<ProcessExitedEventArgs> ProcessExited;

        /// <summary>
        ///     Fired when the process has started and is broken into the debugger, but before any user code is run.
        /// </summary>
        public event EventHandler<EventArgs> ProcessLoaded;

        public event EventHandler<EventArgs> ThreadCreated;

        public event EventHandler<EventArgs> ThreadExited;

        public event EventHandler<OutputEventArgs> ProcessOutput;

        public void Start()
        {
            _process.Start();
            _process.BeginErrorReadLine();

            EventHandler<EventArgs> threadCreated = ThreadCreated;
            if (threadCreated != null)
            {
                threadCreated(this, EventArgs.Empty);
            }
        }

        ~NodeProcess()
        {
            DebugConnectionListener.UnregisterProcess(_processGuid);
        }

        public void WaitForExit()
        {
            _process.WaitForExit();
        }

        public bool WaitForExit(int milliseconds)
        {
            return _process.WaitForExit(milliseconds);
        }

        public void Terminate()
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch (Win32Exception)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        internal void Unregister()
        {
            DebugConnectionListener.UnregisterProcess(_processGuid);
            GC.SuppressFinalize(this);
        }

        public void Detach()
        {
            Debug.Print("Detach");
        }

        /// <summary>
        ///     Maps a filename from the debugger machine to the debugge machine to vice versa.
        ///     The file mapping information is provided by our options when the debugger is started.
        ///     This is used so that we can use the files local on the developers machine which have
        ///     for setting breakpoints and viewing source code even though the files have been
        ///     deployed to a remote machine.  For example the user may have:
        ///     C:\Users\Me\Documents\MyProject\Foo.py
        ///     which is deployed to
        ///     \\mycluster\deploydir\MyProject\Foo.py
        ///     We want the user to be working w/ the local project files during development but
        ///     want to set break points in the cluster deployment share.
        /// </summary>
        internal string MapFile(string file, bool toDebuggee = true)
        {
            if (_dirMapping == null)
            {
                return file;
            }

            foreach (var mappingInfo in _dirMapping)
            {
                string mapFrom = mappingInfo[toDebuggee ? 0 : 1];
                string mapTo = mappingInfo[toDebuggee ? 1 : 0];

                if (file.StartsWith(mapFrom, StringComparison.OrdinalIgnoreCase))
                {
                    if (file.StartsWith(mapFrom, StringComparison.OrdinalIgnoreCase))
                    {
                        int len = mapFrom.Length;
                        if (!mappingInfo[0].EndsWith("\\"))
                        {
                            len++;
                        }

                        string newFile = Path.Combine(mapTo, file.Substring(len));
                        Debug.WriteLine("Filename mapped from {0} to {1}", file, newFile);
                        return newFile;
                    }
                }
            }

            return file;
        }
    }
}