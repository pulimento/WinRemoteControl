using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Extensions.ManagedClient;
using WinRemoteControl.Actions;
using WinRemoteControl.LoggerExtensions;
using WinRemoteControl.Settings;

namespace WinRemoteControl
{
    public partial class MainForm : Form
    {
        private IManagedMqttClient? mqttClient;
        private bool mqttHandlersRegistered;
        private Dictionary<string, IAction> topicsAndActions;

        public MainForm()
        {            
            InitializeComponent();
            this.Load += new EventHandler(this.MainForm_Load);
            LoggerHelper.SetupNONUILogger();
            SetEventListeners();
            topicsAndActions = SetupTopicsAndActions();

            Log.Information("Application started");            
        }

        public void MainForm_Load(object? sender, EventArgs e) 
        {
            LoggerHelper.SetupCompleteLogger(this);

            // Connect automatically if needed
            if (AppUserSettings.Default.AUTO_CONNECT_AT_STARTUP)
            {
                Log.Information("Connecting automatically at startup...");
                DoClientStart();
            }

            // Start from the tray
            if (AppUserSettings.Default.START_MINIMIZED)
            {
                this.ShowInTaskbar = false; // Don't show the form in the taskbar
                this.WindowState = FormWindowState.Minimized;
            }
        }

        public Dictionary<string, IAction> SetupTopicsAndActions(IEnumerable<Config.ActionMapping>? mappings = null)
        {
            var topics = new Dictionary<string, IAction>();
            foreach (var mapping in mappings ?? Config.SettingsFromFile.CreateDefaultActionMappings())
            {
                if (string.IsNullOrWhiteSpace(mapping.Topic) || string.IsNullOrWhiteSpace(mapping.Action))
                {
                    continue;
                }

                var action = CreateAction(mapping.Action);
                if (action == null)
                {
                    Log.Warning("Ignoring unknown configured action '{Action}' for topic '{Topic}'.", mapping.Action, mapping.Topic);
                    continue;
                }

                if (!topics.TryAdd(mapping.Topic, action))
                {
                    Log.Warning("Ignoring duplicate configured topic '{Topic}'.", mapping.Topic);
                }
            }
            return topics;
        }

        private IAction? CreateAction(string action) => action switch
        {
            Constants.ACTION_TOGGLE_TEAMS_MUTE => new ToggleMuteTeamsAction(),
            Constants.ACTION_VOLUME_UP => new VolumeUpAction(this),
            Constants.ACTION_VOLUME_DOWN => new VolumeDownAction(this),
            Constants.ACTION_MEDIA_NEXT_SONG => new MediaNextSongAction(),
            Constants.ACTION_MEDIA_PREV_SONG => new MediaPrevSongAction(),
            Constants.ACTION_PRESS_1 => new KeyPressAction("1"),
            Constants.ACTION_PRESS_2 => new KeyPressAction("2"),
            Constants.ACTION_PRESS_3 => new KeyPressAction("3"),
            _ => null,
        };

        private void DoActionForTopic(string topic, string payload)
        {
            if (topicsAndActions.TryGetValue(topic, out IAction? action))
            {
                action.DoAction();
            }
            else
            {
                Log.Warning("Ignoring message for an unconfigured topic: {Topic}", topic);
            }
        }

        #region UI Callbacks

        private void BtnMute_Click(object sender, EventArgs e)
        {
            new ToggleMuteTeamsAction().DoAction();
        }

        private void BtnOpenSettings_Click(object sender, EventArgs e)
        {
            using var connectionSettingsForm = new ConnectionSettingsForm();
            connectionSettingsForm.ShowDialog(this);
        }

        private void BtnStartClient_Click(object sender, EventArgs e)
        {
            DoClientStart();
        }

        private async void BtnStopClient_Click(object sender, EventArgs e)
        {
            await DoClientStop();
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            var aboutForm = new AboutForm();
            aboutForm.Show();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm();
            settingsForm.Show();
        }

        #endregion

        #region Client management

