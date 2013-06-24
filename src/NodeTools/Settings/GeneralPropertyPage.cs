using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Common;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;

namespace NodeTools.Settings
{
    [ComVisible(true)]
    [Guid(Guids.GeneralPropertyPageString)]
    public sealed class GeneralPropertyPage : SettingsPage
    {
        private int _debuggerPort;
        private string _startupFile;

        public GeneralPropertyPage()
        {
            Name = "General";
        }

        [Category("Node.js Settings")]
        [DisplayName("Startup File")]
        [Description("Specifies the startup file for node.js application.")]
        public string StartupFile
        {
            get { return _startupFile; }
            set
            {
                _startupFile = value;
                IsDirty = true;
            }
        }

        [Category("Node.js Settings")]
        [DisplayName("Debug Port")]
        [Description("Specifies the debugger port for node.js application.")]
        public int DebuggerPort
        {
            get { return _debuggerPort; }
            set
            {
                _debuggerPort = value;
                IsDirty = true;
            }
        }

        protected override void BindProperties()
        {
            _startupFile = ProjectMgr.GetProjectProperty(NodeSettings.StartupFile, false);
            string portValue = ProjectMgr.GetProjectProperty(NodeSettings.DebuggerPort, false);

            if (!int.TryParse(portValue, out _debuggerPort))
            {
                _debuggerPort = 5858;
            }
        }

        protected override int ApplyChanges()
        {
            ProjectMgr.SetProjectProperty(NodeSettings.StartupFile, _startupFile);
            ProjectMgr.SetProjectProperty(NodeSettings.DebuggerPort, _debuggerPort.ToString(CultureInfo.InvariantCulture));

            IsDirty = false;

            return VSConstants.S_OK;
        }
    }
}