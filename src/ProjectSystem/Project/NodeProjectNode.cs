using System;
using System.Drawing;
using System.Windows.Forms;
using Common;
using Common.Ioc;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectSystem.Infrastructure;
using Utilities = ProjectSystem.Infrastructure.Utilities;

namespace ProjectSystem.Project
{
    public sealed class NodeProjectNode : ProjectNode
    {
        private static readonly ImageList Images;
        private static int _index;
        private IProjectLauncher _projectLauncher;

        static NodeProjectNode()
        {
            Images = Utilities.GetImageList(typeof (NodeProjectNode).Assembly.GetManifestResourceStream("ProjectSystem.Resources.NodeProjectNode.bmp"));
        }

        public NodeProjectNode()
        {
            _index = ImageHandler.ImageList.Images.Count;

            foreach (Image img in Images.Images)
            {
                ImageHandler.AddImage(img);
            }

            CanProjectDeleteItems = true;
        }

        public override int ImageIndex
        {
            get { return _index; }
        }

        public override Guid ProjectGuid
        {
            get { return ProjectGuids.NodeProjectFactory; }
        }

        public override string ProjectType
        {
            get { return ProjectConstants.ProjectType; }
        }

        public IProjectLauncher Launcher
        {
            get
            {
                if (_projectLauncher == null)
                {
                    var settings = ServiceLocator.GetInstance<ISettingsProvider>();
                    var pathResolver = new NodePathResolver(settings);
                    _projectLauncher = new NodeLauncher(this, pathResolver, settings);
                }

                return _projectLauncher;
            }
        }

        public override void PrepareBuild(string config, bool cleanBuild)
        {
        }

        public override MSBuildResult Build(uint vsopts, string config, IVsOutputWindowPane output, string target)
        {
            if (output != null)
            {
                output.OutputString("Build for node.js project is not required.\n");
            }

            return MSBuildResult.Successful;
        }

        protected override MSBuildResult InvokeMsBuild(string target)
        {
            return MSBuildResult.Successful;
        }

        internal override void BuildAsync(uint vsopts, string config, IVsOutputWindowPane output, string target, Action<MSBuildResult, string> uiThreadCallback)
        {
            if (output != null)
            {
                output.OutputString("Build for node.js project is not required.\n");
            }

            uiThreadCallback(MSBuildResult.Successful, target);
        }

        /// <summary>
        ///     Get the assembly name for a give configuration
        /// </summary>
        /// <param name="config">the matching configuration in the msbuild file</param>
        /// <returns>assembly name</returns>
        public override string GetAssemblyName(string config)
        {
            return GetProjectProperty(NodeSettings.StartupFile);
        }

        protected override ConfigProvider CreateConfigProvider()
        {
            return new NodeConfigProvider(this);
        }

        protected override Guid[] GetConfigurationIndependentPropertyPages()
        {
            var result = new Guid[1];
            result[0] = Guids.GeneralPropertyPage;
            return result;
        }

        protected override Guid[] GetPriorityProjectDesignerPages()
        {
            var result = new Guid[1];
            result[0] = Guids.GeneralPropertyPage;
            return result;
        }

        protected override ReferenceContainerNode CreateReferenceContainerNode()
        {
            return null;
        }
    }
}