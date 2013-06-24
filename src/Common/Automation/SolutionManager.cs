using System;
using System.Linq;
using Common.Infrastructure;
using Common.Ioc;
using EnvDTE;

namespace Common.Automation
{
    public sealed class SolutionManager : ISolutionManager
    {
        private Project ActiveProject
        {
            get
            {
                var dte = ServiceLocator.GetInstance<DTE>();
                var projects = (Array) dte.ActiveSolutionProjects;
                if (projects.Length == 0)
                {
                    return null;
                }

                return projects.Cast<Project>().FirstOrDefault();
            }
        }

        public string Path
        {
            get
            {
                var dte = ServiceLocator.GetInstance<DTE>();
                return dte.Solution.FullName;
            }
        }

        public string ActiveProjectPath
        {
            get
            {
                Project project = ActiveProject;
                if (project == null)
                {
                    return null;
                }

                return System.IO.Path.GetDirectoryName(project.FullName);
            }
        }

        public void AddFile(string path)
        {
            Project project = ActiveProject;
            if (project == null)
            {
                return;
            }

            project.ProjectItems.AddFromFile(path);
        }

        public void AddDirectory(string path)
        {
            Project project = ActiveProject;
            if (project == null)
            {
                return;
            }

            project.ProjectItems.AddFromDirectory(path);
        }

        public void RemoveFile(string path)
        {
            Project project = ActiveProject;
            if (project == null)
            {
                return;
            }

            ProjectItem projectItem = project.GetProjectItem(path);
            if (projectItem == null)
            {
                return;
            }

            projectItem.Remove();
        }

        public void RemoveDirectory(string path)
        {
            Project project = ActiveProject;
            if (project == null)
            {
                return;
            }

            ProjectItem projectItem = project.GetProjectItem(path);
            if (projectItem == null)
            {
                return;
            }

            projectItem.Remove();
        }
    }
}