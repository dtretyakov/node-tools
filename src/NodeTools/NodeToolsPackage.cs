using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Common;
using Console;
using DebugEngine;
using DebugEngine.Engine;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NodeTools.Debugger;
using NodeTools.Settings;
using ProjectSystem;
using ProjectSystem.Project;

namespace NodeTools
{
    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio
    ///     is to implement the IVsPackage interface and register itself with the shell.
    ///     This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "0.1", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideProjectFactory(typeof (NodeProjectFactory), null, ProjectConstants.ProjectFilter,
        ProjectConstants.ProjectExtension, ProjectConstants.ProjectExtension, ".\\NullPath", LanguageVsTemplate = NodeConstants.LanguageName)]
    //[ProvideToolWindow(typeof (ConsoleToolWindow), Style = VsDockStyle.Tabbed, Window = "{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}", Orientation = ToolWindowOrientation.Right)]
    //[ProvideToolWindowVisibility(typeof (ConsoleToolWindow), "f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideOptionPage(typeof(GeneralOptionPage), "NodeTools", "General", 113, 114, true)]
    [ProvideObject(typeof (GeneralPropertyPage),RegisterUsing = RegistrationMethod.CodeBase)]
    [ProvideDebugEngine(NodeConstants.LanguageName, typeof (AD7ProgramProvider), typeof (AD7Engine), Guids.DebugEngineString)]
    [ProvideDebugException(Guids.DebugEngineString, DebugConstants.Exceptions)]
    [ProvideDebugLanguage(NodeConstants.LanguageName, "{EA87697D-721E-447C-8FF8-827F2FEA1F8D}", "{A4C69196-6702-4CC9-923E-605C03F75EF5}", Guids.DebugEngineString)]
    [Guid(Guids.NodeToolsPackageString)]
    public sealed class NodeToolsPackage : ProjectPackage
    {
        /// <summary>
        ///     Default constructor of the package.
        ///     Inside this method you can place any initialization code that does not require
        ///     any Visual Studio service because at this point the package object is created but
        ///     not sited yet inside Visual Studio environment. The place to do all the other
        ///     initialization is the Initialize method.
        /// </summary>
        public NodeToolsPackage()
        {
            Debug.WriteLine("Entering constructor for: {0}", ToString());
        }

        public override string ProductUserContext
        {
            get { return string.Empty; }
        }

        /// <summary>
        ///     This function is called when the user clicks the menu item that shows the
        ///     tool window. See the Initialize method to see how the menu item is associated to
        ///     this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = FindToolWindow(typeof (ConsoleToolWindow), 0, true);
            if (window == null || window.Frame == null)
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            var windowFrame = (IVsWindowFrame) window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        ///     This function is the callback used to execute a command when the a menu item is clicked.
        ///     See the Initialize method to see how the menu item is associated to this function using
        ///     the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            var uiShell = (IVsUIShell) GetService(typeof (SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                0,
                ref clsid,
                Resources.NodeToolsTitle,
                string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", ToString()),
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0, // false
                out result));
        }

        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so this is the place
        ///     where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine("Entering Initialize() of: {0}", ToString());

            base.Initialize();

            RegisterProjectFactory(new NodeProjectFactory(this));

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof (IMenuCommandService)) as OleMenuCommandService;
            if (mcs == null)
            {
                return;
            }

            // Create the command for the menu item.
            var menuCommandID = new CommandID(Guids.NodeToolsCmdSet, (int) Commands.AttachToProcess);
            var menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
            mcs.AddCommand(menuItem);

            // Create the command for the tool window
            var toolwndCommandID = new CommandID(Guids.NodeToolsCmdSet, (int) Commands.NodePackageManager);
            var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
            mcs.AddCommand(menuToolWin);
        }
    }
}