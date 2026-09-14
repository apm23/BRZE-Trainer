# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks
- Locked main stable fallback: V18.3 (`FINAL_CURRENT.md`).
- V19 = latest pre-integration main-trainer fallback.
- V20 = integrated fallback.
- V21 Direct Hero = runtime-rejected due generic transient-root gating/hold bugs.
- V22 Group-Safe Hero = built/CI-proven integrated fallback while true reset/replace semantics are finalized.
- Unit Clone Lab standalone remains runtime-proven.
- Replay V2 / V11 duration proof remain runtime-proven references.

Do not mutate V18.3 directly.

## Locked duration facts
- Issyl A5 config base nominal `15000`; natural wall ~`10.7s`.
- Grayback C0 config base nominal `60000`; natural wall ~`42.2s`.
- `record+0x1F4 -> config+0x0E0` is the proven duration path.
- Extended config must remain resident through natural effect lifetime, then restore.
- V11 final proof: baseline `10742.1ms`; Issyl nominal `45000` -> `31612.4ms`, ratio `2.942838x`; restore `45000 -> 15000` OK.

Rejected forever:
- repeated native replay refresh / stacking;
- Unit+0x460 duration writes;
- record+0x194 as duration;
- lifecycle-first restore;
- native-helper-return restore;
- assuming duration fully latches at creation.

## User-requested final semantics
Hero Effect APPLY should feel like replacement/reset:
- if selected unit already has same Issyl/Grayback buff, restart that same buff from zero;
- if selected unit does not have it, apply once normally;
- unrelated buffs remain untouched;
- never blindly native-reapply on top of an active same effect.

## V25 Expiry Tail — RUNTIME-PROVEN READ-ONLY
Runtime report established the duration clock flow:
- `record+0x194` is a start/lifecycle timestamp;
- `record+0x1F4 -> config+0xE0` is duration;
- tick function RVA `0x13A5EB` uses `[EBP+8]` as current time/tick input during initialization;
- later code subtracts start timestamp from current time and compares against duration.

## V26 Clock Reset — RUNTIME-REJECTED
State: `HeroEffectClockResetV26/STATE.md`

Runtime report:
- PID `6528`, moduleBase `0x00870000`
- Unit* `0x22B47F2C`, UnitDef* `0x1688993C`, owner `0`
- A5 record `0x235ADCA4` via `Unit+0x1E4->+0x008`
- signature `A5 + target Unit*` valid
- config `0x1D1FA664`, ID A5, duration 15000
- old `record+0x194 = 375600`
- V26 wrote only `record+0x194 = 0`
- field remained zero for ~1 second
- game did NOT reinitialize it
- fail-safe restored old timestamp successfully

Conclusion:
- zero-reset hypothesis is FALSE for an already-active instance;
- never integrate or reuse `record+0x194 = 0` as reset semantics;
- this does NOT invalidate the timestamp role established by V25.

## V27 Current-Time Source — BUILT / CI-PROVEN READ ONLY
State: `HeroEffectCurrentTimeSourceV27/STATE.md`

Purpose:
Find the real source of tick function RVA `0x13A5EB` stack argument `[EBP+8]`, so a future guarded reset can write the **actual current BRZE game-time value** directly into an existing effect's start timestamp instead of guessing or zeroing it.

V27:
- scans direct `E8` call xrefs to RVA `0x13A5EB`;
- reconstructs caller windows;
- samples absolute memory operands 350ms apart and marks changing clock-like values;
- scans non-text sections for relocated pointers to the tick function, covering possible vtable/indirect dispatch;
- recursively summarizes direct callers up to depth 3;
- optionally logs the selected live A5 record as context;
- strict read-only, no hooks/writes/native calls.

### V27 CI pin
Workflow: `Hero Effect Current-Time Source V27 Read Only`
- run `34830108923` — SUCCESS
- job `103931199444` — SUCCESS
- head `564c2bf48f3d0885cc6fd42e0268d9b54364a398`
- artifact `10340943840`
- digest `sha256:6cfd165b07768f89773c68dfc4273727879fd173e997db3ad0cdd9493559b9f3`
- standalone SHA256 `014a74d59dc05ad170057c9bb04d7e033e6e8a29376630bc3c6f65bf26fce36e`
- small SHA256 `5deeeb1437acc3a4eab203959b5fd94e8a947f4cef113da0b8d7dd40cdb021ee`

## EXACT NEXT ACTION — ONE V27 READ-ONLY SCAN
1. Prefer exactly ONE unit with Issyl still active and selected; this gives runtime context, but static scan can run without it.
2. Open `BRZE-Hero-Effect-Current-Time-Source-V27-ReadOnly.exe`.
3. Click `SCAN CURRENT-TIME SOURCE` once.
4. Click `COPY REPORT` and send the full report.

No writes happen in V27.

After the report:
- if a real current-game-time source is identified, build the smallest guarded one-unit A5 proof writing that exact value to `record+0x194`;
- if not, narrow the caller chain again rather than guessing.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. User wants same-buff APPLY to restart/reset without stacking. V26 zero-reset is RUNTIME-REJECTED: +0x194 stayed zero for ~1s and fail-safe restored old timestamp. V25 timestamp/duration flow remains valid. V27 Current-Time Source is BUILT/CI-PROVEN READ ONLY, run 34830108923 SUCCESS, artifact 10340943840. Next: one V27 scan to identify the real source of RVA 0x13A5EB arg [EBP+8], then only if proven do a guarded direct-current-time timestamp write.`
