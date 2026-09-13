# BRZE Hero Effect Duration Probe V2 — STATE

Status: **BUILD/STATIC PROVEN — RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V2 exists
Probe V1 runtime report was successful as a read-only observer but did not isolate a trustworthy duration field in the direct Unit* region or first-level pointed regions.

Observed V1 result from user runtime report:
- high-score Unit+0x028 / +0x02C / +0x030 behaved like position/movement state, not a stable countdown;
- Unit+0x45C..+0x4A4 changed almost every sample with many direction flips, consistent with live transform/state noise rather than a timer;
- [Unit+0x098] fields changed too slowly/irregularly to lock as duration;
- pointers around Unit+0x1E4 / +0x1F4 / +0x20C / +0x21C remained structurally interesting;
- [Unit+0x1E4] exposed repeating pointer/node patterns, suggesting a deeper container/list graph that V1 did not recursively follow.

## V2 architecture
V2 remains strictly read-only:
- process access only PROCESS_VM_READ + PROCESS_QUERY_INFORMATION;
- no game memory writes;
- no hooks;
- no native ability replay;
- no V3 refresh behavior.

Graph traversal:
- root Unit* scan size 0x500;
- child node scan size 0x200;
- recursive pointer depth <= 3;
- max 700 readable graph nodes;
- max 3000 changed dword candidates.

Runtime flow:
1. fresh/reloaded game;
2. select exactly ONE clean normal target unit;
3. CAPTURE BASELINE;
4. cast ONE original Grayback buff once on that target;
5. CAPTURE EFFECT DIFF;
6. START WATCH;
7. do not recast or move/test the unit unnecessarily;
8. when the visible buff disappears naturally, immediately press MARK EFFECT EXPIRED;
9. COPY REPORT and return it for analysis.

Expiry ranking boosts candidates that:
- return exactly to baseline when the visible effect expires;
- hit zero at expiry;
- become unreadable / object freed at expiry;
- changed smoothly with few direction flips during the active effect.

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
- HeroEffectReplayV2 remains runtime-proven for one-shot native application.
- HeroEffectReplayV3 repeated refresh is runtime-rejected and must not be reintroduced.
- Duration work must find the real effect-instance lifetime/timer rather than stacking multiple native effect instances.
