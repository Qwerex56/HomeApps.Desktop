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
- **Speech:** the app uses the bridge's defaults ([bridge.md](bridge.md)): speech is recognised and spoken on this
  computer (Whisper and Piper), so the audio never leaves it, and only text goes to the server. The models live in
  `~/.homeapps/voice-bridge/models` and are not in git; they must be downloaded first, and Piper needs `espeak-ng` on
  the `PATH` (Windows: install it and open a new session).
- **Answers and follow-up:** after every answer the microphone stays open for 8 s without the wake word. While JARVIS
  speaks, only "stop" or "cisza" interrupts him, checked against the playback by a level gate (constants in
  `EchoGate.cs`; change them and rebuild).
- **Tray menu:** "Historia rozmów" opens the history window (title "JARVIS: historia rozmów"): the conversations on
  the left, messages on the right. Filters: the search box (Enter, searches the server), the day picker, and the channel
  (all, voice, text, computer); "Wyczyść filtry" resets them. The window can also send a message to JARVIS.
- **Confirmations (D8):** when JARVIS asks to confirm an action, a small window "JARVIS prosi o potwierdzenie" with
  **Tak** and **Nie** stays on top of other windows. Closing it without an answer leaves the action pending, and it can be answered later in the history window ("czeka na potwierdzenie"). A spoken
  "tak" or "nie" is NOT an answer in the tray app: it is ignored, so the yes/no is always given on the screen (in the
  history window too). Native system notifications are deferred and not used yet.
- **Experimental computer tools (2.10):** tray menu "Funkcje eksperymentalne" has six switches, all off by default: open an
  app, open a page (https only), media control, "what is playing" (read-only), volume, timer/notification. A switch changes
  only after you answer Tak in the confirmation window; the choice is kept in `~/.homeapps/desktop/experimental.json`.
  A tool that is off is not announced to the server, so JARVIS does not see it; the server also refuses computer tools
  for satellites and external clients (docs/jarvis.md in HomeApps). macOS uses `osascript`/`open`; Windows uses PowerShell
  (media keys through `keybd_event`; "what is playing" reads Spotify's window title). No clipboard, screenshots or system
  shortcuts. Windows is not tested on a real PC yet. Linux has none of these tools.
- **Sign-in:** the app signs in to HomeApps as the first-party OAuth client `homeapps-desktop`; the browser asks for
  consent on the first run and the token is kept on this computer.
- **Busy on another computer:** when the server reports that the turn is held by another computer of the same person,
  the app goes silent and idle at once.
- **Playback:** `afplay` on macOS, PowerShell's `SoundPlayer` on Windows, `aplay` on Linux.
- **Godot app:** retired (owner decision 2026-11-28); the `app/` folder and its packaging scripts are gone. Packages are built from version tags (D5).
- **Install:** release packages and autostart: [install.md](install.md).
