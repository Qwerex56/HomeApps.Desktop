# HomeApps.Desktop

JARVIS on a computer, next to (not part of) [HomeApps](../HomeApps): the voice bridge and the desktop app. Plan: HomeApps `plans/features/jarvis-satellites.md`, stage S-2; the rework to Avalonia is spec `jarvis-desktop-rework` (stage D1 done).

- `src/Jarvis.VoiceBridge.Core`: the voice bridge as a class library (wake word, microphone, speaker, login, satellite
  pairing, computer actions). `VoiceBridgeHost` runs it inside another app and reports its states.
- `src/Jarvis.VoiceBridge`: the bridge as a console app on top of Core (`dotnet run`, `--ipc`, `--pair`, ...).
  How to run it: [docs/bridge.md](docs/bridge.md).
- `src/HomeApps.Desktop`: the Avalonia tray app (menu bar on macOS, system tray on Windows and Linux). It hosts the
  bridge in-process, shows its state in the tray icon and has the global mute hotkey. See [docs/desktop.md](docs/desktop.md).
- `tests/Jarvis.Desktop.Tests` and `tests/Jarvis.VoiceBridge.Tests`: tests (`dotnet test`).
- `app/`: the Godot app (hologram window and menu-bar icon) that runs the bridge with `--ipc`. It is still here and
  will be removed in stage D4 of the rework: [docs/app.md](docs/app.md).

It talks to HomeApps only over HTTP and WebSocket, so it has no reference to HomeApps' code.
