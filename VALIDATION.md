# Validation

Run on Windows with the .NET 10 SDK:

```powershell
dotnet build WinRemoteControl.sln -c Release
dotnet run --project WinRemoteControl.Tests -c Release -- --ui --mqtt
backlog check
```

The test executable uses isolated temporary configuration directories and an ephemeral loopback MQTT broker. It substitutes an action executor, so it does not send keystrokes, activate Teams, change volume, edit your startup registry entry, or use your real broker/settings. UI checks render each settings tab into PNG files in the reported test directory.

Coverage includes legacy migration, preservation of deleted/empty mappings, validation and atomic save backups, YAML schema/platform validation, credential exclusion, profile replacement and preview, history persistence/bounds, retained messages, disabled control, command expiry, duplicate suppression across reconnects, queued-message invalidation on Stop, reconnect subscriptions, simulated power/network events, and rapid Start/Stop. UI checks cover retaining edits across tabs, Save, and Revert.

Implementation verification (2026-09-25): Release build passed without warnings; the combined UI/MQTT suite passed 81 assertions. The General, Actions, Mapping Profiles, History, and About renders were visually inspected. CI runs the non-UI suite with the loopback broker.

Follow-up verification: both Debug and Release passed 94 assertions, including exact default topic order, four editable default actions, Windows GChat modifiers, recovery profiles, build-specific display/assembly names and icon resources, and the shared data path. The Debug icon now uses a deterministic pixel tint of each original Release icon frame; original Release icon files are unchanged.

Manual validation still required on a Windows desktop:

- Open Settings at 100%, 150%, and 200% scaling; check all six tabs, profile preview, long history failures, and window resizing.
- Test Enter, 0–9, F1–F20, modifier combinations, system mute, volume, and media keys against suitable applications.
- Focus an editable field, use Voice typing, and verify Windows microphone/voice-typing setup. The action sends Win+H; it cannot confirm dictation state.
- Test Ctrl+D with the intended GChat/Meet meeting focused, and Teams mute with Teams running.
- Toggle launch at sign-in, auto-connect, and start minimized independently. Sign out/in to check actual startup behavior.
- With a real broker, sleep and wake the PC and change network interfaces. Confirm reconnection while enabled and no reconnect after explicit Stop. Automated tests simulate notifications without sleeping the PC.
- Close Settings with unsaved changes; cancel and confirm discard. Import/export via native file dialogs, preview/apply a saved profile, and check About update checking.

Deferred feature IDs are recorded in `backlog/tasks/`; they are not acceptance requirements for this implementation.
