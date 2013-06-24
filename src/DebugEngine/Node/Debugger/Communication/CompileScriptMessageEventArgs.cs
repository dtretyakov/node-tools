using DebugEngine.Node.Debugger.Serialization;

namespace DebugEngine.Node.Debugger.Communication
{
    internal class CompileScriptMessageEventArgs
    {
        public CompileScriptMessageEventArgs(CompileScriptMessage message)
        {
            Message = message;
        }

        public CompileScriptMessage Message { get; private set; }
    }
}