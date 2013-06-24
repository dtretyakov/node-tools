using DebugEngine.Node.Debugger;

namespace DebugEngine.Node
{
    /// <summary>
    ///     Break point.
    /// </summary>
    internal class NodeBreakpoint
    {
        private readonly string _filename;
        private readonly IDebuggerManager _manager;
        private string _condition;
        private bool _enabled = true;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="manager">Debugger manager.</param>
        /// <param name="filename">File name.</param>
        /// <param name="line">Line number.</param>
        /// <param name="column">Column number.</param>
        /// <param name="condition">Condition.</param>
        public NodeBreakpoint(IDebuggerManager manager, string filename, int line, int column, string condition)
        {
            _manager = manager;
            _filename = filename;
            _condition = condition;
            Line = line;
            Column = column;
        }

        /// <summary>
        ///     Gets a filename.
        /// </summary>
        public string Filename
        {
            get { return _filename; }
        }

        /// <summary>
        ///     Gets a line number.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        ///     Gets a column number.
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        ///     Gets or sets a condition expression.
        /// </summary>
        public string Condition
        {
            get { return _condition; }
            set
            {
                if (_condition == value)
                {
                    return;
                }

                _condition = value;

                _manager.ChangeBreakpointAsync(this);
            }
        }

        /// <summary>
        ///     Gets or sets an identifier.
        /// </summary>
        internal int Id { get; set; }

        /// <summary>
        ///     Gets or sets a enabled state.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                _manager.ChangeBreakpointAsync(this);
            }
        }

        /// <summary>
        ///     Removes a break point.
        /// </summary>
        public void Remove()
        {
            _manager.RemoveBreakpointAsync(this);
        }
    }
}