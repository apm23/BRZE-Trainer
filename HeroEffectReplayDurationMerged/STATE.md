# BRZE Hero Effect Replay + Duration — MERGED STATE

Status: **V10.1 BUILT / CI-PROVEN — RUNTIME RETEST PENDING**
Date: 2026-09-14 JST

## Purpose
Single-window / single-EXE merge of the known-good HeroEffectReplayV2 native replay path and the V10 duration controller.

The original proven V2 replay core remains linked directly and unchanged:
- `HeroEffectReplayV2/ReplayCore.cs`

The duration controller remains the V10 guarded config controller:
- `HeroEffectReplayDurationV10/ReplayDurationCore.cs`

## Locked runtime evidence
V9 causally proved the duration field:
- A5 config+0x0E0 baseline 15000 -> 10738.0 ms
- controlled patch 30000 -> 21222.5 ms
- ratio 1.976390x
- restore 30000 -> 15000 succeeded

Therefore `parent+0x1F4 -> config+0x0E0` is the Issyl nominal duration parameter.

## First merged runtime test — REJECTED TIMING
Original merged build restored 30000 -> 15000 when the replay lifecycle first became visible.

Runtime report:
- baseline ORIGINAL Issyl: 10749.7 ms
- merged Replay V2 2X attempt: 10623.7 ms
- ratio: 0.988283x
- replay parent resolved to the same A5 config
- restore succeeded

Conclusion:
- lifecycle visibility occurs BEFORE the Replay V2 native apply helper has finished consuming/copying the duration value;
- restoring on first lifecycle visibility is too early;
- the failed 0.988283x result does NOT invalidate config+0x0E0, because V9 already causally proved 15000 -> 30000 nearly doubles lifetime;
- do not reuse lifecycle-first restore timing.

## V10.1 timing fix
Merged `Program.cs` now gates V10 ticking during the 2X replay queue.

Sequence:
1. V10 guards and patches A5 duration 15000 -> 30000.
2. Known-good V2 queues one Issyl native replay.
3. While V2 reports `native calls:0`, merged UI intentionally does NOT call `ReplayDurationCore.Tick()`.
4. Replay V2 increments its CALLS counter only after the native apply helper returns.
5. Once `native calls > 0`, V10 is allowed to tick, observe lifecycle, and restore 30000 -> 15000.
6. Natural replay lifetime is then measured to expiry.

This keeps 30000 alive through the complete native helper call without modifying the proven V2 replay core/stub.

Fail-safe:
- if V2 reaches QUEUE DONE with zero native calls, merged UI restores 15000 rather than leaving the patch resident.
- manual RESTORE and window shutdown still restore through existing V10 guards.

## V10.1 build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Replay Duration Merged`
Run: `34796740941` — SUCCESS
Job: `103831200764` — SUCCESS
Head: `71f7c1f048be4db14a30a70141e09856e4116e63`
Artifact: `10330240927`
Artifact digest: `sha256:f1235fd559129d18201faca31ae84beb37933128f4f67cf93a9a6338ee618503`

Binaries:
- Standalone V10.1: 151,080,162 bytes — SHA-256 `30423192bcf813d9f7d0a285755291e15a2b4ae01e54078a84e31e7d9d22f64f`
- Small V10.1: 165,636 bytes — SHA-256 `0a908dc8ce91991168b064bc0109eac2748ea2ea9276d1191ceec8e5d387ac82`

## Runtime retest flow
1. Fresh/reload BRZE.
2. Open only V10.1 merged EXE.
3. Select exactly one clean target.
4. Click `1) CAPTURE ISSYL BASELINE`.
5. Cast ORIGINAL Issyl Haste once and wait for baseline expiry.
6. Select the same clean target again.
7. Click `2) REPLAY ISSYL 2X` once.
8. Do not manually cast Issyl the second time.
9. Wait for COMPLETE and COPY REPORT.

Expected proof:
- baseline ~10.7 s;
- replay ~21 s;
- ratio near 2.0x;
- replay parent resolves to same A5 config;
- current config returns to 15000 after native call return;
- no repeated application / no stacking.

## Locked rejects
- do not restore duration on first lifecycle visibility;
- no repeated native reapplication;
- no Unit+0x460 duration writes;
- no parent+0x194 duration writes;
- no hard-coded transient parent path;
- keep known-good Replay V2 behavior/source path unchanged as fallback.
