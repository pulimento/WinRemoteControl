using FluentResults;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Serilog;
using System.Diagnostics;
using System.Linq;

namespace WinRemoteControl.Actions
{
    class ToggleMuteTeamsAction : IAction
    {
        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        public Result DoAction()
        {
            Log.Information("Toggling Teams mute");

            Process? p = Process.GetProcessesByName("Teams").FirstOrDefault();
            if (p != null)
            {
                IntPtr h = p.MainWindowHandle;
                SetForegroundWindow(h);
                SendKeys.SendWait("^+{m}"); // CTRL + SHIFT + M
            }
            else
            {
                Log.Error("Can't find Teams process. Is it running?");
            }
            return Result.Ok();
        }
    }
}
