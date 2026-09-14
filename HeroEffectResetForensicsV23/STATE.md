# BRZE Hero Effect Reset Forensics V23 — STATE

Status: **BUILT / CI-PROVEN READ-ONLY — RUNTIME SCAN PENDING**
Date: 2026-09-14 JST

## Goal
Find a real per-instance cleanup/expire path for active hero effects so final trainer semantics can become:

`APPLY again = remove/reset old A5/C0 instance on selected unit -> one-shot apply new instance -> new duration starts from zero`

without stacking/repeated refresh.

## Why this probe exists
User requested replacement/reset semantics instead of V22 skip/hold behavior.

Rejected implementation:
- blindly call Replay V2 again on a unit that already has the same active effect;
- V3 runtime already proved repeated native application can compound/stack into extreme speed, invulnerability-like damage behavior, one-hit buildings, and other corruption.

Therefore V23 does NOT modify or remove anything. It performs strict read-only forensics first.

## V23 method
1. user selects exactly one unit with ACTIVE Issyl A5;
2. content-signature lock using proven V6 identity:
   - `record+0x058 == 0xA5`
   - `record+0x17C == selected Unit*`;
3. capture:
   - parent record address;
   - `parent+0x000` vtable/type pointer;
   - `parent+0x194` teardown-related field;
   - `parent+0x1F4` ability config pointer;
4. parse the live BRZE PE `.text` section;
5. use Iced x86 decoder to inspect:
   - first 48 vtable slots that resolve into `.text`;
   - decoded module instructions referencing displacement `+0x194`;
   - local co-occurrence with known record offsets `+0x058`, `+0x17C`, `+0x1F4`;
   - likely writes to `+0x194` and nearby calls;
6. rank candidate RVAs for follow-up native cleanup identification.

## Safety
- PROCESS_VM_READ + PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no VirtualAllocEx;
- no hooks;
- no native ability call;
- no object destructor invocation;
- no vtable modification;
- no CreateRemoteThread.

## Locked prior evidence
- V6 content-signature identity is robust; transient root path is not.
- `parent+0x194` is NOT duration; observed values tracked teardown/global timing, so it is used only as a static-analysis lead here.
- V7 found no safe continuously-changing per-instance timer.
- V11 duration config path remains proven and unchanged.
- V22 remains fallback integrated trainer while reset semantics are researched.

## CI build pin
Authoritative build workflow: `Hero Effect Reset Forensics V23B Read Only`
- run `34825682469` — SUCCESS
- job `103917157659` — SUCCESS
- head `5a3c22b563eb879be2212afa20a05c49b9af625a`
- artifact `10340067680`
- artifact digest `sha256:60153b70296f7f254eb9785812f2518c9a85f60f9ba8648d9f81cfb9cf704f6b`
- standalone SHA256 `db5005e355f6be87b6330a033ed100ddf4b906079b2f5a593874178e8d6273d7`
- small SHA256 `cef5f68a820b95512b22939d2ed18e19ef584cc83823fa6461a1fd2bff73630c`

CI passed:
- strict read-only architecture verifier;
- Iced x86 decoder compile compatibility patch;
- compile smoke;
- standalone publish;
- small publish;
- output hashes;
- artifact upload.

The first V23 workflow attempt failed only on C# Iced API compatibility (`Decoder` ambiguity / missing `CanDecode`), before any runtime probe existed. V23B fixes those compile-only issues without changing the read-only research design.

## Exact runtime test
Use a clean throwaway save/runtime:
1. apply Issyl to one unit using original game or V22;
2. while Issyl is visibly active, select exactly that ONE unit;
3. open V23B read-only probe;
4. click `SCAN ACTIVE ISSYL`;
5. click `COPY REPORT` and send the report back.

Only one scan is expected. The next step will be chosen from the ranked native candidates; do not test random writes or native calls before the report is analyzed.
