# BRZE Hero Effect Reset Deep Scan V24 — STATE

Status: **BUILT / CI-PROVEN READ-ONLY — RUNTIME DEEP SCAN PENDING**
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

## V24 CI pin
Workflow: `Hero Effect Reset Deep Scan V24 Read Only`
- run `34826622129` — SUCCESS
- job `103920156334` — SUCCESS
- head `2a8e5acac7402f66a08d7a826b2bc77b7ccad91e`
- artifact `10339563933`
- artifact digest `sha256:ab1103571e04c8de3a92c291bb58b45d0a60fde0f3f251ac9190a7b86d62dbf2`
- standalone SHA256 `333cd6826f0592e8408a8fa5862410b2e432e038738ebea826ec97952ed98ab2`
- small SHA256 `fe903cb0630d9ca6be8451a97dfb738efc1d4fa17ad8f532dfd85d76af4ff228`

CI passed:
- strict read-only architecture verifier;
- compile smoke;
- standalone publish;
- small publish;
- output hash step;
- artifact upload.

## Exact runtime test
1. Give Issyl to exactly ONE unit using the original game or known-good trainer path.
2. While Issyl is visibly active, select only that unit.
3. Open V24.
4. Click `DEEP SCAN ACTIVE ISSYL` once.
5. Click `COPY REPORT` and send the full report.

No waiting for natural expiry is required for this scan. V24 does not modify the game.

## Decision rule after runtime report
Do NOT choose a cleanup call merely because it writes `+0x194`.

Advance only if V24 shows a structurally coherent function/call chain tied to the exact effect record and natural expiry/unlink/destruction semantics. Otherwise build a smaller trace probe around the narrowed call boundary instead of guessing.
