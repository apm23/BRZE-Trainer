# BRZE Hero Effect Duration Probe — STATE

Status: **BUILD/STATIC PROVEN — READ-ONLY RUNTIME PROBE PENDING**
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

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `245f712a635bfd2f661e2d6bcaa162512e3e3467`
Workflow: `Hero Effect Duration Probe Read Only`
Run: `34789742039` — SUCCESS
Job: `103811637139` — SUCCESS
Artifact: `10328450262`
Artifact digest: `sha256:17ed088b97c58bdd74d881cab07da420cef718662d77b3852b88b67c294417e7`

Binaries:
- Standalone SHA-256 `28a83ed1fa601e9c1406b207bbdaceec80620e33a5a7078fb48c5fc293686b1f`
- Small SHA-256 `1a17d8ed2c6bb018f2f02cd99675019cebbe09c231684a9bb64a3fbfa40a2122`

## Exact runtime test
1. Fully close V3 and restart/reload BRZE to remove the contaminated stacked state.
2. Start this read-only probe.
3. Select exactly ONE clean normal unit.
4. Press `1 CAPTURE BASELINE`.
5. Open/use the runtime-proven V2 and apply **only one** effect to that exact unit: Grayback `0xC0` OR Issyl `0xA5`, exactly once.
6. Return to this probe immediately and press `2 CAPTURE EFFECT DIFF`.
7. Press `3 START WATCH` and leave the unit/effect alone until the visible buff expires naturally.
8. Press `COPY REPORT` near the end / just after visible expiry and send the report back for analysis.
9. Repeat later from a fresh state for the other ability if needed.

## Important rule
This is observation only. Do not infer a timer from one snapshot. A candidate must be runtime-proven across time and preferably across both repeated clean tests before any write experiment is built.

## Known anchors
- Selection list RVA: `0x441708`.
- Proven native V2 effect IDs remain Grayback `0xC0`, Issyl `0xA5`.
- V3 periodic refresh is permanently rejected.
