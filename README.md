# HomeApps.Desktop

JARVIS on a computer, next to (not part of) [HomeApps](../HomeApps): the voice bridge and the hologram app (Godot). Plan: HomeApps `plans/features/jarvis-satellites.md`, stage S-2.

- `src/Jarvis.VoiceBridge`: the bridge (wake word, microphone, speaker, login, satellite pairing, computer actions).
  How to run it: [docs/bridge.md](docs/bridge.md).
- `tests/Jarvis.VoiceBridge.Tests`: its tests (`dotnet test`).
- `app/`: the Godot app (hologram window and menu-bar icon) that runs the bridge with `--ipc`:
  [docs/app.md](docs/app.md).

It talks to HomeApps only over HTTP and WebSocket, so it has no reference to HomeApps' code.
