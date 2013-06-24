using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Console.Types;
using Console.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Task = System.Threading.Tasks.Task;

namespace Console.Console
{
    internal interface IPrivateConsoleDispatcher : IConsoleDispatcher, IDisposable
    {
        event EventHandler<EventArgs<Tuple<SnapshotSpan, bool>>> ExecuteInputLine;
        void PostInputLine(InputLine inputLine);
        void PostKey(VsKeyInfo key);
        void CancelWaitKey();
    }


    /// <summary>
    ///     This class handles input line posting and command line dispatching/execution.
    /// </summary>
    internal class ConsoleDispatcher : IPrivateConsoleDispatcher
    {
        private readonly BlockingCollection<VsKeyInfo> _keyBuffer = new BlockingCollection<VsKeyInfo>();
        private readonly object _lockObj = new object();
        private CancellationTokenSource _cancelWaitKeySource;

        /// <summary>
        ///     Child dispatcher based on host type. Its creation is postponed to Start(), so that
        ///     a WpfConsole's dispatcher can be accessed while inside a host construction.
        /// </summary>
        private Dispatcher _dispatcher;

        private bool _isExecutingReadKey;

        public ConsoleDispatcher(IPrivateWpfConsole wpfConsole)
        {
            UtilityMethods.ThrowIfArgumentNull(wpfConsole);

            WpfConsole = wpfConsole;
        }

        /// <summary>
        ///     The IPrivateWpfConsole instance this dispatcher works with.
        /// </summary>
        private IPrivateWpfConsole WpfConsole { get; set; }

        public event EventHandler StartCompleted;

        public event EventHandler StartWaitingKey;

        public bool IsExecutingCommand
        {
            get { return (_dispatcher != null) && _dispatcher.IsExecuting; }
        }

        public void PostKey(VsKeyInfo key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            _keyBuffer.Add(key);
        }

        public bool IsExecutingReadKey
        {
            get { return _isExecutingReadKey; }
        }

        public bool IsKeyAvailable
        {
            get
            {
                // In our BlockingCollection<T> producer/consumer this is
                // not critical so no need for locking. 
                return _keyBuffer.Count > 0;
            }
        }

        public void CancelWaitKey()
        {
            if (_isExecutingReadKey && !_cancelWaitKeySource.IsCancellationRequested)
            {
                _cancelWaitKeySource.Cancel();
            }
        }

        public void AcceptKeyInput()
        {
            Debug.Assert(_dispatcher != null);

            if (_dispatcher != null && WpfConsole != null)
            {
                WpfConsole.BeginInputLine();
            }
        }

        public VsKeyInfo WaitKey()
        {
            try
            {
                // raise the StartWaitingKey event on main thread
                RaiseEventSafe(StartWaitingKey);

                // set/reset the cancellation token
                _cancelWaitKeySource = new CancellationTokenSource();
                _isExecutingReadKey = true;

                // blocking call
                VsKeyInfo key = _keyBuffer.Take(_cancelWaitKeySource.Token);

                return key;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                _isExecutingReadKey = false;
            }
        }

        public bool IsStartCompleted { get; private set; }

