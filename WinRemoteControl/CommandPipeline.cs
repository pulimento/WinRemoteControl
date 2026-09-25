using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using WinRemoteControl.Actions;
using WinRemoteControl.Settings;

namespace WinRemoteControl;

public sealed record CommandRecord(DateTimeOffset Timestamp, string Action, string Source, string Trigger, string Target, string Result, string? Failure);

public sealed class CommandPipeline
{
    private readonly SettingsStore settings;
    private readonly string historyPath;
    private readonly Func<DateTimeOffset> now;
    private readonly Action<string, RemoteSettings> execute;
    private readonly Func<string> target;
    private readonly Dictionary<string, DateTimeOffset> seen = new(StringComparer.Ordinal);
    public IReadOnlyList<CommandRecord> History => history.AsReadOnly();
    private List<CommandRecord> history = [];
    public event Action? HistoryChanged;
    public string? PersistenceError { get; private set; }

    public CommandPipeline(SettingsStore settings, string? directory = null, Func<DateTimeOffset>? now = null,
        Action<string, RemoteSettings>? execute = null, Func<string>? target = null)
    {
        this.settings = settings;
        this.now = now ?? (() => DateTimeOffset.UtcNow);
        this.execute = execute ?? ActionCatalog.Execute;
        this.target = target ?? ForegroundApplication;
        historyPath = Path.Combine(directory ?? SettingsStore.DataDirectory, "History.json");
        if (File.Exists(historyPath))
        {
            try { history = (JsonSerializer.Deserialize<List<CommandRecord>>(File.ReadAllText(historyPath)) ?? []).Take(100).ToList(); }
            catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
            { PersistenceError = "History could not be loaded; the original file has been preserved."; }
        }
    }

    // Call on the UI thread. Payloads and command IDs never enter history or logs.
    public CommandRecord Process(string topic, byte[]? payload, bool retained, string? testAction = null, DateTimeOffset? receivedAt = null)
    {
        var instant = now();
        foreach (var key in seen.Where(p => instant - p.Value >= TimeSpan.FromMinutes(10)).Select(p => p.Key).ToArray()) seen.Remove(key);
        string source = testAction == null ? "MQTT" : "Test";
        string? id = testAction ?? settings.Current.Connection.ActionMappings.FirstOrDefault(m => m.Topic == topic)?.Action;
        string? failure = retained ? "Retained MQTT commands are ignored." : !settings.Current.Enabled ? "Remote control is disabled." : null;
        string? commandId = null;
        var timestamp = receivedAt ?? instant;
        if (payload?.Length > 4096) failure ??= "Command payload exceeds 4096 bytes.";
        else if (payload is { Length: > 0 })
        {
            try
            {
                using var json = JsonDocument.Parse(payload);
                if (json.RootElement.ValueKind == JsonValueKind.Object &&
                    (json.RootElement.TryGetProperty("winRemoteCommand", out var version) || json.RootElement.TryGetProperty("macRemoteCommand", out version)))
                {
                    if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int v) || v != 1 ||
                        !json.RootElement.TryGetProperty("commandID", out var command) || command.ValueKind != JsonValueKind.String ||
                        string.IsNullOrEmpty(commandId = command.GetString()) || Encoding.UTF8.GetByteCount(commandId) > 128 ||
                        !json.RootElement.TryGetProperty("timestamp", out var time) || time.ValueKind != JsonValueKind.String ||
                        !DateTimeOffset.TryParseExact(time.GetString(),
                            ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"],
                            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp))
                        failure ??= "Invalid command metadata.";
                }
            }
            catch (JsonException) { /* Ordinary, non-envelope button payloads are valid. */ }
        }
        if (instant - timestamp > TimeSpan.FromSeconds(30)) failure ??= "Command expired (maximum age: 30 seconds).";
        if (timestamp - instant > TimeSpan.FromSeconds(5)) failure ??= "Command timestamp is in the future.";
        string? duplicateKey = commandId == null ? null : JsonSerializer.Serialize(new[] { source, topic, commandId });
        if (duplicateKey != null && seen.ContainsKey(duplicateKey)) failure ??= "Duplicate command ignored.";
        if (id == null) failure ??= "No current mapping matches this command.";
        string application = "";
        if (failure == null)
        {
            if (duplicateKey != null)
            {
                if (seen.Count >= 512) seen.Remove(seen.MinBy(p => p.Value).Key);
                seen[duplicateKey] = instant;
            }
            try { application = target(); execute(id!, settings.Current); }
            catch (Exception e) { failure = e.Message; }
        }
        var record = new CommandRecord(instant, id == null ? "Unknown action" : ActionCatalog.Name(id, settings.Current), source,
            topic, application, failure == null ? "Dispatched" : "Failed", failure);
        history.Insert(0, record);
        if (history.Count > 100) history.RemoveRange(100, history.Count - 100);
        try
        {
            SettingsStore.AtomicWrite(historyPath, JsonSerializer.Serialize(history, SettingsStore.JsonOptions));
            PersistenceError = null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { PersistenceError = "History could not be saved. Check access to the configuration directory."; }
        HistoryChanged?.Invoke();
        return record;
    }

    private static string ForegroundApplication()
    {
        try { GetWindowThreadProcessId(GetForegroundWindow(), out uint id); using var process = System.Diagnostics.Process.GetProcessById((int)id); return process.ProcessName; }
        catch { return "Unknown"; }
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
