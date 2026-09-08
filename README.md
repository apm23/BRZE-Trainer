# BRZE Trainer

Offline/single-player trainer for **Battle Realms: Zen Edition 1.60**.

## Target hotkeys
- F1 Infinite Rice
- F2 Infinite Water
- F3 Infinite Yin + Yang
- F4 Unlimited Population
- F5 Infinite Stamina — currently selected local units only
- F6 HP Lock — currently selected local units only
- F7 Instant Unit Training
- F8 Instant Building
- F9 Master toggle

## Proven runtime data (BRZE 1.60)
- Local player id RVA: `0x4416D0`
- Player array pointer RVA: `0x4416A0`
- Player struct stride: `0x5E8`
- Rice: `player+0xD8`
- Water: `player+0xDC`
- Yin: `player+0x2E8`
- Yang: `player+0x2EC`
- Max-units array RVA: `0x467B90`
- Unit pool pointer RVA: `0x4796A0`
- Unit stride: `0x818`
- Unit owner: `unit+0x240`
- Selected flags: `unit+0x3A8 == 1` and `unit+0x3AC == 1`
- Current HP: `unit+0x404` (16.16 fixed point)
- Current stamina: `unit+0x408` (16.16 fixed point)
- Definition pointer: `unit+0x74`
- Base max HP: `[unit+0x74]+0x6C`
- Base max stamina: `[unit+0x74]+0x80`

`unit+0x6A4` is intentionally blacklisted because it causes visibility/fog side effects.

## Build goal
Produce a lightweight native Windows `.exe` via GitHub Actions, so the trainer can be launched directly without PowerShell.
