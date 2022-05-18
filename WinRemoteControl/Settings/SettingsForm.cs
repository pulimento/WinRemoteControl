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


            // Listeners
            cbLaunchAppAtLogon.CheckedChanged += this.CbLaunchAppAtLogon_CheckedChanged;
            cbAutoStartListening.CheckedChanged += this.CbAutoStartListening_CheckedChanged;            
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
    }
}
