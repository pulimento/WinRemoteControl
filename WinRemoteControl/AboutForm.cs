using WinRemoteControl.Updater;

namespace WinRemoteControl
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void BtnGoToGitHub_Click(object sender, EventArgs e)
        {
            // It crashes the app
            System.Diagnostics.Process.Start(Constants.REPO_URL);
        }

        private void BtnCheckForUpdates_Click(object sender, EventArgs e)
        {
            // See https://github.com/ravibpatel/AutoUpdater.NET
            // JSON config files already created, but beware! Must be modified
            // to the ones referring to the master branch
            AppUpdater.CheckForUpdates();
        }
    }
}
