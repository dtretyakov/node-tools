using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace NodeTools.Settings
{
    [SuppressMessage("Microsoft.Interoperability", "CA1408:DoNotUseAutoDualClassInterfaceType")]
    [Guid("4A5474CC-3838-4F95-8AF1-138D1B78DF96")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class GeneralOptionPage : DialogPage, IServiceProvider
    {
        private GeneralOptionControl _optionsWindow;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        protected override IWin32Window Window
        {
            get { return GeneralControl; }
        }

        /// <summary>
        ///     Gets or sets a node interperter location.
        /// </summary>
        public string NodeFolder { get; set; }

        /// <summary>
        ///     Gets or sets a node startup arguments.
        /// </summary>
        public string NodeArguments { get; set; }

        private GeneralOptionControl GeneralControl
        {
            get
            {
                if (_optionsWindow == null)
                {
                    _optionsWindow = new GeneralOptionControl(this);
                    _optionsWindow.Location = new Point(0, 0);
                }

                return _optionsWindow;
            }
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            return GetService(serviceType);
        }

        protected override void OnClosed(EventArgs e)
        {
            GeneralControl.OnClosed();
            base.OnClosed(e);
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            GeneralControl.Font = VsShellUtilities.GetEnvironmentFont(this);
            GeneralControl.OnActivated();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            GeneralControl.OnApply();
            SaveSettingsToStorage();
        }
    }
}