# BRZE Hero Effect Current-Tick Reset V29 — STATE

Status: **RUNTIME-PROVEN PASS — RESET PRIMITIVE LOCKED**
Date: 2026-09-14 JST

## Proven current-tick source
V28 exact register flow proved the tick argument source:
- moduleBase during proof: `0x00870000`;
- tick target RVA `0x13A5EB`;
- `0x140106`: `EBX <- [EBP+8]`;
- `0x140120`: `PUSH EBX`;
- caller `0x1C3243`: `PUSH ESI`;
- `0x1C3181`: `ESI <- [0x00CB0A3C]`.

Therefore the BRZE current simulation tick source is module RVA **`0x440A3C`**.

BRZE pauses its simulation/game clock while minimized. A static current-tick sample while minimized is expected and is not evidence of a bad source.

## V29 runtime proof
User report:
- PID `6528`, moduleBase `0x00870000`;
- selected Unit* `0x22B47F2C`, UnitDef* `0x1688993C`, owner `0`;
- active A5 record `0x235B0E40` via `Unit+0x1E4->+0x008`;
- signature remained `ability=A5 + target Unit*`;
- config `0x1D1FA664`, ID A5, duration 15000;
- old `record+0x194 = 507200`;
- current BRZE tick from module RVA `0x440A3C = 519700`;
- pre-reset elapsed delta `12500`;
- V29 wrote ONLY `record+0x194 = 519700`;
- exact post-write readback `519700`;
- same A5+target signature remained valid;
- no native ability helper call and no second effect instance was created by V29.

Visual runtime result from user:
- the selected/reset unit kept Issyl active longer than Issyl himself / the comparison unit that was not reset.

## Locked conclusion
**For an already-active same-effect instance, writing the current BRZE simulation tick from module RVA `0x440A3C` into `record+0x194` restarts that instance's lifetime from the reset moment without native reapply or stacking.**

This is now the approved reset primitive for integration.

## Rejected forever
- `record+0x194 = 0` zero-reset (V26 runtime rejected);
- blind repeated native replay/refresh on an already-active same effect (V3 stacking/compound failure);
- treating `record+0x194` as duration — it is the start/lifecycle timestamp;
- guessed wall-clock conversion instead of the actual BRZE tick source.

## Integration
V30 uses this primitive for existing same-effect records and only uses one-shot Replay V2 native apply for effects that are actually missing. Duration continues to use the separately proven `record+0x1F4 -> config+0xE0` full-lifetime hold/restore rules.
