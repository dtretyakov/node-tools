using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Common;
using Common.Ioc;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectSystem.Infrastructure;
using ProjectSystem.Project.Automation;
using Utilities = ProjectSystem.Infrastructure.Utilities;

namespace ProjectSystem.Project
{
    /// <summary>
    ///     Node.js project node.
    /// </summary>
    public sealed class NodeProjectNode : ProjectNode
    {
        private static readonly ImageList Images;
        private static int _index;
        private bool _disposed;
        private ImageHandler _imageHandler;
        private IProjectLauncher _projectLauncher;

        /// <summary>
        ///     Static constructor.
        /// </summary>
        static NodeProjectNode()
        {
            Images = Utilities.GetImageList(GetResourceStream("ProjectSystem.Resources.NodeProjectNode.bmp"));
        }

        /// <summary>
        ///     Instance constructor.
        /// </summary>
        public NodeProjectNode()
        {
            _index = ImageHandler.ImageList.Images.Count;

            foreach (Image img in Images.Images)
            {
                ImageHandler.AddImage(img);
            }

            CanProjectDeleteItems = true;
        }

        /// <summary>
        ///     Gets an ImageHandler for the project node.
        /// </summary>
        public override ImageHandler ImageHandler
        {
            get
            {
                if (_imageHandler == null)
                {
                    _imageHandler = new ImageHandler(GetResourceStream("ProjectSystem.Resources.imagelist.bmp"));
                }

                return _imageHandler;
            }
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

        private static Stream GetResourceStream(string name)
        {
            return typeof (NodeProjectNode).Assembly.GetManifestResourceStream(name);
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

        /// <summary>
        ///     Creates a reference node.
        /// </summary>
        /// <returns></returns>
        protected override ReferenceContainerNode CreateReferenceContainerNode()
        {
            // Project does not support references.
            return null;
        }

        /// <summary>
        ///     Creates a new automation object.
        /// </summary>
        /// <returns>Automation object.</returns>
        public override object GetAutomationObject()
        {
            return new NodeOAProject(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (_imageHandler != null)
                {
                    _imageHandler.Close();
                    _imageHandler = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
                _disposed = true;
            }
        }
    }
}