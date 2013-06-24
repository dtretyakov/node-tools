/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Common;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProjectSystem.Infrastructure;

namespace ProjectSystem.Project
{
    /// <summary>
    ///     Implements functionality of starting a project or a file with or without debugging.
    /// </summary>
    internal sealed class NodeLauncher : IProjectLauncher
    {
        private const string Executable = "node.exe";
        private readonly IPathResolver _pathResolver;
        private readonly ISettingsProvider _settings;
        private readonly NodeProjectNode _project;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="project">Project.</param>
        /// <param name="pathResolver">File path resolver.</param>
        /// <param name="settings">Settings.</param>
        public NodeLauncher(NodeProjectNode project, IPathResolver pathResolver, ISettingsProvider settings)
        {
            Utilities.ArgumentNotNull("project", project);
            _project = project;
            _pathResolver = pathResolver;
            _settings = settings;
        }

        /// <summary>
        ///     Launches a project interpeter.
        /// </summary>
        /// <param name="debug">Defines a debug mode.</param>
        /// <returns>Result.</returns>
        public int LaunchProject(bool debug)
        {
            string startupFile = _project.GetProjectProperty(NodeSettings.StartupFile);
            return LaunchFile(startupFile, debug);
        }

        /// <summary>
        ///     Launches a file debugging.
        /// </summary>
        /// <param name="file">File path.</param>
        /// <param name="debug">Defines a debug mode.</param>
        /// <returns>Result.</returns>
        public int LaunchFile(string file, bool debug)
        {
            if (debug)
            {
                StartWithDebugger(file);
            }
            else
            {
                StartWithoutDebugger(file);
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        ///     Creates language specific command line for starting the project without debugging.
        /// </summary>
        public string CreateArgumentsNoDebug(string startupFile)
        {
            string arguments = _settings.GetOption(NodeSettings.NodeArguments);
            if (string.IsNullOrEmpty(arguments))
            {
                return startupFile;
            }

            return string.Format("{0} {1}", startupFile, arguments);
        }

        /// <summary>
        ///     Creates language specific command line for starting the project with debugging.
        /// </summary>
        public string CreateArgumentsDebug(string startupFile)
        {
            string arguments = CreateArgumentsNoDebug(startupFile);

            int port;
            string portValue = _project.GetProjectProperty(NodeSettings.DebuggerPort);
            if (!int.TryParse(portValue, out port))
            {
                port = 5858;
            }

            return string.Format("--debug-brk={0} {1}", port, arguments);
        }

        /// <summary>
        ///     Default implementation of the "Start without Debugging" command.
        /// </summary>
        private void StartWithoutDebugger(string startupFile)
        {
            Process.Start(CreateProcessStartInfoNoDebug(startupFile));
        }

        /// <summary>
        ///     Default implementation of the "Start Debugging" command.
        /// </summary>
        private void StartWithDebugger(string startupFile)
        {
            var dbgInfo = new VsDebugTargetInfo();
            dbgInfo.cbSize = (uint) Marshal.SizeOf(dbgInfo);

            SetupDebugInfo(ref dbgInfo, startupFile);
            LaunchDebugger(_project.Package, dbgInfo);
        }

        /// <summary>
        ///     Launches a debugger with provided parameters.
        /// </summary>
        /// <param name="provider">Service provider.</param>
        /// <param name="dbgInfo">Debugger information.</param>
        private static void LaunchDebugger(IServiceProvider provider, VsDebugTargetInfo dbgInfo)
        {
            if (!Directory.Exists(UnquotePath(dbgInfo.bstrCurDir)))
            {
                string message = string.Format("Working directory \"{0}\" does not exist.", dbgInfo.bstrCurDir);
                MessageBox.Show(message, Resources.NodeToolsTitle);
                return;
            }

            if (!File.Exists(UnquotePath(dbgInfo.bstrExe)))
            {
                string message = String.Format("Unable to find \"{0}\" executable location.\nPlease setup node.js directory in the project settings.", dbgInfo.bstrExe);
                MessageBox.Show(message, Resources.NodeToolsTitle);
                return;
            }

            VsShellUtilities.LaunchDebugger(provider, dbgInfo);
        }

        /// <summary>
        ///     Removes pair quotes from path.
        /// </summary>
        /// <param name="path">Path value.</param>
        /// <returns></returns>
        private static string UnquotePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path.StartsWith("\"") && path.EndsWith("\""))
            {
                return path.Substring(1, path.Length - 2);
            }

            return path;
        }

        /// <summary>
        ///     Sets up debugger information.
        /// </summary>
        private void SetupDebugInfo(ref VsDebugTargetInfo dbgInfo, string startupFile)
        {
            dbgInfo.dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;
            dbgInfo.bstrExe = _pathResolver.FindFilePath(Executable);
            dbgInfo.bstrCurDir = _project.ProjectFolder;
            dbgInfo.bstrArg = CreateArgumentsDebug(startupFile);
            dbgInfo.bstrRemoteMachine = null;
            dbgInfo.fSendStdoutToOutputWindow = 0;

            // Set the Node debugger
            dbgInfo.clsidCustom = Guids.DebugEngine;
            dbgInfo.grfLaunch = (uint) __VSDBGLAUNCHFLAGS.DBGLAUNCH_StopDebuggingOnEnd;
        }

        /// <summary>
        ///     Creates process info used to start the project with no debugging.
        /// </summary>
        private ProcessStartInfo CreateProcessStartInfoNoDebug(string startupFile)
        {
            string arguments = CreateArgumentsNoDebug(startupFile);
            string filePath = _pathResolver.FindFilePath(Executable);

            var startInfo = new ProcessStartInfo(filePath, arguments)
                {
                    WorkingDirectory = _project.ProjectFolder,
                    UseShellExecute = false
                };

            return startInfo;
        }
    }
}