        void IDisposable.Dispose()
        {
            try
            {
                Dispose(true);
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        #region IConsoleDispatcher

        public void Start()
        {
            // Only Start once
            lock (_lockObj)
            {
                if (_dispatcher == null)
                {
                    IHost host = WpfConsole.Host;

                    if (host == null)
                    {
                        throw new InvalidOperationException("Can't start Console dispatcher. Host is null.");
                    }

                    if (host is IAsyncHost)
                    {
                        _dispatcher = new AsyncHostConsoleDispatcher(this);
                    }
                    else
                    {
                        _dispatcher = new SyncHostConsoleDispatcher(this);
                    }

                    // capture the cultures to assign to the worker thread below
                    CultureInfo currentCulture = CultureInfo.CurrentCulture;
                    CultureInfo currentUICulture = CultureInfo.CurrentUICulture;

                    Task.Factory.StartNew(
                        // gives the host a chance to do initialization works before the console starts accepting user inputs
                        () =>
                            {
                                // apply the culture of the main thread to this thread so that the PowerShell engine
                                // will have the same culture as Visual Studio.
                                Thread.CurrentThread.CurrentCulture = currentCulture;
                                Thread.CurrentThread.CurrentUICulture = currentUICulture;

                                host.Initialize(WpfConsole);
                            }
                        ).ContinueWith(
                            task =>
                                {
                                    if (task.IsFaulted)
                                    {
                                        Exception exception = ExceptionHelper.Unwrap(task.Exception);
                                        WriteError(exception.Message);
                                    }

                                    if (host.IsCommandEnabled)
                                    {
                                        ThreadHelper.Generic.Invoke(_dispatcher.Start);
                                    }

                                    RaiseEventSafe(StartCompleted);
                                    IsStartCompleted = true;
                                },
                            TaskContinuationOptions.NotOnCanceled
                        );
                }
            }
        }

        public void ClearConsole()
        {
            Debug.Assert(_dispatcher != null);
            if (_dispatcher != null)
            {
                _dispatcher.ClearConsole();
            }
        }

        private void WriteError(string message)
        {
            if (WpfConsole != null)
            {
                WpfConsole.Write(message + Environment.NewLine, Colors.Red, null);
            }
        }

        #endregion

        #region IPrivateConsoleDispatcher

        public event EventHandler<EventArgs<Tuple<SnapshotSpan, bool>>> ExecuteInputLine;

        public void PostInputLine(InputLine inputLine)
        {
            Debug.Assert(_dispatcher != null);
            if (_dispatcher != null)
            {
                _dispatcher.PostInputLine(inputLine);
            }
        }

        private void OnExecute(SnapshotSpan inputLineSpan, bool isComplete)
        {
            ExecuteInputLine.Raise(this, Tuple.Create(inputLineSpan, isComplete));
        }

        #endregion

        private void RaiseEventSafe(EventHandler handler)
        {
            if (handler != null)
            {
                ThreadHelper.Generic.Invoke(() => handler(this, EventArgs.Empty));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_keyBuffer != null)
                {
                    _keyBuffer.Dispose();
                }

                if (_cancelWaitKeySource != null)
                {
                    _cancelWaitKeySource.Dispose();
                }
            }
        }

        ~ConsoleDispatcher()
        {
            Dispose(false);
        }

        /// <summary>
        ///     This class dispatches inputs for asynchronous hosts.
        /// </summary>
        private class AsyncHostConsoleDispatcher : Dispatcher
        {
            private readonly Marshaler _marshaler;
            private Queue<InputLine> _buffer;

            public AsyncHostConsoleDispatcher(ConsoleDispatcher parentDispatcher)
                : base(parentDispatcher)
            {
                _marshaler = new Marshaler(this);
            }

            private bool IsStarted
            {
                get { return _buffer != null; }
            }

            public override void Start()
            {
                if (IsStarted)
                {
                    // Can only start once... ConsoleDispatcher is already protecting this.
                    throw new InvalidOperationException();
                }
                _buffer = new Queue<InputLine>();

                var asyncHost = WpfConsole.Host as IAsyncHost;
                if (asyncHost == null)
                {
                    // ConsoleDispatcher is already checking this.
                    throw new InvalidOperationException();
                }

                asyncHost.ExecuteEnd += _marshaler.AsyncHost_ExecuteEnd;
                PromptNewLine();
            }

            public override void PostInputLine(InputLine inputLine)
            {
                // The editor should be completely readonly unless started.
                Debug.Assert(IsStarted);

                if (IsStarted)
                {
                    _buffer.Enqueue(inputLine);
                    ProcessInputs();
                }
            }

            private void ProcessInputs()
            {
                if (IsExecuting)
                {
                    return;
                }

                if (_buffer.Count > 0)
                {
                    InputLine inputLine = _buffer.Dequeue();
                    Tuple<bool, bool> executeState = Process(inputLine);
                    if (executeState.Item1)
                    {
                        IsExecuting = true;

                        if (!executeState.Item2)
                        {
                            // If NOT really executed, processing the same as ExecuteEnd event
                            OnExecuteEnd();
                        }
                    }
                }
            }

            private void OnExecuteEnd()
            {
                if (IsStarted)
                {
                    // Filter out noise. A host could execute private commands.
                    Debug.Assert(IsExecuting);
                    IsExecuting = false;

                    PromptNewLine();
                    ProcessInputs();
                }
            }

            /// <summary>
            ///     This private Marshaler marshals async host event to main thread so that the dispatcher
            ///     doesn't need to worry about threading.
            /// </summary>
            private class Marshaler : Marshaler<AsyncHostConsoleDispatcher>
            {
                public Marshaler(AsyncHostConsoleDispatcher impl)
                    : base(impl)
                {
                }

                public void AsyncHost_ExecuteEnd(object sender, EventArgs e)
                {
                    Invoke(() => _impl.OnExecuteEnd());
                }
            }
        }

        private abstract class Dispatcher
        {
            private bool _isExecuting;

            protected Dispatcher(ConsoleDispatcher parentDispatcher)
            {
                ParentDispatcher = parentDispatcher;
                WpfConsole = parentDispatcher.WpfConsole;
            }

            protected ConsoleDispatcher ParentDispatcher { get; private set; }
            protected IPrivateWpfConsole WpfConsole { get; private set; }

            public bool IsExecuting
            {
                get { return _isExecuting; }
                protected set
                {
                    _isExecuting = value;
                    WpfConsole.SetExecutionMode(_isExecuting);
                }
            }

            /// <summary>
            ///     Process a input line.
            /// </summary>
            /// <param name="inputLine"></param>
            protected Tuple<bool, bool> Process(InputLine inputLine)
            {
                SnapshotSpan inputSpan = inputLine.SnapshotSpan;

                if (inputLine.Flags.HasFlag(InputLineFlag.Echo))
                {
                    WpfConsole.BeginInputLine();

                    if (inputLine.Flags.HasFlag(InputLineFlag.Execute))
                    {
                        WpfConsole.WriteLine(inputLine.Text);
                        inputSpan = WpfConsole.EndInputLine(true).Value;
                    }
                    else
                    {
                        WpfConsole.Write(inputLine.Text);
                    }
                }

                if (inputLine.Flags.HasFlag(InputLineFlag.Execute))
                {
                    string command = inputLine.Text;
                    bool isExecuted = WpfConsole.Host.Execute(WpfConsole, command, null);
                    WpfConsole.InputHistory.Add(command);
                    ParentDispatcher.OnExecute(inputSpan, isExecuted);
                    return Tuple.Create(true, isExecuted);
                }
                return Tuple.Create(false, false);
            }

            protected void PromptNewLine()
            {
                WpfConsole.Write(WpfConsole.Host.Prompt + (char) 32); // 32 is the space
                WpfConsole.BeginInputLine();
            }

            public void ClearConsole()
            {
                // When inputting commands
                if (WpfConsole.InputLineStart != null)
                {
                    WpfConsole.Host.Abort(); // Clear constructing multi-line command
                    WpfConsole.Clear();
                    PromptNewLine();
                }
                else
                {
                    WpfConsole.Clear();
                }
            }

            public abstract void Start();
            public abstract void PostInputLine(InputLine inputLine);
        }

        /// <summary>
        ///     This class dispatches inputs for synchronous hosts.
        /// </summary>
        private class SyncHostConsoleDispatcher : Dispatcher
        {
            public SyncHostConsoleDispatcher(ConsoleDispatcher parentDispatcher)
                : base(parentDispatcher)
            {
            }

            public override void Start()
            {
                PromptNewLine();
            }

            public override void PostInputLine(InputLine inputLine)
            {
                IsExecuting = true;
                try
                {
                    if (Process(inputLine).Item1)
                    {
                        PromptNewLine();
                    }
                }
                finally
                {
                    IsExecuting = false;
                }
            }
        }
    }

    [Flags]
    internal enum InputLineFlag
    {
        Echo = 1,
        Execute = 2
    }

    internal class InputLine
    {
        public InputLine(string text, bool execute)
        {
            Text = text;
            Flags = InputLineFlag.Echo;

            if (execute)
            {
                Flags |= InputLineFlag.Execute;
            }
        }

        public InputLine(SnapshotSpan snapshotSpan)
        {
            SnapshotSpan = snapshotSpan;
            Text = snapshotSpan.GetText();
            Flags = InputLineFlag.Execute;
        }

        public SnapshotSpan SnapshotSpan { get; private set; }
        public string Text { get; private set; }
        public InputLineFlag Flags { get; private set; }
    }
}