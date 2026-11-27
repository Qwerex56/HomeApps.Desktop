| Feature | Stage | State (todo/doing/done/blocked) | Branch | ADR | Updated |
|---|---|---|---|---|---|
| jarvis-desktop-rework | D1 | done | feat/jarvis-desktop-rework-d1 | ../HomeApps/specs/jarvis-desktop-rework-adr.md | 2026-11-27 |
| jarvis-desktop-rework | D2 | todo | | | |
| jarvis-desktop-rework | D3 | todo | | | |
| jarvis-desktop-rework | D4 | todo | | | |

## Decisions
- 2026-11-27: global mute hotkey via SharpHook (Ctrl+Alt+M); bridge hosted in-process in HomeApps.Desktop; Godot removal stays in D4.

## Blockers
- Packaging scripts publish only the bridge, not the tray app (decide before shipping).
