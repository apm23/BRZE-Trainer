# BRZE Hero Effect Replay + Duration — MERGED STATE

Status: **V10.2 BUILT / CI-PROVEN — RUNTIME TEST PENDING**
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

IMPORTANT correction from source review:
- V9 did **not** restore immediately after effect creation.
- V9 kept `config+0x0E0=30000` for the **entire patched effect lifetime**.
- V9 restored to `15000` only after natural expiry.
- Therefore the previous assumption that duration is copied/latching at creation was not proven and is now rejected.

## Merged runtime attempt #1 — REJECTED
Restore timing: first lifecycle visibility.

Runtime:
- baseline `10749.7 ms`
- replay `10623.7 ms`
- ratio `0.988283x`
- same A5 config verified
- restore succeeded

Conclusion:
- restoring at first lifecycle visibility is too early / incompatible with the proven V9 timing.
- never reuse lifecycle-first restore timing.

## Merged runtime attempt #2 / V10.1 — REJECTED
Restore timing: after Replay V2 native apply helper returned (`native calls > 0`).

Runtime:
- baseline `10717.2 ms`
- replay `10611.9 ms`
- ratio `0.990176x`
- same A5 config verified
- restore succeeded

Conclusion:
- even waiting until the V2 native helper returns is still too early.
- duration is not safely latched by the time that helper returns.
- never reuse native-call-return restore timing.

## V10.2 design — mirrors the proven V9 timing exactly
V10.2 keeps Issyl `config+0x0E0=30000` resident for the **entire replay effect lifetime**.

Sequence:
1. Capture original Issyl baseline and A5 config.
2. Select the clean target again.
3. Guard and patch `15000 -> 30000`.
4. Queue one known-good V2 Issyl replay.
5. A separate read-only watcher tracks the pinned Unit transient roots (`+1E4/+1E8/+20C/+210/+214`).
6. While replay is active, V10.2 intentionally does **not** call the old V10 lifecycle restore path.
7. Keep `30000` for the whole natural replay lifetime.
8. After roots return to the clean baseline for 4 consecutive polls, restore `30000 -> 15000`.
9. Measure replay lifetime and ratio automatically.

This deliberately duplicates the timing model that V9 already proved at `1.976390x`.

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

## Runtime test flow
1. Fresh/reload BRZE.
2. Open only V10.2 merged EXE.
3. Select exactly one clean target.
4. Click `1) CAPTURE ISSYL BASELINE`.
5. Cast ORIGINAL Issyl Haste once and wait for baseline expiry.
6. Select the same clean target again.
7. Click `2) REPLAY ISSYL 2X` once.
8. Do not manually cast Issyl the second time.
9. Wait until V10.2 reports COMPLETE.
10. COPY REPORT.

Expected proof:
- baseline ~10.7 s;
- replay ~21 s;
- ratio near 2.0x;
- config remains 30000 throughout replay and returns to 15000 only after expiry;
- no repeated application / no stacking.

## Locked rejects
- no lifecycle-first restore timing;
- no native-call-return restore timing;
- no claim that duration is copied at effect creation;
- no repeated native reapplication;
- no Unit+0x460 duration writes;
- no parent+0x194 duration writes;
- no hard-coded transient parent path;
- keep known-good Replay V2 source/binary behavior untouched as fallback.
