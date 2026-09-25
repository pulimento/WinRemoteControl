namespace WinRemoteControl;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var instance = new Mutex(true, AppBranding.InstanceMutexName, out bool firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show("WinRemoteControl is already running. Debug and Release share settings; close the running version before opening another.", AppBranding.DisplayName);
            return;
        }
        try
        {
            if (args.Contains("--apply-default-profile")) new Settings.SettingsStore().ApplyDefaultWithRecoveryProfile();
            Application.Run(new MainForm(args.Contains("--settings")));
        }
        catch (Exception e)
        {
            MessageBox.Show($"WinRemoteControl could not start. Your settings have not been reset.\n\n{e.Message}\n\nConfiguration: {Settings.SettingsStore.DataDirectory}",
                AppBranding.DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { instance.ReleaseMutex(); }
    }
}
