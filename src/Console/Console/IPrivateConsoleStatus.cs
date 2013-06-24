
using Console.Types;

namespace Console.Console
{
    internal interface IPrivateConsoleStatus : IConsoleStatus
    {
        void SetBusyState(bool isBusy);
    }
}
