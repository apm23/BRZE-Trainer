# BRZE Hero Effect Register Flow V28 — STATE

Status: **RUNTIME-PROVEN — CURRENT BRZE TICK SOURCE IDENTIFIED**
Date: 2026-09-14 JST

## Runtime result
V28 exact operand/register tracing successfully resolved the stack argument consumed by hero-effect tick RVA `0x13A5EB` to a concrete BRZE global.

Runtime context:
- PID `6528`
- moduleBase `0x00870000`
- selected Unit* `0x22B47F2C`
- active A5 record `0x235B0A48`
- record+0x194 start timestamp `486200`
- config `0x1D1FA664`, duration `15000`

## Proven argument chain
Unique caller chain:

`0x144691 -> 0x144794 -> 0x1C3107 -> 0x1400F0 -> 0x13A5EB`

Exact lower-level flow:
- RVA `0x140106`: `EBX <- [EBP+8]`
- RVA `0x140120`: `PUSH EBX`
- RVA `0x140125`: CALL tick `0x13A5EB`

Its direct caller:
- RVA `0x1C3243`: `PUSH ESI`
- V28 backward trace: RVA `0x1C3181`: `ESI <- [0x00CB0A3C]`

Therefore the concrete source of the tick function's current-time argument is:
- runtime absolute `0x00CB0A3C` for module base `0x00870000`
- equivalent module RVA **`0x440A3C`**

Observed values at runtime:
- current BRZE tick global `[0x00CB0A3C] = 489500`
- active A5 start timestamp `486200`
- elapsed delta `3300`

This is structurally consistent with a live effect that has already been active for several game ticks.

The 350ms re-sample showed delta 0 because BRZE simulation may pause while the game is unfocused. This does not weaken the register-flow proof: the value was proven as the exact argument source by instruction/data flow, not inferred from movement alone.

## Safety / rejected paths
- V28 was strict read-only.
- V26 `record+0x194 = 0` remains permanently rejected.
- Never guess a wall-clock conversion for reset.
- Never blindly native-reapply on an already-active same effect.

## V28 CI pin
Workflow: `Hero Effect Register Flow V28 Read Only`
- run `34833344064` — SUCCESS
- job `103941510533` — SUCCESS
- head `7078921824d79139db3784a3a0916cf5a5d80d80`
- artifact `10343645827`
- artifact digest `sha256:24c0897627c3d972d5e3988722be7ea11873905df1a48e81bc52fe71057daee8`
- standalone SHA256 `1bfda24034eead2692a94382fe281deb325685ff07b340d2932bba194c721053`
- small SHA256 `8d7000c5b78f4509923204da274aebeafd1b99b9f5af861b67030d534623b17b`

## Decision
**GO to one guarded current-tick write proof.**

Next proof may write only the exact current BRZE tick read from module RVA `0x440A3C` into the active stock A5 record's `+0x194`, with strict A5+target/config guards and no native replay.
