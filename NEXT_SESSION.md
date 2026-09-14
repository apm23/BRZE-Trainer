# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks
- Locked main stable fallback: V18.3 (`FINAL_CURRENT.md`).
- V19 = latest pre-integration main-trainer fallback.
- V20 = integrated fallback.
- V21 Direct Hero = runtime-rejected due generic transient-root gating/hold bugs.
- V22 Group-Safe Hero = built/CI-proven integrated fallback while true reset/replace semantics are researched.
- Unit Clone Lab standalone remains runtime-proven.
- Replay V2 / V11 duration proof remain runtime-proven references.

Do not mutate V18.3 directly.

## Locked duration facts
- Issyl A5 config base nominal `15000`; natural wall ~`10.7s`.
- Grayback C0 config base nominal `60000`; natural wall ~`42.2s`.
- `parent+0x1F4 -> config+0x0E0` is the proven duration path.
- Extended config must remain resident through natural effect lifetime, then restore.
- V11 final proof: baseline `10742.1ms`; Issyl nominal `45000` -> `31612.4ms`, ratio `2.942838x`; restore `45000 -> 15000` OK.

Rejected forever:
- repeated native replay refresh / stacking;
- Unit+0x460 duration writes;
- parent+0x194 as duration;
- lifecycle-first restore;
- native-helper-return restore;
- assuming duration fully latches at creation.

## User-requested final semantics
Hero Effect APPLY should behave like replacement/reset:

`if selected unit already has same Issyl/Grayback buff -> remove/reset OLD same-effect instance safely -> one-shot fresh apply -> requested duration starts again from zero`

Unrelated buffs must remain untouched.

Never implement by blindly applying again on an already-active same effect. V3 proved repeated application can stack/compound into extreme speed, invulnerability-like behavior, one-hit buildings, and other corruption.

## V22 Group-Safe Hero — current integrated fallback
Uses exact content signature:
- effect record `+0x058 = ability ID`;
- effect record `+0x17C = target Unit*`;
- transient root path itself is NOT stable and must never be hard-coded.

V22 CI:
- run `34823248974` SUCCESS
- job `103909409512` SUCCESS
- head `a8e55e21d5b20e468fd08db14b00d3d6cba5dc73`
- artifact `10339202775`

## V23 Reset Forensics — RUNTIME-PROVEN READ-ONLY DISCOVERY
V23 runtime successfully locked a live A5 effect and ranked +0x194 references.

Important correction after V24:
- V23 strongest raw hit `0x13DA9C` is NOT cleanup proof.
- Never call it merely because it writes `+0x194`.

## V24 Deep Scan — RUNTIME-PROVEN READ-ONLY, STRUCTURE NARROWED
State: `HeroEffectResetDeepScanV24/STATE.md`

Runtime report:
- Unit* `0x22B47F2C`
- A5 parent `0x235ACCC4`
- path `Unit+0x1E4->+0x008`
- vtable `0x00C138EC`
- parent+0x194 `275200`
- config `0x1D1FA664`
- config ID `A5`
- duration `15000`

V24 conclusions:
- `0x13DA9C` = short contiguous field-copy tail; REJECT as cleanup entry.
- `0x2151DE` and `0x24A7B8` = massive contiguous field-copy/init style writers; REJECT as cleanup.
- `0x16B76A` = repetitive registration/assignment style code; REJECT as cleanup.
- validated function boundary `0x13A5EB` is the strongest structural lead.

Inside `0x13A5EB`:
- target is read from record `+0x17C`;
- config is read from record `+0x1F4`;
- at `0x13A8B3`, record `+0x194` is checked for zero;
- at `0x13A8BC..0x13A8BF`, function arg `[EBP+8]` is copied into record `+0x194` when zero.

Interpretation: `+0x194` behaves like lifecycle/start/current timestamp, NOT duration and NOT a cleanup function pointer.

V24 output ended before the full tail of `0x13A5EB`, so the actual duration comparison / expiry branch is still missing.

## V25 Expiry Tail — BUILT / CI-PROVEN READ ONLY
State: `HeroEffectExpiryTailV25/STATE.md`

Purpose:
- decode the full tail of `0x13A5EB` after the +0x194 marker;
- track register flow from record `+0x1F4` into nested config `+0xE0/+0xE4`;
- tag `[EBP+8]`, record `+0x194`, record `+0x17C`, record `+0x1F4`;
- print focus windows around config duration accesses;
- list direct CALL targets around the expiry decision;
- dump RVA `0x13A349` and all direct xrefs to it.

V25 is strict read-only: no writes, hooks, native calls, destructor calls, or CreateRemoteThread.

### V25 CI pin
Workflow: `Hero Effect Expiry Tail V25 Read Only`
- run `34827531830` — SUCCESS
- job `103923007903` — SUCCESS
- head `45e0bc3972544908fd1b92877d4bca52cb1effa3`
- artifact `10341240369`
- digest `sha256:9d7879a39c9b47085ab99e40cba0fb41d6bfd36f4714649a47bdf0237eeee2aa`
- standalone SHA256 `4b569a3e0155fce68d74fd344cda4f0c679c08eb7c7bddd01a31db62b42bac57`
- small SHA256 `07b3ad5af41a3247014d8355005a4581941e5c4e7a271d171363874da8dd2de1`

## EXACT NEXT ACTION — ONE V25 READ-ONLY SCAN
1. Give Issyl to exactly ONE unit.
2. While Issyl is visibly active, select only that unit.
3. Open `BRZE-Hero-Effect-Expiry-Tail-V25-ReadOnly.exe`.
4. Click `SCAN EXPIRY TAIL` once.
5. Click `COPY REPORT` and send the full report.

No natural-expiry wait is required. V25 modifies nothing.

After V25:
- only if the report ties config+0xE0 duration to a specific natural-expiry branch/call do the smallest guarded cleanup proof;
- otherwise narrow again rather than guessing.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. User wants true Hero Effect RESET/REPLACE, never repeated stacking. V24 runtime rejected raw +0x194 writers as cleanup and narrowed the real lifecycle logic to function RVA 0x13A5EB: target +0x17C, config +0x1F4, +0x194 initialized from arg [EBP+8]. V25 Expiry Tail is BUILT/CI-PROVEN READ ONLY, run 34827531830 SUCCESS, artifact 10341240369. Next: ONE V25 scan on exactly one selected unit while Issyl is active, then send COPY REPORT.`
