using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;

namespace ProjectSystem.Project.Automation
{
    [ComVisible(true), CLSCompliant(false)]
    public sealed class NodeOAFileItem : OAFileItem
    {
        public NodeOAFileItem(OAProject project, FileNode node) : base(project, node)
        {
        }

        /// <summary>
        ///     Gets the ProjectItems for the object.
        /// </summary>
        public override ProjectItems ProjectItems
        {
            get
            {
                return UIThread.DoOnUIThread(delegate
                    {
                        if (Project.Project.CanFileNodesHaveChilds)
                        {
                            return new NodeOAProjectItems(Project, Node);
                        }

                        return base.ProjectItems;
                    });
            }
        }
    }
}