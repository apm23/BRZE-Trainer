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
- assuming duration fully latches at creation;
- V26 zero-reset (`record+0x194 = 0`) for an already-active effect.

## User-requested final semantics
Hero Effect APPLY should feel like replacement/reset:
- if selected unit already has same Issyl/Grayback buff, restart that same buff from zero;
- if selected unit does not have it, apply once normally;
- unrelated buffs remain untouched;
- never blindly native-reapply on top of an active same effect.

## V25 Expiry Tail — RUNTIME-PROVEN READ-ONLY
Runtime established:
- `record+0x194` is a start/lifecycle timestamp;
- `record+0x1F4 -> config+0xE0` is duration;
- tick RVA `0x13A5EB` uses `[EBP+8]` as current time/tick input during initialization;
- later code subtracts start timestamp from current time and compares against duration.

## V26 Clock Reset — RUNTIME-REJECTED
- old `record+0x194 = 375600`
- writing only `record+0x194 = 0` did not trigger reinitialization within ~1s
- fail-safe restore of `375600` succeeded.

Never reuse zero-reset.

## V27 Current-Time Source — RUNTIME-PROVEN READ-ONLY DISCOVERY
V27 narrowed the unique caller chain:
`0x144691 -> 0x144794 -> 0x1C3107 -> 0x1400F0 -> 0x13A5EB`

## V28 Register Flow — RUNTIME-PROVEN CURRENT-TICK SOURCE
State: `HeroEffectRegisterFlowV28/STATE.md`

Runtime:
- PID `6528`, moduleBase `0x00870000`
- selected Unit* `0x22B47F2C`
- active A5 record `0x235B0A48`
- A5 start timestamp `486200`
- config `0x1D1FA664`, duration `15000`

Exact lower-level register flow:
- `0x140106`: `EBX <- [EBP+8]`
- `0x140120`: `PUSH EBX`
- `0x140125`: CALL tick `0x13A5EB`

Direct caller flow:
- `0x1C3243`: `PUSH ESI`
- V28 backward trace: `0x1C3181`: `ESI <- [0x00CB0A3C]`

Therefore the concrete BRZE current-time source is:
- runtime absolute `0x00CB0A3C` for moduleBase `0x00870000`
- equivalent module RVA **`0x440A3C`**

Observed at scan:
- current tick `489500`
- A5 start stamp `486200`
- elapsed delta `3300`

The 350ms sample remained unchanged because BRZE may pause simulation while unfocused; source provenance is still proven by exact instruction/register flow.

V28 CI:
- run `34833344064` SUCCESS
- job `103941510533` SUCCESS
- artifact `10343645827`

## V29 Current-Tick Reset — BUILT / CI-PROVEN, RUNTIME PROOF NEXT
State: `HeroEffectCurrentTickResetV29/STATE.md`

Purpose:
Perform the smallest guarded reset proof on exactly ONE active stock Issyl A5 instance:
1. signature-lock A5 + selected Unit*;
2. require stock A5 config ID/duration 15000;
3. read old `record+0x194`;
4. read current BRZE tick from module RVA `0x440A3C`;
5. require current tick > old stamp and plausible elapsed delta 100..60000;
6. write exactly `record+0x194 = current BRZE tick`;
7. verify exact readback and same A5+target signature;
8. no native replay, no second effect instance, no hooks.

V29 CI:
- workflow `Hero Effect Current-Tick Reset V29 Guarded`
- run `34834013421` SUCCESS
- job `103943588874` SUCCESS
- head `ab6f4f7a2a5b55d09c1281749b6b6e8bf249f238`
- artifact `10343193391`
- digest `sha256:aaf1e8ef8e815d6366c7e7cff01acaa87a9c5287457e73a191d833698c9566cf`
- standalone SHA256 `77571e840f9cfc5a07f23cd7b6367d6c2320e9c5edfafd99903d471e12696a1b`
- small SHA256 `671088f9018547bdc35c269ecfecf7d2e24e0af668ff9d4a5fcf797bd023be14`

## EXACT NEXT ACTION — ONE V29 GUARDED RESET
1. Give stock Issyl to exactly ONE unit.
2. Wait several seconds while Issyl is still visibly active.
3. Select only that unit.
4. Open `BRZE-Hero-Effect-Current-Tick-Reset-V29-Guarded.exe`.
5. Click `RESET ACTIVE ISSYL TO CURRENT BRZE TICK` once.
6. Return to BRZE and verify Issyl lasts approximately one fresh stock lifetime from reset moment.
7. Copy/send the full report.

PASS requires exact current-tick write/readback, same A5 signature, no crash/corruption, and visual lifetime restart.

If V29 passes, integrate reset semantics into the trainer: existing same buff -> reset its start timestamp to current BRZE tick; no existing same buff -> one-shot native apply. Keep duration config full-lifetime hold semantics.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. User wants same-buff APPLY to restart/reset without stacking. V26 zero-reset is rejected forever. V28 runtime PROVED current tick source module RVA 0x440A3C via exact register flow: 0x1C3181 ESI<-[0x00CB0A3C] -> PUSH ESI -> caller [EBP+8] -> EBX -> tick RVA 0x13A5EB. Runtime currentTick 489500 vs A5 start 486200. V29 guarded direct-current-tick reset is BUILT/CI-PROVEN, run 34834013421 SUCCESS, artifact 10343193391. Next: one V29 guarded reset on one active stock Issyl and send report + visual lifetime result.`
