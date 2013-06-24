using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Common;

namespace ProjectSystem.Infrastructure
{
    [Export(typeof(IPathResolver))]
    internal class NodePathResolver : IPathResolver
    {
        private readonly ISettingsProvider _settings;

        [ImportingConstructor]
        public NodePathResolver(ISettingsProvider settings)
        {
            _settings = settings;
        }

        public string FindFilePath(string filename)
        {
            string path = _settings.GetOption(NodeSettings.NodeFolder);
            if (string.IsNullOrEmpty(path))
            {
                return GetFileFullPath(filename) ?? filename;
            }

            return Path.Combine(path, filename);
        }

        /// <summary>
        ///     Try to find out node.js full path.
        /// </summary>
        /// <param name="executable">Executable name.</param>
        /// <returns>Full path to the node.exe.</returns>
        public static string GetFileFullPath(string executable)
        {
            if (File.Exists(executable))
            {
                return Path.GetFullPath(executable);
            }

            return PredictFileFolders(executable).FirstOrDefault(File.Exists);
        }

        /// <summary>
        ///     Retuens possible file locations.
        /// </summary>
        /// <param name="executable">Executable name.</param>
        /// <returns>Enumeration of locations.</returns>
        private static IEnumerable<string> PredictFileFolders(string executable)
        {
            string nodeFolder = string.Format(@"nodejs\{0}", executable);

            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), nodeFolder);
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), nodeFolder);

            string paths = Environment.GetEnvironmentVariable("PATH");
            if (paths == null)
            {
                yield break;
            }

            foreach (string path in paths.Split(';'))
            {
                yield return Path.Combine(path, executable);
            }
        }
    }
}