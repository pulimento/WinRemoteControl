using AutoUpdaterDotNET;

namespace WinRemoteControl.Updater;

internal class Updater
{
    public static void CheckForUpdates()
    {
        string updaterURL = Environment.Is64BitProcess ?
            Constants.UPDATE_URL_WIN64 :
            Constants.UPDATE_URL_WIN32;

        AutoUpdater.Start(updaterURL);
    }
}
