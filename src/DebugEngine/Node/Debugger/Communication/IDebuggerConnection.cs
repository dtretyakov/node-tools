using System;
using System.Threading.Tasks;

namespace DebugEngine.Node.Debugger.Communication
{
    internal interface IDebuggerConnection : IDisposable
    {
        Task SendCommandAsync(string message);

        event EventHandler<StringEventArgs> OutputMessage;

        event EventHandler<EventArgs> ConnectionClosed;
    }
}