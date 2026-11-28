| Feature | Stage | State (todo/doing/done/blocked) | Branch | ADR | Updated |
|---|---|---|---|---|---|
| jarvis-desktop-rework | D1 | done | feat/jarvis-desktop-rework-d1 | ../HomeApps/specs/jarvis-desktop-rework-adr.md | 2026-11-27 |
| jarvis-desktop-rework | D2 | done (PR pending) | feat/jarvis-desktop-rework-d2 | ../HomeApps/specs/jarvis-desktop-rework-adr.md | 2026-11-27 |
| jarvis-desktop-rework | D3 | done (PR pending) | feat/jarvis-desktop-rework-d3 | ../HomeApps/specs/jarvis-desktop-rework-adr.md | 2026-11-28 |
| jarvis-desktop-rework | D4 | done (PR pending; Godot retired by owner decision 2026-11-28; packaging of the tray app is still missing now that package-*.sh are gone) | feat/jarvis-desktop-rework-d4 | ../HomeApps/specs/jarvis-desktop-rework-adr.md | 2026-11-28 |
| jarvis-desktop-rework | D6 | done (PR pending) | feat/jarvis-desktop-rework-d6 | ../HomeApps/specs/jarvis-desktop-rework-adr.md | 2026-11-28 |
| jarvis-desktop-rework | D5 | done (PR pending; release workflow not run on GitHub yet) | feat/jarvis-desktop-rework-d5 | ../HomeApps/specs/jarvis-desktop-rework-adr.md | 2026-11-28 |

## Decisions
- 2026-11-27: global mute hotkey via SharpHook (Ctrl+Alt+M); bridge hosted in-process in HomeApps.Desktop; Godot removal stays in D4.

- 2026-11-27 (owner): espeak-ng accepted as a system dependency; AEC is a level gate, tuned on hardware later.

## Blockers
- Packaging scripts publish only the bridge, not the tray app (decide before shipping).
