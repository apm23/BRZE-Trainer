# BRZE Hero Effect Expiry Tail V25 — STATE

Status: **BUILT / CI-PROVEN READ-ONLY — RUNTIME EXPIRY-TAIL SCAN PENDING**
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
8. dump RVA `0x13A349`, already observed as a direct call from the V24 tick body, plus all direct `.text` xrefs to it.

## Safety
STRICT READ-ONLY:
- PROCESS_VM_READ + PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no VirtualAllocEx / VirtualProtectEx;
- no hooks;
- no native ability calls;
- no destructor/cleanup invocation;
- no CreateRemoteThread.

## V25 CI pin
Workflow: `Hero Effect Expiry Tail V25 Read Only`
- run `34827531830` — SUCCESS
- job `103923007903` — SUCCESS
- head `45e0bc3972544908fd1b92877d4bca52cb1effa3`
- artifact `10341240369`
- artifact digest `sha256:9d7879a39c9b47085ab99e40cba0fb41d6bfd36f4714649a47bdf0237eeee2aa`
- standalone SHA256 `4b569a3e0155fce68d74fd344cda4f0c679c08eb7c7bddd01a31db62b42bac57`
- small SHA256 `07b3ad5af41a3247014d8355005a4581941e5c4e7a271d171363874da8dd2de1`

CI passed:
- strict read-only architecture verifier;
- compile smoke;
- standalone publish;
- small publish;
- hash step;
- artifact upload.

## Exact runtime test
1. Give Issyl to exactly ONE unit.
2. While Issyl is visibly active, select only that unit.
3. Open `BRZE-Hero-Effect-Expiry-Tail-V25-ReadOnly.exe`.
4. Click `SCAN EXPIRY TAIL` once.
5. Click `COPY REPORT` and send the full report.

No wait for natural expiry is required. V25 performs no game writes, hooks, or native calls.

## Decision rule after V25 runtime report
Advance to a guarded cleanup proof only if the report structurally identifies:
- the duration comparison using config+0xE0 (or an equivalent clearly duration-derived value), AND
- the exact branch/call that represents natural expiry/unlink for this effect instance.

If not, narrow again. Do not guess or call a generic destructor merely because it is in the vtable.
