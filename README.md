# WinRemoteControl

A Windows tray application that turns MQTT topics into keyboard, media, and system-volume actions. It works with physical remotes, Home Assistant, or any MQTT publisher.

## Settings

The main window has one **Settings** button. Its six tabs are:

- **General**: MQTT connection, independent launch-at-sign-in, connect-at-startup, and start-minimized options.
- **Mappings**: add, edit, and delete exact MQTT topics and select their actions.
- **Actions**: create named keyboard shortcuts and test built-in or saved custom actions.
- **Mapping Profiles**: save snapshots, import/export Windows YAML profiles, and preview replacements before applying them.
- **History**: the latest 100 commands, retained across restarts, with time, action, source, trigger, foreground target, dispatch result, and failure reason.
- **About**: version, repository link, and update checking.

Mapping, action, and General edits survive tab switches. **Save** commits them together; **Revert** restores saved values. Closing with edits asks before discarding. Changing saved connection settings or mappings reconnects an already-started listener. Connect uses saved settings.

**Enabled** in the main window blocks or permits actions without disconnecting MQTT. Its value survives restarts. **Stop** disconnects and cancels reconnection; sleep/wake and network changes respect that choice.

## Quick start

1. Open Settings → General; enter the broker, port, unique client ID, and optional credentials.
2. Review Mappings and click Save.
3. Click Connect in General or Start in the main window.
4. Publish to a mapped topic. Ordinary message payloads do not select the action.

The six default mappings match the Mac app's topics, using Windows actions. As on Mac, Press 1, Press 2, Press 3, and GChat are four editable custom actions included in the Default profile:

| Topic | Windows action |
| --- | --- |
| `control/press_enter` | Enter |
| `control/start_dictation` | Voice typing, Win+H |
| `control/press_1` | Press 1 |
| `control/press_2` | Press 2 |
| `control/press_3` | Press 3 |
| `control/mute_gchat` | Ctrl+D in the focused meeting |

