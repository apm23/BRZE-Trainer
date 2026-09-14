# BRZE Hero Effect Register Flow V28 — STATE

Status: **BUILT NEXT — READ-ONLY EXACT OPERAND/STACK TRACE**
Date: 2026-09-14 JST

## Why V28 exists
V27 runtime proved a unique direct caller chain to the hero-effect tick function:

`0x144691 -> 0x144794 -> 0x1C3107 -> 0x1400F0 -> 0x13A5EB`

However V27 printed operand kinds but not exact register names, so the stack argument that becomes `[EBP+8]` inside `0x13A5EB` could not yet be traced to a concrete source.

## V28 purpose
Strict read-only operand-aware tracing:
- print exact register names for the call windows;
- identify the last PUSH before each direct call;
- backward-slice pushed registers through MOV chains;
- explicitly detect `[EBP+8]` propagation;
- detect absolute/global memory sources;
- sample absolute memory candidates 350 ms apart and flag changing clock-like values;
- walk the unique caller chain upward up to five levels.

## Safety
- PROCESS_VM_READ | PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no hooks;
- no allocation/injection;
- no native ability calls;
- no thread context mutation.

## Decision after runtime
The next write proof is allowed only if V28 identifies the current game-time argument source concretely. V26 zero-reset remains rejected forever.
