# BRZE Hero Effect Clock Reset V26 — STATE

Status: **BUILT NEXT — GUARDED ONE-FIELD RESET PROOF / CI TRIGGERED**
Date: 2026-09-14 JST

## Evidence from V25
V25 runtime disassembly proved the active effect record uses `+0x194` as a start/lifecycle timestamp:
- in tick function RVA `0x13A5EB`, when `record+0x194 == 0`, the game stores `[EBP+8]` current tick argument into `record+0x194`;
- later, code at RVA `0x13B1C3` computes current time minus `record+0x194`;
- it loads `record+0x1F4 -> config+0xE0` and compares elapsed time against duration;
- exceeding that threshold branches toward expiry handling.

This does NOT reclassify `+0x194` as duration. It remains a timestamp/start-clock field.

## V26 hypothesis
A true reset/restart may not require cleanup + native reapply at all.

For ONE active Issyl A5 instance:
1. signature-lock exact effect record with `record+0x58 == 0xA5` and `record+0x17C == selected Unit*`;
2. require stock A5 config identity and duration `15000`;
3. capture nonzero old `record+0x194`;
4. write exactly one field: `record+0x194 = 0`;
5. do NOT call any native ability helper;
6. poll the same record and verify the game itself replaces zero with a new nonzero timestamp.

If confirmed, this means the existing effect instance can be restarted from zero without stacking a second effect instance.

## Safety
- exactly ONE selected unit;
- active A5+target signature required;
- config ID must be A5;
- first proof requires stock duration 15000;
- only one 4-byte write is attempted: `record+0x194 = 0`;
- no hooks;
- no allocation;
- no CreateRemoteThread;
- no native replay/application call;
- no destructor/cleanup call;
- if timestamp stays zero for ~1 second while signature remains valid, V26 attempts to restore the original timestamp.

## Decision after runtime
PASS requires:
- same A5 record remains signature-valid;
- old timestamp is nonzero;
- after writing zero, game repopulates `+0x194` with a DIFFERENT nonzero timestamp;
- no crash/freeze/corruption.

If PASS, next integrate reset semantics as:
- same active ability on selected unit -> reset existing instance clock, do NOT replay;
- no existing same ability -> one-shot native apply;
- duration config continues to use the already-proven guarded full-lifetime hold rules.

CI trigger note: workflow file was created in the preceding commit; this state-only commit intentionally triggers the new V26 workflow without altering runtime code.
