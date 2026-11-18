# The JARVIS app (S-2c, S-2d)

`app/` is a Godot 4.7 project (GDScript, GL Compatibility): JARVIS's face on the computer. It does no listening or
talking itself; it starts the bridge with `--ipc` ([bridge.md](bridge.md)) and shows what the bridge reports.

- **Window:** 280×280, borderless, transparent, always on top, never takes focus and lets every click through (S5),
  in the bottom-right corner of the screen. It is empty until JARVIS listens, thinks or speaks.
- **Hologram (S-2d):** an orb shader (`scenes/hologram/hologram.gdshader`) with a bright rim, moving scan lines and
  a short glitch now and then. It pulses with JARVIS's voice (`level`), sends arcs round the rim while he thinks,
  fades in at `listening` and out 1.2 s after he goes back to `idle`. Colour, size and effects are shader
  uniforms, editable on the `Orb` node's material.
- **Menu-bar icon:** a left click is "Mów" (like Enter); the menu has Mów, Wycisz mikrofon and Wyjdź. The tooltip
  says what JARVIS is doing, or what is wrong.
- **The bridge:** started from `data/settings/bridge_settings.tres` (server, dev certificate, local speech, path,
  extra arguments). Its log appears in Godot's output as `[most] …`. When it dies it is started again, at most 5
  times; quitting the app closes it.

## Run it in dev

```bash
dotnet build src/Jarvis.VoiceBridge          # the app starts the built bridge, not dotnet run
# then open app/project.godot in Godot 4.7 and press Play (F5)
```

Rebuild the bridge after pulling bridge changes: an older build without `--ipc` still runs, but the app only logs
its text and the hologram stays dark. Opening the project in the editor once imports the icon and the scripts'
class cache; without that a plain `godot --path app` run cannot find them.

The first time, the bridge opens the browser to sign in (or uses `--pair` from the terminal beforehand: the app
starts whatever the bridge is paired as). The defaults point at `https://app.localhost`; for production change
`server` and turn `allows_dev_certificate` off in `bridge_settings.tres`.

Known limits: on macOS the app still has a Dock icon while run from the editor (`LSUIElement` comes with the export,
S-2f); on Linux with Wayland "always on top" and transparency may not work (S8).
