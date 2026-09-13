# BRZE Hero Effect Lab — STATE

Status: **BUILD/STATIC PROVEN — RUNTIME PENDING**
Date: 2026-09-14 JST

## Goal
Apply Grayback Wolf's Howl target-side buff and Issyl Haste target-side buff to all currently selected units through BRZE's native ability path.

## Native chain
- Source UnitDef: BattleGear1 `+0x14C`, BattleGear2 `+0x150`, BattleGear3 `+0x154`.
- BattleGear table: base pointer RVA `0x43FE34`, id map RVA `0x43FF3C`, stride `0x38`.
- BattleGear record `+0x0C` = `AbilityType` (confirmed from `Data_BattleGear` parser).
- Ability table: base pointer RVA `0x43FE28`, id map RVA `0x43FF30`, stride `0x2E4`.
- AbilityDef `+0x288` = `CreateMagicAtTarget` (confirmed from `Data_Abilities` parser).
- Native target-side apply helper: preferred VA `0x5F0C32`, RVA `0x1F0C32`.
- Selection list RVA `0x441708`; max 120 selected units.
- Apply work executes on the BRZE render/game thread via frame hook RVA `0x135C43`, max 4 selected units per frame.
- No raw HP/stamina/movement/attack-speed writes and no CreateRemoteThread.
- FXSAVE/FXRSTOR + pushfd/pushad preserve CPU/FPU/SSE state.
- Hook restore is ownership-checked before restoring stock bytes.

## UI/runtime flow
1. Close the main trainer and Unit Clone Lab so the shared frame-hook site is stock.
2. Select Grayback and press `CAPTURE SELECTED GRAYBACK`.
3. Select Issyl and press `CAPTURE SELECTED ISSYL`.
4. Each capture resolves the hero's live BattleGear entries and displays `gear -> root AbilityType -> target AbilityType`.
5. Select target units (up to 120).
6. Press `APPLY GRAYBACK TO SELECTED`, `APPLY ISSYL TO SELECTED`, or `APPLY BOTH TO SELECTED`.

The combo box automatically selects the first BattleGear entry whose `CreateMagicAtTarget` is valid, but all resolved BG slots are visible for manual choice if a hero exposes more than one.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `6955844d97405f1d35301dd6e25093e562afb2ee`
Workflow: `Hero Effect Lab Native Grayback Issyl`
Run: `34769032391`
Job: `103755098395`
Artifact: `10320254697`
Artifact digest: `sha256:052c6ccaea52a098970c31221e6dff7f087e1b9233a6a6a0bfa400f92c5df4e9`

Binaries:
- Standalone: 66,005,146 bytes, SHA-256 `0316e3bf9f3651a769f1111275fdb925c1c2d3cff5d227e274d9b83734dde62f`
- Small: 153,234 bytes, SHA-256 `4d1413eab38912f1dfa7a6a31f06a25a7da489749a704dba264f6a211f67801b`

## Runtime verdict needed
The first proof should test `APPLY BOTH TO SELECTED` on a visible group (for example 20–30 Spearmen) after capturing Grayback and Issyl. Confirm separately whether Wolf's Howl damage buff appears/behaves correctly, Issyl Haste visibly speeds movement/attack/recovery, both stack on the same units, and the game remains stable after the effects expire.
