using System.Net.NetworkInformation;
using Microsoft.Win32;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using WinRemoteControl.Settings;

namespace WinRemoteControl;

// Epochs invalidate callbacks, including messages queued to the UI, before replacement.
public sealed class MqttConnection : IAsyncDisposable
{
    private readonly SettingsStore store;
    private readonly Action<Action> dispatch;
    private readonly CommandPipeline pipeline;
    private readonly SemaphoreSlim replacement = new(1, 1);
    private IManagedMqttClient? client;
    private long epoch;
    private volatile bool desired, suspended, disposed;
    private string connectionFingerprint;
    public string Status { get; private set; } = "Stopped";
    public event Action? StatusChanged;

    public MqttConnection(SettingsStore store, CommandPipeline pipeline, Action<Action> dispatch)
    {
        this.store = store; this.pipeline = pipeline; this.dispatch = dispatch;
        connectionFingerprint = Fingerprint();
        store.Changed += SettingsChanged;
        SystemEvents.PowerModeChanged += PowerChanged;
        NetworkChange.NetworkAddressChanged += NetworkChanged;
        NetworkChange.NetworkAvailabilityChanged += AvailabilityChanged;
    }
    private string Fingerprint() => System.Text.Json.JsonSerializer.Serialize(store.Current.Connection);
    private void SettingsChanged()
    {
        string next = Fingerprint();
        if (next == connectionFingerprint) return;
        connectionFingerprint = next;
        QueueReplacement();
    }
    public Task StartAsync() { desired = true; return ReplaceAsync(Interlocked.Increment(ref epoch)); }
    public Task StopAsync() { desired = false; return ReplaceAsync(Interlocked.Increment(ref epoch)); }
    private void QueueReplacement() { if (!disposed) _ = ReplaceAsync(Interlocked.Increment(ref epoch)); }
    private void NetworkChanged(object? sender, EventArgs e) => QueueReplacement();
    private void AvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) => QueueReplacement();
    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) suspended = true;
        else if (e.Mode == PowerModes.Resume) suspended = false;
        else return;
        QueueReplacement();
    }
    private bool Current(long generation) => generation == Interlocked.Read(ref epoch) && !disposed;
    private void Report(long generation, string status) => dispatch(() =>
    {
        if (!Current(generation)) return;
        Status = status;
        StatusChanged?.Invoke();
    });
    private async Task ReplaceAsync(long generation)
    {
        await replacement.WaitAsync();
        try
        {
            if (!Current(generation)) return;
            if (client != null)
            {
                var previous = client; client = null;
                try { await previous.StopAsync(); }
                finally { previous.Dispose(); }
            }
            if (!Current(generation)) return;
            if (!desired || suspended) { Report(generation, suspended && desired ? "Suspended" : "Stopped"); return; }
            var settings = SettingsStore.Clone(store.Current.Connection);
            if (string.IsNullOrWhiteSpace(settings.ClientID) || settings.ClientID.Contains('\0') ||
                Encoding.UTF8.GetByteCount(settings.ClientID) > 65535 || string.IsNullOrWhiteSpace(settings.TCPServerIP) ||
                settings.TCPServerPort is < 1 or > 65535)
            { Report(generation, "Configure the MQTT server, port, and client ID in Settings."); return; }
            var next = new MqttFactory().CreateManagedMqttClient(); client = next;
            next.UseConnectedHandler(_ => { Report(generation, "Connected"); });
            next.UseDisconnectedHandler(_ => { Report(generation, "Disconnected — retrying"); });
            next.UseApplicationMessageReceivedHandler(message =>
            {
                if (!Current(generation)) return;
                var payload = message.ApplicationMessage.Payload;
                var bounded = payload is { Length: > 4096 } ? new byte[4097] : payload?.ToArray();
                string topic = message.ApplicationMessage.Topic;
                bool retained = message.ApplicationMessage.Retain;
                var receivedAt = DateTimeOffset.UtcNow;
                dispatch(() => { if (Current(generation) && desired && !suspended) pipeline.Process(topic, bounded, retained, receivedAt: receivedAt); });
            });
            await next.SubscribeAsync(settings.ActionMappings.Select(m => new MqttTopicFilter { Topic = m.Topic }).ToList());
            Report(generation, "Connecting…");
            await next.StartAsync(new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(Math.Max(1, settings.AutoReconnectDelayInSeconds)))
                .WithClientOptions(new MqttClientOptionsBuilder().WithClientId(settings.ClientID)
                    .WithTcpServer(settings.TCPServerIP, settings.TCPServerPort)
                    .WithCredentials(settings.TCPServerUsername, settings.TCPServerPassword)
                    .WithCommunicationTimeout(TimeSpan.FromMinutes(Math.Clamp(settings.CommunicationTimeoutInMinutes, 1, 1440)))
                    .WithCleanSession().Build()).Build());
        }
        catch (Exception)
        { Report(generation, "MQTT connection failed. Check the server, credentials, and network, then Connect again."); }
        finally { replacement.Release(); }
    }
    public async ValueTask DisposeAsync()
    {
        disposed = true; desired = false; Interlocked.Increment(ref epoch);
        store.Changed -= SettingsChanged;
        SystemEvents.PowerModeChanged -= PowerChanged;
        NetworkChange.NetworkAddressChanged -= NetworkChanged;
        NetworkChange.NetworkAvailabilityChanged -= AvailabilityChanged;
        await replacement.WaitAsync();
        try
        {
            if (client != null)
            {
                try { await client.StopAsync(); }
                finally { client.Dispose(); client = null; }
            }
        }
        finally { replacement.Release(); }
    }
}
