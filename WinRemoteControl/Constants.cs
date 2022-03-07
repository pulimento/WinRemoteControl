namespace WinRemoteControl
{
    static class Constants
    {
        // Constants to send keystrokes
        public const int APPCOMMAND_VOLUME_UP = 0xA0000;
        public const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        public const int WM_APPCOMMAND = 0x319;

        // Topic
        public const string TOPIC_TOGGLE_TEAMS_MUTE = "control/toggle_teams_mute";
        public const string TOPIC_VOLUME_UP = "control/volume_up";
        public const string TOPIC_VOLUME_DOWN = "control/volume_down";
    }
}
