using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinRemoteControl.Settings;

public sealed class RemoteSettings
{
    public Config.SettingsFromFile Connection { get; set; } = new()
    {
        ClientID = "WinRemoteControl", TCPServerIP = "", TCPServerPort = 1883,
        CommunicationTimeoutInMinutes = 1, AutoReconnectDelayInSeconds = 5
    };
    public bool Enabled { get; set; } = true;
    public bool LaunchAtLogon { get; set; }
    public bool AutoConnect { get; set; }
    public bool StartMinimized { get; set; }
    public List<CustomAction> Actions { get; set; } = MappingProfile.CreateDefaultActions();
    public List<MappingProfile> Profiles { get; set; } = [];
}

public sealed class CustomAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Key { get; set; } = "Return";
    public bool Control { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Windows { get; set; }
}

public sealed class MappingProfile
{
    public const string Press1Id = "C1EF1E47-41F4-4978-B84E-B11CA52E74AC";
    public const string Press2Id = "3372509D-7542-4BBC-A51B-562CFFB6466A";
    public const string Press3Id = "9E01120E-157A-4D23-B23A-151F23162977";
    public const string GChatId = "61D922DD-ABBB-482B-AAFC-3BDAA4426825";
    public static List<CustomAction> CreateDefaultActions() =>
    [
        new() { Id = Press1Id, Name = "Press 1", Key = "D1" },
        new() { Id = Press2Id, Name = "Press 2", Key = "D2" },
        new() { Id = Press3Id, Name = "Press 3", Key = "D3" },
        new() { Id = GChatId, Name = "Mute GChat (Ctrl+D)", Key = "D", Control = true }
    ];
    public int SchemaVersion { get; set; } = 1;
    public string Platform { get; set; } = "Windows";
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public List<Config.ActionMapping> Mappings { get; set; } = [];
    public List<CustomAction> Actions { get; set; } = [];
    public override string ToString() => Name;

    public static MappingProfile Default => new()
    {
        Id = "builtin-default", Name = "Default", Mappings = Config.SettingsFromFile.CreateDefaultActionMappings(), Actions = CreateDefaultActions()
    };

    public void Validate()
    {
        if (SchemaVersion != 1 || Platform != "Windows")
            throw new InvalidDataException("Only Windows mapping profiles with schemaVersion: 1 are supported.");
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Id))
            throw new InvalidDataException("Profile name and ID are required.");
        SettingsStore.ValidateMappings(Mappings, Actions);
    }

    public static string Export(MappingProfile profile)
    {
        profile.Validate();
        return new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build().Serialize(profile);
    }

    public static MappingProfile Import(string yaml)
    {
        if (yaml.Length > 1_000_000) throw new InvalidDataException("Profile is too large (maximum 1 MB).");
        var profile = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDuplicateKeyChecking().Build().Deserialize<MappingProfile>(yaml)
            ?? throw new InvalidDataException("The profile is empty.");
        // Require an explicit version/platform; constructor defaults must not accept unversioned files.
        var document = new YamlDotNet.RepresentationModel.YamlStream();
        document.Load(new StringReader(yaml));
        if (document.Documents.Count != 1 || document.Documents[0].RootNode is not YamlDotNet.RepresentationModel.YamlMappingNode root ||
            !root.Children.ContainsKey(new YamlDotNet.RepresentationModel.YamlScalarNode("schemaVersion")) ||
            !root.Children.ContainsKey(new YamlDotNet.RepresentationModel.YamlScalarNode("platform")))
            throw new InvalidDataException("A Windows platform and schemaVersion are required.");
        profile.Validate();
        // An imported snapshot can never replace the protected built-in profile.
        profile.Id = Guid.NewGuid().ToString();
        if (profile.Name == "Default") profile.Name = "Imported Default";
        return profile;
    }

    public static IReadOnlyList<string> Preview(MappingProfile profile, RemoteSettings settings)
    {
        profile.Validate();
        var changes = new List<string>();
        string ActionName(string id) => WinRemoteControl.Actions.ActionCatalog.BuiltIns.GetValueOrDefault(id)
            ?? profile.Actions.FirstOrDefault(a => a.Id == id)?.Name ?? settings.Actions.FirstOrDefault(a => a.Id == id)?.Name ?? id;
        Compare(settings.Connection.ActionMappings, profile.Mappings, m => m.Topic!, m => m.Action!, "Mapping", changes, valueLabel: ActionName);
        string Shortcut(CustomAction action) => action.Name + " — " + string.Join("+", new[]
        {
            action.Control ? "Ctrl" : null, action.Alt ? "Alt" : null, action.Shift ? "Shift" : null,
            action.Windows ? "Win" : null, action.Key
        }.Where(s => s != null));
        Compare(settings.Actions, profile.Actions, a => a.Id, Shortcut, "Action", changes, ActionName);
        return changes;
    }

    private static void Compare<T>(IEnumerable<T> oldItems, IEnumerable<T> newItems, Func<T, string> key,
        Func<T, string> value, string kind, List<string> changes, Func<string, string>? keyLabel = null, Func<string, string>? valueLabel = null)
    {
        var before = oldItems.ToDictionary(key, value);
        var after = newItems.ToDictionary(key, value);
        foreach (var id in before.Keys.Union(after.Keys))
        {
            string label = keyLabel?.Invoke(id) ?? id;
            string Display(string text) => valueLabel?.Invoke(text) ?? text;
            if (!before.ContainsKey(id)) changes.Add($"Added {kind}: {label} → {Display(after[id])}");
            else if (!after.ContainsKey(id)) changes.Add($"Removed {kind}: {label} → {Display(before[id])}");
            else if (before[id] != after[id]) changes.Add($"Changed {kind}: {label}\r\n  {Display(before[id])} → {Display(after[id])}");
        }
    }
}

