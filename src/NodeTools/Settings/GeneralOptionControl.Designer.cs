namespace NodeTools.Settings
{
    partial class GeneralOptionControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralOptionControl));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.browseFolderButton = new System.Windows.Forms.Button();
            this.nodeLocationText = new System.Windows.Forms.TextBox();
            this.nodeLocationLabel = new System.Windows.Forms.Label();
            this.nodeArgumentsLabel = new System.Windows.Forms.Label();
            this.nodeArgumentsText = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.browseFolderButton, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.nodeLocationText, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // browseFolderButton
            // 
            resources.ApplyResources(this.browseFolderButton, "browseFolderButton");
            this.browseFolderButton.Name = "browseFolderButton";
            this.browseFolderButton.UseVisualStyleBackColor = true;
            this.browseFolderButton.Click += new System.EventHandler(this.OnBrowseFolderButtonClick);
            // 
            // nodeLocationText
            // 
            resources.ApplyResources(this.nodeLocationText, "nodeLocationText");
            this.nodeLocationText.Name = "nodeLocationText";
            // 
            // nodeLocationLabel
            // 
            resources.ApplyResources(this.nodeLocationLabel, "nodeLocationLabel");
            this.nodeLocationLabel.Name = "nodeLocationLabel";
            // 
            // nodeArgumentsLabel
            // 
            resources.ApplyResources(this.nodeArgumentsLabel, "nodeArgumentsLabel");
            this.nodeArgumentsLabel.Name = "nodeArgumentsLabel";
            // 
            // nodeArgumentsText
            // 
            resources.ApplyResources(this.nodeArgumentsText, "nodeArgumentsText");
            this.nodeArgumentsText.Name = "nodeArgumentsText";
            // 
            // GeneralOptionControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.nodeArgumentsText);
            this.Controls.Add(this.nodeArgumentsLabel);
            this.Controls.Add(this.nodeLocationLabel);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "GeneralOptionControl";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button browseFolderButton;
        private System.Windows.Forms.TextBox nodeLocationText;
        private System.Windows.Forms.Label nodeLocationLabel;
        private System.Windows.Forms.Label nodeArgumentsLabel;
        private System.Windows.Forms.TextBox nodeArgumentsText;
    }
}
