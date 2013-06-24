using DebugEngine.Node.Debugger.Serialization;

namespace DebugEngine.Node.Debugger.Communication
{
    internal class ExceptionMessageEventArgs
    {
        public ExceptionMessageEventArgs(ExceptionMessage message)
        {
            Message = message;
        }

        public ExceptionMessage Message { get; private set; }
    }
}