Voice typing requires Windows voice-typing/microphone setup and focus in a text field. It sends the [Windows voice-typing shortcut](https://support.microsoft.com/en-us/accessibility/windows/use-voice-typing-to-talk-instead-of-type-on-your-pc), rather than observing dictation state. GChat mute uses the [Google Meet Windows shortcut](https://support.google.com/meet/answer/15738543?hl=en); it does not locate or activate a meeting automatically.

Other built-in actions remain available: Teams mute, volume up/down, next/previous track, plus system mute and media play/pause. Custom keyboard actions support letters, digits, F1–F20, navigation keys, and Ctrl/Alt/Shift/Windows modifiers. Add a custom action using the blank Actions row; select a row and press Delete to remove it. Remove its mappings too before saving.

Action tests wait three seconds so you can focus the test field or another application. Tests respect Enabled and appear in History. Dispatched means Windows accepted input, not that the target application reached a confirmed state.

## Profiles

A profile snapshots every saved mapping and custom action, including unmapped actions. Save or revert edits before creating or applying a profile. **Default** is protected from deletion and overwriting.

**Preview & Apply** shows added, removed, and changed mappings/actions. Applying explicitly replaces the complete active mapping/action set; connection details, startup options, Enabled, and saved profiles remain unchanged.

YAML uses `schemaVersion: 1` and `platform: Windows`. Profiles exclude host, credentials, and client ID. Mac profiles are not supported, and no keyboard-code translation is attempted. Imported profiles are saved as independent snapshots and are not automatically activated. Unsupported versions, unknown fields, duplicate keys/topics, missing actions, and unsupported keys are rejected.

## Configuration and migration

Runtime data lives in `%LOCALAPPDATA%\WinRemoteControl\`:

- `Settings.json`: connection, preferences, mappings, custom actions, and saved profiles.
- `History.json`: at most 100 command records, without payloads or command IDs.
- `.bak` siblings: previous versions retained by atomic saves.
- `legacy-settings.json`: backup of an imported installation-directory configuration.

On first use, an existing `settings.json` beside the executable is imported without changing the original. Existing mappings are preserved exactly, including an intentionally empty list. Older configurations without a mappings field retain their original eight Windows defaults. Existing startup preferences are imported too. Subsequent launches use the per-user settings, and applying Default is an explicit replacement operation.

New installations receive the six defaults above. `settings_example.json` documents the legacy connection-object format; it is not the active per-user settings file. Invalid settings are reported without silently resetting them. Credentials remain local in Settings.json; keep this directory private.

## Debug and Release

Release keeps the existing **WinRemoteControl** name and icon. Debug is **WinRemoteControl DEBUG**, with a magenta-tinted version of that same original icon in the executable, window, Settings/About, and notification area. Each original icon size retains its geometry, transparency, and neutral border; no badge or replacement symbol is added. Both use the same `%LOCALAPPDATA%\WinRemoteControl` configuration and history. A shared single-instance guard prevents Debug and Release from running concurrently with competing MQTT clients or settings writes.

Build and open Debug Settings with:

```powershell
dotnet build WinRemoteControl/WinRemoteControl.csproj -c Debug
./scripts/StartDebug.ps1
```

The script gracefully closes an earlier Debug instance, including one hidden in the tray; it never force-kills unsaved edits. To explicitly replace active mappings/actions with Default while saving the previous set as a recovery profile, run `./scripts/StartDebug.ps1 -ApplyDefaultProfile`. This uses the application's `--apply-default-profile` option. `--settings` opens Settings on startup.

Regenerate only the Debug icon with `./scripts/GenerateDebugIcon.ps1`, then use `./scripts/StartDebug.ps1 -Rebuild` to gracefully close, rebuild, and restart Debug. Release icon assets are unchanged. When switching build configurations, allow the normal NuGet restore step so project-reference metadata follows the Debug assembly name.

## MQTT command handling

Retained MQTT messages never execute. Optional metadata adds expiry and duplicate protection:

```json
{"winRemoteCommand":1,"commandID":"unique-press-id","timestamp":"2026-09-25T12:00:00Z"}
```

Use a fresh ID per press and synchronized clocks. The Mac sender envelope marker `macRemoteCommand` is also accepted for existing button publishers; this does not enable Mac profile sharing. Commands older than 30 seconds or over 5 seconds in the future are rejected. IDs are limited to 128 UTF-8 bytes and remembered per source/topic for 10 minutes, with a maximum of 512 IDs. The duplicate cache survives reconnects, but not application restarts. Payloads over 4096 bytes are rejected. Ordinary topic-only senders use receipt time and cannot be deduplicated without an ID.

Connection replacement is serialized, stale callbacks are ignored, and queued commands from stopped sessions are discarded. Sleep releases the MQTT connection; wake and network changes reconnect only while connection is requested. No sleep-prevention mechanism is used. The current status is **Connected**; subscription-acknowledgement readiness diagnostics are deferred.

## Build, validation, and backlog

Open `WinRemoteControl.sln` in Visual Studio or use the .NET 10 SDK:

```powershell
dotnet build WinRemoteControl.sln -c Release
dotnet run --project WinRemoteControl.Tests -c Release -- --ui --mqtt
```

See [VALIDATION.md](VALIDATION.md) for automated coverage and desktop checks. Tests use isolated settings and a loopback broker, with input execution replaced by a test double.

Deferred Mac features are tracked as T0001–T0009 in [backlog/tasks](backlog/tasks). Use `backlog list` and `backlog check`.

Installers are available from [Releases](https://github.com/pulimento/WinRemoteControl/releases). The installer is per-user and does not require administrator rights. `GenerateInstaller.ps1` builds the installer.

Dependencies include MQTTnet, YamlDotNet, AutoUpdater.NET, FluentResults, and Serilog; packaging uses Inno Setup.
