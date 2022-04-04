using AutoUpdaterDotNET;
using System.Diagnostics;
using System.Reflection;
using WinRemoteControl.Updater;

namespace WinRemoteControl
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(this.AboutForm_Load);
        }

        public void AboutForm_Load(object? sender, EventArgs e)
        {
            if(Assembly.GetEntryAssembly() != null)
            {
                Assembly entryAssembly = Assembly.GetEntryAssembly()!;
                if (entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() != null)
                {
                    AssemblyInformationalVersionAttribute infoVersionAttr = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!;
                    string currentVersion = infoVersionAttr.InformationalVersion;
                    this.lbVersion.Text = "Version " + currentVersion;
                    return;
                }
            }            
        }

        private void BtnGoToGitHub_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Constants.REPO_URL,
                UseShellExecute = true
            });
        }

        private void BtnCheckForUpdates_Click(object sender, EventArgs e)
        {
            // See https://github.com/ravibpatel/AutoUpdater.NET
            // JSON config files already created, but beware! Must be modified
            // to the ones referring to the master branch
            AppUpdater.CheckForUpdates(UpdateEventHandler);
        }

        private void UpdateEventHandler(UpdateInfoEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show(
                    $"There was an error while checking for updates - {args.Error}",
                    "Error");
                return;
            }
            if (args.IsUpdateAvailable == false)
            {
                MessageBox.Show(
                    $"You already have the latest version installed - {args.InstalledVersion}",
                    "No new version available");
                return;
            }
            // After that, the updater must show its own UI with its own options (install, skip, remind me later, etc...)
        }
    }
}
