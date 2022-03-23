namespace WinRemoteControl
{
    partial class AboutForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.BtnGoToGitHub = new System.Windows.Forms.Button();
            this.BtnCheckForUpdates = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lbVersion = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // BtnGoToGitHub
            // 
            this.BtnGoToGitHub.Location = new System.Drawing.Point(12, 101);
            this.BtnGoToGitHub.Name = "BtnGoToGitHub";
            this.BtnGoToGitHub.Size = new System.Drawing.Size(200, 30);
            this.BtnGoToGitHub.TabIndex = 0;
            this.BtnGoToGitHub.Text = "View code on GitHub";
            this.BtnGoToGitHub.UseVisualStyleBackColor = true;
            this.BtnGoToGitHub.Click += new System.EventHandler(this.BtnGoToGitHub_Click);
            // 
            // BtnCheckForUpdates
            // 
            this.BtnCheckForUpdates.Location = new System.Drawing.Point(216, 101);
            this.BtnCheckForUpdates.Name = "BtnCheckForUpdates";
            this.BtnCheckForUpdates.Size = new System.Drawing.Size(200, 30);
            this.BtnCheckForUpdates.TabIndex = 1;
            this.BtnCheckForUpdates.Text = "Check for updates";
            this.BtnCheckForUpdates.UseVisualStyleBackColor = true;
            this.BtnCheckForUpdates.Click += new System.EventHandler(this.BtnCheckForUpdates_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(249, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "WinFormsControl, from @pulimento";
            // 
            // lbVersion
            // 
            this.lbVersion.AutoSize = true;
            this.lbVersion.Location = new System.Drawing.Point(26, 38);
            this.lbVersion.Name = "lbVersion";
            this.lbVersion.Size = new System.Drawing.Size(57, 20);
            this.lbVersion.TabIndex = 3;
            this.lbVersion.Text = "Version";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(26, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(248, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Please visit the GitHub repo for help";
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 143);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbVersion);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.BtnCheckForUpdates);
            this.Controls.Add(this.BtnGoToGitHub);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AboutForm";
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button BtnGoToGitHub;
        private Button BtnCheckForUpdates;
        private Label label1;
        private Label lbVersion;
        private Label label2;
    }
}