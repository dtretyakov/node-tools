using System;
using System.Windows.Forms;
using NodeTools.Infrastructure;

namespace NodeTools.Settings
{
    public partial class GeneralOptionControl : UserControl
    {
        private readonly GeneralOptionPage _dialogPage;
        private bool _initialized;

        public GeneralOptionControl()
        {
            InitializeComponent();
        }

        public GeneralOptionControl(GeneralOptionPage dialogPage)
            : this()
        {
            _dialogPage = dialogPage;
        }

        internal void OnActivated()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            nodeLocationText.Text = _dialogPage.NodeFolder;
            nodeArgumentsText.Text = _dialogPage.NodeArguments;
        }

        internal void OnApply()
        {
            _dialogPage.NodeFolder = nodeLocationText.Text;
            _dialogPage.NodeArguments = nodeArgumentsText.Text;
        }

        internal void OnClosed()
        {
            _initialized = false;
        }

        private void OnBrowseFolderButtonClick(object sender, EventArgs e)
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var browser = new FolderBrowser2
                {
                    DirectoryPath = folderPath
                };

            if (!string.IsNullOrEmpty(nodeLocationText.Text))
            {
                browser.DirectoryPath = nodeLocationText.Text;
            }

            if (browser.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            nodeLocationText.Text = browser.DirectoryPath;
        }
    }
}