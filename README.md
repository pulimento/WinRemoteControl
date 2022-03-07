# WinRemoteControl

This app has the main goal to be able to execute some actions on a Windows computer, when subscribed to a MQTT server.

In this example, I'm using [Home Assistant](https://www.home-assistant.io/) automations, Zigbee and a [IKEA remote](https://www.ikea.com/us/en/p/tradfri-remote-control-00443130/) to trigger messages via MQTT.

Current actions include turn the system volume up and down, and toggling Microsoft Teams self-mute.

You will need an MQTT server to connect to. If you have one, you can use anything to publish to a certain MQTT topic.

## How to install

An Inno Setup-generated installer is available from the [Releases](https://github.com/pulimento/WinRemoteControl/releases) section.

Alternatively, you can download and run the code using Visual Studio or equivalent.

## Usage

Settings are stored on a JSON file in the same folder of the executable, edit it with your configuration before starting the server.

## Some third party software used

- [MQTTNet](https://github.com/dotnet/MQTTnet)
- [InnoSetup](https://jrsoftware.org/isdl.php)
