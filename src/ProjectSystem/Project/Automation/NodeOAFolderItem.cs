using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;

namespace ProjectSystem.Project.Automation
{
    [ComVisible(true), CLSCompliant(false)]
    public sealed class NodeOAFolderItem : OAFolderItem
    {
        public NodeOAFolderItem(OAProject project, FolderNode node) : base(project, node)
        {
        }

        public override ProjectItems Collection
        {
            get
            {
                return UIThread.DoOnUIThread(delegate
                    {
                        ProjectItems items = new NodeOAProjectItems(Project, Node);
                        return items;
                    });
            }
        }
    }
}