using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

namespace NodeTools.Infrastructure
{
    public class FolderNameEditor2 : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var browser = new FolderBrowser2
                {
                    DirectoryPath = folderPath
                };

            if (value != null)
            {
                browser.DirectoryPath = value.ToString();
            }

            if (browser.ShowDialog(null) == DialogResult.OK)
            {
                return browser.DirectoryPath;
            }

            return value;
        }
    }
}