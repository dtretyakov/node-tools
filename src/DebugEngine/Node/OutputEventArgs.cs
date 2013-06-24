using System;

namespace DebugEngine.Node
{
    internal sealed class OutputEventArgs : EventArgs
    {
        private readonly string _output;
        private readonly NodeProcess _process;

        public OutputEventArgs(NodeProcess process, string output)
        {
            _process = process;
            _output = output;
        }

        public NodeProcess Process
        {
            get { return _process; }
        }

        public string Output
        {
            get { return _output; }
        }
    }
}