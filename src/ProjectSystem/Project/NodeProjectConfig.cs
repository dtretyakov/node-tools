using Common;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProjectSystem.Project
{
    public sealed class NodeProjectConfig : ProjectConfig
    {
        private readonly NodeProjectNode _project;

        public NodeProjectConfig(NodeProjectNode project, string configuration)
            : base(project, configuration)
        {
            _project = project;
        }

        public override int DebugLaunch(uint flags)
        {
            var launchFlags = (__VSDBGLAUNCHFLAGS) flags;
            if ((launchFlags & __VSDBGLAUNCHFLAGS.DBGLAUNCH_NoDebug) == __VSDBGLAUNCHFLAGS.DBGLAUNCH_NoDebug)
            {
                //Start project with no debugger
                return _project.Launcher.LaunchProject(false);
            }

            //Start project with debugger 
            return _project.Launcher.LaunchProject(true);
        }

        public override int QueryDebugLaunch(uint flags, out int fCanLaunch)
        {
            bool definedStartupFile = !string.IsNullOrEmpty(_project.GetProjectProperty(NodeSettings.StartupFile));

            fCanLaunch = definedStartupFile ? 1 : 0;

            return VSConstants.S_OK;
        }

        public override int get_BuildableProjectCfg(out IVsBuildableProjectCfg pb)
        {
            pb = new NodeBuildableProjectConfig(this);
            return VSConstants.S_OK;
        }
    }
}