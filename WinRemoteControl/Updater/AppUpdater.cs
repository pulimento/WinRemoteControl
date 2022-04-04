using AutoUpdaterDotNET;

namespace WinRemoteControl.Updater;

public class AppUpdater
{
    public static void CheckForUpdates(AutoUpdater.CheckForUpdateEventHandler updateEventHandler)
    {
        string updaterURL = Environment.Is64BitProcess ?
            Constants.UPDATE_URL_WIN64 :
            Constants.UPDATE_URL_WIN32;

        AutoUpdater.CheckForUpdateEvent += updateEventHandler;
        AutoUpdater.Start(updaterURL);       
    }
}
