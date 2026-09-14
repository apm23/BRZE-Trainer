# BRZE Hero Effect Expiry Tail V25 — STATE

Status: **BUILT NEXT — READ-ONLY NATURAL EXPIRY DECISION SCAN**
Date: 2026-09-14 JST

## Input from V24 runtime
V24 runtime report successfully re-locked active Issyl A5 and showed:
- A5 parent signature valid (`record+0x058 == A5`, `record+0x17C == selected Unit*`);
- strongest V23 raw hit `0x13DA9C` is a short field-copy tail and NOT a justified cleanup entry;
- `0x2151DE` / `0x24A7B8` are massive contiguous field writers/copy/init style code, not cleanup candidates;
- `0x16B76A` is repetitive field-registration/assignment style code;
- the structurally important function is the validated function starting at RVA `0x13A5EB`.

Inside `0x13A5EB`, V24 showed:
- target access at record `+0x17C`;
- config pointer access at record `+0x1F4`;
- `record+0x194` is checked for zero and then initialized from function argument `[EBP+8]` at RVA `0x13A8BC..0x13A8BF`;
- therefore `+0x194` is consistent with a start/current timestamp or lifecycle stamp, NOT duration and NOT by itself cleanup.

V24 report was truncated before the function tail that should contain the natural expiry decision. That is the only reason V25 exists.

## V25 goal
Resolve the smallest missing structural proof:

`record +0x194 start/timestamp` + `record+0x1F4 -> config+0xE0 duration` -> comparison/arithmetic -> exact branch/call/return that marks the effect expired or hands it to unlink/cleanup.

V25 is NOT a write/call probe.

## V25 method
1. signature-lock exactly one active A5 record;
2. decode the full function beginning at RVA `0x13A5EB` far enough past the V24 `+0x194` marker;
3. track register loads from `record+0x1F4` and annotate later memory accesses through that register to `+0xE0` / `+0xE4`;
4. tag accesses to `[EBP+8]`, record `+0x194`, record `+0x17C`, and record `+0x1F4`;
5. print the tick-function tail from before the stamp marker through the first return;
6. print focus windows around proven nested `config+0xE0` / `config+0xE4` accesses;
7. list direct CALL targets near the duration decision;
8. also dump RVA `0x13A349`, already observed as a direct call from the V24 tick body, plus all direct `.text` xrefs to it.

## Safety
STRICT READ-ONLY:
- PROCESS_VM_READ + PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no VirtualAllocEx / VirtualProtectEx;
- no hooks;
- no native ability calls;
- no destructor/cleanup invocation;
- no CreateRemoteThread.

## Decision rule after V25 runtime report
Advance to a guarded cleanup proof only if the report structurally identifies:
- the duration comparison using config+0xE0 (or an equivalent clearly duration-derived value), AND
- the exact branch/call that represents natural expiry/unlink for this effect instance.

If not, narrow again. Do not guess or call a generic destructor merely because it is in the vtable.
