using FluentResults;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WinRemoteControl.Actions;

class ToggleMuteTeamsAction : IAction
{
    [DllImport("User32.dll")]
    static extern int SetForegroundWindow(IntPtr point);

    public Result DoAction()
    {
        Log.Information("Trying to toggle Teams mute...");

        Process? p = Process.GetProcessesByName("Teams").FirstOrDefault();
        if (p == null)
        {
            // Try with the "new" Microsoft Teams
            Log.Information("Classic Teams not found. Trying with the  \"new\" one...");
            p = Process.GetProcessesByName("ms-teams").FirstOrDefault();
        }

        if (p != null)
        {
            IntPtr h = p.MainWindowHandle;
            SetForegroundWindow(h);
            SendKeys.SendWait("^+{m}"); // CTRL + SHIFT + M
            Log.Verbose("Keys were sent to Teams");
        }
        else
        {
            Log.Error("Can't find Teams process. Is it running?");
        }
        return Result.Ok();
    }
}
