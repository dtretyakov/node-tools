using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using Common.Ioc;
using Console.Types;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;

namespace Console.OutputConsole
{
    [Export(typeof (IOutputConsoleProvider))]
    public class OutputConsoleProvider : IOutputConsoleProvider
    {
        private IConsole _console;

        public IConsole CreateOutputConsole(bool requirePowerShellHost)
        {
            if (_console == null)
            {
                var serviceProvider = ServiceLocator.GetInstance<IServiceProvider>();
                var outputWindow = (IVsOutputWindow) serviceProvider.GetService(typeof (SVsOutputWindow));
                Debug.Assert(outputWindow != null);

                _console = new OutputConsole(outputWindow);
            }

            // only instantiate the PS host if necessary (e.g. when package contains PS script files)
            if (requirePowerShellHost && _console.Host == null)
            {
                IHostProvider hostProvider = GetPowerShellHostProvider();
                _console.Host = hostProvider.CreateHost(async: false);
            }

            return _console;
        }

        private static IHostProvider GetPowerShellHostProvider()
        {
            // The PowerConsole design enables multiple hosts (PowerShell, Python, Ruby)
            // For the Output window console, we're only interested in the PowerShell host. 
            // Here we filter out the PowerShell host provider based on its name.

            // The PowerShell host provider name is defined in HostProvider.cs
            const string PowerShellHostProviderName = "NuGetConsole.Host.PowerShell";

            IComponentModel componentModel = ServiceLocator.GetGlobalService<SComponentModel, IComponentModel>();
            ExportProvider exportProvider = componentModel.DefaultExportProvider;
            IEnumerable<Lazy<IHostProvider, IHostMetadata>> hostProviderExports = exportProvider.GetExports<IHostProvider, IHostMetadata>();
            Lazy<IHostProvider, IHostMetadata> psProvider = hostProviderExports.Single(export => export.Metadata.HostName == PowerShellHostProviderName);

            return psProvider.Value;
        }
    }
}