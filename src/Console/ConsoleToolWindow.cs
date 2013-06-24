using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Common;
using Common.Ioc;
using Console.Console;
using Console.ConsoleWindow;
using Console.OutputConsole;
using Console.Types;
using Console.Utils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Console
{
    /// <summary>
    ///     This class implements the tool window exposed by this package and hosts a user control.
    ///     In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    ///     usually implemented by the package implementer.
    ///     This class derives from the ToolWindowPane class provided from the MPF in order to use its
    ///     implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid(Guids.ToolWindowPersistanceString)]
    public class ConsoleToolWindow : ToolWindowPane, IOleCommandTarget, IConsoleService
    {
        private ConsoleContainer _consoleParentPane;
        private FrameworkElement _pendingFocusPane;
        private IVsTextView _vsTextView;
        private IWpfConsole _wpfConsole;

        /// <summary>
        ///     Standard constructor for the tool window.
        /// </summary>
        public ConsoleToolWindow() :
            base(null)
        {
            Caption = Resources.ToolWindowTitle;
            BitmapResourceID = 301;
            BitmapIndex = 0;
        }

        /// <summary>
        ///     Get VS IComponentModel service.
        /// </summary>
        private IComponentModel ComponentModel
        {
            get { return this.GetService<IComponentModel>(typeof (SComponentModel)); }
        }

        private ConsoleWindow.ConsoleWindow ConsoleWindow
        {
            get { return ComponentModel.GetService<IConsoleWindow>() as ConsoleWindow.ConsoleWindow; }
        }

        private IVsUIShell VsUIShell
        {
            get { return this.GetService<IVsUIShell>(typeof (SVsUIShell)); }
        }

        private bool IsToolbarEnabled
        {
            get
            {
                return _wpfConsole != null &&
                       _wpfConsole.Dispatcher.IsStartCompleted &&
                       _wpfConsole.Host != null &&
                       _wpfConsole.Host.IsCommandEnabled;
            }
        }

        private HostInfo ActiveHostInfo
        {
            get { return ConsoleWindow.ActiveHostInfo; }
        }

        private FrameworkElement PendingFocusPane
        {
            get { return _pendingFocusPane; }
            set
            {
                if (_pendingFocusPane != null)
                {
                    _pendingFocusPane.Loaded -= PendingFocusPane_Loaded;
                }

                _pendingFocusPane = value;

                if (_pendingFocusPane != null)
                {
                    _pendingFocusPane.Loaded += PendingFocusPane_Loaded;
                }
            }
        }

        /// <summary>
        ///     Get the WpfConsole of the active host.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private IWpfConsole WpfConsole
        {
            get
            {
                if (_wpfConsole == null)
                {
                    Debug.Assert(ActiveHostInfo != null);

                    try
                    {
                        _wpfConsole = ActiveHostInfo.WpfConsole;
                    }
                    catch (Exception x)
                    {
                        _wpfConsole = ActiveHostInfo.WpfConsole;
                        _wpfConsole.Write(x.ToString());
                    }
                }

                return _wpfConsole;
            }
        }

        /// <summary>
        ///     Get the VsTextView of current WpfConsole if exists.
        /// </summary>
        private IVsTextView VsTextView
        {
            get
            {
                if (_vsTextView == null && _wpfConsole != null)
                {
                    _vsTextView = (IVsTextView) (WpfConsole.VsTextView);
                }

                return _vsTextView;
            }
        }

        /// <summary>
        ///     Get the parent pane of console panes. This serves as the Content of this tool window.
        /// </summary>
        private ConsoleContainer ConsoleParentPane
        {
            get
            {
                if (_consoleParentPane == null)
                {
                    _consoleParentPane = new ConsoleContainer();
                }

                return _consoleParentPane;
            }
        }

        public override object Content
        {
            get { return ConsoleParentPane; }
            set { base.Content = value; }
        }

        #region IConsoleService Region

        private IConsoleStatus _consoleStatus;
        private int _previousPosition;
        private ITextSnapshot _snapshot;


        private IConsole _vsOutputConsole;

        private IConsole VSOutputConsole
        {
            get
            {
                if (_vsOutputConsole == null)
                {
                    var outputConsoleProvider = ServiceLocator.GetInstance<IOutputConsoleProvider>();
                    if (null != outputConsoleProvider)
                    {
                        _vsOutputConsole = outputConsoleProvider.CreateOutputConsole(requirePowerShellHost: false);
                    }
                }

                return _vsOutputConsole;
            }
        }

        private IConsoleStatus ConsoleStatus
        {
            get
            {
                if (_consoleStatus == null)
                {
                    _consoleStatus = ServiceLocator.GetInstance<IConsoleStatus>();
                    Debug.Assert(_consoleStatus != null);
                }

                return _consoleStatus;
            }
        }

        public event EventHandler ExecuteEnd;

        public bool Execute(string command, object[] inputs)
        {
            if (ConsoleStatus.IsBusy)
            {
                VSOutputConsole.WriteLine(Resources.PackageManagerConsoleBusy);
                throw new NotSupportedException(Resources.PackageManagerConsoleBusy);
            }

            if (!String.IsNullOrEmpty(command))
            {
                WpfConsole.SetExecutionMode(true);
                // Cast the ToolWindowPane to ConsoleToolWindow
                // Access the IHost from ConsoleToolWindow as follows ConsoleToolWindow.WpfConsole.Host
                // Cast IHost to IAsyncHost
                // Also, register for IAsyncHost.ExecutedEnd and return only when the command is completed
                var powerShellConsole = (IPrivateWpfConsole) WpfConsole;
                IHost host = powerShellConsole.Host;

                var asynchost = host as IAsyncHost;
                if (asynchost != null)
                {
                    asynchost.ExecuteEnd += ConsoleCommandExecuteEnd;
                }

                // Here, we store the snapshot of the powershell Console output text buffer
                // Snapshot has reference to the buffer and the current length of the buffer
                // And, upon execution of the command, (check the commandexecuted handler)
                // the changes to the buffer is identified and copied over to the VS output window
                if (powerShellConsole.InputLineStart != null && powerShellConsole.InputLineStart.Value.Snapshot != null)
                {
                    _snapshot = powerShellConsole.InputLineStart.Value.Snapshot;
                }

                // We should write the command to the console just to imitate typical user action before executing it
                // Asserts get fired otherwise. Also, the log is displayed in a disorderly fashion
                powerShellConsole.WriteLine(command);

                return host.Execute(powerShellConsole, command, null);
            }

            return false;
        }

        private void ConsoleCommandExecuteEnd(object sender, EventArgs e)
        {
            // Flush the change in console text buffer onto the output window for testability
            // If the VSOutputConsole could not be obtained, just ignore
            if (VSOutputConsole != null && _snapshot != null)
            {
                if (_previousPosition < _snapshot.Length)
                {
                    VSOutputConsole.WriteLine(_snapshot.GetText(_previousPosition, (_snapshot.Length - _previousPosition)));
                }
                _previousPosition = _snapshot.Length;
            }

            ((IAsyncHost) sender).ExecuteEnd -= ConsoleCommandExecuteEnd;
            WpfConsole.SetExecutionMode(false);

            // This does NOT imply that the command succeeded. It just indicates that the console is ready for input now
            VSOutputConsole.WriteLine(Resources.PackageManagerConsoleCommandExecuted);
            ExecuteEnd.Raise(this, EventArgs.Empty);
        }

        #endregion

        /// <summary>
        ///     Override to forward to editor or handle accordingly if supported by this tool window.
        /// </summary>
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            int hr = OleCommandFilter.OLECMDERR_E_NOTSUPPORTED;

            if (VsTextView != null)
            {
                var cmdTarget = (IOleCommandTarget) VsTextView;
                hr = cmdTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            if (hr == OleCommandFilter.OLECMDERR_E_NOTSUPPORTED ||
                hr == OleCommandFilter.OLECMDERR_E_UNKNOWNGROUP)
            {
                var target = GetService(typeof (IOleCommandTarget)) as IOleCommandTarget;
                if (target != null)
                {
                    hr = target.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                }
            }

            return hr;
        }

        /// <summary>
        ///     Override to forward to editor or handle accordingly if supported by this tool window.
        /// </summary>
        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int hr = OleCommandFilter.OLECMDERR_E_NOTSUPPORTED;

            if (VsTextView != null)
            {
                var cmdTarget = (IOleCommandTarget) VsTextView;
                hr = cmdTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            if (hr == OleCommandFilter.OLECMDERR_E_NOTSUPPORTED ||
                hr == OleCommandFilter.OLECMDERR_E_UNKNOWNGROUP)
            {
                var target = GetService(typeof (IOleCommandTarget)) as IOleCommandTarget;
                if (target != null)
                {
                    hr = target.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                }
            }

            return hr;
        }

        public override void OnToolWindowCreated()
        {
            // Register key bindings to use in the editor
            var windowFrame = (IVsWindowFrame) Frame;
            Guid cmdUi = VSConstants.GUID_TextEditorFactory;
            windowFrame.SetGuidProperty((int) __VSFPROPID.VSFPROPID_InheritKeyBindings, ref cmdUi);

            // pause for a tiny moment to let the tool window open before initializing the host
            var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(0)
                };

            timer.Tick += (o, e) =>
                {
                    timer.Stop();
                    LoadConsoleEditor();
                };

            timer.Start();

            base.OnToolWindowCreated();
        }

        protected override void OnClose()
        {
            base.OnClose();

            WpfConsole.Dispose();
        }

        /// <summary>
        ///     This override allows us to forward these messages to the editor instance as well
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        protected override bool PreProcessMessage(ref Message m)
        {
            var vsWindowPane = VsTextView as IVsWindowPane;
            if (vsWindowPane != null)
            {
                var pMsg = new MSG[1];
                pMsg[0].hwnd = m.HWnd;
                pMsg[0].message = (uint) m.Msg;
                pMsg[0].wParam = m.WParam;
                pMsg[0].lParam = m.LParam;

                return vsWindowPane.TranslateAccelerator(pMsg) == 0;
            }

            return base.PreProcessMessage(ref m);
        }

        private void SourcesList_Exec(object sender, EventArgs e)
        {
            var args = e as OleMenuCmdEventArgs;
            if (args != null)
            {
                if (args.InValue != null || args.OutValue == IntPtr.Zero)
                {
                    throw new ArgumentException("Invalid argument", "e");
                }

                Marshal.GetNativeVariantForObject(ConsoleWindow.PackageSources, args.OutValue);
            }
        }

        private void LoadConsoleEditor()
        {
            if (WpfConsole != null)
            {
                // allow the console to start writing output
                WpfConsole.StartWritingOutput();

                var consolePane = WpfConsole.Content as FrameworkElement;
                ConsoleParentPane.AddConsoleEditor(consolePane);

                // WPF doesn't handle input focus automatically in this scenario. We
                // have to set the focus manually, otherwise the editor is displayed but
                // not focused and not receiving keyboard inputs until clicked.
                if (consolePane != null)
                {
                    PendingMoveFocus(consolePane);
                }
            }
        }

        /// <summary>
        ///     Set pending focus to a console pane. At the time of setting active host,
        ///     the pane (UIElement) is usually not loaded yet and can't receive focus.
        ///     In this case, we need to set focus in its Loaded event.
        /// </summary>
        /// <param name="consolePane"></param>
        private void PendingMoveFocus(FrameworkElement consolePane)
        {
            if (consolePane.IsLoaded && consolePane.IsConnectedToPresentationSource())
            {
                PendingFocusPane = null;
                MoveFocus(consolePane);
            }
            else
            {
                PendingFocusPane = consolePane;
            }
        }

        private void PendingFocusPane_Loaded(object sender, RoutedEventArgs e)
        {
            MoveFocus(PendingFocusPane);
            PendingFocusPane = null;
        }

        private void MoveFocus(FrameworkElement consolePane)
        {
            // TAB focus into editor (consolePane.Focus() does not work due to editor layouts)
            consolePane.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

            // Try start the console session now. This needs to be after the console
            // pane getting focus to avoid incorrect initial editor layout.
            StartConsoleSession(consolePane);
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We really don't want exceptions from the console to bring down VS")]
        private void StartConsoleSession(FrameworkElement consolePane)
        {
            if (WpfConsole != null && WpfConsole.Content == consolePane && WpfConsole.Host != null)
            {
                try
                {
                    if (WpfConsole.Dispatcher.IsStartCompleted)
                    {
                        OnDispatcherStartCompleted();
                        // if the dispatcher was started before we reach here, 
                        // it means the dispatcher has been in read-only mode (due to _startedWritingOutput = false).
                        // enable key input now.
                        WpfConsole.Dispatcher.AcceptKeyInput();
                    }
                    else
                    {
                        WpfConsole.Dispatcher.StartCompleted += (sender, args) => OnDispatcherStartCompleted();
                        WpfConsole.Dispatcher.StartWaitingKey += OnDispatcherStartWaitingKey;
                        WpfConsole.Dispatcher.Start();
                    }
                }
                catch (Exception x)
                {
                    // hide the text "initialize host" when an error occurs.
                    ConsoleParentPane.NotifyInitializationCompleted();

                    WpfConsole.WriteLine(x.GetBaseException().ToString());
                    ExceptionHelper.WriteToActivityLog(x);
                }
            }
            else
            {
                ConsoleParentPane.NotifyInitializationCompleted();
            }
        }

        private void OnDispatcherStartWaitingKey(object sender, EventArgs args)
        {
            WpfConsole.Dispatcher.StartWaitingKey -= OnDispatcherStartWaitingKey;
            // we want to hide the text "initialize host..." when waiting for key input
            ConsoleParentPane.NotifyInitializationCompleted();
        }

        private void OnDispatcherStartCompleted()
        {
            WpfConsole.Dispatcher.StartWaitingKey -= OnDispatcherStartWaitingKey;

            ConsoleParentPane.NotifyInitializationCompleted();

            // force the UI to update the toolbar
            VsUIShell.UpdateCommandUI(0 /* false = update UI asynchronously */);

            NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.PackageManagerConsoleLoaded);
        }
    }
}