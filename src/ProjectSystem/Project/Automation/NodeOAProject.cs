using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Project.Automation;

namespace ProjectSystem.Project.Automation
{
    [ComVisible(true), CLSCompliant(false)]
    public sealed class NodeOAProject : OAProject
    {
        private const string JavaScriptProjectGuid = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
        private readonly NodeProjectNode _project;

        public NodeOAProject(NodeProjectNode project) : base(project)
        {
            _project = project;
        }

        public override string Kind
        {
            get { return JavaScriptProjectGuid; }
        }

        /// <summary>
        ///     Gets a ProjectItems collection for the Project object.
        /// </summary>
        public override ProjectItems ProjectItems
        {
            get { return new NodeOAProjectItems(this, _project); }
        }
    }
}