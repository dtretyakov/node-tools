using System.IO;

namespace DebugEngine.Node
{
    /// <summary>
    ///     Node script.
    /// </summary>
    internal class NodeScript
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="scriptId">Script identifier.</param>
        /// <param name="filename">Script file name.</param>
        public NodeScript(int scriptId, string filename)
        {
            Id = scriptId;
            Filename = filename;
        }

        /// <summary>
        ///     Gets a script identifier.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        ///     Gets a script name.
        /// </summary>
        public string Name
        {
            get
            {
                if (Filename.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                {
                    return Path.GetFileName(Filename);
                }

                return Filename;
            }
        }

        /// <summary>
        ///     Gets a script file name.
        /// </summary>
        public string Filename { get; private set; }
    }
}