using Console.Types;

namespace Console.OutputConsole
{
    public interface IOutputConsoleProvider
    {
        IConsole CreateOutputConsole(bool requirePowerShellHost);
    }
}
