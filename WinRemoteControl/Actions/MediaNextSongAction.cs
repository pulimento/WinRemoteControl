using FluentResults;
using System;
using System.Runtime.InteropServices;
using Serilog;

namespace WinRemoteControl.Actions
{
    class MediaNextSongAction : IAction
    {
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

        public Result DoAction()
        {
            Log.Information("Skipping to next song");
            keybd_event(Constants.VK_MEDIA_NEXT_TRACK, 0, Constants.KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);  // Next Track
            return Result.Ok();
        }
    }
}
