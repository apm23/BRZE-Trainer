# BRZE Hero Effect Register Flow V28 — STATE

Status: **BUILT / CI-PROVEN — READ-ONLY EXACT OPERAND/STACK TRACE, RUNTIME REPORT PENDING**
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

## V28 CI pin
Workflow: `Hero Effect Register Flow V28 Read Only`
- run `34833344064` — SUCCESS
- job `103941510533` — SUCCESS
- head `7078921824d79139db3784a3a0916cf5a5d80d80`
- artifact `10343645827`
- artifact digest `sha256:24c0897627c3d972d5e3988722be7ea11873905df1a48e81bc52fe71057daee8`
- standalone SHA256 `1bfda24034eead2692a94382fe281deb325685ff07b340d2932bba194c721053`
- small SHA256 `8d7000c5b78f4509923204da274aebeafd1b99b9f5af861b67030d534623b17b`

CI PASS:
- strict read-only architecture verifier;
- compile smoke;
- standalone publish;
- small publish;
- hash;
- artifact upload.

## Exact runtime action
1. Prefer one selected unit with active Issyl for context.
2. Open `BRZE-Hero-Effect-Register-Flow-V28-ReadOnly.exe`.
3. Click `TRACE TICK ARG FLOW` once.
4. Click `COPY REPORT` and send the full report.

## Decision after runtime
The next write proof is allowed only if V28 identifies the current game-time argument source concretely. V26 zero-reset remains rejected forever.
