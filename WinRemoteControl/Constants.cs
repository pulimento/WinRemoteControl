namespace WinRemoteControl;

static class Constants
{
    // MQTT topics
    public const string TOPIC_TOGGLE_TEAMS_MUTE = "control/toggle_teams_mute";
    public const string TOPIC_VOLUME_UP = "control/volume_up";
    public const string TOPIC_VOLUME_DOWN = "control/volume_down";
    public const string TOPIC_MEDIA_NEXT_SONG = "control/media_next_song";
    public const string TOPIC_MEDIA_PREV_SONG = "control/media_prev_song";
    public const string TOPIC_PRESS_1 = "control/press_1";
    public const string TOPIC_PRESS_2 = "control/press_2";
    public const string TOPIC_PRESS_3 = "control/press_3";

    // Configurable action identifiers
    public const string ACTION_TOGGLE_TEAMS_MUTE = "toggle_teams_mute";
    public const string ACTION_VOLUME_UP = "volume_up";
    public const string ACTION_VOLUME_DOWN = "volume_down";
    public const string ACTION_MEDIA_NEXT_SONG = "media_next_song";
    public const string ACTION_MEDIA_PREV_SONG = "media_prev_song";
    public const string ACTION_PRESS_1 = "press_1";
    public const string ACTION_PRESS_2 = "press_2";
    public const string ACTION_PRESS_3 = "press_3";

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
        "https://raw.githubusercontent.com/pulimento/WinRemoteControl/master/updater/latestversion_win32.xml";
    public const string UPDATE_URL_WIN64 =
        "https://raw.githubusercontent.com/pulimento/WinRemoteControl/master/updater/latestversion_win64.xml";

    // Misc
    public const string REPO_URL = "https://github.com/pulimento/WinRemoteControl";
}
