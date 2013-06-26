using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Project;

namespace ProjectSystem.Project
{
    [Guid(ProjectGuids.NodeProjectFactoryString)]
    public sealed class NodeProjectFactory : ProjectFactory
    {
        private readonly ProjectPackage _package;

        public NodeProjectFactory(ProjectPackage package)
            : base(package)
        {
            _package = package;
        }

        protected override string ProjectTypeGuids(string file)
        {
            base.ProjectTypeGuids(file);
            return GetType().GUID.ToString("B");
        }

        protected override ProjectNode CreateProject()
        {
            var project = new NodeProjectNode
                {
                    Package = _package
                };

            project.SetSite((IServiceProvider) ((System.IServiceProvider) _package).GetService(typeof (IServiceProvider)));

            return project;
        }
    }
}