public sealed class SettingsStore
{
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinRemoteControl");
    private readonly string directory;
    public RemoteSettings Current { get; private set; }
    public event Action? Changed;
    public static JsonSerializerOptions JsonOptions { get; } = new() { WriteIndented = true };
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    public SettingsStore(string? directory = null, string? legacyDirectory = null)
    {
        this.directory = directory ?? DataDirectory;
        Directory.CreateDirectory(this.directory);
        string file = Path.Combine(this.directory, "Settings.json");
        if (File.Exists(file))
            Current = JsonSerializer.Deserialize<RemoteSettings>(File.ReadAllText(file))
                ?? throw new InvalidDataException("Settings.json is empty. Restore its backup or correct the file.");
        else
        {
            Current = new RemoteSettings();
            string legacy = Path.Combine(legacyDirectory ?? AppContext.BaseDirectory, "settings.json");
            if (File.Exists(legacy))
            {
                Current.Actions = [];
                string legacyJson = File.ReadAllText(legacy);
                Current.Connection = JsonSerializer.Deserialize<Config.SettingsFromFile>(legacyJson)
                    ?? throw new InvalidDataException("Legacy settings are empty.");
                using var document = JsonDocument.Parse(legacyJson);
                if (!document.RootElement.TryGetProperty("ActionMappings", out var mappings) || mappings.ValueKind == JsonValueKind.Null)
                {
                    // Older releases had these eight hard-coded actions. Preserve that behavior.
                    Current.Connection.ActionMappings = new[] { "toggle_teams_mute", "volume_up", "volume_down", "media_next_song", "media_prev_song", "press_1", "press_2", "press_3" }
                        .Select(id => new Config.ActionMapping { Topic = "control/" + id, Action = id }).ToList();
                }
                string backup = Path.Combine(this.directory, "legacy-settings.json");
                if (!File.Exists(backup)) File.Copy(legacy, backup, false);
            }
            if (directory == null)
            {
                Current.LaunchAtLogon = AppUserSettings.Default.LAUNCH_AT_LOGON;
                Current.AutoConnect = AppUserSettings.Default.AUTO_CONNECT_AT_STARTUP;
                Current.StartMinimized = AppUserSettings.Default.START_MINIMIZED;
            }
            Save(Current);
        }
        Validate(Current);
    }

    public void Save(RemoteSettings settings)
    {
        Validate(settings);
        var snapshot = Clone(settings);
        AtomicWrite(Path.Combine(directory, "Settings.json"), JsonSerializer.Serialize(snapshot, JsonOptions));
        Current = snapshot;
        Changed?.Invoke();
    }

    public void ApplyProfile(MappingProfile profile)
    {
        profile.Validate();
        var next = Clone(Current);
        next.Connection.ActionMappings = Clone(profile.Mappings);
        next.Actions = Clone(profile.Actions);
        Save(next);
    }

    public void ApplyDefaultWithRecoveryProfile()
    {
        var next = Clone(Current);
        next.Profiles.Add(new MappingProfile
        {
            Name = "Before Mac defaults " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"),
            Mappings = Clone(next.Connection.ActionMappings), Actions = Clone(next.Actions)
        });
        var defaults = MappingProfile.Default;
        next.Connection.ActionMappings = defaults.Mappings;
        next.Actions = defaults.Actions;
        Save(next);
    }

    public static void AtomicWrite(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, contents);
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
        else File.Move(temporary, path);
    }

    public static void Validate(RemoteSettings settings)
    {
        if (settings.Connection == null || settings.Profiles == null) throw new InvalidDataException("Missing settings sections.");
        ValidateMappings(settings.Connection.ActionMappings, settings.Actions);
        foreach (var profile in settings.Profiles)
        {
            profile.Validate();
            if (profile.Id == "builtin-default" || profile.Name == "Default") throw new InvalidDataException("Default is a protected profile.");
        }
        if (settings.Profiles.Select(p => p.Id).Distinct().Count() != settings.Profiles.Count)
            throw new InvalidDataException("Profile IDs must be unique.");
    }

    public static void ValidateMappings(List<Config.ActionMapping> mappings, List<CustomAction> actions)
    {
        if (mappings == null || actions == null) throw new InvalidDataException("Mappings and actions are required.");
        var ids = new HashSet<string>(Actions.ActionCatalog.BuiltIns.Keys);
        foreach (var action in actions)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Id) || string.IsNullOrWhiteSpace(action.Name) ||
                !ids.Add(action.Id) || !Actions.ActionCatalog.Keys.Contains(action.Key))
                throw new InvalidDataException("Custom actions need a unique ID, a name, and a supported key.");
        }
        var topics = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.Topic) || mapping.Topic.IndexOfAny(['#', '+', '\0']) >= 0 ||
                Encoding.UTF8.GetByteCount(mapping.Topic) > 65535 || !topics.Add(mapping.Topic) ||
                mapping.Action == null || !ids.Contains(mapping.Action))
                throw new InvalidDataException("Mappings need unique, exact MQTT topics and existing actions (no wildcards).");
        }
    }
}
