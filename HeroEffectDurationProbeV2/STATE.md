# BRZE Hero Effect Duration Probe V2 — STATE

Status: **RUNTIME-PROVEN OBSERVER — TIMER NOT YET PROVEN**
Date: 2026-09-14 JST

## Why V2 exists
Probe V1 runtime report was successful as a read-only observer but did not isolate a trustworthy duration field in the direct Unit* region or first-level pointed regions. V2 expanded this to a recursive pointer graph and added a natural-expiry marker.

## V2 architecture
V2 remains strictly read-only:
- process access only `PROCESS_VM_READ + PROCESS_QUERY_INFORMATION`;
- no game memory writes;
- no hooks;
- no native ability replay;
- no repeated re-apply behavior.

Graph traversal:
- root Unit* scan size `0x500`;
- child node scan size `0x200`;
- recursive pointer depth <= 3;
- max 700 readable graph nodes;
- max 3000 changed dword candidates.

## Runtime result — user report 2026-09-14
The recursive observer completed successfully against one target using the ORIGINAL Grayback skill and natural visible expiry.

Findings:
- the new object reached through `Unit+0x1E4 -> +0x008` becomes/clears with the effect, but values at `+0x028/+0x02C/+0x030` closely match target world-space XYZ-like coordinates (approximately 105.56 / 134.32 / 10.00 in the captured run). Treat this object as likely visual/particle/transform state, not a proven gameplay-duration field;
- new/container-like structures beneath `Unit+0x094`, including paths through `+0x010`, `+0x014`, and `+0x184`, contain recurring record/ID-like integer patterns and remain interesting for future effect-instance work, but nothing there is proven safe to write;
- direct `Unit+0x460` is the strongest direct timer/accumulator candidate: it started from zero and changed nearly every observed sample with zero direction flips. In the latest run it reached integer 178500 with 89 changes / 0 flips at the expiry mark;
- an earlier V1 run also showed `Unit+0x460` changing smoothly from zero with zero flips, ending at 102000. The differing totals mean it may be a generic unit/game clock rather than Grayback duration itself.

Therefore V2 does NOT prove a duration offset yet. No writes to `Unit+0x460` or any recursive object are authorized from this evidence alone.

## Next phase
`HeroEffectDurationProbeV3` performs a same-unit 15-second NO-BUFF idle control before the Grayback-active observation.

Decision rule:
- if `Unit+0x460` changes during no-buff control, reject it as Grayback-specific duration;
- if it remains stable in control and changes smoothly only while Grayback is active, promote it to a high-confidence candidate for a later isolated direction/scale experiment.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `7cfaa4e4e410f24c5812ea055496309f3525a3f6`
Workflow: `Hero Effect Duration Probe V2 Recursive Read Only`
Run: `34790442232` — SUCCESS
Job: `103813532429` — SUCCESS
Artifact: `10327766605`
Artifact digest: `sha256:83bcfda67467a3d0b2c85f0e13b9644a02a399f7c5fd38cacea5dcd9617a0901`

Binaries:
- Standalone SHA-256 `acb1b4f87266eab44d760ab0d6ac18257ff60ffd5696f3bdc9d08eebed1ea825`
- Small SHA-256 `2ef44674881ebf6b0d2d5f0aa9c0b9250ab405f9c2d02cc6eecb4270c5633991`

## Locked safety rules
- `HeroEffectReplayV2` remains runtime-proven for one-shot native application.
- `HeroEffectReplayV3` repeated refresh is runtime-rejected and must not be reintroduced.
- duration work must find/correlate the real effect-instance lifetime before any write experiment.
