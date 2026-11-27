# The desktop tray app

`src/HomeApps.Desktop` is an Avalonia (.NET 10) app that lives in the menu bar (macOS) or the system tray. It runs the
voice bridge in-process through `Jarvis.VoiceBridge.Core` (`VoiceBridgeHost`), so no separate bridge process is needed
for it. The tray icon shows the bridge's state (connecting, idle, listening, thinking, speaking, muted).

```bash
dotnet run --project src/HomeApps.Desktop
```

- **Mute hotkey: Ctrl+Alt+M.** Toggles the microphone (same as muting in the tray menu). It works system-wide, also
  when no window of the app has focus. Either Ctrl and either Alt key work.
- **macOS permissions:** the hotkey needs **Accessibility** and **Input Monitoring** for the app (System Settings →
  Privacy & Security). Without them the hotkey does nothing; the app logs the failure. The microphone permission is
  asked for as usual.
- **Reduced motion:** the tray animations follow the system's "reduce motion" setting. To override it, set
  `HOMEAPPS_REDUCE_MOTION=1` (motion off) or `HOMEAPPS_REDUCE_MOTION=0` (motion on) before starting the app; any
  other value is ignored and the system setting is used.
- **Godot app:** the older Godot app in `app/` is still in the repo and works as described in [app.md](app.md); it
  is removed in stage D4 of the rework.
