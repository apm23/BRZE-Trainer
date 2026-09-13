# BRZE Hero Effect Duration Probe V3 Control Differential — STATE

Status: **SOURCE READY — BUILD/RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V3 exists
Recursive read-only Probe V2 completed a useful runtime pass but did not yet prove a writable duration field.

V2 runtime findings:
- the new object reached through `Unit+0x1E4 -> +0x008` contains values at `+0x028/+0x02C/+0x030` that closely track target world coordinates and clears at visible expiry; treat it as likely visual/particle/transform state, not the gameplay duration timer;
- new/container-like records beneath `Unit+0x094` remain structurally interesting but are not safe to patch yet;
- direct `Unit+0x460` is the strongest timer/accumulator candidate seen so far: it starts at zero, changes almost every sample, and has zero direction flips in repeated observations;
- `Unit+0x460` is still unproven because it may be a generic per-unit clock rather than a Grayback-specific lifetime.

## V3 goal
Perform a same-unit no-buff CONTROL vs Grayback-active differential before any write experiment.

If `Unit+0x460` changes during the 15-second idle no-buff control, reject it as Grayback duration-specific.
If it stays stable during control but changes smoothly only while Grayback is active, promote it to a high-confidence elapsed/duration candidate for a later isolated experiment.

## Read-only architecture
- process access: `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION` only;
- no `WriteProcessMemory`;
- no hooks;
- no `VirtualAllocEx` / `VirtualProtectEx`;
- no `CreateRemoteThread`;
- no native ability call/replay;
- original Grayback skill is cast manually by the user.

Known runtime anchors:
- selection list RVA `0x441708`;
- UnitDef pointer `Unit+0x74`;
- owner `Unit+0x240`;
- direct Unit scan size `0x500`.

## Runtime flow
1. fresh/reloaded BRZE;
2. select exactly ONE normal clean target unit and keep it completely idle;
3. press `1 START 15s CONTROL` and do not move/attack/cast anything until it auto-stops;
4. press `2 CAPTURE PRE-EFFECT`;
5. cast ORIGINAL Grayback exactly once on the same target;
6. immediately press `3 START EFFECT WATCH`;
7. keep the target idle; do not move, attack, or recast;
8. the instant the visible Grayback effect ends naturally, press `4 MARK EXPIRED`;
9. press `COPY REPORT` and return it for analysis.

`Unit+0x460` is always pinned at the top of the report.

## Interpretation rule
- `Unit+0x460 CTRL chg > 0`: reject as Grayback-specific timer; continue toward the effect-instance/container path.
- `Unit+0x460 CTRL chg = 0` with many smooth FX changes and low/zero FX flips: promote to high-confidence candidate, but still do not write until direction/scale is understood.

## Locked safety rules
- `HeroEffectReplayV2` one-shot native replay remains runtime-proven and untouched.
- `HeroEffectReplayV3` repeated re-apply/refresh is runtime-rejected and must never be reused.
- no duration write is allowed until a candidate is correlated against a no-buff control and natural expiry.
