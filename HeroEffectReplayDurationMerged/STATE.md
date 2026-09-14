# BRZE Hero Effect Replay + Duration — MERGED STATE

Status: **V10.2 RUNTIME-PROVEN — REPLAY + EXTENDED DURATION CONFIRMED**
Date: 2026-09-14 JST

## Purpose
Single-window / single-EXE merge of known-good HeroEffectReplayV2 native replay and the proven hero-effect duration config field.

Locked components:
- V2 replay core: `HeroEffectReplayV2/ReplayCore.cs` — runtime-proven one-shot replay, unchanged.
- Duration config path: `parent+0x1F4 -> config+0x0E0`.

## Locked duration proof from V9
V9 causally proved the Issyl duration field:
- baseline config `15000` -> `10738.0 ms`
- patched config `30000` -> `21222.5 ms`
- ratio `1.976390x`
- restore `30000 -> 15000` succeeded

IMPORTANT source correction:
- V9 kept `config+0x0E0=30000` for the entire patched effect lifetime.
- Restore to `15000` happened only after natural expiry.
- Therefore duration is NOT proven to latch/copy at creation.

## Merged attempt #1 — REJECTED
Restore at first lifecycle visibility:
- baseline `10749.7 ms`
- replay `10623.7 ms`
- ratio `0.988283x`
- same A5 config verified
- restore succeeded

Never reuse lifecycle-first restore timing.

## Merged attempt #2 / V10.1 — REJECTED
Restore after Replay V2 native helper returned (`native calls > 0`):
- baseline `10717.2 ms`
- replay `10611.9 ms`
- ratio `0.990176x`
- same A5 config verified
- restore succeeded

Never reuse native-call-return restore timing.

## V10.2 — RUNTIME-PROVEN
V10.2 mirrors V9 timing exactly: keep Issyl `config+0x0E0=30000` resident for the full replay lifetime, then restore after natural expiry.

Runtime result:
- baseline ORIGINAL Issyl: `10755.0 ms`
- merged Replay V2 2X: `21115.9 ms`
- replay/baseline ratio: `1.963355x`
- config restored after natural expiry: `30000 -> 15000`
- current config after test: `15000`
- hold active after completion: `False`
- no repeated native application / no refresh loop

This is direct runtime proof that known-good Replay V2 and the proven duration config work together when the duration value is held for the full effect lifetime.

The old companion report remains `ReadyForReplay / replay=0.0` because V10.2 intentionally bypasses the old V10 lifecycle state machine during the full-lifetime hold. For V10.2, the authoritative runtime block is `MERGED V10.2 FULL-LIFETIME HOLD`.

## V10.2 build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Replay Duration Merged`
Run: `34797240949` — SUCCESS
Job: `103832611033` — SUCCESS
Head: `0062f32fa0d3d903f85ef613eb71dd8034de95a3`
Artifact: `10330421265`
Artifact digest: `sha256:4dcdf829c606197fb631257248753befa5d6d00757a38e32c3281b33675a7b77`

Binaries:
- Standalone V10.2: 151,084,258 bytes — SHA-256 `d522c611f48e84b1a9b0259938e8847f87e9c968ec07bbb2cfb2c10f58f51c29`
- Small V10.2: 172,292 bytes — SHA-256 `6a22bc4611edc41daa174a024d3334c6b152b7481838887d4edc1c270892c17d`

## Locked implementation rule
For Issyl replay duration extension:
1. guard A5 config identity;
2. patch desired duration before queueing Replay V2;
3. keep the patched value resident for the entire active effect lifetime;
4. restore original duration only after natural expiry is stable;
5. no repeated application / refresh.

Because the A5 config object is global/shared, while the override is resident any other Issyl effect created in that interval may also observe the modified duration. Final integration must account for this global exposure.

## Next engineering target
Build a configurable-duration version above V10.2, while preserving:
- V2 native replay behavior unchanged;
- full-lifetime hold semantics;
- automatic restore after expiry;
- strict config identity/value guards;
- known-good V2 binary/source as fallback.

Do not integrate into locked V18.3 main trainer until the configurable merged version is runtime-proven.

## Locked rejects
- no lifecycle-first restore timing;
- no native-call-return restore timing;
- no claim that duration is copied at effect creation;
- no repeated native reapplication;
- no Unit+0x460 duration writes;
- no parent+0x194 duration writes;
- no hard-coded transient parent path;
- keep known-good Replay V2 source/binary behavior untouched as fallback.
