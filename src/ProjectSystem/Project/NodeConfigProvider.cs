using Microsoft.VisualStudio.Project;

namespace ProjectSystem.Project
{
    public sealed class NodeConfigProvider : ConfigProvider
    {
        private readonly NodeProjectNode _projectNode;

        public NodeConfigProvider(NodeProjectNode projectNode)
            : base(projectNode)
        {
            _projectNode = projectNode;
        }

        protected override ProjectConfig CreateProjectConfiguration(string configName)
        {
            return new NodeProjectConfig(_projectNode, configName);
        }
    }
}