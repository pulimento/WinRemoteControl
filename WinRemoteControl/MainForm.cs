using FluentResults;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinRemoteControl
{
    public partial class MainForm : Form
    {
        private IManagedMqttClient? mqttClient;

        private MqttTopicFilter[] topicsToSubscribe = {
            new MqttTopicFilter { Topic =  Constants.TOPIC_TOGGLE_TEAMS_MUTE },
            new MqttTopicFilter { Topic =  Constants.TOPIC_VOLUME_UP },
            new MqttTopicFilter { Topic =  Constants.TOPIC_VOLUME_DOWN }
        };

        [DllImport("user32.dll")]
        static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        public MainForm()
        {
            InitializeComponent();
            SetEventListeners();
            Log("APP", "Application started", logToTextBox: false);
        }

        #region Actions
        private void DoActionForTopic(string topic, string payload)
        {
            if (topic == Constants.TOPIC_TOGGLE_TEAMS_MUTE)
            {
                MuteTeams();
            }
            else if (topic == Constants.TOPIC_VOLUME_UP)
            {
                DoVolumeUp();
            }
            else if (topic == Constants.TOPIC_VOLUME_DOWN)
            {
                DoVolumeDown();
            }
        }

        private void MuteTeams()
        {
            this.Log("ACTION", "Toggle Teams mute");

            Process? p = Process.GetProcessesByName("Teams").FirstOrDefault();
            if (p != null)
            {
                IntPtr h = p.MainWindowHandle;
                SetForegroundWindow(h);
                SendKeys.SendWait("^+{m}"); // CTRL + SHIFT + M
            }
            else
            {
                this.Log("ERROR", "Can't find Teams process. Is it running?");
            }
        }

        private void DoVolumeUp()
        {
            this.Log("ACTION", "Volume UP");
            this.BeginInvoke((MethodInvoker)delegate
            {
                SendMessageW(this.Handle, Constants.WM_APPCOMMAND, this.Handle, (IntPtr)Constants.APPCOMMAND_VOLUME_UP);
            });
        }

        private void DoVolumeDown()
        {
            this.Log("ACTION", "Volume DOWN");
            this.BeginInvoke((MethodInvoker)delegate
            {
                SendMessageW(this.Handle, Constants.WM_APPCOMMAND, this.Handle, (IntPtr)Constants.APPCOMMAND_VOLUME_DOWN);
            });
        }

        #endregion

        #region UI Callbacks

        private void btnMute_Click(object sender, EventArgs e)
        {
            MuteTeams();
        }

        private void btnOpenSettings_Click(object sender, EventArgs e)
        {
            var result = Config.ExploreSettingsFile();
            if (result.IsFailed)
            {
                Log("ERROR", $"Error opening settings: {ResultErrorsToString(result.Errors)}");
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
                Log("ERROR", $"Error checking settings: {ResultErrorsToString(checkSettingsResult.Errors)}");
                return;
            }

            if (this.mqttClient == null)
            {
                var mqttFactory = new MqttFactory();
                this.mqttClient = mqttFactory.CreateManagedMqttClient();
            }
            if (this.mqttClient.IsStarted)
            {
                this.Log("WARN", $"Client already started, doing nothing");
                return;
            }
            if (this.mqttClient.IsConnected)
            {
                this.Log("WARN", $"Client already connected, doing nothing");
                return;
            }

            // Handlers
            mqttClient.UseConnectedHandler(this.HandleConnectedAsync);
            mqttClient.UseDisconnectedHandler(this.HandleDisconnectedAsync);
            mqttClient.UseApplicationMessageReceivedHandler(this.HandleApplicationMessageReceivedAsync);

            var clientConfig = Config.LoadClientConfigFromFile();
            if (clientConfig.IsFailed)
            {
                Log("ERROR", $"Error loading settings: {ResultErrorsToString(checkSettingsResult.Errors)}");
            }
            else
            {
                await this.mqttClient.StartAsync(clientConfig.Value);
            }

            this.Log("START", $"MQTT client started sucessfully");
        }

        #endregion

        #region Mqtt Event Handlers
        public async Task<Task> HandleConnectedAsync(MqttClientConnectedEventArgs x)
        {
            var item =
                $"ResultCode: {x.ConnectResult.ResultCode} | " +
                $"Reason: {x.ConnectResult.ReasonString} | " +
                $"ResponseInfo: {x.ConnectResult.ResponseInformation}";
            this.Log("CONNECTED", $"MQTT client connected - {item}");

            // Subscribe to topics
            this.Log("SUBSCRIBE", $"About to subscribe to topics: [{string.Join(",", topicsToSubscribe.Select(t => t.Topic))}]");
            if (this.mqttClient != null)
            {
                await this.mqttClient.SubscribeAsync(topicsToSubscribe);
            }
            else
            {
                Log("ERROR", "MQTT Client not ready, impossible to subscribe. Please try again later");
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

            this.Log("MESSAGE", item);
            this.DoActionForTopic(topic, payload);

            return Task.CompletedTask;
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs x)
        {
            var item =
                $"ResultCode: {x.ConnectResult.ResultCode} | " +
                $"Reason: {x.ConnectResult.ReasonString} | " +
                $"ResponseInfo: {x.ConnectResult.ResponseInformation}";
            this.Log("DISCONNECTED", item);

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

        private string ResultErrorsToString(List<IError> e)
        {
            return string.Join(",", e.Select(e => e.Message));
        }

        #endregion
    }
}
