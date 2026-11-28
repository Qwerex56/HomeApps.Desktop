# Installing the tray app

Packages are built by the `Release` workflow when a version tag is pushed (`git tag v1.2.3 && git push origin v1.2.3`;
the tag without `v` becomes the app version). They are self-contained: no .NET install is needed. They do **not**
contain the speech models.

## Download

Open the repository's **Releases** page and take the file for your system:

| System | File |
|---|---|
| Windows 10/11 x64 | `HomeApps.Desktop-<version>-win-x64.zip` |
| macOS, Apple silicon | `HomeApps.Desktop-<version>-osx-arm64.zip` |
| macOS, Intel | `HomeApps.Desktop-<version>-osx-x64.zip` |
| Linux x64 | `HomeApps.Desktop-<version>-linux-x64.tar.gz` |

Unpack it anywhere (macOS: drag `HomeApps.Desktop.app` to Applications) and start `HomeApps.Desktop`.

## Models (Whisper and Piper)

Speech is recognised and spoken on this computer, so the models must be downloaded once. The app looks for them in
`~/.homeapps/voice-bridge/models` (Windows: `%USERPROFILE%\.homeapps\voice-bridge\models`). Which files and where to
get them: [bridge.md](bridge.md). Piper also needs `espeak-ng` on the `PATH` (Windows: install it, then open a new
session).

## First run

The icon appears in the menu bar (macOS) or the system tray. The browser opens once to sign in to HomeApps and ask for
consent. The mute hotkey is Ctrl+Alt+M. More: [desktop.md](desktop.md).

## macOS: Gatekeeper and permissions

The app is signed ad hoc, not notarized, so macOS refuses a normal double-click the first time. **Right-click the app,
choose Open, then Open again.** (If it still says the app is damaged: `xattr -dr com.apple.quarantine
/Applications/HomeApps.Desktop.app`.) Then allow, in System Settings, Privacy & Security: **Microphone** (asked on first
use), **Accessibility** and **Input Monitoring** (for the mute hotkey). After an update, permissions may have to be
granted again because the ad hoc signature changes.

## Windows and Linux

Windows may show SmartScreen ("More info", then "Run anyway"); the exe is not signed. The microphone needs access under
Settings, Privacy, Microphone. Linux needs a tray that supports the StatusNotifier/AppIndicator protocol (on GNOME, the
AppIndicator extension), PulseAudio or PipeWire, and `aplay`.

## Autostart

- **macOS:** System Settings, General, Login Items, add `HomeApps.Desktop.app` under "Open at Login".
- **Windows:** press Win+R, run `shell:startup`, and put a shortcut to `HomeApps.Desktop.exe` there.
- **Linux:** create `~/.config/autostart/homeapps-desktop.desktop`:

  ```ini
  [Desktop Entry]
  Type=Application
  Name=HomeApps Desktop
  Exec=/path/to/HomeApps.Desktop/HomeApps.Desktop
  ```

## Building a package locally

`scripts/package-macos.sh <version> [osx-arm64|osx-x64]`, `scripts/package-linux.sh <version>` and
`pwsh scripts/package-windows.ps1 -Version <version>` write into `dist/` (git-ignored).
