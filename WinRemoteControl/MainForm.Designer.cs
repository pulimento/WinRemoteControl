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
            this.SuspendLayout();
            // 
            // btnMute
            // 
            this.btnMute.Location = new System.Drawing.Point(271, 8);
            this.btnMute.Name = "btnMute";
            this.btnMute.Size = new System.Drawing.Size(140, 30);
            this.btnMute.TabIndex = 0;
            this.btnMute.Text = "Toggle Teams Mute";
            this.btnMute.UseVisualStyleBackColor = true;
            // 
            // btnStartClient
            // 
            this.btnStartClient.Location = new System.Drawing.Point(422, 8);
            this.btnStartClient.Name = "btnStartClient";
            this.btnStartClient.Size = new System.Drawing.Size(140, 30);
            this.btnStartClient.TabIndex = 2;
            this.btnStartClient.Text = "Start listening";
            this.btnStartClient.UseVisualStyleBackColor = true;
            // 
            // textBoxLog
            // 
            this.textBoxLog.Location = new System.Drawing.Point(12, 48);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.PlaceholderText = "Ready to start";
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLog.Size = new System.Drawing.Size(806, 337);
            this.textBoxLog.TabIndex = 3;
            // 
            // btnGoToBackground
            // 
            this.btnGoToBackground.Location = new System.Drawing.Point(743, 8);
            this.btnGoToBackground.Name = "btnGoToBackground";
            this.btnGoToBackground.Size = new System.Drawing.Size(75, 23);
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
            this.btnOpenSettings.Location = new System.Drawing.Point(12, 8);
            this.btnOpenSettings.Name = "btnOpenSettings";
            this.btnOpenSettings.Size = new System.Drawing.Size(114, 22);
            this.btnOpenSettings.TabIndex = 5;
            this.btnOpenSettings.Text = "Open settings file";
            this.btnOpenSettings.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(830, 397);
            this.Controls.Add(this.btnOpenSettings);
            this.Controls.Add(this.btnGoToBackground);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.btnStartClient);
            this.Controls.Add(this.btnMute);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
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
    }
}

