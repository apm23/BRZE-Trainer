# BRZE Hero Effect Duration Probe V2 — STATE

Status: **BUILD PENDING — READ-ONLY RECURSIVE PROBE**
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

## Locked safety rules
- HeroEffectReplayV2 remains runtime-proven for one-shot native application.
- HeroEffectReplayV3 repeated refresh is runtime-rejected and must not be reintroduced.
- Duration work must find the real effect-instance lifetime/timer rather than stacking multiple native effect instances.
