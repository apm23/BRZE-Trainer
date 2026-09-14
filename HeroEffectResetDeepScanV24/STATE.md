# BRZE Hero Effect Reset Deep Scan V24 — STATE

Status: **RUNTIME-PROVEN READ-ONLY — STRUCTURE NARROWED, NOT CLEANUP-PROVEN**
Date: 2026-09-14 JST

## Runtime input
V24 successfully re-locked one live Issyl A5 effect:
- Unit* `0x22B47F2C`
- UnitDef* `0x1688993C`
- owner `0`
- A5 parent `0x235ACCC4`
- path `Unit+0x1E4->+0x008`
- parent vtable/type `0x00C138EC`
- parent+0x194 `275200`
- config `0x1D1FA664`
- config ID `0xA5`
- config+0xE0 duration `15000`

## Main conclusions
V24 invalidated the naive interpretation that the strongest V23 +0x194 writer is itself cleanup.

### `0x13DA9C`
Boundary unresolved, but the local body is a short contiguous field-write tail:
- writes record `+0x194`, `+0x198`, `+0x19C`, `+0x1A0`, `+0x1F4`, `+0x1F8`;
- immediately returns.

Conclusion: **do not call `0x13DA9C` as cleanup**. It is structurally consistent with copy/init assignment, not natural expiry/unlink proof.

### `0x2151DE` and `0x24A7B8`
Both are long contiguous field writers spanning a very large range of offsets.

Conclusion: **copy/init style code, rejected as cleanup candidates**.

### `0x16B76A`
Shows repetitive assignment + calls to the same helper across many offsets.

Conclusion: **registration/initialization-style code, not justified cleanup**.

### Function RVA `0x13A5EB`
This is the strongest structural lead.
V24 resolved a validated classic x86 function boundary at `0x13A5EB` containing the V23 marker `0x13A8B3`.

Within the function:
- reads target from record `+0x17C`;
- repeatedly reads ability config from record `+0x1F4`;
- at `0x13A8B3` checks record `+0x194` for zero;
- if zero, `0x13A8BC..0x13A8BF` copies function argument `[EBP+8]` into record `+0x194`.

This strongly supports `+0x194` as a lifecycle/start/current timestamp field. It remains HARD REJECTED as duration and is not itself a cleanup target.

V24 output ended before the full tail of `0x13A5EB`, so the exact natural-expiry duration comparison / unlink branch was not yet visible.

## Relevant vtable observations
A5 vtable methods were decoded at:
- VT[4]  `0x14053A`
- VT[5]  `0x1405A2`
- VT[20] `0x142450`
- VT[23] `0x142C28`
- VT[24] `0x142C6E`

None of these is promoted to cleanup solely from V24. Do not invoke generic/destructor-looking vtable methods without a natural-expiry call-chain proof.

## V24 CI pin
Workflow: `Hero Effect Reset Deep Scan V24 Read Only`
- run `34826622129` — SUCCESS
- job `103920156334` — SUCCESS
- head `2a8e5acac7402f66a08d7a826b2bc77b7ccad91e`
- artifact `10339563933`
- artifact digest `sha256:ab1103571e04c8de3a92c291bb58b45d0a60fde0f3f251ac9190a7b86d62dbf2`
- standalone SHA256 `333cd6826f0592e8408a8fa5862410b2e432e038738ebea826ec97952ed98ab2`
- small SHA256 `fe903cb0630d9ca6be8451a97dfb738efc1d4fa17ad8f532dfd85d76af4ff228`

## Next action
V25 (`HeroEffectExpiryTailV25`) is the next read-only probe.
It targets the full tail of function `0x13A5EB`, follows register flow from record `+0x1F4` into config `+0xE0/+0xE4`, and looks for the exact duration comparison plus natural-expiry branch/call.

Do not call/write any cleanup candidate before V25 evidence.
