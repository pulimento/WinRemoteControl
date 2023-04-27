using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinRemoteControl.Settings;

namespace WinRemoteControl
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();

            // Load current values
            cbLaunchAppAtLogon.Checked = AppUserSettings.Default.LAUNCH_AT_LOGON;
            cbAutoStartListening.Checked = AppUserSettings.Default.AUTO_CONNECT_AT_STARTUP;
            cbStartMinimized.Checked = AppUserSettings.Default.START_MINIMIZED;


            // Listeners
            cbLaunchAppAtLogon.CheckedChanged += this.CbLaunchAppAtLogon_CheckedChanged;
            cbAutoStartListening.CheckedChanged += this.CbAutoStartListening_CheckedChanged;
            cbStartMinimized.CheckedChanged += this.CbStartMinimized_CheckChanged;
        }

        private void CbStartMinimized_CheckChanged(object? sender, EventArgs e)
        {
            bool currentValue = AppUserSettings.Default.START_MINIMIZED;
            if ((sender as CheckBox)?.Checked != currentValue)
            {
                bool newValue = (sender as CheckBox)!.Checked;
                AppUserSettings.Default.START_MINIMIZED = newValue;
                AppUserSettings.Default.Save();
                Log.Debug($"Updating config: {nameof(AppUserSettings.START_MINIMIZED)}, new value: {newValue}");

                SetStartupAtLogon(newValue);
            }
            else
            {
                Log.Error($"Error setting config value: {nameof(AppUserSettings.START_MINIMIZED)}");
            }
        }

        private void CbLaunchAppAtLogon_CheckedChanged(object? sender, EventArgs e)
        {
            bool currentValue = AppUserSettings.Default.LAUNCH_AT_LOGON;
            if((sender as CheckBox)?.Checked != currentValue)
            {
                bool newValue = (sender as CheckBox)!.Checked;
                AppUserSettings.Default.LAUNCH_AT_LOGON = newValue;
                AppUserSettings.Default.Save();
                Log.Debug($"Updating config: {nameof(AppUserSettings.LAUNCH_AT_LOGON)}, new value: {newValue}");

                SetStartupAtLogon(newValue);
            } 
            else
            {
                Log.Error($"Error setting config value: {nameof(AppUserSettings.LAUNCH_AT_LOGON)}");
            }
        }

        private void CbAutoStartListening_CheckedChanged(object? sender, EventArgs e)
        {
            bool currentValue = AppUserSettings.Default.AUTO_CONNECT_AT_STARTUP;
            if ((sender as CheckBox)?.Checked != currentValue)
            {
                bool newValue = (sender as CheckBox)!.Checked;
                AppUserSettings.Default.AUTO_CONNECT_AT_STARTUP = newValue;
                AppUserSettings.Default.Save();
                Log.Debug($"Updating config: {nameof(AppUserSettings.AUTO_CONNECT_AT_STARTUP)}, new value: {newValue}");
            }
            else
            {
                Log.Error($"Error setting config value: {nameof(AppUserSettings.AUTO_CONNECT_AT_STARTUP)}");
            }
        }

        private void SetStartupAtLogon(bool runAtStartup)
        {
            const string RegistryRunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            string SubKey = Application.ProductName;
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true))
            {
                if(key == null)
                {
                    // Key does not exist. Strange(!)
                    Log.Error("Error trying to set the app to start at logon: Registry key does not exist");
                    return;
                }

                // Get rid of that nullable variable
                var RunKey = key!;

                if (runAtStartup)
                {
                    // Add registry key                
                    RunKey.SetValue(SubKey, Application.ExecutablePath.ToString());
                }
                else
                {
                    if (RunKey.GetValueNames().Contains(SubKey))
                    {
                        RunKey.DeleteValue(SubKey);
                    }                    
                }
                RunKey.Close();
            }            
        }
    }
}
