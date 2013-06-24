using DebugEngine.Node.Debugger.Serialization;

namespace DebugEngine.Node.Debugger.Communication
{
    internal class BreakpointMessageEventArgs
    {
        public BreakpointMessageEventArgs(BreakpointMessage message)
        {
            Message = message;
        }

        public BreakpointMessage Message { get; private set; }
    }
}