using System.ComponentModel.Composition;
using Console.Types;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Console.ConsoleWindow
{

    [Export(typeof(IClassifierProvider))]
    [ContentType(ConsoleWindow.ContentType)]
    class ClassifierProvider : IClassifierProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [Import]
        public IWpfConsoleService WpfConsoleService { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return WpfConsoleService.GetClassifier(textBuffer) as IClassifier;
        }
    }
}
