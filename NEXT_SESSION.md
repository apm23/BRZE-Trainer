# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks
- Locked main stable fallback: V18.3 (`FINAL_CURRENT.md`).
- V19 = latest pre-integration main-trainer fallback.
- V20 = integrated fallback.
- V21 Direct Hero = runtime-rejected due generic transient-root gating/hold bugs.
- V22 Group-Safe Hero = built/CI-proven integrated fallback while reset semantics are researched.
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

Never implement this by blindly applying again on an already-active same effect. V3 proved repeated application can stack/compound into extreme speed, invulnerability-like behavior, one-hit buildings, and other corruption.

## V22 Group-Safe Hero — current integrated fallback
V22 fixes V21 generic transient-root problems using exact content signature:
- effect record `+0x058 = ability ID`;
- effect record `+0x17C = target Unit*`;
- transient root path itself is NOT stable and must never be hard-coded.

V22 CI:
- run `34823248974` SUCCESS
- job `103909409512` SUCCESS
- head `a8e55e21d5b20e468fd08db14b00d3d6cba5dc73`
- artifact `10339202775`

## V23 Reset Forensics — RUNTIME-PROVEN READ-ONLY DISCOVERY
State: `HeroEffectResetForensicsV23/STATE.md`

Runtime report from 2026-09-14 successfully locked:
- Unit* `0x22B47F2C`
- A5 parent `0x235ABEE0`
- path `Unit+0x1E4->+0x008`
- vtable `0x00C138EC`
- config `0x1D1FA664`
- config ID `0xA5`
- nominal duration `15000`

Relevant vtable methods:
- VT[4]  `0x14053A`
- VT[5]  `0x1405A2`
- VT[20] `0x142450`
- VT[23] `0x142C28`
- VT[24] `0x142C6E`

Strongest cleanup/teardown lead from V23:
- RVA `0x13DA9C`
- writes displacement `+0x194`
- local window also touches `+0x058`, `+0x17C`, `+0x194`, `+0x1F4`
- score `44`, highest V23 result.

Secondary leads: `0x13A8B3`, `0x13A8BF`, `0x16B76A`, `0x2151DE`, `0x24A7B8`.

IMPORTANT: V23 proves only an instruction reference, NOT a callable cleanup function. Do not call `0x13DA9C` and do not write `+0x194`.

## V24 Deep Scan — BUILT / CI-PROVEN READ ONLY
State: `HeroEffectResetDeepScanV24/STATE.md`

Purpose:
- resolve probable function boundary containing the strongest V23 marker;
- decode full containing function with operand kinds, memory displacement and near call/branch targets;
- inspect top secondary candidate functions;
- decode the five relevant A5 vtable methods from exact runtime pointers;
- remain strictly read-only.

V24 primary candidates:
- `0x13DA9C`
- `0x13A8B3`
- `0x16B76A`
- `0x2151DE`
- `0x24A7B8`

V24 CI pin:
- workflow `Hero Effect Reset Deep Scan V24 Read Only`
- run `34826622129` — SUCCESS
- job `103920156334` — SUCCESS
- head `2a8e5acac7402f66a08d7a826b2bc77b7ccad91e`
- artifact `10339563933`
- digest `sha256:ab1103571e04c8de3a92c291bb58b45d0a60fde0f3f251ac9190a7b86d62dbf2`
- standalone SHA256 `333cd6826f0592e8408a8fa5862410b2e432e038738ebea826ec97952ed98ab2`
- small SHA256 `fe903cb0630d9ca6be8451a97dfb738efc1d4fa17ad8f532dfd85d76af4ff228`

## EXACT NEXT ACTION — ONE V24 READ-ONLY DEEP SCAN
1. Give Issyl to exactly ONE unit.
2. While Issyl is visibly active, select only that unit.
3. Open `BRZE-Hero-Effect-Reset-DeepScan-V24.exe`.
4. Click `DEEP SCAN ACTIVE ISSYL` once.
5. Click `COPY REPORT` and send the full report.

No natural-expiry wait is needed. V24 performs no game writes/hooks/native calls.

After the report:
- if a structurally coherent natural-expiry/unlink/destruction boundary is identified, make the smallest guarded proof;
- otherwise build a narrower trace probe around the exact call boundary rather than guessing.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. User wants true Hero Effect RESET/REPLACE semantics, never repeated stacking. V23 runtime-proven read-only discovery found strongest cleanup lead RVA 0x13DA9C (write +0x194 with nearby +0x58/+0x17C/+0x1F4), but it is NOT yet callable proof. V24 deep function scan is BUILT/CI-PROVEN READ ONLY, run 34826622129 SUCCESS, artifact 10339563933. Next: ONE V24 scan on exactly one selected unit while Issyl is active, then send COPY REPORT.`
