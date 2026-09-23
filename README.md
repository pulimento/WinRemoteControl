# WinRemoteControl

WinRemoteControl is a Windows desktop app that listens to MQTT topics and turns them into useful local actions. It is designed for physical remotes and home-automation systems.For example, I'm using [Home Assistant](https://www.home-assistant.io/, Zigbee, and an [IKEA remote](https://www.ikea.com/us/en/p/tradfri-remote-control-00443130/), but any MQTT publisher can control it.

## Features

- Connect to any MQTT broker with optional username and password authentication.
- Configure MQTT topics and actions from the app; no hand-editing is required for normal setup.
- Control system volume, media playback, Microsoft Teams mute, and simulated `1`, `2`, or `3` key presses.
- Start and stop the MQTT client from the main window; stopping also cancels reconnect attempts.
- Run from the notification area and optionally start minimized or at Windows sign-in.
- Check for application updates from the About window.

## Installation

An Inno Setup-generated installer is available from the [Releases](https://github.com/pulimento/WinRemoteControl/releases) page.

The installer is per-user and does not require administrator rights. To build the application from source instead, open `WinRemoteControl.sln` in Visual Studio and build or publish the project.

## Quick start

1. Start WinRemoteControl.
2. Select **Connection settings**.
3. Enter a unique MQTT client ID, broker host name or IP address, port, and credentials if your broker requires them.
4. Review or change the MQTT topic/action mappings, then select **Save**.
5. Select **Start** in the main window.
6. Publish a message to one of the configured topics. The message payload is not used; publishing to the topic triggers its mapped action.

The app creates `settings.json` alongside its executable. `settings_example.json` in the same folder shows the underlying format and default mappings.

## Default MQTT mappings

| Topic | Action |
| --- | --- |
| `control/toggle_teams_mute` | Toggle Microsoft Teams mute |
| `control/volume_up` | Increase system volume |
| `control/volume_down` | Decrease system volume |
| `control/media_next_song` | Next media track |
| `control/media_prev_song` | Previous media track |
| `control/press_1` | Press `1` |
| `control/press_2` | Press `2` |
| `control/press_3` | Press `3` |

Topics must be unique. You can change topic names and assign the available actions in **Connection settings**.

## Application settings

Use **Open config** in the main window to manage application behavior:

- Launch WinRemoteControl at Windows sign-in.
- Connect to MQTT automatically at startup.
- Start minimized in the notification area.

Select **To tray** to hide the window; click the notification-area icon to restore it.

## Updates

Use the **About** window to check for updates. Or install a newer version on top of it

## Third-party software

- [MQTTnet](https://github.com/dotnet/MQTTnet)
- [Autoupdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)
- [Inno Setup](https://jrsoftware.org/isdl.php)
