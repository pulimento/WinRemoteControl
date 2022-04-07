using System;
using System.Windows.Forms;

namespace WinRemoteControl
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.btnMute = new System.Windows.Forms.Button();
            this.btnStartClient = new System.Windows.Forms.Button();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.btnGoToBackground = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.btnOpenSettings = new System.Windows.Forms.Button();
            this.BtnAbout = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnMute
            // 
            this.btnMute.Location = new System.Drawing.Point(310, 11);
            this.btnMute.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnMute.Name = "btnMute";
            this.btnMute.Size = new System.Drawing.Size(160, 40);
            this.btnMute.TabIndex = 0;
            this.btnMute.Text = "Toggle Teams Mute";
            this.btnMute.UseVisualStyleBackColor = true;
            // 
            // btnStartClient
            // 
            this.btnStartClient.Location = new System.Drawing.Point(482, 11);
            this.btnStartClient.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnStartClient.Name = "btnStartClient";
            this.btnStartClient.Size = new System.Drawing.Size(160, 40);
            this.btnStartClient.TabIndex = 2;
            this.btnStartClient.Text = "Start listening";
            this.btnStartClient.UseVisualStyleBackColor = true;
            // 
            // textBoxLog
            // 
            this.textBoxLog.Location = new System.Drawing.Point(14, 64);
            this.textBoxLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.PlaceholderText = "Ready to start";
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLog.Size = new System.Drawing.Size(921, 448);
            this.textBoxLog.TabIndex = 3;
            // 
            // btnGoToBackground
            // 
            this.btnGoToBackground.Location = new System.Drawing.Point(849, 11);
            this.btnGoToBackground.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnGoToBackground.Name = "btnGoToBackground";
            this.btnGoToBackground.Size = new System.Drawing.Size(86, 31);
            this.btnGoToBackground.TabIndex = 4;
            this.btnGoToBackground.Text = "To tray";
            this.btnGoToBackground.UseVisualStyleBackColor = true;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "WinRemoteControl";
            // 
            // btnOpenSettings
            // 
            this.btnOpenSettings.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnOpenSettings.Location = new System.Drawing.Point(14, 11);
            this.btnOpenSettings.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOpenSettings.Name = "btnOpenSettings";
            this.btnOpenSettings.Size = new System.Drawing.Size(130, 29);
            this.btnOpenSettings.TabIndex = 5;
            this.btnOpenSettings.Text = "Open settings file";
            this.btnOpenSettings.UseVisualStyleBackColor = true;
            // 
            // BtnAbout
            // 
            this.BtnAbout.Location = new System.Drawing.Point(757, 10);
            this.BtnAbout.Name = "BtnAbout";
            this.BtnAbout.Size = new System.Drawing.Size(86, 31);
            this.BtnAbout.TabIndex = 6;
            this.BtnAbout.Text = "About";
            this.BtnAbout.UseVisualStyleBackColor = true;
            this.BtnAbout.Click += new System.EventHandler(this.BtnAbout_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(949, 529);
            this.Controls.Add(this.BtnAbout);
            this.Controls.Add(this.btnOpenSettings);
            this.Controls.Add(this.btnGoToBackground);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.btnStartClient);
            this.Controls.Add(this.btnMute);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.Text = "WinRemoteControl";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public TextBox textBoxLog;
        private Button btnMute;
        private Button btnStartClient;
        private Button btnGoToBackground;
        private NotifyIcon notifyIcon1;

        private void SetEventListeners()
        {
            btnMute.Click += new EventHandler(this.btnMute_Click);
            btnStartClient.Click += new EventHandler(this.btnStartClient_Click);
            btnGoToBackground.Click += new EventHandler(this.btnGoToBackground_Click);
            btnOpenSettings.Click += new EventHandler(this.btnOpenSettings_Click);
            this.Resize += new EventHandler(this.Form_Resize);
            this.notifyIcon1.MouseClick += new MouseEventHandler(this.notifyIcon_RestoreWindow);
            this.notifyIcon1.MouseDoubleClick += new MouseEventHandler(this.notifyIcon_RestoreWindow);
        }

        private Button btnOpenSettings;
        private Button BtnAbout;
    }
}

