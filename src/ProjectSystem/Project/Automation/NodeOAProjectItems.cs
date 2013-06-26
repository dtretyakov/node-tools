using System;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;

namespace ProjectSystem.Project.Automation
{
    [ComVisible(true), CLSCompliant(false)]
    public sealed class NodeOAProjectItems : OAProjectItems
    {
        public NodeOAProjectItems(OAProject project, HierarchyNode nodeWithItems) : base(project, nodeWithItems)
        {
        }

        /// <summary>
        ///     Creates a new project item from an existing directory and all files and subdirectories
        ///     contained within it.
        /// </summary>
        /// <param name="directory">The full path of the directory to add.</param>
        /// <returns>A ProjectItem object.</returns>
        public override ProjectItem AddFromDirectory(string directory)
        {
            CheckProjectIsValid();

            ProjectItem result = AddFolder(directory, null);

            if (!Directory.Exists(directory))
            {
                return result;
            }

            foreach (string subdirectory in Directory.EnumerateDirectories(directory))
            {
                result.ProjectItems.AddFromDirectory(Path.Combine(directory, subdirectory));
            }

            foreach (string filename in Directory.EnumerateFiles(directory))
            {
                result.ProjectItems.AddFromFile(Path.Combine(directory, filename));
            }

            return result;
        }

        private void CheckProjectIsValid()
        {
            if (Project == null || Project.Project == null || Project.Project.Site == null || Project.Project.IsClosed)
            {
                throw new InvalidOperationException();
            }
        }
    }
}