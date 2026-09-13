# BRZE Hero Effect Duration Probe — STATE

Status: **READ-ONLY OBSERVATION BUILD — RUNTIME PENDING**
Date: 2026-09-14 JST

## Purpose
Find the timer/effect-instance state behind the runtime-proven Grayback `0xC0` and Issyl `0xA5` buffs without re-applying abilities and without writing game memory.

This probe exists because `HeroEffectReplayV3` is runtime-rejected: repeated native re-application stacks/compounds effect state and corrupts combat behavior.

## Safety architecture
- Opens BRZE with `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION` only.
- Imports `ReadProcessMemory` only; no `WriteProcessMemory`.
- No `VirtualAllocEx`, no `VirtualProtectEx`, no `CreateRemoteThread`, no code hooks.
- Does not call the native ability helper.
- Does not alter HP, stamina, movement, attack speed, damage, or duration.
- Reads exactly one selected Unit* as the anchor.

## Probe method
1. Fresh/reloaded BRZE runtime.
2. Select exactly ONE clean normal unit.
3. `CAPTURE BASELINE` snapshots:
   - Unit* first `0x500` bytes;
   - up to 160 readable first-level pointer regions, first `0x180` bytes each.
4. Use the separately runtime-proven V2 to apply Grayback OR Issyl exactly ONCE.
5. `CAPTURE EFFECT DIFF` compares direct Unit memory + pointer regions and builds changed dword candidates.
6. `START WATCH` samples candidate addresses every 500 ms and ranks fields that keep changing in a smooth direction.
7. Observe until the visible buff expires. Candidate countdown/timer fields should show coherent time evolution and/or disappear/reset around expiry.

## Important rule
This is observation only. Do not infer a timer from one snapshot. A candidate must be runtime-proven across time and preferably across both repeated clean tests before any write experiment is built.

## Known anchors
- Selection list RVA: `0x441708`.
- Proven native V2 effect IDs remain Grayback `0xC0`, Issyl `0xA5`.
- V3 periodic refresh is permanently rejected.
