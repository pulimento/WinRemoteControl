using FluentResults;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace WinRemoteControl
{
    public class Config
    {
        private static string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        private static string settingsFilePath = $"{currentPath}\\settings.json";
        private static string defaultSettingsFilePath = $"{currentPath}\\settings_example.json";

        public static Result CheckSettingsFile()
        {
            // First of all, let's check if file exists
            if (File.Exists(settingsFilePath))
            {
                // Settings file exists. Let's check if has the same content of the example file
                if (File.Exists(defaultSettingsFilePath))
                {
                    // We can try to compare file lenghts, but could be the case that a legit config and the
                    // example one are identical in size. Next thing could be a checksum or a byte-by-byte compare,
                    // but those files may differ on BOM, line-endings and so-on. Given that those objects are
                    // small enough, let's deserialize them and compare on the deserialized side. It may not be 
                    // the most efficient way to do it, though.

                    using var settingsReader = new StreamReader(settingsFilePath);
                    var jsonSettingsString = settingsReader.ReadToEnd();
                    SettingsFromFile? settings = JsonSerializer.Deserialize<SettingsFromFile>(jsonSettingsString);

                    using var defaultSettingsReader = new StreamReader(defaultSettingsFilePath);
                    var jsonDefaultSettingsString = defaultSettingsReader.ReadToEnd();
                    SettingsFromFile? defaultSettings = JsonSerializer.Deserialize<SettingsFromFile>(jsonDefaultSettingsString);

                    if (settings != null && defaultSettings != null)
                    {
                        if (settings.Equals(defaultSettings))
                        {
                            return Result.Fail("It seems that you have the default settings. Please change them in the file.");
                        }
                        else
                        {
                            // Settings file exists, and settings aren't the default ones
                            return Result.Ok();
                        }
                    }
                    else
                    {
                        // At least one of them is null. That would be a corner case, because we've already checked if the file exists.
                        // Just return OK
                        return Result.Ok();
                    }
                }
                else
                {
                    // Can't compare with default settings file, but at least exists.
                    return Result.Ok();
                }
            }
            else
            {
                // Settings file doesn't exist. Let's try to copy them from the example one
                if (File.Exists(defaultSettingsFilePath))
                {
                    // Copy them!
                    File.Copy(defaultSettingsFilePath, settingsFilePath);

                    if (File.Exists(settingsFilePath))
                    {
                        return Result.Fail("Settings file was created, but it will need to be modified in order to work. " +
                            "Please change it. Use the button 'Open settings file' for that");
                    }
                    else
                    {
                        return Result.Fail("Settings file missing, and can't copy them from example ones");
                    }
                }
                else
                {
                    return Result.Fail("Settings file missing, and can't copy them from example ones");
                }
            }
        }

        public static Result<IManagedMqttClientOptions> LoadClientConfigFromFile()
        {
            // Read config from file
            SettingsFromFile? settings = null;

            if (File.Exists(settingsFilePath))
            {
                using var reader = new StreamReader(settingsFilePath);
                var jsonString = reader.ReadToEnd();
                settings = JsonSerializer.Deserialize<SettingsFromFile>(jsonString);
            }
            else
            {
                return Result.Fail("Settings file path not found");
            }

            if (settings == null)
            {
                return Result.Fail("General error loading settings");
            }

            IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(settings.ClientID)
                .WithTcpServer(settings.TCPServerIP, settings.TCPServerPort)
                .WithCredentials(settings.TCPServerUsername, settings.TCPServerPassword)
                .WithCommunicationTimeout(TimeSpan.FromMinutes(settings.CommunicationTimeoutInMinutes))
                .WithCleanSession()
                .Build();

            IManagedMqttClientOptions managedClientOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(settings.AutoReconnectDelayInSeconds))
                .WithClientOptions(mqttClientOptions)
                .Build();

            return Result.Ok(managedClientOptions);
        }

        public static Result<bool> ExploreSettingsFile()
        {
            string? filePath = null;
            // Try to open the exact file selected. If not, at least try to open the containing folder
            if (File.Exists(settingsFilePath))
            {
                filePath = settingsFilePath;
            }
            else
            {
                // File doesn't exists, let's try to open the folder
                string? settingsDirectoryName = Path.GetDirectoryName(settingsFilePath);
                if (settingsDirectoryName != null && Directory.Exists(settingsDirectoryName))
                {
                    filePath = settingsDirectoryName;
                }
                else
                {
                    return Result.Fail("Settings directory path does not exist");
                }
            }
            if (string.IsNullOrEmpty(filePath))
            {
                return Result.Fail("Settings file path not found");
            }
            // Clean up file path
            filePath = Path.GetFullPath(filePath);
            System.Diagnostics.Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
            return Result.Ok();
        }

        public class SettingsFromFile
        {
            public string? ClientID { get; set; }
            public string? TCPServerIP { get; set; }
            public int TCPServerPort { get; set; }
            public string? TCPServerUsername { get; set; }
            public string? TCPServerPassword { get; set; }
            public int CommunicationTimeoutInMinutes { get; set; }
            public int AutoReconnectDelayInSeconds { get; set; }

            public bool Equals(SettingsFromFile? other)
            {
                return other!=null&&
                       this.ClientID==other.ClientID&&
                       this.TCPServerIP==other.TCPServerIP&&
                       this.TCPServerPort==other.TCPServerPort&&
                       this.TCPServerUsername==other.TCPServerUsername&&
                       this.TCPServerPassword==other.TCPServerPassword&&
                       this.CommunicationTimeoutInMinutes==other.CommunicationTimeoutInMinutes&&
                       this.AutoReconnectDelayInSeconds==other.AutoReconnectDelayInSeconds;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.ClientID, this.TCPServerIP, this.TCPServerPort, this.TCPServerUsername, 
                    this.TCPServerPassword, this.CommunicationTimeoutInMinutes, this.AutoReconnectDelayInSeconds);
            }
        }
    }
}
