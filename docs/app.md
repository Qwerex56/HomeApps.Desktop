# The JARVIS app (S-2c to S-2f)

`app/` is a Godot 4.7 project (GDScript, GL Compatibility): JARVIS's face on the computer. It does no listening or
talking itself; it starts the bridge with `--ipc` ([bridge.md](bridge.md)) and shows what the bridge reports.

- **Window:** 320×420, borderless, transparent, always on top, never takes focus and lets every click through (S5),
  in the bottom-right corner of the screen. It is empty until JARVIS listens, thinks or speaks.
- **Hologram (S-2d):** an orb shader (`scenes/hologram/hologram.gdshader`) with a bright rim, moving scan lines and
  a short glitch now and then. It pulses with JARVIS's voice (`level`), sends arcs round the rim while he thinks,
  fades in at `listening` and out 1.2 s after he goes back to `idle`. Colour, size and effects are shader
  uniforms, editable on the `Orb` node's material. To tune it without the bridge, run
  `scenes/hologram/hologram_preview.tscn` (F6): it plays a whole turn in a loop with a fake voice level.
- **Subtitles:** under the orb, what JARVIS heard (`heard`, dimmer) and then his answer (`reply`), at most 4 lines;
  cleared when a new turn starts. Turn them off with `shows_captions` on the Hologram node.
- **Menu-bar icon:** a left click is "Mów" (like Enter); the menu has Mów, Wycisz mikrofon, Ustawienia…, Uruchom
  ponownie, Zaloguj ponownie and Wyjdź. The tooltip
  says what JARVIS is doing, or what is wrong.
- **Settings (S-2e):** `~/.homeapps/hologram/settings.cfg`, written with the defaults from
  `data/settings/bridge_settings.tres` on the first run: server, dev certificate, local speech and its engine
  (on by default, S1: Speaches on `localhost:8000`), the computer's name, the bridge's path and extra arguments.
  An exported app's first file points at `https://home-apps.dev` instead. "Ustawienia…" in the menu shows the file in Finder; "Uruchom ponownie" starts the bridge again with your edits.
- **Account (S-2e):** the bridge signs in in the browser (OAuth with PKCE) and shows up in "Połączone aplikacje" as
  "JARVIS na komputerze", and in Konto → Komputery as this computer (`--link`). "Zaloguj ponownie" forgets the login
  (and a satellite pairing) on this computer and signs in again.
- **The bridge:** started with those settings. Its log appears in Godot's output as `[most] …`. When it dies it is started again, at most 5
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
`server` and turn `allows_dev_certificate` off in `~/.homeapps/hologram/settings.cfg`.

## Package (S-2f)

Both need .NET 10 and Godot 4.7 with its export templates (Editor → Manage Export Templates → Download, once).
The bridge goes in self-contained, so the person installing it needs no .NET.

```bash
scripts/package-macos.sh     # build/macos/JARVIS.app (Apple silicon), GODOT=/path/to/Godot for another Godot
scripts/package-linux.sh     # build/linux/jarvis-linux-x86_64.tar.gz, GODOT=godot by default
```

- **macOS:** the bridge sits in `JARVIS.app/Contents/Resources/bridge`. The script sets `LSUIElement` (no Dock
  icon, only the menu-bar icon) and the microphone question, then signs everything ad hoc: no Apple Developer account,
  so on another Mac the first start is right-click → Otwórz. Copy it to `/Applications`. macOS asks for the
  microphone on the first "Hey Jarvis" listening, on behalf of JARVIS (the bridge is its child process).
- **Linux:** unpack anywhere, e.g. `~/.local/share/jarvis`, and run `jarvis/jarvis.x86_64`; the bridge sits in
  `jarvis/bridge`. Playing sound needs `aplay` (alsa-utils).
- **Start at login:** "Uruchamiaj przy logowaniu" in the menu writes a LaunchAgent
  (`~/Library/LaunchAgents/dev.home-apps.jarvis.plist`) or an XDG entry (`~/.config/autostart/jarvis.desktop`) for
  the app where it is now; untick it before moving the app. Greyed out when run from the editor.
- **Windows:** later (S8).

Known limits: run from the editor the app still has a Dock icon (only the package sets `LSUIElement`); on Linux
with Wayland "always on top" and transparency may not work (S8).
