# BRZE Hero Effect Replay V2 — STATE

Status: **BUILD/STATIC PROVEN — RUNTIME PENDING**
Date: 2026-09-14 JST

## Runtime-derived mapping
Captured by `HeroEffectSniffer` from original skills, in user test order:
- Grayback native runtime ability: `0xC0`
- Issyl native runtime ability: `0xA5`

Both were observed traversing the native MAGIC CREATE + TARGET HELPER path with call counts increasing to 8.

## Replay architecture
- Selected units read from selection list RVA `0x441708`, up to 120 units.
- Native target ability helper RVA `0x1F0C32` / preferred VA `0x5F0C32`.
- No BattleGear re-resolution; V1 static-derived IDs are rejected.
- Runtime IDs are applied directly on the BRZE render/game thread through frame hook RVA `0x135C43`.
- Up to 4 selected units processed per frame.
- `GRAYBACK 0xC0`, `ISSYL 0xA5`, and `APPLY BOTH` modes.
- FXSAVE/FXRSTOR + pushfd/pushad preserve CPU/FPU/SSE state.
- Hook teardown restores stock bytes only when the hook bytes still belong to this lab.
- No raw HP/stamina/movement/attack-speed writes and no CreateRemoteThread.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `9e581cae4272dd3f95b70d1129fca2f44b4f241a`
Workflow: `Hero Effect Replay V2 Runtime IDs`
Run: `34787689041` — SUCCESS
Job: `103806054332` — SUCCESS
Artifact: `10327416796`
Artifact digest: `sha256:a192f72156a0c24387a96bdb6ca0bbcada725ab39498609595573acf1a9a1eb4`

Binaries:
- Standalone SHA-256 `2b5671e9a4d13768a2e3adccd12a82b0137419cbd926d2a3cbb6870fe930a702`
- Small SHA-256 `c0e5f4b3ca847612c9fff3094bb5bbc43639a1720aaf341356034277595e0ca1`

## Runtime proof requested
1. Close main trainer, Clone Lab and Sniffer.
2. Select a visible normal group, e.g. 20–30 Spearmen.
3. Test `GRAYBACK 0xC0` and confirm visible/behavioral buff.
4. Let it expire if applicable, then test `ISSYL 0xA5` and confirm Haste behavior.
5. Test `APPLY BOTH` and confirm stacking on the same selection.
6. Confirm queue reaches DONE and native call count equals selected count for single mode or twice selected count for BOTH.

If all three pass, mark V2 native replay runtime-proven and integrate the feature into the main trainer while preserving the V18.3/V19 locked game-side cores.
