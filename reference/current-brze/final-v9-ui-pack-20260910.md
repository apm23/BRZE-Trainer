# BRZE Trainer FINAL V9 UI Pack — 2026-09-10

## Locked runtime-proven cores preserved
- Instant Death hover targeting: V6 architecture, runtime proven.
- Instant Death BURST / ERASER + SINGLE: runtime proven.
- Reveal Map ON/OFF: runtime proven.
- Unlimited Wolves: V8 Wolves Den local-owner/type-0x44 latched stock byte +0x250=250, runtime proven.
- Rejected V5 global death pointer and V7 per-unit wolf-cap hook are explicitly forbidden by CI.

## Final UI
- Fixed 1130x670 WinForms layout.
- Exactly two switch rows, six switches each.
- Row 1: Rice, Water, Yin/Yang, Population, Training, Peasant 3s.
- Row 2: Health, Stamina, Horses, Wolves, Reveal, Death Burst.
- Custom rounded switch controls with visible ON/OFF state.
- Action bar: SINGLE KILL, BUILD NOW, ALL ON, ALL OFF.
- F1..F7 legacy hotkeys preserved where applicable; F10 Wolves; PageDown Single Kill; PageUp HP+Stamina; Delete/F8 Build; F9 master toggle.
- All telemetry moved into one central SYSTEM STATUS box.
- Legacy experimental Watchtower/Demolition UI removed from final surface.

## Background/readability
Background is procedural Battle-Realm-style art rendered by WinForms (gradient dusk, moon glow, mountain and temple silhouettes). No external bitmap is embedded, keeping the binary small. All controls/status surfaces use solid dark high-contrast cards so background cannot cover or obscure text.

## Final build pin
- branch: `instant-death-v4-hover-telemetry`
- head: `04d51241423bdb3eb4a5da92e3ccdb4c6485c272`
- workflow: `Final V9 Pack`
- run: `34451899885` SUCCESS
- job: `102789297707` SUCCESS
- artifact: `10141910073` (`BRZE-Trainer-FINAL-V9-Pack`)

## Sizes and hashes
Standalone compressed single-file:
- `BRZE-Trainer-FINAL-V9-Standalone.exe`
- 66,020,277 bytes (~62.96 MiB)
- SHA-256 `496599acbe5a09c4db38721ba621ad59db6a670cfe4885574c5bb0f57cba5f79`
- self-contained, no .NET Desktop Runtime installation required.

Small framework-dependent single-file:
- `BRZE-Trainer-FINAL-V9-Small.exe`
- 193,182 bytes (~0.18 MiB / ~189 KiB)
- SHA-256 `61917877a1c8df26669b97de9e3b4b8e2c57ee39b54e3b66995981f20dd6113d`
- requires compatible .NET 8 Windows Desktop Runtime x86 on the PC.

Decision: Standalone is the default no-hassle build. Small is the ultra-compact optional build. Do not switch to unsafe packers merely to reduce the standalone size unless a new explicit requirement outweighs loader/AV reliability.

## Runtime state
The core cheats above are runtime-proven from V8 and earlier. V9 changes UI/layout/publish packaging; final visual/layout regression still needs one user launch to confirm the interface renders as intended on their Windows desktop.
