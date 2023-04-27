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
            this.cbLaunchAppAtLogon=new CheckBox();
            this.cbAutoStartListening=new CheckBox();
            this.cbStartMinimized=new CheckBox();
            this.SuspendLayout();
            // 
            // cbLaunchAppAtLogon
            // 
            this.cbLaunchAppAtLogon.AutoSize=true;
            this.cbLaunchAppAtLogon.Location=new Point(14, 16);
            this.cbLaunchAppAtLogon.Margin=new Padding(3, 4, 3, 4);
            this.cbLaunchAppAtLogon.Name="cbLaunchAppAtLogon";
            this.cbLaunchAppAtLogon.Size=new Size(167, 24);
            this.cbLaunchAppAtLogon.TabIndex=0;
            this.cbLaunchAppAtLogon.Text="Launch app at logon";
            this.cbLaunchAppAtLogon.UseVisualStyleBackColor=true;
            // 
            // cbAutoStartListening
            // 
            this.cbAutoStartListening.AutoSize=true;
            this.cbAutoStartListening.Location=new Point(14, 49);
            this.cbAutoStartListening.Margin=new Padding(3, 4, 3, 4);
            this.cbAutoStartListening.Name="cbAutoStartListening";
            this.cbAutoStartListening.Size=new Size(307, 24);
            this.cbAutoStartListening.TabIndex=1;
            this.cbAutoStartListening.Text="Automatically connect to server at startup";
            this.cbAutoStartListening.UseVisualStyleBackColor=true;
            // 
            // cbStartMinimized
            // 
            this.cbStartMinimized.AutoSize=true;
            this.cbStartMinimized.Location=new Point(14, 81);
            this.cbStartMinimized.Margin=new Padding(3, 4, 3, 4);
            this.cbStartMinimized.Name="cbStartMinimized";
            this.cbStartMinimized.Size=new Size(348, 24);
            this.cbStartMinimized.TabIndex=2;
            this.cbStartMinimized.Text="Hide main window at startup. Show only on tray";
            this.cbStartMinimized.UseVisualStyleBackColor=true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions=new SizeF(8F, 20F);
            this.AutoScaleMode=AutoScaleMode.Font;
            this.ClientSize=new Size(382, 131);
            this.Controls.Add(this.cbStartMinimized);
            this.Controls.Add(this.cbAutoStartListening);
            this.Controls.Add(this.cbLaunchAppAtLogon);
            this.Icon=(Icon)resources.GetObject("$this.Icon");
            this.Margin=new Padding(3, 4, 3, 4);
            this.Name="SettingsForm";
            this.Text="Settings";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private CheckBox cbLaunchAppAtLogon;
        private CheckBox cbAutoStartListening;
        private CheckBox cbStartMinimized;
    }
}