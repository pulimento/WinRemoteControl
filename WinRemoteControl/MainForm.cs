using FluentResults;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinRemoteControl.Actions;
using Serilog;
using WinRemoteControl.LoggerExtensions;

namespace WinRemoteControl
{
    public partial class MainForm : Form
    {
        private IManagedMqttClient? mqttClient;
        private readonly Dictionary<string, IAction> topicsAndActions;

        public MainForm()
        { 
            SetupLog();
            InitializeComponent();
            SetEventListeners();
            topicsAndActions = SetupTopicsAndActions();

            Log.Information("Application started");            
        }

        public Dictionary<string, IAction> SetupTopicsAndActions()
        {
            return new Dictionary<string, IAction>() {
                { Constants.TOPIC_TOGGLE_TEAMS_MUTE , new ToggleMuteTeamsAction() },
                { Constants.TOPIC_VOLUME_UP , new VolumeDownAction(this) },
                { Constants.TOPIC_VOLUME_DOWN , new VolumeDownAction(this) },
                { Constants.TOPIC_MEDIA_NEXT_SONG , new MediaNextSongAction() },
                { Constants.TOPIC_MEDIA_PREV_SONG , new MediaPrevSongAction() },
            };
        }

        private void DoActionForTopic(string topic, string payload)
        {
            IAction action = topicsAndActions[topic];
            if (action != null)
            {
                action.DoAction();
            } 
            else
            {
                Log.Warning("Error doing action for topic, topic received is not a known one");
            }
        }

        #region UI Callbacks

        private void btnMute_Click(object sender, EventArgs e)
        {
            new ToggleMuteTeamsAction().DoAction();
        }

        private void btnOpenSettings_Click(object sender, EventArgs e)
        {
            var result = Config.ExploreSettingsFile();
            if (result.IsFailed)
            {
                Log.Error($"Error opening settings: {ResultErrorsToString(result.Errors)}");
            }
        }

        private void btnStartClient_Click(object sender, EventArgs e)
        {
            DoClientStart();
        }

        #endregion

        #region Client management

        private async void DoClientStart()
        {
            var checkSettingsResult = Config.CheckSettingsFile();
            if (checkSettingsResult.IsFailed)
            {
                Log.Error($"Error checking settings: {ResultErrorsToString(checkSettingsResult.Errors)}");
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

            // Handlers
            mqttClient.UseConnectedHandler(this.HandleConnectedAsync);
            mqttClient.UseDisconnectedHandler(this.HandleDisconnectedAsync);
            mqttClient.UseApplicationMessageReceivedHandler(this.HandleApplicationMessageReceivedAsync);

            var clientConfig = Config.LoadClientConfigFromFile();
            if (clientConfig.IsFailed)
            {
                Log.Error($"Error loading settings: {ResultErrorsToString(checkSettingsResult.Errors)}");
            }
            else
            {
                await this.mqttClient.StartAsync(clientConfig.Value);
            }

            Log.Information($"MQTT client started sucessfully, trying to connect...");
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
            Log.Information($"Client disconnected - {item}");

            return Task.CompletedTask;
        }

        #endregion

        #region Minimise and background

        private void notifyIcon_RestoreWindow(object sender, MouseEventArgs e)
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

        private void btnGoToBackground_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        #endregion

        #region Utilities

        /*
        private void Log(string tag, string message, bool logToTextBox = true)
        {
            var lineToLog = $"{DateTime.Now:MM/dd/yy H:mm:ss.fff} # {tag} # {message}";
            Debug.WriteLine(lineToLog);
            if (logToTextBox)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    this.textBoxLog.AppendText(lineToLog + Environment.NewLine);
                });
            }
        }
        */        

        private void SetupLog()
        {
            var outputTemplate =
                "{Timestamp:MM/dd/yy H:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: outputTemplate)
                .WriteTo.Debug(outputTemplate: outputTemplate)
                .WriteTo.File("logs/log.txt", 
                    rollingInterval: RollingInterval.Month,
                    outputTemplate: outputTemplate)
                .WriteTo.WindowsFormsSink(this, outputTemplate: outputTemplate)
                .CreateLogger();
        }

        private string ResultErrorsToString(List<IError> e)
        {
            return string.Join(",", e.Select(e => e.Message));
        }

        #endregion
    }
}