        private async void DoClientStart()
        {
            Log.Information("Starting MQTT client...");
            var checkSettingsResult = Config.CheckSettingsFile();
            if (checkSettingsResult.IsFailed)
            {
                Log.Error($"Error checking settings: {LoggerHelper.ResultErrorsToString(checkSettingsResult.Errors)}");
                return;
            }

            if (this.mqttClient == null)
            {
                var mqttFactory = new MqttFactory();
                this.mqttClient = mqttFactory.CreateManagedMqttClient();
            }
            if (this.mqttClient.IsStarted)
            {
                Log.Warning($"Client already started, doing nothing");
                return;
            }
            if (this.mqttClient.IsConnected)
            {
                Log.Warning($"Client already connected, doing nothing");
                return;
            }

            if (!mqttHandlersRegistered)
            {
                mqttClient.UseConnectedHandler(this.HandleConnectedAsync);
                mqttClient.UseDisconnectedHandler(this.HandleDisconnectedAsync);
                mqttClient.UseApplicationMessageReceivedHandler(this.HandleApplicationMessageReceivedAsync);
                mqttHandlersRegistered = true;
            }

            var clientConfig = Config.LoadClientConfigFromFile();
            if (clientConfig.IsFailed)
            {
                Log.Error($"Error loading settings: {LoggerHelper.ResultErrorsToString(clientConfig.Errors)}");
            }
            else
            {
                try
                {
                    var mappingSettings = Config.LoadSettingsForEditing();
                    if (mappingSettings.IsFailed)
                    {
                        Log.Error("Unable to load action mappings: {Errors}", LoggerHelper.ResultErrorsToString(mappingSettings.Errors));
                        return;
                    }

                    topicsAndActions = SetupTopicsAndActions(mappingSettings.Value.ActionMappings);
                    if (topicsAndActions.Count == 0)
                    {
                        Log.Error("No valid MQTT topic/action mappings are configured. Add at least one mapping in Connection settings.");
                        return;
                    }

                    await this.mqttClient.StartAsync(clientConfig.Value);
                    btnStartClient.Enabled = false;
                    btnStopClient.Enabled = true;
                    Log.Information("MQTT client started. Connecting to the broker now; connection results will appear here.");
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "MQTT client could not start. Check the server address, port, credentials, and network connection.");
                }
            }
        }

        private async Task DoClientStop()
        {
            if (mqttClient == null || !mqttClient.IsStarted)
            {
                Log.Information("MQTT client is already stopped.");
                return;
            }

            try
            {
                Log.Information("Stopping MQTT client and cancelling reconnect attempts...");
                await mqttClient.StopAsync();
                btnStartClient.Enabled = true;
                btnStopClient.Enabled = false;
                Log.Information("MQTT client stopped.");
            }
            catch (Exception exception)
            {
                Log.Error(exception, "MQTT client could not stop cleanly.");
            }
        }

        #endregion

        #region Mqtt Event Handlers
        public async Task<Task> HandleConnectedAsync(MqttClientConnectedEventArgs x)
        {
            var item =
                $"ResultCode: {x.ConnectResult.ResultCode} | " +
                $"Reason: {x.ConnectResult.ReasonString} | " +
                $"ResponseInfo: {x.ConnectResult.ResponseInformation}";
            Log.Information($"MQTT client connected - {item}");

            // Subscribe to topics
            Log.Information($"About to subscribe to topics: [{string.Join(",", topicsAndActions.Keys)}]");
            List<MqttTopicFilter> filtersToSubscribe = topicsAndActions.Keys.Select(topic => new MqttTopicFilter { Topic = topic }).ToList();
            if (this.mqttClient != null)
            {
                await this.mqttClient.SubscribeAsync(filtersToSubscribe);
            }
            else
            {
                Log.Error("MQTT Client not ready, impossible to subscribe. Please try again later");
            }

            return Task.CompletedTask;
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs x)
        {
            string topic = x.ApplicationMessage.Topic;
            string payload = x.ApplicationMessage.ConvertPayloadToString();
            var item =
                $"Topic: {topic} | " +
                $"Payload: {payload} | " +
                $"QoS: {x.ApplicationMessage.QualityOfServiceLevel}";

            Log.Debug($"MQTT Message: {item}");
            this.DoActionForTopic(topic, payload);

            return Task.CompletedTask;
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs x)
        {
            var item =
                $"ResultCode: {x.ConnectResult.ResultCode} | " +
                $"Reason: {x.ConnectResult.ReasonString} | " +
                $"ResponseInfo: {x.ConnectResult.ResponseInformation}";
            if (x.Exception != null)
            {
                Log.Error(x.Exception,
                    "MQTT connection failed or was lost - {Details}. The client will keep retrying until you press Stop.",
                    item);
            }
            else
            {
                Log.Warning("MQTT client disconnected - {Details}. Press Stop to cancel any reconnect attempts.", item);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Minimise and background

        private void NotifyIcon_RestoreWindow(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            this.notifyIcon1.Visible = false;
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void BtnGoToBackground_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        #endregion
    }
}
