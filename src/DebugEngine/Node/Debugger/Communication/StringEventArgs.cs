using System;

namespace DebugEngine.Node.Debugger.Communication
{
    public sealed class StringEventArgs : EventArgs
    {
        public StringEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}