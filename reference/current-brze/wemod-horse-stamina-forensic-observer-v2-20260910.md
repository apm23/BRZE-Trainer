# WeMod Horse/Stamina forensic observer v2 — 2026-09-10

Purpose: obtain runtime evidence from the user's proven Wand/WeMod BRZE 1.60 trainer without guessing horse or stamina fields.

## User-provided cache snapshots
- `Wand_BRZE_trainers1.zip`: user states Unlimited Horses + Unlimited Stamina were active.
- `Wand_BRZE_trainers.zip`: user states Unlimited Horses + Unlimited Stamina + Unlimited Wolves were active.
- Both uploads are 1,829,739 bytes. Equal size alone does not prove equal content; cache artifacts may be static while toggles live only in runtime state.
- Archive parsing is not available in the current long-running tool session, so no claim is made that WLC/DLL contents have been decoded yet.

## Observer v2
Branch: `main-merge-v2-peasant3s-wemod-port`
Source commit: `6b91d5fecceea9e3dceb475004114be47ac6255b`
Workflow validation commit: `2901ec89a35299f0ec705f0c72b225ffbf3a568f`
Workflow: `WeMod Horse Stamina Diff Observer v2`
Run: `34424973632` — SUCCESS
Artifact: `BRZE-WeMod-Horse-Stamina-Forensic-Observer-v2`
Artifact ID: `10132314107`
ZIP digest: `5d787d7eb82464c0e2538ca8e6825ace7445030c5e4aed4a0c0cb0c9708771a5`

## Read-only contract
The observer imports OpenProcess, ReadProcessMemory, CloseHandle only for target access. CI explicitly rejects any `WriteProcessMemory` string in the observer source.

## Capture scope
Each capture records:
- BRZE `.text` section
- every writable PE image section readable from the module
- currently selected building object (`0x6A4` bytes)
- currently selected unit object (`0x818` bytes)
- local player horse/stat block through global RVA `0x4416A4`, stride `0xA0`

## Tri-snapshot method
For each cheat independently:
A. capture with that WeMod cheat OFF
B. enable ONLY that cheat and capture ON
C. disable the same cheat and capture OFF again

The report includes ordinary A->B changes and a stronger reversible filter: bytes where `A == C` but `B != A`. This is especially useful for code patches/detours and toggle fields because unrelated game-state/timer noise is less likely to return exactly to A.

Horse test: keep the same Stable selected for A/B/C.
Stamina test: keep the same local unit selected for A/B/C.
Reports save to Desktop as `BRZE-WeMod-HORSE-Diff-*.txt` and `BRZE-WeMod-STAMINA-Diff-*.txt`.

## Current interpretation
- Do not reintroduce `HorseRespawnTime=0`; user runtime disproved it as Stable-stock unlimited.
- Do not call the AddStamina-only hook complete; user runtime proved run/skill consumption bypasses it.
- Static lead remains `UnitSetStamina` -> central setter around VA `0x5CCE79` / RVA `0x1CCE79`, but it must not be patched blindly before comparison with runtime WeMod evidence.

Next hinge: analyze the two observer text reports, map exact reversible RVA/data changes to the authoritative BRZE 1.60 executable, then port only those horse/stamina semantics into merged v2.