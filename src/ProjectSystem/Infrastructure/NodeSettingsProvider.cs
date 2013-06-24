using System.ComponentModel.Composition;
using System.Linq;
using Common;
using Common.Ioc;
using EnvDTE;

namespace ProjectSystem.Infrastructure
{
    [Export(typeof(ISettingsProvider))]
    internal class NodeSettingsProvider : ISettingsProvider
    {
        public string GetOption(string name)
        {
            DTE dte = ServiceLocator.GetGlobalService<DTE, DTE>();
            if (dte == null)
            {
                return null;
            }

            Properties properties = dte.Properties[NodeSettings.Category, NodeSettings.GeneralPage];

            Property property = properties.Cast<Property>().FirstOrDefault(p => p.Name == name);
            if (property == null)
            {
                return null;
            }

            return (string) property.Value;
        }
    }
}