using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Registry = Microsoft.Win32.Registry;
using WinRemoteControl.Actions;

namespace WinRemoteControl.Settings;

public sealed class UnifiedSettingsForm : Form
{
    private readonly SettingsStore store;
    private readonly CommandPipeline pipeline;
    private readonly MqttConnection connection;
    private RemoteSettings draft;
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly TextBox host = new(), clientId = new(), username = new(), password = new() { UseSystemPasswordChar = true };
    private readonly NumericUpDown port = new() { Minimum = 1, Maximum = 65535 }, timeout = new() { Minimum = 1, Maximum = 1440 }, reconnect = new() { Minimum = 1, Maximum = 3600 };
    private readonly CheckBox launch = new() { Text = "Launch at Windows sign-in", AutoSize = true }, auto = new() { Text = "Connect MQTT at app startup", AutoSize = true }, minimized = new() { Text = "Start minimized in the tray", AutoSize = true };
    private readonly Label status = new() { AutoSize = true }, notice = new() { AutoSize = true, ForeColor = Color.Firebrick }, historyNotice = new() { AutoSize = true };
    private readonly DataGridView mappings = Grid(), actions = Grid(), history = Grid();
    private readonly ListBox profiles = new() { Dock = DockStyle.Fill, DisplayMember = "Name" };
    private readonly TextBox profileName = new() { Width = 200, PlaceholderText = "New profile name" };
    private readonly ComboBox testChoice = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, DisplayMember = "Name", ValueMember = "Id" };
    private readonly TextBox testText = new() { Dock = DockStyle.Top, Multiline = true, Height = 75, PlaceholderText = "Keyboard / dictation test field" };
    private bool loading;
    private string baseline = "";
    private readonly System.Windows.Forms.Timer testTimer = new() { Interval = 3000 };
    private string? pendingTest;

    public UnifiedSettingsForm(SettingsStore store, CommandPipeline pipeline, MqttConnection connection)
    {
        this.store = store; this.pipeline = pipeline; this.connection = connection;
        draft = SettingsStore.Clone(store.Current);
        Text = AppBranding.DisplayName + " Settings";
        Icon = AppBranding.CreateIcon();
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 600); ClientSize = new Size(1000, 680);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8) };
        footer.Controls.Add(Button("Save", Save)); footer.Controls.Add(Button("Revert", Reload)); footer.Controls.Add(notice);
        Controls.Add(tabs); Controls.Add(footer);
        notice.MaximumSize = new Size(700, 0);
        BuildGeneral(); BuildMappings(); BuildActions(); BuildProfiles(); BuildHistory(); BuildAbout();
        Reload();
        pipeline.HistoryChanged += RefreshHistory;
        connection.StatusChanged += RefreshStatus;
        RefreshStatus(); RefreshHistory();
        testTimer.Tick += (_, _) =>
        {
            testTimer.Stop();
            if (pendingTest != null)
            {
                var result = pipeline.Process("", null, false, pendingTest);
                notice.Text = result.Failure ?? "Test input dispatched. See History for details.";
                pendingTest = null;
            }
        };
        FormClosing += (_, e) =>
        {
            if (Dirty() && MessageBox.Show(this, "Discard unsaved settings, mapping, and action edits?", "Unsaved edits",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) e.Cancel = true;
        };
        FormClosed += (_, _) =>
        {
            pipeline.HistoryChanged -= RefreshHistory; connection.StatusChanged -= RefreshStatus; testTimer.Dispose();
        };
    }

    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, AutoGenerateColumns = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = SystemColors.Window,
        RowHeadersWidth = 28
    };
    private TabPage Page(string title) { var page = new TabPage(title) { Padding = new Padding(12) }; tabs.TabPages.Add(page); return page; }
    private Button Button(string title, Action handler)
    {
        var button = new Button { Text = title, AutoSize = true };
        button.Click += (_, _) => Guard(handler);
        return button;
    }
    private void Guard(Action handler)
    {
        try { handler(); }
        catch (Exception e) { notice.Text = e is YamlDotNet.Core.YamlException ? "Invalid Windows profile YAML. Check its fields and structure." : e.Message; }
    }
    private static FlowLayoutPanel Bar() => new() { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
    private static void Column(DataGridView grid, string property, string title, bool readOnly = false)
        => grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = title, ReadOnly = readOnly });

    private void BuildGeneral()
    {
        var page = Page("General");
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Row(string text, Control control)
        {
            int row = table.RowCount++; control.Dock = DockStyle.Fill;
            table.Controls.Add(new Label { Text = text, AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, 0, row);
            table.Controls.Add(control, 1, row);
        }
        Row("MQTT server", host); Row("Port", port); Row("Client ID", clientId); Row("Username", username); Row("Password", password);
        Row("Communication timeout (minutes)", timeout); Row("Reconnect delay (seconds)", reconnect);
        Row("Startup", launch); Row("", auto); Row("", minimized); Row("MQTT status", status);
        var buttons = new FlowLayoutPanel { AutoSize = true };
        buttons.Controls.Add(Button("Connect saved settings", () => { _ = connection.StartAsync(); }));
        buttons.Controls.Add(Button("Disconnect", () => { _ = connection.StopAsync(); }));
        Row("", buttons);
        Row("Configuration directory", new TextBox { ReadOnly = true, Text = SettingsStore.DataDirectory });
        page.Controls.Add(table);
    }
    private void BuildMappings()
    {
        var page = Page("Mappings");
        Column(mappings, nameof(Config.ActionMapping.Topic), "MQTT topic");
        mappings.Columns.Add(new DataGridViewComboBoxColumn { DataPropertyName = nameof(Config.ActionMapping.Action), HeaderText = "Action", DisplayMember = "Name", ValueMember = "Id" });
        mappings.DataError += (_, e) => { e.ThrowException = false; notice.Text = "Choose an existing action for each mapping."; };
        page.Controls.Add(mappings);
        page.Controls.Add(new Label { Dock = DockStyle.Top, AutoSize = true, Text = "Add in the blank row; select a row and press Delete to remove. Save commits edits; Revert discards them." });
    }
    private void BuildActions()
    {
        var page = Page("Actions");
        Column(actions, nameof(CustomAction.Name), "Custom action name");
        actions.Columns.Add(new DataGridViewComboBoxColumn { DataPropertyName = nameof(CustomAction.Key), HeaderText = "Key", DisplayMember = "Name", ValueMember = "Id",
            DataSource = ActionCatalog.Keys.Select(k => new ActionOption(k, k.Length == 2 && k[0] == 'D' && char.IsDigit(k[1]) ? k[1..] : k == "Back" ? "Backspace" : k)).ToList() });
        foreach (string modifier in new[] { "Control", "Alt", "Shift", "Windows" })
            actions.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = modifier, HeaderText = modifier, FillWeight = 45 });
        actions.DataError += (_, e) => { e.ThrowException = false; notice.Text = "Select a supported key."; };
        var bar = Bar(); bar.Controls.Add(testChoice);
        bar.Controls.Add(Button("Test in 3 seconds", () =>
        {
            RequireSaved();
            pendingTest = (testChoice.SelectedItem as ActionOption)?.Id ?? throw new InvalidOperationException("Choose an action to test.");
            notice.Text = "Test in 3 seconds: focus the test field or the target application.";
            testText.Focus(); testTimer.Stop(); testTimer.Start();
        }));
        page.Controls.Add(actions); page.Controls.Add(testText); page.Controls.Add(bar);
        page.Controls.Add(new Label { Dock = DockStyle.Top, AutoSize = true, Text = "Create named shortcuts below. Built-in and saved custom actions are available in the test selector.\r\nVoice typing uses Windows Win+H; focus a text field. GChat mute uses Ctrl+D in the active meeting." });
    }
    private void BuildProfiles()
    {
        var page = Page("Mapping Profiles");
        var bar = Bar();
        bar.Controls.Add(profileName); bar.Controls.Add(Button("Save snapshot", CreateProfile));
        bar.Controls.Add(Button("Import YAML", ImportProfile)); bar.Controls.Add(Button("Export YAML", ExportProfile));
        bar.Controls.Add(Button("Preview & Apply", PreviewProfile)); bar.Controls.Add(Button("Delete", DeleteProfile));
        page.Controls.Add(profiles); page.Controls.Add(bar);
        page.Controls.Add(new Label { Dock = DockStyle.Top, AutoSize = true, Text = "Profiles contain all mappings and custom actions, without connection settings. Windows profiles only.\r\nSave or revert edits before saving or applying a profile. Default cannot be overwritten or deleted." });
    }
    private void BuildHistory()
    {
        var page = Page("History");
        history.ReadOnly = true; history.AllowUserToAddRows = false; history.AllowUserToDeleteRows = false;
        foreach (string property in new[] { "Timestamp", "Action", "Source", "Trigger", "Target", "Result", "Failure" }) Column(history, property, property);
        history.Columns[0].DefaultCellStyle.Format = "g";
        history.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        history.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        page.Controls.Add(history); historyNotice.Dock = DockStyle.Top; page.Controls.Add(historyNotice);
    }
    private void BuildAbout()
    {
        var page = Page("About");
        var about = new AboutForm { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Dock = DockStyle.Fill };
        page.Controls.Add(about); about.Show();
    }

    private void RefreshStatus() => status.Text = connection.Status;
    private void RefreshHistory()
    {
        history.DataSource = pipeline.History.ToList();
        historyNotice.Text = pipeline.PersistenceError ?? "Latest 100 commands, saved across restarts. Dispatched means input was sent; application state is not verified.";
    }
    private void Reload()
    {
        loading = true;
        try
        {
            draft = SettingsStore.Clone(store.Current);
            host.Text = draft.Connection.TCPServerIP; clientId.Text = draft.Connection.ClientID;
            username.Text = draft.Connection.TCPServerUsername; password.Text = draft.Connection.TCPServerPassword;
            port.Value = Math.Clamp(draft.Connection.TCPServerPort, 1, 65535);
            timeout.Value = Math.Clamp(draft.Connection.CommunicationTimeoutInMinutes, 1, 1440);
            reconnect.Value = Math.Clamp(draft.Connection.AutoReconnectDelayInSeconds, 1, 3600);
            launch.Checked = draft.LaunchAtLogon; auto.Checked = draft.AutoConnect; minimized.Checked = draft.StartMinimized;
            actions.DataSource = new BindingList<CustomAction>(draft.Actions);
            RefreshActionChoices();
            mappings.DataSource = new BindingList<Config.ActionMapping>(draft.Connection.ActionMappings);
            profiles.DataSource = new[] { MappingProfile.Default }.Concat(store.Current.Profiles).ToList();
            CaptureEdits(); baseline = EditableJson(); notice.Text = "";
        }
        finally { loading = false; }
        actions.CellEndEdit -= ActionsEdited; actions.CellEndEdit += ActionsEdited;
    }
    private void ActionsEdited(object? sender, DataGridViewCellEventArgs e) => RefreshActionChoices();
    private void RefreshActionChoices()
    {
        var options = ActionCatalog.BuiltIns.Select(p => new ActionOption(p.Key, p.Value))
            .Concat(draft.Actions.Select(a => new ActionOption(a.Id, a.Name))).ToList();
        // Keep removed references visible until validation explains why they cannot be saved.
        foreach (var mapping in draft.Connection.ActionMappings)
            if (mapping.Action != null && options.All(a => a.Id != mapping.Action)) options.Add(new(mapping.Action, "Missing: " + mapping.Action));
        ((DataGridViewComboBoxColumn)mappings.Columns[1]).DataSource = options;
        testChoice.DataSource = ActionCatalog.BuiltIns.Select(p => new ActionOption(p.Key, p.Value))
            .Concat(store.Current.Actions.Select(a => new ActionOption(a.Id, a.Name))).ToList();
    }
    private void CaptureEdits()
    {
        Validate(); mappings.EndEdit(); actions.EndEdit();
        if (mappings.DataSource != null) BindingContext![mappings.DataSource]?.EndCurrentEdit();
        if (actions.DataSource != null) BindingContext![actions.DataSource]?.EndCurrentEdit();
        draft.Connection.TCPServerIP = host.Text.Trim(); draft.Connection.ClientID = clientId.Text.Trim();
        draft.Connection.TCPServerUsername = username.Text; draft.Connection.TCPServerPassword = password.Text;
        draft.Connection.TCPServerPort = (int)port.Value; draft.Connection.CommunicationTimeoutInMinutes = (int)timeout.Value;
        draft.Connection.AutoReconnectDelayInSeconds = (int)reconnect.Value;
        draft.LaunchAtLogon = launch.Checked; draft.AutoConnect = auto.Checked; draft.StartMinimized = minimized.Checked;
    }
    private string EditableJson() => JsonSerializer.Serialize(new { draft.Connection, draft.Actions, draft.LaunchAtLogon, draft.AutoConnect, draft.StartMinimized });
    private bool Dirty() { if (loading) return false; CaptureEdits(); return baseline != EditableJson(); }
    private void RequireSaved() { if (Dirty()) throw new InvalidOperationException("Save or revert your edits first."); }
    private void Save()
    {
        CaptureEdits();
        draft.Enabled = store.Current.Enabled; draft.Profiles = SettingsStore.Clone(store.Current.Profiles);
        SettingsStore.Validate(draft);
        bool startupChanged = draft.LaunchAtLogon != store.Current.LaunchAtLogon;
        using var key = startupChanged ? Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run") : null;
        const string name = "WinRemoteControl";
        object? previous = key?.GetValue(name);
        try
        {
            if (startupChanged)
            {
                if (draft.LaunchAtLogon) key!.SetValue(name, "\"" + Application.ExecutablePath + "\"");
                else key!.DeleteValue(name, false);
            }
            store.Save(draft);
        }
        catch
        {
            if (startupChanged) { if (previous == null) key!.DeleteValue(name, false); else key!.SetValue(name, previous); }
            throw;
        }
        Reload(); notice.Text = "Saved.";
    }
    private MappingProfile SelectedProfile() => profiles.SelectedItem as MappingProfile ?? throw new InvalidOperationException("Select a profile.");
    private void CreateProfile()
    {
        RequireSaved(); string name = profileName.Text.Trim();
        if (string.IsNullOrEmpty(name) || name.Equals("Default", StringComparison.OrdinalIgnoreCase) || store.Current.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Choose a unique profile name other than Default.");
        var next = SettingsStore.Clone(store.Current);
        next.Profiles.Add(new MappingProfile { Name = name, Mappings = SettingsStore.Clone(next.Connection.ActionMappings), Actions = SettingsStore.Clone(next.Actions) });
        store.Save(next); Reload(); profileName.Clear();
    }
    private void ImportProfile()
    {
        RequireSaved();
        using var dialog = new OpenFileDialog { Filter = "Windows mapping profile (*.yaml;*.yml)|*.yaml;*.yml" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (new FileInfo(dialog.FileName).Length > 1_000_000) throw new InvalidDataException("Profile exceeds 1 MB.");
        var profile = MappingProfile.Import(File.ReadAllText(dialog.FileName));
        var next = SettingsStore.Clone(store.Current);
        string original = profile.Name; int suffix = 2;
        while (next.Profiles.Any(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase))) profile.Name = original + " (" + suffix++ + ")";
        next.Profiles.Add(profile); store.Save(next); Reload();
        notice.Text = "Profile imported. Use Preview & Apply to activate it.";
    }
    private void ExportProfile()
    {
        var profile = SelectedProfile();
        using var dialog = new SaveFileDialog { Filter = "Windows mapping profile (*.yaml)|*.yaml", FileName = "mapping-profile.yaml", DefaultExt = "yaml" };
        if (dialog.ShowDialog(this) == DialogResult.OK) File.WriteAllText(dialog.FileName, MappingProfile.Export(profile));
    }
    private void DeleteProfile()
    {
        RequireSaved(); var profile = SelectedProfile();
        if (profile.Id == "builtin-default") throw new InvalidOperationException("Default cannot be deleted.");
        if (MessageBox.Show(this, $"Delete saved profile '{profile.Name}'? Active mappings will remain unchanged.", "Delete profile", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        var next = SettingsStore.Clone(store.Current); next.Profiles.RemoveAll(p => p.Id == profile.Id); store.Save(next); Reload();
    }
    private void PreviewProfile()
    {
        RequireSaved(); var profile = SelectedProfile();
        var changes = MappingProfile.Preview(profile, store.Current);
        using var preview = new Form { Text = "Apply " + profile.Name + "?", Size = new Size(760, 520), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false };
        var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both,
            Text = changes.Count == 0 ? "No mapping or action changes." : string.Join("\r\n\r\n", changes) };
        var bar = Bar();
        var apply = new Button { Text = "Replace mappings and actions", AutoSize = true, DialogResult = DialogResult.OK, Enabled = changes.Count > 0 };
        bar.Controls.Add(apply); bar.Controls.Add(new Button { Text = "Cancel", DialogResult = DialogResult.Cancel });
        preview.Controls.Add(text); preview.Controls.Add(bar);
        if (preview.ShowDialog(this) != DialogResult.OK) return;
        store.ApplyProfile(profile); Reload(); notice.Text = "Applied " + profile.Name + ".";
    }
    private sealed record ActionOption(string Id, string Name);
}
