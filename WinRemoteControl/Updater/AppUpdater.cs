using AutoUpdaterDotNET;

namespace WinRemoteControl.Updater;

public static class AppUpdater
{
    public static void CheckForUpdates()
    {
        string updaterURL = Environment.Is64BitProcess ?
            Constants.UPDATE_URL_WIN64 :
            Constants.UPDATE_URL_WIN32;

        AutoUpdater.Start(updaterURL);
    }
}
