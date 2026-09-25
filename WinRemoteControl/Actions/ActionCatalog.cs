using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinRemoteControl.Settings;

namespace WinRemoteControl.Actions;

public static class ActionCatalog
{
    public static IReadOnlyDictionary<string, string> BuiltIns { get; } = new Dictionary<string, string>
    {
        ["press_enter"] = "Press Enter", ["start_dictation"] = "Voice typing (Win+H)",
        ["mute_gchat"] = "Mute GChat (Ctrl+D)", ["press_1"] = "Press 1", ["press_2"] = "Press 2", ["press_3"] = "Press 3",
        ["toggle_teams_mute"] = "Toggle Teams mute", ["volume_up"] = "Volume up", ["volume_down"] = "Volume down",
        ["media_next_song"] = "Next track", ["media_prev_song"] = "Previous track", ["system_mute"] = "System mute",
        ["media_play_pause"] = "Media play / pause"
    };
    public static string[] Keys { get; } = ["Return", "Escape", "Space", "Tab", "Back", "Delete", "Up", "Down", "Left", "Right", "Home", "End", "PageUp", "PageDown",
        .. Enumerable.Range(0, 10).Select(i => "D" + i), .. Enumerable.Range(1, 20).Select(i => "F" + i),
        .. Enumerable.Range('A', 26).Select(i => ((char)i).ToString())];

    public static string Name(string id, RemoteSettings settings) => BuiltIns.GetValueOrDefault(id)
        ?? settings.Actions.FirstOrDefault(a => a.Id == id)?.Name ?? id;

    public static void Execute(string id, RemoteSettings settings)
    {
        var custom = settings.Actions.FirstOrDefault(a => a.Id == id);
        if (custom != null)
        {
            Send(custom.Key, custom.Control, custom.Alt, custom.Shift, custom.Windows);
            return;
        }
        switch (id)
        {
            case "press_enter": Send("Return"); break;
            case "start_dictation": Send("H", windows: true); break;
            case "mute_gchat": Send("D", control: true); break;
            case "press_1": Send("D1"); break;
            case "press_2": Send("D2"); break;
            case "press_3": Send("D3"); break;
            case "volume_up": Send("VolumeUp"); break;
            case "volume_down": Send("VolumeDown"); break;
            case "system_mute": Send("VolumeMute"); break;
            case "media_next_song": Send("MediaNextTrack"); break;
            case "media_prev_song": Send("MediaPreviousTrack"); break;
            case "media_play_pause": Send("MediaPlayPause"); break;
            case "toggle_teams_mute":
                var processes = Process.GetProcessesByName("ms-teams").Concat(Process.GetProcessesByName("Teams")).ToArray();
                try
                {
                    var target = processes.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                    if (target == null || !SetForegroundWindow(target.MainWindowHandle))
                        throw new InvalidOperationException("Teams is not running or its window could not be activated.");
                    Send("M", control: true, shift: true);
                }
                finally { foreach (var process in processes) process.Dispose(); }
                break;
            default: throw new InvalidOperationException("This action no longer exists.");
        }
    }

    private static void Send(string key, bool control = false, bool alt = false, bool shift = false, bool windows = false)
    {
        var keys = new List<ushort>();
        if (control) keys.Add(0x11);
        if (alt) keys.Add(0x12);
        if (shift) keys.Add(0x10);
        if (windows) keys.Add(0x5B);
        keys.Add((ushort)Enum.Parse<System.Windows.Forms.Keys>(key));
        Input Make(ushort code, bool up) => new() { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput
        { VirtualKey = code, Flags = (up ? 2u : 0u) | (IsExtended(code) ? 1u : 0u) } } };
        var inputs = keys.Select(k => Make(k, false)).Concat(keys.AsEnumerable().Reverse().Select(k => Make(k, true))).ToArray();
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            var releases = keys.AsEnumerable().Reverse().Select(k => Make(k, true)).ToArray();
            SendInput((uint)releases.Length, releases, Marshal.SizeOf<Input>());
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not dispatch all keys. Check the foreground app and its privilege level.");
        }
    }

    private static bool IsExtended(ushort key) => key is >= 0x21 and <= 0x28 or 0x2D or 0x2E or 0x5B or >= 0xAD and <= 0xB3;
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    { public ushort VirtualKey, ScanCode; public uint Flags, Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput
    { public int X, Y; public uint MouseData, Flags, Time; public UIntPtr ExtraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
}
