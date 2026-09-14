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
Runtime:
- A5 record `0x235ADCA4`
- old `record+0x194 = 375600`
- writing only `record+0x194 = 0` did not trigger reinitialization within ~1s
- fail-safe restore of `375600` succeeded.

Never reuse zero-reset.

## V27 Current-Time Source — RUNTIME-PROVEN READ-ONLY DISCOVERY
State: `HeroEffectCurrentTimeSourceV27/STATE.md`

Runtime context:
- PID `6528`, moduleBase `0x00870000`
- Unit* `0x22B47F2C`, UnitDef* `0x1688993C`, owner `0`
- active A5 record `0x235AE09C` via `Unit+0x1E4->+0x008`
- stamp `400600`
- config `0x1D1FA664`, duration `15000`

V27 found exactly one direct E8 call to tick RVA `0x13A5EB`:
- call `0x140125`
- caller start `0x1400F0`

No non-text runtime pointer refs to tick function.

Unique recursive caller chain:
`0x144691 -> 0x144794 -> 0x1C3107 -> 0x1400F0 -> 0x13A5EB`

Limitation:
V27 printed operand kinds but not exact register names, so the `[EBP+8]` current-time source is not yet concrete. No write is justified yet.

## V28 Register Flow — BUILT / CI-PROVEN READ ONLY
State: `HeroEffectRegisterFlowV28/STATE.md`

Purpose:
- print exact register operands in each caller window;
- identify the last PUSH before each call;
- backward-trace pushed registers through MOV chains;
- detect `[EBP+8]` propagation between caller levels;
- identify absolute/global sources;
- sample absolute sources 350ms apart and mark changing clock-like values;
- walk the unique caller chain upward up to five levels.

V28 CI:
- run `34833344064` — SUCCESS
- job `103941510533` — SUCCESS
- head `7078921824d79139db3784a3a0916cf5a5d80d80`
- artifact `10343645827`
- digest `sha256:24c0897627c3d972d5e3988722be7ea11873905df1a48e81bc52fe71057daee8`
- standalone SHA256 `1bfda24034eead2692a94382fe281deb325685ff07b340d2932bba194c721053`
- small SHA256 `8d7000c5b78f4509923204da274aebeafd1b99b9f5af861b67030d534623b17b`

## EXACT NEXT ACTION — ONE V28 READ-ONLY TRACE
1. Prefer exactly ONE unit with active Issyl selected for runtime context.
2. Open `BRZE-Hero-Effect-Register-Flow-V28-ReadOnly.exe`.
3. Click `TRACE TICK ARG FLOW` once.
4. Click `COPY REPORT` and send the full report.

No writes happen in V28.

After V28:
- if a concrete current-BRZE-time source is identified, build the smallest guarded proof writing that exact current value into the existing same-effect record `+0x194`;
- otherwise narrow one more static caller/source step rather than guessing.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. User wants same-buff APPLY to restart/reset without stacking. V26 zero-reset is permanently rejected. V25 timestamp/duration flow remains valid. V27 runtime found a unique caller chain 0x144691 -> 0x144794 -> 0x1C3107 -> 0x1400F0 -> 0x13A5EB but exact register source of tick arg [EBP+8] is still unresolved. V28 exact register-flow probe is BUILT/CI-PROVEN READ ONLY, run 34833344064 SUCCESS, artifact 10343645827. Next: one V28 TRACE TICK ARG FLOW report, then only if current game-time source is concrete do a guarded timestamp write.`
