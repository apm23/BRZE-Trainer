# BRZE Hero Effect Reset Forensics V23 — STATE

Status: **BUILT NEXT — READ-ONLY NATIVE CLEANUP DISCOVERY**
Date: 2026-09-14 JST

## Goal
Find a real per-instance cleanup/expire path for active hero effects so final trainer semantics can become:

`APPLY again = remove/reset old A5/C0 instance on selected unit -> one-shot apply new instance -> new duration starts from zero`

without stacking/repeated refresh.

## Why this probe exists
User requested replacement/reset semantics instead of V22 skip/hold behavior.

Rejected implementation:
- blindly call Replay V2 again on a unit that already has the same active effect.
- V3 runtime already proved repeated native application can compound/stack into extreme speed, invulnerability-like damage behavior, and other corruption.

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

## Runtime test
Use a clean throwaway save/runtime:
1. apply Issyl to one unit using original game or V22;
2. while Issyl is visibly active, select exactly that one unit;
3. close any other standalone hook probe if necessary; V23 itself does not hook;
4. open V23 and click `SCAN ACTIVE ISSYL`;
5. `COPY REPORT` and send the report back.

Only one scan is expected. The next step will be chosen from the ranked native candidates; do not test random writes.
