using System.ComponentModel.Composition;
using Common.Automation;
using Console.Types;

namespace Console.Host
{
    [Export(typeof (IHostProvider))]
    [HostName(HostName)]
    [DisplayName("NuGet Provider")]
    internal class HostProvider : IHostProvider
    {
        /// <summary>
        ///     PowerConsole host name of PowerShell host.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public const string HostName = "NodeTools.Host.CommandLine";

        /// <summary>
        ///     This is a host name. Used for PowerShell "$host".
        /// </summary>
        public const string PowerConsoleHostName = "Node Package Manager Host";

        public IHost CreateHost(bool @async)
        {
            return new DefaultHost(new SolutionManager());
        }
    }
}