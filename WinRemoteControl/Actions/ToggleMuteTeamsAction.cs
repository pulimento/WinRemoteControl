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
            int HRESULT = SetForegroundWindow(h);
            if (HRESULT != 0)
            {
                SendKeys.SendWait("^+{m}"); // CTRL + SHIFT + M
                Log.Verbose("Keys were sent to Teams");
            } 
            else
            {
                byte[] bytes_hresult = BitConverter.GetBytes(HRESULT);
                uint uint_hresult = BitConverter.ToUInt32(bytes_hresult, 0);
                if (uint_hresult == 0x80070005u || uint_hresult == 0x8001011Bu)
                {
                    // Access denied or security restriction error
                    Log.Error("Error trying to set foreground window: Access denied or security restriction error");
                }
                else
                {
                    Log.Error($"Unknown error trying to set foreground window, HRESULT: {uint_hresult}");
                }
            }
        }
        else
        {
            Log.Error("Can't find Teams process. Is it running?");
        }
        return Result.Ok();
    }
}
