# BRZE Hero Effect Current-Time Source V27 — STATE

Status: **RUNTIME-PROVEN READ-ONLY — UNIQUE CALLER CHAIN FOUND, EXACT ARG SOURCE STILL UNRESOLVED**
Date: 2026-09-14 JST

## Why V27 exists
V26 runtime rejected the hypothesis that writing `record+0x194 = 0` causes an already-active effect to re-enter the normal timestamp-initialization path.

V26 runtime result:
- live A5 record `0x235ADCA4`
- old `record+0x194 = 375600`
- V26 wrote only `+0x194 = 0`
- game did not repopulate it within ~1 second
- fail-safe restored `375600` successfully

Therefore zero-reset is permanently rejected.

## Still-proven V25 fact
Function RVA `0x13A5EB` consumes a stack argument at `[EBP+8]` that participates as the current game-time/tick input:
- creation/init path copies `[EBP+8]` into `record+0x194` when the record is being initialized;
- later expiry logic subtracts `record+0x194` from current time and compares elapsed time with `record+0x1F4 -> config+0xE0`.

## V27 CI pin
Workflow: `Hero Effect Current-Time Source V27 Read Only`
- run `34830108923` — SUCCESS
- job `103931199444` — SUCCESS
- head `564c2bf48f3d0885cc6fd42e0268d9b54364a398`
- artifact `10340943840`
- artifact digest `sha256:6cfd165b07768f89773c68dfc4273727879fd173e997db3ad0cdd9493559b9f3`
- standalone SHA256 `014a74d59dc05ad170057c9bb04d7e033e6e8a29376630bc3c6f65bf26fce36e`
- small SHA256 `5deeeb1437acc3a4eab203959b5fd94e8a947f4cef113da0b8d7dd40cdb021ee`

## V27 runtime result
Runtime context:
- PID `6528`, moduleBase `0x00870000`
- selected Unit* `0x22B47F2C`, UnitDef* `0x1688993C`, owner `0`
- active A5 record `0x235AE09C` via `Unit+0x1E4->+0x008`
- record+0x194 `400600`
- config `0x1D1FA664`, duration `15000`

Direct call discovery:
- exactly ONE direct E8 call to tick RVA `0x13A5EB`:
  - call site `0x140125`
  - caller function start `0x1400F0`
- no runtime pointer refs to tick function outside `.text`.

Unique recursive direct-caller chain:
`0x144691 -> 0x144794 -> 0x1C3107 -> 0x1400F0 -> 0x13A5EB`

Important limitation:
- V27 printed operand kinds but not exact register names;
- therefore the value pushed immediately before `0x140125`, and its propagation from upper callers, is not yet concretely identified;
- V27 did NOT identify a clock global and did NOT justify a write.

## Decision
Advance to V28 exact-register/stack tracing. No timestamp write yet.
