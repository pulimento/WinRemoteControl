using WinRemoteControl.Settings;

namespace WinRemoteControl;

public partial class MainForm : Form
{
    private readonly SettingsStore store;
    private readonly CommandPipeline pipeline;
    private readonly MqttConnection connection;
    private UnifiedSettingsForm? settingsWindow;
    private readonly CheckBox enabled = new() { Text = "Enabled", AutoSize = true, Location = new Point(145, 12) };
    private bool closing, canClose;

    public MainForm(bool openSettings = false)
    {
        InitializeComponent();
        Text = AppBranding.DisplayName;
        Icon = AppBranding.CreateIcon();
        notifyIcon1.Icon = AppBranding.CreateIcon();
        notifyIcon1.Text = AppBranding.DisplayName;
        SetEventListeners();
        store = new SettingsStore();
        pipeline = new CommandPipeline(store);
        connection = new MqttConnection(store, pipeline, action =>
        {
            if (!IsDisposed && IsHandleCreated) { try { BeginInvoke(action); } catch (InvalidOperationException) { } }
        });
        btnOpenSettings.Text = "Settings";
        btnSettings.Visible = false; BtnAbout.Visible = false; btnMute.Visible = false;
        btnStopClient.Enabled = true;
        Controls.Add(enabled);
        enabled.Checked = store.Current.Enabled;
        enabled.CheckedChanged += (_, _) =>
        {
            if (enabled.Checked == store.Current.Enabled) return;
            try { var next = SettingsStore.Clone(store.Current); next.Enabled = enabled.Checked; store.Save(next); }
            catch (Exception e) { enabled.Checked = store.Current.Enabled; MessageBox.Show(this, e.Message, "Could not save settings"); }
        };
        store.Changed += () => { enabled.Checked = store.Current.Enabled; RefreshStatus(); };
        connection.StatusChanged += RefreshStatus;
        pipeline.HistoryChanged += RefreshStatus;
        textBoxLog.ReadOnly = true;
        Load += async (_, _) =>
        {
            RefreshStatus();
            if (openSettings) OpenSettings();
            else if (store.Current.StartMinimized) WindowState = FormWindowState.Minimized;
            if (store.Current.AutoConnect) await connection.StartAsync();
        };
        FormClosing += OnClosing;
    }
    private void RefreshStatus()
    {
        textBoxLog.Text = $"MQTT: {connection.Status}\r\nRemote control: {(store.Current.Enabled ? "Enabled" : "Disabled")}\r\n\r\n" +
            "Open Settings to manage mappings, actions, profiles, and command history." +
            (pipeline.PersistenceError == null ? "" : "\r\n" + pipeline.PersistenceError);
    }
    private void OpenSettings()
    {
        if (settingsWindow is { IsDisposed: false }) { settingsWindow.Activate(); return; }
        settingsWindow = new UnifiedSettingsForm(store, pipeline, connection);
        settingsWindow.Show(this);
    }
    private void BtnOpenSettings_Click(object sender, EventArgs e) => OpenSettings();
    private void BtnSettings_Click(object sender, EventArgs e) => OpenSettings();
    private void BtnAbout_Click(object sender, EventArgs e) => OpenSettings();
    private void BtnMute_Click(object sender, EventArgs e) => pipeline.Process("", null, false, "toggle_teams_mute");
    private async void BtnStartClient_Click(object sender, EventArgs e) => await connection.StartAsync();
    private async void BtnStopClient_Click(object sender, EventArgs e) => await connection.StopAsync();
    private void BtnGoToBackground_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;
    private void NotifyIcon_RestoreWindow(object sender, MouseEventArgs e)
    { Show(); WindowState = FormWindowState.Normal; notifyIcon1.Visible = false; }
    private void Form_Resize(object sender, EventArgs e)
    { if (WindowState == FormWindowState.Minimized) { Hide(); notifyIcon1.Visible = true; } }
    private async void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (canClose) return;
        e.Cancel = true;
        if (closing) return;
        if (settingsWindow is { IsDisposed: false })
        {
            settingsWindow.Close();
            if (!settingsWindow.IsDisposed) return;
        }
        closing = true;
        try { await connection.DisposeAsync(); }
        catch (Exception) { /* The session is invalidated and disposed even when disconnect fails. */ }
        finally { canClose = true; Close(); }
    }
}
