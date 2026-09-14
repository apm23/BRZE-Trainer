# BRZE Hero Effect Current-Tick Reset V29 — STATE

Status: **BUILT NEXT — GUARDED DIRECT-CURRENT-TICK RESET PROOF**
Date: 2026-09-14 JST

## V28 runtime proof that unlocks V29
V28 exact register flow proved the tick argument source, not merely a candidate.

Observed runtime:
- moduleBase `0x00870000`
- active A5 record `0x235B0A48`
- record+0x194 start timestamp `486200`
- tick target RVA `0x13A5EB`

Exact argument flow:
- in caller RVA `0x1400F0`, `EBX <- [EBP+8]` at `0x140106`, then `PUSH EBX` at `0x140120`, then CALL tick at `0x140125`;
- its caller RVA `0x1C3107` passes `ESI` at `0x1C3243`;
- V28 backward trace proves `ESI <- [0x00CB0A3C]` at `0x1C3181`;
- therefore the concrete current-time source for the tick argument is absolute `0x00CB0A3C` in this runtime, equivalent to module RVA `0x440A3C`.

At the V28 scan:
- `[0x00CB0A3C] = 489500`
- active A5 start timestamp = `486200`
- elapsed difference = `3300`

This is structurally consistent with a live effect that has already been running for several game ticks.

The 350 ms sampling window showed no change because BRZE can pause simulation while unfocused; this does not weaken the exact register-flow provenance of the argument source.

## V26 rejection retained
Writing `record+0x194 = 0` is permanently rejected. V26 proved an already-active instance does not re-enter timestamp initialization merely because this field becomes zero.

## V29 guarded hypothesis
For exactly ONE selected unit with an active stock Issyl A5:
1. signature-lock the exact record with `+0x58 == A5` and `+0x17C == selected Unit*`;
2. require config ID A5 and stock duration 15000;
3. read old `record+0x194`;
4. read current BRZE game tick from `moduleBase + 0x440A3C`;
5. require current tick > old timestamp and a plausible elapsed delta;
6. write exactly ONE field: `record+0x194 = current BRZE tick`;
7. verify exact readback and same A5+target signature;
8. do NOT call any native ability helper and do NOT create a second effect instance.

If the timestamp controls elapsed lifetime exactly as V25 proved, this should restart the existing Issyl lifetime from the reset moment without stacking.

## Safety
- exactly ONE selected unit;
- active A5+target signature required;
- config ID must be A5;
- first proof requires stock duration 15000;
- current tick must come directly from proven RVA `0x440A3C`;
- current tick must be newer than old start timestamp;
- elapsed delta must be at least 100 and at most 60000;
- only intended mutation is one 4-byte write to `record+0x194`;
- no hooks;
- no allocation;
- no CreateRemoteThread;
- no native replay/application call;
- no destructor/cleanup call.

## Runtime PASS requirements
- V29 reports exact old timestamp and current BRZE tick;
- post-write `record+0x194` equals that exact current BRZE tick;
- same A5+target signature remains valid;
- no crash/freeze/corruption;
- visually, Issyl persists for approximately one fresh stock lifetime from the reset moment.

If PASS, the reset primitive is ready to be integrated into selected-unit same-buff replacement semantics. Grayback C0 should then receive the same primitive only after one equivalent guarded proof or by reusing the same clock-field semantics with strict C0 signature/config guards.
