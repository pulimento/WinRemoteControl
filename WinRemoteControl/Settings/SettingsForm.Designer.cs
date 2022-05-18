namespace WinRemoteControl
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.cbLaunchAppAtLogon = new System.Windows.Forms.CheckBox();
            this.cbAutoStartListening = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // cbLaunchAppAtLogon
            // 
            this.cbLaunchAppAtLogon.AutoSize = true;
            this.cbLaunchAppAtLogon.Location = new System.Drawing.Point(12, 12);
            this.cbLaunchAppAtLogon.Name = "cbLaunchAppAtLogon";
            this.cbLaunchAppAtLogon.Size = new System.Drawing.Size(135, 19);
            this.cbLaunchAppAtLogon.TabIndex = 0;
            this.cbLaunchAppAtLogon.Text = "Launch app at logon";
            this.cbLaunchAppAtLogon.UseVisualStyleBackColor = true;
            // 
            // cbAutoStartListening
            // 
            this.cbAutoStartListening.AutoSize = true;
            this.cbAutoStartListening.Location = new System.Drawing.Point(12, 37);
            this.cbAutoStartListening.Name = "cbAutoStartListening";
            this.cbAutoStartListening.Size = new System.Drawing.Size(247, 19);
            this.cbAutoStartListening.TabIndex = 1;
            this.cbAutoStartListening.Text = "Automatically connect to server at startup";
            this.cbAutoStartListening.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(303, 85);
            this.Controls.Add(this.cbAutoStartListening);
            this.Controls.Add(this.cbLaunchAppAtLogon);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CheckBox cbLaunchAppAtLogon;
        private CheckBox cbAutoStartListening;
    }
}