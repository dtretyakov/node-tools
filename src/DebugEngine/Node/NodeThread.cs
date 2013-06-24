using System.Collections.Generic;

namespace DebugEngine.Node
{
    internal class NodeThread
    {
        private readonly long _identity;
        private readonly NodeProcess _process;

        public NodeThread(NodeProcess process, long identity)
        {
            _process = process;
            _identity = identity;
            Name = "Worker Thread";
        }

        public bool IsWorkerThread
        {
            get { return true; }
        }

        public string Name { get; private set; }

        public long Id
        {
            get { return _identity; }
        }

        public IList<NodeStackFrame> Frames
        {
            get { return _process.Debugger.GetStackFramesAsync().Result; }
        }

        public void ClearSteppingState()
        {
        }
    }
}