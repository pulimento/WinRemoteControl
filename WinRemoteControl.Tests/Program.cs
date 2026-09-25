using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MQTTnet;
using MQTTnet.Server;
using Microsoft.Win32;
using WinRemoteControl;
using WinRemoteControl.Settings;

internal static class Program
{
    private static int assertions;
    private static string root = "";
    [STAThread]
    private static int Main(string[] args)
    {
        root = Path.Combine(Path.GetTempPath(), "WinRemoteControlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            SettingsAndProfiles(); Commands();
            if (args.Contains("--ui")) Ui();
            if (args.Contains("--mqtt")) Mqtt();
            Console.WriteLine($"PASS: {assertions} assertions. Test artifacts: {root}");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static void Check(bool condition, string message)
    { assertions++; if (!condition) throw new Exception("FAILED: " + message); }
    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch { rejected = true; }
        Check(rejected, message);
    }
    private static SettingsStore Store(string name) => new(Path.Combine(root, name), Path.Combine(root, "absent"));
    private static void SettingsAndProfiles()
    {
        var store = Store("settings");
        Check(store.Current.Connection.ActionMappings.Count == 6, "six default mappings");
        Check(store.Current.Connection.ActionMappings[0].Topic == "control/press_enter", "Mac-style Enter topic");
        Check(store.Current.Connection.ActionMappings.Select(m => m.Topic).SequenceEqual(new[] { "control/press_enter", "control/start_dictation", "control/press_1", "control/press_2", "control/press_3", "control/mute_gchat" }), "exact Mac default topic order");
        Check(store.Current.Actions.Count == 4, "four editable default actions as on Mac");
        Check(store.Current.Actions.Take(3).Select(a => a.Key).SequenceEqual(new[] { "D1", "D2", "D3" }), "default digit shortcuts");
        Check(store.Current.Actions.Single(a => a.Id == MappingProfile.GChatId) is { Key: "D", Control: true, Windows: false }, "GChat shortcut adapted to Windows");
        Check(store.Current.Connection.ActionMappings.Skip(2).All(m => store.Current.Actions.Any(a => a.Id == m.Action)), "default shortcuts are editable custom actions");
        var draft = SettingsStore.Clone(store.Current);
        draft.Connection.TCPServerPassword = "SECRET-PASSWORD";
        draft.Connection.TCPServerIP = "SECRET-HOST";
        draft.Connection.ActionMappings = [];
        draft.Actions.Clear();
        draft.Actions.Add(new CustomAction { Id = "test", Name = "Test shortcut", Key = "F20", Control = true, Windows = true });
        store.Save(draft);
        draft.Actions.Clear();
        Check(store.Current.Actions.Count == 1, "save clones the draft");
        var loaded = Store("settings");
        Check(loaded.Current.Connection.ActionMappings.Count == 0, "empty mappings stay empty after restart");
        Check(loaded.Current.Actions.Count == 1, "custom actions persist");
        var profile = new MappingProfile { Name = "Work", Actions = SettingsStore.Clone(loaded.Current.Actions), Mappings = [new() { Topic = "custom/key", Action = "test" }] };
        string yaml = MappingProfile.Export(profile);
        Check(!yaml.Contains("SECRET"), "YAML excludes credentials/connection");
        var imported = MappingProfile.Import(yaml);
        Check(imported.Actions.Single().Windows && imported.Mappings.Single().Action == "test", "YAML roundtrip");
        Check(imported.Id != profile.Id, "import gets independent identity");
        Reject(() => MappingProfile.Import(yaml.Replace("platform: Windows", "platform: macOS")), "reject Mac profiles");
        Reject(() => MappingProfile.Import(yaml.Replace("schemaVersion: 1", "schemaVersion: 2")), "reject unsupported schema");
        Reject(() => MappingProfile.Import(yaml.Replace("schemaVersion: 1", "")), "require explicit schema");
        Reject(() => MappingProfile.Import(yaml + "\nname: duplicate\n"), "reject duplicate YAML keys");
        Reject(() => MappingProfile.Import(yaml + "\npassword: secret\n"), "reject unknown fields");
        var builtIn = MappingProfile.Import(MappingProfile.Export(MappingProfile.Default));
        Check(builtIn.Id != "builtin-default" && builtIn.Name != "Default", "import cannot overwrite Default");
        var preview = MappingProfile.Preview(profile, loaded.Current);
        Check(preview.Any(p => p.StartsWith("Added Mapping")), "preview added mapping");
        var changed = SettingsStore.Clone(profile);
        changed.Mappings[0].Topic = "other/topic";
        changed.Actions[0].Key = "F1";
        var before = SettingsStore.Clone(loaded.Current); before.Connection.ActionMappings = profile.Mappings;
        preview = MappingProfile.Preview(changed, before);
        Check(preview.Any(p => p.StartsWith("Removed Mapping")) && preview.Any(p => p.StartsWith("Changed Action")), "preview removal and action change");
        var invalid = SettingsStore.Clone(profile); invalid.Mappings[0].Action = "unknown";
        Reject(invalid.Validate, "reject missing actions");
        invalid = SettingsStore.Clone(profile); invalid.Mappings.Add(SettingsStore.Clone(invalid.Mappings[0]));
        Reject(invalid.Validate, "reject duplicate topics");
        invalid = SettingsStore.Clone(profile); invalid.Mappings[0].Topic = "wild/+";
        Reject(invalid.Validate, "reject wildcard topics");
        invalid = SettingsStore.Clone(profile); invalid.Actions[0].Key = "Invalid";
        Reject(invalid.Validate, "reject unsupported key");
        var invalidSettings = SettingsStore.Clone(loaded.Current); invalidSettings.Profiles.Add(MappingProfile.Default);
        Reject(() => loaded.Save(invalidSettings), "protect built-in profile");
        Check(loaded.Current.Profiles.Count == 0, "invalid save leaves current settings intact");
        loaded.ApplyProfile(profile);
        Check(loaded.Current.Connection.TCPServerPassword == "SECRET-PASSWORD" && loaded.Current.Connection.TCPServerIP == "SECRET-HOST", "applying profile preserves connection");
        Check(loaded.Current.Connection.ActionMappings.Single().Topic == "custom/key" && loaded.Current.Actions.Count == 1, "applying replaces mappings and actions");
        loaded.ApplyProfile(MappingProfile.Default);
        Check(loaded.Current.Connection.ActionMappings.Count == 6 && loaded.Current.Actions.Count == 4, "Default replaces complete active snapshot");
        loaded.ApplyProfile(profile);
        loaded.ApplyDefaultWithRecoveryProfile();
        Check(loaded.Current.Profiles.Single().Mappings.Single().Topic == "custom/key", "default replacement saves recovery mappings");
        Check(loaded.Current.Profiles.Single().Actions.Single().Id == "test", "default replacement saves recovery actions");
        Check(loaded.Current.Connection.TCPServerPassword == "SECRET-PASSWORD", "recovery replacement preserves connection");
        Check(File.Exists(Path.Combine(root, "settings", "Settings.json.bak")), "atomic save keeps backup");
        string legacyDir = Path.Combine(root, "legacy"); Directory.CreateDirectory(legacyDir);
        var legacy = new Config.SettingsFromFile { ClientID = "original", TCPServerIP = "broker", TCPServerPort = 1883,
            ActionMappings = [new() { Topic = "my/old/topic", Action = "toggle_teams_mute" }] };
        string legacyJson = JsonSerializer.Serialize(legacy);
        File.WriteAllText(Path.Combine(legacyDir, "settings.json"), legacyJson);
        var migrated = new SettingsStore(Path.Combine(root, "migrated"), legacyDir);
        Check(migrated.Current.Connection.ActionMappings.Single().Topic == "my/old/topic", "preserve legacy mapping exactly");
        Check(migrated.Current.Connection.ClientID == "original", "preserve legacy client ID");
        Check(File.ReadAllText(Path.Combine(legacyDir, "settings.json")) == legacyJson, "legacy source unchanged");
        File.WriteAllText(Path.Combine(legacyDir, "settings.json"), "invalid");
        Check(new SettingsStore(Path.Combine(root, "migrated"), legacyDir).Current.Connection.ClientID == "original", "migration only once");
        File.WriteAllText(Path.Combine(legacyDir, "settings.json"), "{\"ClientID\":\"oldest\",\"TCPServerIP\":\"broker\",\"TCPServerPort\":1883}");
        var oldest = new SettingsStore(Path.Combine(root, "oldest"), legacyDir);
        Check(oldest.Current.Connection.ActionMappings.Count == 8 && oldest.Current.Connection.ActionMappings[0].Action == "toggle_teams_mute", "legacy absent mappings preserve original defaults");
        string malformed = Path.Combine(root, "broken"); Directory.CreateDirectory(malformed);
        File.WriteAllText(Path.Combine(malformed, "Settings.json"), "invalid");
        Reject(() => new SettingsStore(malformed, legacyDir), "corrupt settings fail without resetting");
        Check(File.ReadAllText(Path.Combine(malformed, "Settings.json")) == "invalid", "corrupt source preserved");
    }
    private static void Commands()
    {
        var store = Store("commands"); int dispatched = 0;
        var now = DateTimeOffset.UtcNow;
        string directory = Path.Combine(root, "commands");
        var pipeline = new CommandPipeline(store, directory, () => now, (_, _) => dispatched++, () => "test-app");
        const string topic = "control/press_enter";
        byte[] Envelope(string id, DateTimeOffset timestamp) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { winRemoteCommand = 1, commandID = id, timestamp = timestamp.ToString("O") }));
        Check(pipeline.Process(topic, Encoding.UTF8.GetBytes("ordinary button"), false).Result == "Dispatched", "ordinary button payload");
        Check(dispatched == 1, "ordinary button executes exactly once");
        Check(pipeline.Process(topic, null, true).Failure!.Contains("Retained"), "reject retained");
        Check(dispatched == 1, "retained never executes");
        var next = SettingsStore.Clone(store.Current); next.Enabled = false; store.Save(next);
        Check(pipeline.Process(topic, null, false).Failure!.Contains("disabled"), "global disable");
        Check(pipeline.Process("", null, false, "press_enter").Result == "Failed", "tests also respect disable");
        next.Enabled = true; store.Save(next);
        Check(pipeline.Process(topic, Envelope("old", now.AddSeconds(-31)), false).Failure!.Contains("expired"), "expiry");
        Check(pipeline.Process(topic, Envelope("future", now.AddSeconds(6)), false).Failure!.Contains("future"), "future timestamp");
        Check(pipeline.Process(topic, new byte[4097], false).Failure!.Contains("4096"), "payload size bound");
        Check(pipeline.Process(topic, Encoding.UTF8.GetBytes("{\"winRemoteCommand\":1}"), false).Failure!.Contains("metadata"), "invalid envelope");
        Check(pipeline.Process(topic, Encoding.UTF8.GetBytes("{\"winRemoteCommand\":1,\"commandID\":\"timezone\",\"timestamp\":\"2026-09-25T12:00:00\"}"), false).Failure!.Contains("metadata"), "require explicit timestamp timezone");
        Check(pipeline.Process(topic, Envelope(new string('x', 129), now), false).Failure!.Contains("metadata"), "command ID size bound");
        Check(pipeline.Process(topic, Envelope("id-SECRET", now), false).Result == "Dispatched", "valid envelope");
        Check(pipeline.Process(topic, Envelope("id-SECRET", now), false).Failure!.Contains("Duplicate"), "duplicate suppression");
        Check(pipeline.Process("control/press_1", Envelope("id-SECRET", now), false).Result == "Dispatched", "dedup scoped per trigger");
        Check(pipeline.Process(topic, null, false, receivedAt: now.AddSeconds(-31)).Failure!.Contains("expired"), "queued ordinary commands expire");
        Check(pipeline.Process("unmapped", null, false).Result == "Failed", "unmapped rejected");
        now = now.AddMinutes(11);
        Check(pipeline.Process(topic, Envelope("id-SECRET", now), false).Result == "Dispatched", "dedup expires");
        for (int i = 0; i < 105; i++) pipeline.Process(topic, null, false);
        Check(pipeline.History.Count == 100, "history bounded at 100");
        Check(new CommandPipeline(store, directory).History.Count == 100, "history survives restart");
        string history = File.ReadAllText(Path.Combine(directory, "History.json"));
        Check(!history.Contains("id-SECRET") && !history.Contains("ordinary button"), "no IDs or payloads in history");
        var failures = new CommandPipeline(store, Path.Combine(root, "failures"), () => now, (_, _) => throw new InvalidOperationException("Cannot dispatch"), () => "test-app");
        Check(failures.Process(topic, null, false).Failure == "Cannot dispatch", "execution failure recorded");
    }
    private static void Ui()
    {
        Application.EnableVisualStyles();
        var store = Store("ui");
        var pipeline = new CommandPipeline(store, Path.Combine(root, "ui"), execute: (_, _) => { }, target: () => "test-app");
        var connection = new MqttConnection(store, pipeline, action => action());
        using var form = new UnifiedSettingsForm(store, pipeline, connection);
        form.Location = new Point(-20000, -20000); form.StartPosition = FormStartPosition.Manual; form.ShowInTaskbar = false;
        form.Show(); Application.DoEvents();
        var tabs = form.Controls.OfType<TabControl>().Single();
        Check(tabs.TabCount == 6, "six settings tabs including About");
        Check(form.Text == AppBranding.DisplayName + " Settings", "settings build branding");
        using (var appIcon = AppBranding.CreateIcon()) Check(appIcon.Width > 0, "build icon resource loads");
#if DEBUG
        Check(AppBranding.DisplayName == "WinRemoteControl DEBUG", "Debug display name");
        Check(typeof(AppBranding).Assembly.GetName().Name == "WinRemoteControl DEBUG", "Debug executable identity");
#else
        Check(AppBranding.DisplayName == "WinRemoteControl", "Release display name preserved");
        Check(typeof(AppBranding).Assembly.GetName().Name == "WinRemoteControl", "Release executable identity preserved");
#endif
        Check(SettingsStore.DataDirectory.EndsWith("WinRemoteControl") && !SettingsStore.DataDirectory.Contains("DEBUG"), "shared configuration independent of branding");
        var grid = tabs.TabPages.Cast<TabPage>().Single(p => p.Text == "Mappings").Controls.OfType<DataGridView>().Single();
        tabs.SelectedIndex = 1;
        grid.Rows[0].Cells[0].Value = "edited/topic";
        tabs.SelectedIndex = 2; tabs.SelectedIndex = 1;
        Check(grid.Rows[0].Cells[0].Value?.ToString() == "edited/topic", "tab switch preserves mapping draft");
        Check(store.Current.Connection.ActionMappings[0].Topic == "control/press_enter", "draft does not mutate active mappings");
        form.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);
        Check(store.Current.Connection.ActionMappings[0].Topic == "edited/topic", "Save commits mapping draft");
        grid.Rows[0].Cells[0].Value = "discard/topic";
        form.GetType().GetMethod("Reload", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);
        Check(grid.Rows[0].Cells[0].Value?.ToString() == "edited/topic", "Revert restores saved mapping");
        var actionGrid = tabs.TabPages.Cast<TabPage>().Single(p => p.Text == "Actions").Controls.OfType<DataGridView>().Single();
        tabs.SelectedIndex = 2;
        actionGrid.CurrentCell = actionGrid.Rows[actionGrid.NewRowIndex].Cells[0]; actionGrid.BeginEdit(true);
        ((TextBox)actionGrid.EditingControl!).Text = "Shortcut test";
        actionGrid.EndEdit();
        tabs.SelectedIndex = 1;
        form.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);
        Check(store.Current.Actions.Count == 5 && store.Current.Actions.Last().Name == "Shortcut test", "create custom action using grid blank row");
        grid.Rows[0].Cells[1].Value = store.Current.Actions.Last().Id;
        form.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);
        Check(store.Current.Connection.ActionMappings[0].Action == store.Current.Actions.Last().Id, "map newly created custom action");
        pipeline.Process("edited/topic", null, false);
        pipeline.Process("control/press_1", null, true);
        foreach (TabPage page in tabs.TabPages)
        {
            tabs.SelectedTab = page; Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(root, page.Text + ".png"));
        }
        form.Close(); connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static void Mqtt()
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); int port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        var options = new MqttServerOptionsBuilder().WithDefaultEndpointPort(port).Build();
        options.DefaultEndpointOptions.BoundInterNetworkAddress = IPAddress.Loopback;
        options.DefaultEndpointOptions.BoundInterNetworkV6Address = IPAddress.IPv6Loopback;
        using var broker = new MqttFactory().CreateMqttServer();
        int subscriptions = 0;
        broker.ClientSubscribedTopicHandler = new MqttServerClientSubscribedTopicHandlerDelegate(_ => { Interlocked.Increment(ref subscriptions); });
        broker.StartAsync(options).GetAwaiter().GetResult();
        var store = Store("mqtt");
        var settings = SettingsStore.Clone(store.Current);
        settings.Connection.TCPServerIP = "127.0.0.1"; settings.Connection.TCPServerPort = port;
        settings.Connection.AutoReconnectDelayInSeconds = 1; store.Save(settings);
        var queue = new ConcurrentQueue<Action>(); int dispatched = 0;
        var pipeline = new CommandPipeline(store, Path.Combine(root, "mqtt"), execute: (_, _) => dispatched++, target: () => "test-app");
        var connection = new MqttConnection(store, pipeline, queue.Enqueue);
        void Drain() { while (queue.TryDequeue(out var action)) action(); }
        void Until(Func<bool> done, string description, bool drain = true)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!done() && DateTime.UtcNow < deadline) { if (drain) Drain(); Thread.Sleep(10); }
            Check(done(), description);
        }
        void Publish(string topic, string payload = "button", bool retained = false) => broker.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic).WithPayload(payload).WithRetainFlag(retained).Build()).GetAwaiter().GetResult();
        void Power(PowerModes mode) => typeof(MqttConnection).GetMethod("PowerChanged", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(connection, [connection, new PowerModeChangedEventArgs(mode)]);
        try
        {
            Publish("control/press_enter", "old retained", true);
            connection.StartAsync().GetAwaiter().GetResult();
            Until(() => subscriptions >= 6, "MQTT subscribes to mappings");
            Until(() => pipeline.History.Any(h => h.Failure?.Contains("Retained") == true), "broker retained replay rejected");
            Check(dispatched == 0, "retained replay never dispatches");
            Publish("control/press_1"); Until(() => dispatched == 1, "live MQTT command dispatches");
            string envelope = JsonSerializer.Serialize(new { winRemoteCommand = 1, commandID = "reconnect-id", timestamp = DateTimeOffset.UtcNow.ToString("O") });
            Publish("control/press_2", envelope); Until(() => dispatched == 2, "live envelope dispatches");
            int before = subscriptions;
            connection.StartAsync().GetAwaiter().GetResult();
            Until(() => subscriptions >= before + 6, "serialized reconnect restores subscriptions");
            Publish("control/press_2", envelope);
            Until(() => pipeline.History.Any(h => h.Failure?.Contains("Duplicate") == true), "dedup survives reconnect");
            Check(dispatched == 2, "reconnect duplicate never dispatches");
            Drain(); Publish("control/press_1"); Until(() => !queue.IsEmpty, "message queued for UI", false);
            connection.StopAsync().GetAwaiter().GetResult(); Drain();
            Check(dispatched == 2, "Stop invalidates already-queued messages");
            before = subscriptions;
            typeof(MqttConnection).GetMethod("NetworkChanged", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(connection, [null, EventArgs.Empty]);
            Drain(); Check(connection.Status == "Stopped" && subscriptions == before, "network change respects stopped intent");
            connection.StartAsync().GetAwaiter().GetResult(); Until(() => subscriptions >= before + 6, "manual restart connects");
            Power(PowerModes.Suspend); Until(() => connection.Status == "Suspended", "sleep releases connection");
            before = subscriptions; Power(PowerModes.Resume); Until(() => subscriptions >= before + 6, "wake reconnects when desired");
            settings = SettingsStore.Clone(store.Current); settings.Enabled = false; store.Save(settings);
            int historyCount = pipeline.History.Count; Publish("control/press_1");
            Until(() => pipeline.History.Count > historyCount, "disabled listener remains connected");
            Check(pipeline.History[0].Failure?.Contains("disabled") == true && dispatched == 2, "disabled command rejected over MQTT");
            Power(PowerModes.Suspend); Until(() => connection.Status == "Suspended", "second suspend");
            connection.StopAsync().GetAwaiter().GetResult(); Drain(); before = subscriptions;
            Power(PowerModes.Resume); Drain();
            Check(connection.Status == "Stopped" && subscriptions == before, "wake respects Stop during sleep");
            Task.WhenAll(connection.StartAsync(), connection.StopAsync(), connection.StartAsync(), connection.StopAsync()).GetAwaiter().GetResult();
            Drain(); Check(connection.Status == "Stopped", "rapid start/stop leaves final requested state");
        }
        finally
        {
            connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
            broker.StopAsync().GetAwaiter().GetResult();
        }
    }
}
