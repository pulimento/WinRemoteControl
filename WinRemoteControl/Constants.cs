namespace WinRemoteControl;

static class Constants
{
    // MQTT topics
    public const string TOPIC_TOGGLE_TEAMS_MUTE = "control/toggle_teams_mute";
    public const string TOPIC_VOLUME_UP = "control/volume_up";
    public const string TOPIC_VOLUME_DOWN = "control/volume_down";
    public const string TOPIC_MEDIA_NEXT_SONG = "control/media_next_song";
    public const string TOPIC_MEDIA_PREV_SONG = "control/media_prev_song";

    // Constants to send keystrokes
    public const int APPCOMMAND_VOLUME_UP = 0xA0000;
    public const int APPCOMMAND_VOLUME_DOWN = 0x90000;
    public const int WM_APPCOMMAND = 0x319;
    public const int VK_MEDIA_NEXT_TRACK = 0xB0;
    public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
    public const int VK_MEDIA_PREV_TRACK = 0xB1;
    public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
    public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag

    // App updater
    public const string UPDATE_URL_WIN32 = 
        "https://raw.githubusercontent.com/pulimento/WinRemoteControl/feature/net-6-and-auto-updater/updater/latestversion_win32.json";
    public const string UPDATE_URL_WIN64 =
        "https://raw.githubusercontent.com/pulimento/WinRemoteControl/feature/net-6-and-auto-updater/updater/latestversion_win64.json";

    // Misc
    public const string REPO_URL = "https://github.com/pulimento/WinRemoteControl";
}
