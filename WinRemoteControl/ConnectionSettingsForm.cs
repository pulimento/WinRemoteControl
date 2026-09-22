using WinRemoteControl.Settings;
using System.ComponentModel;

namespace WinRemoteControl;

public sealed class ConnectionSettingsForm : Form
{
    private readonly TextBox clientIdTextBox = new();
    private readonly TextBox serverTextBox = new();
    private readonly NumericUpDown portInput = new() { Minimum = 1, Maximum = 65535 };
    private readonly TextBox usernameTextBox = new();
    private readonly TextBox passwordTextBox = new() { UseSystemPasswordChar = true };
    private readonly NumericUpDown timeoutInput = new() { Minimum = 1, Maximum = 1440 };
    private readonly NumericUpDown reconnectDelayInput = new() { Minimum = 0, Maximum = 3600 };
    private readonly DataGridView mappingsGrid = new()
    {
        AutoGenerateColumns = false,
        AllowUserToAddRows = true,
        AllowUserToDeleteRows = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        Dock = DockStyle.Fill,
    };
    private readonly Label messageLabel = new() { AutoSize = true };

    public ConnectionSettingsForm()
    {
        Text = "Connection settings";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(600, 590);
        ClientSize = new Size(620, 560);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 11,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        for (var row = 0; row < 8; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddRow(layout, 0, "Client ID", clientIdTextBox);
        AddRow(layout, 1, "MQTT server", serverTextBox);
        AddRow(layout, 2, "Port", portInput);
        AddRow(layout, 3, "Username", usernameTextBox);
        AddRow(layout, 4, "Password", passwordTextBox);
        AddRow(layout, 5, "Timeout (minutes)", timeoutInput);
        AddRow(layout, 6, "Reconnect delay (seconds)", reconnectDelayInput);

        layout.Controls.Add(new Label
        {
            Text = "MQTT topic actions (use the blank row to add; select a row and press Delete to remove)",
            AutoSize = true,
        }, 0, 7);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 7)!, 2);

        ConfigureMappingsGrid();
        layout.Controls.Add(mappingsGrid, 0, 8);
        layout.SetColumnSpan(mappingsGrid, 2);

        messageLabel.ForeColor = Color.Firebrick;
        layout.Controls.Add(messageLabel, 0, 9);
        layout.SetColumnSpan(messageLabel, 2);

        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += SaveButton_Click;
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Right };
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        layout.Controls.Add(buttons, 0, 10);
        layout.SetColumnSpan(buttons, 2);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.Add(layout);

        LoadSettings();
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control input)
    {
        input.Dock = DockStyle.Fill;
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(input, 1, row);
    }

    private void LoadSettings()
    {
        var result = Config.LoadSettingsForEditing();
        if (result.IsFailed)
        {
            messageLabel.Text = string.Join(Environment.NewLine, result.Errors.Select(error => error.Message));
            return;
        }

        var settings = result.Value;
        clientIdTextBox.Text = settings.ClientID ?? string.Empty;
        serverTextBox.Text = settings.TCPServerIP ?? string.Empty;
        portInput.Value = Clamp(settings.TCPServerPort, portInput.Minimum, portInput.Maximum);
        usernameTextBox.Text = settings.TCPServerUsername ?? string.Empty;
        passwordTextBox.Text = settings.TCPServerPassword ?? string.Empty;
        timeoutInput.Value = Clamp(settings.CommunicationTimeoutInMinutes, timeoutInput.Minimum, timeoutInput.Maximum);
        reconnectDelayInput.Value = Clamp(settings.AutoReconnectDelayInSeconds, reconnectDelayInput.Minimum, reconnectDelayInput.Maximum);
        mappingsGrid.DataSource = new BindingList<Config.ActionMapping>(
            (settings.ActionMappings ?? Config.SettingsFromFile.CreateDefaultActionMappings()).ToList());
    }

    private void ConfigureMappingsGrid()
    {
        mappingsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "topicColumn",
            HeaderText = "MQTT topic",
            DataPropertyName = nameof(Config.ActionMapping.Topic),
        });

        var actionColumn = new DataGridViewComboBoxColumn
        {
            Name = "actionColumn",
            HeaderText = "Action",
            DataPropertyName = nameof(Config.ActionMapping.Action),
            DisplayMember = nameof(ActionOption.DisplayName),
            ValueMember = nameof(ActionOption.Id),
            DataSource = GetActionOptions(),
        };
        mappingsGrid.Columns.Add(actionColumn);
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(clientIdTextBox.Text) || string.IsNullOrWhiteSpace(serverTextBox.Text))
        {
            messageLabel.Text = "Client ID and MQTT server are required.";
            return;
        }

        var settings = new Config.SettingsFromFile
        {
            ClientID = clientIdTextBox.Text.Trim(),
            TCPServerIP = serverTextBox.Text.Trim(),
            TCPServerPort = Decimal.ToInt32(portInput.Value),
            TCPServerUsername = usernameTextBox.Text,
            TCPServerPassword = passwordTextBox.Text,
            CommunicationTimeoutInMinutes = Decimal.ToInt32(timeoutInput.Value),
            AutoReconnectDelayInSeconds = Decimal.ToInt32(reconnectDelayInput.Value),
            ActionMappings = GetMappings(),
        };

        if (settings.ActionMappings.Count == 0)
        {
            messageLabel.Text = "Add at least one MQTT topic action mapping.";
            return;
        }

        if (settings.ActionMappings.Any(mapping => string.IsNullOrWhiteSpace(mapping.Topic) || string.IsNullOrWhiteSpace(mapping.Action)))
        {
            messageLabel.Text = "Each action mapping needs both a topic and an action.";
            return;
        }

        if (settings.ActionMappings.GroupBy(mapping => mapping.Topic, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            messageLabel.Text = "Each MQTT topic can be mapped only once.";
            return;
        }

        var result = Config.SaveSettings(settings);
        if (result.IsFailed)
        {
            messageLabel.Text = string.Join(Environment.NewLine, result.Errors.Select(error => error.Message));
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static decimal Clamp(int value, decimal minimum, decimal maximum)
    {
        return Math.Clamp((decimal)value, minimum, maximum);
    }

    private List<Config.ActionMapping> GetMappings()
    {
        return mappingsGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => new Config.ActionMapping
            {
                Topic = row.Cells["topicColumn"].Value?.ToString()?.Trim(),
                Action = row.Cells["actionColumn"].Value?.ToString(),
            })
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Topic) || !string.IsNullOrWhiteSpace(mapping.Action))
            .ToList();
    }

    private static List<ActionOption> GetActionOptions() =>
    [
        new(Constants.ACTION_TOGGLE_TEAMS_MUTE, "Toggle Microsoft Teams mute"),
        new(Constants.ACTION_VOLUME_UP, "Volume up"),
        new(Constants.ACTION_VOLUME_DOWN, "Volume down"),
        new(Constants.ACTION_MEDIA_NEXT_SONG, "Next media track"),
        new(Constants.ACTION_MEDIA_PREV_SONG, "Previous media track"),
        new(Constants.ACTION_PRESS_1, "Press 1"),
        new(Constants.ACTION_PRESS_2, "Press 2"),
        new(Constants.ACTION_PRESS_3, "Press 3"),
    ];

    private sealed record ActionOption(string Id, string DisplayName);
}
