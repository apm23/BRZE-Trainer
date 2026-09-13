# BRZE Hero Effect Replay V3 Duration — STATE

Status: **BUILD/STATIC PROVEN — RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V3 exists
Hero Effect Replay V2 is now user-reported runtime-successful for the runtime-sniffed native ability path. The target helper signature used by V2 is effectively `Unit* + ability ID`; no duration argument is exposed by that proven call path.

V3 therefore preserves the exact native replay architecture and extends effective duration by scheduled native re-application rather than by raw movement/attack-speed/HP/stamina writes or an unproven timer offset.

## Runtime IDs preserved
- Grayback native runtime ability: `0xC0`
- Issyl native runtime ability: `0xA5`
- Native target ability helper RVA: `0x1F0C32`
- Selection list RVA: `0x441708`
- Frame/game-thread hook RVA: `0x135C43`
- Maximum captured selection: 120
- Up to 4 target units processed per frame

## Duration architecture
- On START, V3 captures the current selected Unit* pointers once.
- Each captured target also stores UnitDef pointer (`Unit + 0x74`) and owner (`Unit + 0x240`).
- Before every refresh, V3 re-validates both values and skips invalid/stale targets.
- Buff refresh is performed by the same native target helper used by runtime-proven V2.
- Refresh never uses CreateRemoteThread.
- No raw HP/stamina/movement/attack-speed writes.
- The V2 project/artifact remains untouched as rollback/proven baseline.

UI presets:
- Hold duration: 30 seconds / 60 seconds / 5 minutes / INFINITE
- Refresh interval: 2 seconds / 5 seconds / 10 seconds
- Modes: Grayback `0xC0`, Issyl `0xA5`, APPLY BOTH
- STOP HOLD stops future refreshes only; it does not forcibly strip a currently active buff.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `0f0d0c34cccff6df6f8e31c1c98436ba3a717fcc`
Workflow: `Hero Effect Replay V3 Duration`
Run: `34788874055` — SUCCESS
Job: `103809270556` — SUCCESS
Artifact: `10327388613`
Artifact digest: `sha256:604312730b0de8bad8fbc7cced242d93fc4a2589ec7332f195cc8cce75e054e4`

Binaries:
- Standalone SHA-256 `d82e642ac9287ca39f17690832d5c1022e91954d337a8c81828b90d8058271d9`
- Small SHA-256 `36dccb03b0041d300201dab20142e860002e4e212b3ea5580a4c47cfc3e6c7a0`

## Exact runtime proof requested
1. Close V2, main trainer, Clone Lab, Sniffer and other frame-hook labs.
2. Select a visible group, preferably 10–30 normal units.
3. Start with `60 seconds` + `5 seconds` refresh.
4. Test Grayback and observe whether the buff remains beyond its normal expiry time.
5. Repeat for Issyl Haste.
6. Test APPLY BOTH and verify both remain active while HOLD is active.
7. Deselect the units after starting; verify V3 continues targeting the captured group.
8. Press STOP HOLD and confirm the buff eventually expires naturally after refreshing stops.

If re-applying while an effect is active does not reset/extend its timer, mark this refresh approach runtime-rejected and build a dedicated effect-instance duration probe rather than guessing offsets.
