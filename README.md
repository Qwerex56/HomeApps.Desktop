# HomeApps.Desktop

JARVIS on a computer, next to (not part of) [HomeApps](../HomeApps): the voice bridge today, the hologram app
(Godot) later. Plan: HomeApps `plans/features/jarvis-satellites.md`, stage S-2.

- `src/Jarvis.VoiceBridge`: the bridge (wake word, microphone, speaker, login, satellite pairing, computer actions).
  How to run it: [docs/bridge.md](docs/bridge.md).
- `tests/Jarvis.VoiceBridge.Tests`: its tests (`dotnet test`).

It talks to HomeApps only over HTTP and WebSocket, so it has no reference to HomeApps' code.
