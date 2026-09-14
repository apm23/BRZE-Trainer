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
- Issyl A5 config base nominal `15000`; natural wall ~`10.7271s`.
- Grayback C0 config base nominal `60000`; natural wall ~`42.2221s`.
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
User wants Hero Effect APPLY to behave like replacement/reset:

`if selected unit already has same Issyl/Grayback buff -> remove/reset OLD same-effect instance safely -> one-shot fresh apply -> requested duration starts again from zero`

Unrelated buffs must remain untouched.

Do NOT implement this by simply calling native apply again on an already-active same effect. Old V3 proved repeated application can compound/stack into extreme speed, invulnerability-like behavior, one-hit buildings, and other corruption.

## V22 Group-Safe Hero — current integrated fallback
V22 fixes V21 generic transient-root problems using content signature:
- effect record `+0x058 = ability ID`;
- effect record `+0x17C = target Unit*`;
- transient path itself is NOT stable and must never be hard-coded.

V22 CI:
- workflow `Final V22 Group Safe Hero Effect`
- run `34823248974` SUCCESS
- job `103909409512` SUCCESS
- head `a8e55e21d5b20e468fd08db14b00d3d6cba5dc73`
- artifact `10339202775`

V22 remains fallback only; user prefers true reset/replace behavior instead of skip/hold semantics.

## V23 Reset Forensics — BUILT / CI-PROVEN READ ONLY
State: `HeroEffectResetForensicsV23/STATE.md`

Purpose: identify a REAL native/per-instance cleanup/expire path before any reset write/call is attempted.

V23B is strictly read-only:
- PROCESS_VM_READ + PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no VirtualAllocEx;
- no hooks;
- no native ability calls;
- no destructor invocation;
- no CreateRemoteThread.

Method:
1. select exactly one unit with active Issyl A5;
2. signature-lock exact effect record with:
   - `record+0x058 == 0xA5`
   - `record+0x17C == selected Unit*`;
3. capture parent vtable/type pointer, `parent+0x194`, and `parent+0x1F4` config;
4. parse live BRZE PE `.text`;
5. disassemble with Iced x86;
6. inspect first 48 vtable slots and rank `.text` references to `+0x194`, including likely writes/calls and co-occurrence with known effect offsets.

`parent+0x194` remains HARD REJECTED as duration; here it is ONLY a teardown/static-analysis lead.

### V23B CI pin
Workflow: `Hero Effect Reset Forensics V23B Read Only`
- run `34825682469` — SUCCESS
- job `103917157659` — SUCCESS
- head `5a3c22b563eb879be2212afa20a05c49b9af625a`
- artifact `10340067680`
- digest `sha256:60153b70296f7f254eb9785812f2518c9a85f60f9ba8648d9f81cfb9cf704f6b`
- standalone SHA256 `db5005e355f6be87b6330a033ed100ddf4b906079b2f5a593874178e8d6273d7`
- small SHA256 `cef5f68a820b95512b22939d2ed18e19ef584cc83823fa6461a1fd2bff73630c`

First V23 workflow attempt failed only on Iced C# API compile compatibility before runtime; V23B fixed Decoder qualification / decode loop and passed all CI.

## EXACT NEXT ACTION — ONE READ-ONLY RUNTIME SCAN
Do not start a broad test loop.

1. Launch BRZE.
2. Give Issyl to exactly one unit using original game or V22.
3. While Issyl is visibly ACTIVE, select ONLY that unit.
4. Open `BRZE-Hero-Effect-Reset-Forensics-V23B-ReadOnly.exe`.
5. Click `SCAN ACTIVE ISSYL` once.
6. Click `COPY REPORT` and send the report.

The scan does not modify the game. After the report, analyze ranked vtable / +0x194 candidates and choose the smallest structurally justified cleanup proof. Do NOT call or write any cleanup candidate until evidence supports it.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. User wants true Hero Effect RESET/REPLACE semantics, never repeated stacking. V22 remains integrated fallback. V23B read-only cleanup forensics is BUILT/CI-PROVEN: signature-lock active A5 record, inspect vtable + live .text teardown candidates, no writes/hooks/native calls. CI run 34825682469 SUCCESS, artifact 10340067680. Next: ONE runtime scan on exactly one selected unit while Issyl is visibly active, then send COPY REPORT.`
