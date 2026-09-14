# BRZE Hero Effect Reset Deep Scan V24 — STATE

Status: **BUILT NEXT — READ-ONLY FUNCTION BOUNDARY / CALL-CHAIN ANALYSIS**
Date: 2026-09-14 JST

## Input from runtime-proven V23
V23 successfully signature-locked a live Issyl A5 effect record and ranked `.text` references to teardown-related `parent+0x194`.

Strongest lead:
- RVA `0x13DA9C`
- writes `+0x194`
- local window also references all known effect fields `+0x058`, `+0x17C`, `+0x194`, `+0x1F4`
- highest V23 score `44`

Relevant A5 parent vtable methods seen in V23:
- VT[4]  RVA `0x14053A`
- VT[5]  RVA `0x1405A2`
- VT[20] RVA `0x142450`
- VT[23] RVA `0x142C28`
- VT[24] RVA `0x142C6E`

## Why V24 exists
V23 did NOT prove that `0x13DA9C` itself is callable or that it is cleanup. It only proved an instruction reference.

V24 resolves more structure before any write/native call:
1. signature-lock one active A5 record again;
2. parse live `.text`;
3. for top V23 candidates, scan backward for a validated classic x86 function prologue whose decoded instruction stream reaches the exact V23 marker;
4. dump the containing function from the resolved boundary through the first return after the marker;
5. include operand kinds, memory base/index/displacement, near call/branch targets, and tags for known effect offsets;
6. decode the five relevant A5 record vtable methods from their exact runtime pointers.

Primary candidate set:
- `0x13DA9C`
- `0x13A8B3`
- `0x16B76A`
- `0x2151DE`
- `0x24A7B8`

## Safety
STRICT READ-ONLY:
- PROCESS_VM_READ + PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no VirtualAllocEx / VirtualProtectEx;
- no hooks;
- no native ability calls;
- no destructor/cleanup calls;
- no CreateRemoteThread.

## Decision rule after runtime report
Do NOT choose a cleanup call merely because it writes `+0x194`.

Advance only if V24 shows a structurally coherent function/call chain tied to the exact effect record and natural expiry/unlink/destruction semantics. Otherwise build a smaller trace probe around the narrowed call boundary instead of guessing.
