# BRZE Hero Effect Clock Reset V26 — STATE

Status: **BUILT / CI-PROVEN — GUARDED ONE-FIELD RESET RUNTIME PROOF PENDING**
Date: 2026-09-14 JST

## Runtime evidence from V25
V25 runtime disassembly proved the active effect record uses `+0x194` as the start/lifecycle timestamp, not as duration.

Observed live A5:
- selected Unit* `0x22B47F2C`
- A5 record `0x235AD8AC`
- config `0x1D1FA664`
- config ID `0xA5`
- config+0xE0 `15000`
- record+0x194 `355400`

Exact tick flow:
- RVA `0x13A8B3`: compare `record+0x194` against zero;
- RVA `0x13A8BC..0x13A8BF`: if zero, copy current tick argument `[EBP+8]` into `record+0x194`;
- RVA `0x13B1C3`: load current time;
- RVA `0x13B1C6`: subtract `record+0x194`;
- RVA `0x13B1CC`: load `config+0xE0` nominal duration;
- RVA `0x13B1E2`: compare elapsed vs duration-derived value;
- RVA `0x13B1E5`: branch to `0x13B74D` when elapsed exceeds threshold.

This strongly supports a safer reset primitive than cleanup + reapply: zero the existing instance start timestamp and let the game's own tick initialize a fresh start time.

## V26 guarded hypothesis
For ONE active stock Issyl A5 instance:
1. signature-lock exact record with `record+0x58 == 0xA5` and `record+0x17C == selected Unit*`;
2. require config identity A5 and stock config+0xE0 `15000`;
3. capture nonzero old `record+0x194`;
4. write exactly one field: `record+0x194 = 0`;
5. do NOT call any native ability helper;
6. poll the SAME record and verify BRZE itself replaces zero with a different nonzero timestamp.

If confirmed, existing Issyl can restart from zero without creating a second effect instance and without V3-style stacking.

## Safety
- exactly ONE selected unit;
- active A5+target signature required before write;
- config ID must be A5;
- first proof requires stock duration 15000;
- only intended mutation is one 4-byte timestamp zero write;
- no hooks;
- no allocation;
- no CreateRemoteThread;
- no native replay/application call;
- no destructor/cleanup call;
- if same signature remains but timestamp stays zero for ~1 second, V26 attempts to restore the original timestamp.

## V26 CI pin
Workflow: `Hero Effect Clock Reset V26 Guarded`
- run `34828770662` — SUCCESS
- job `103926891243` — SUCCESS
- head `14f9fd9d9e8e5bfe294d518e58be2ad31503a330`
- artifact `10341645845`
- artifact digest `sha256:4e4fb7a7918bf1510626798bfefb1f9ad56bc206891352cf3e8b791d81322e9a`
- standalone SHA256 `0900937462afc93b1aae290c135872b07ff673de1884b208b5648411d18dcaab`
- small SHA256 `f0723b84921604b2558dee30da212db574d42fe6f05e942d1a77fba374b55828`

CI PASS:
- guarded architecture verifier;
- compile smoke;
- standalone publish;
- small publish;
- hash step;
- artifact upload.

## Exact runtime proof
1. Use a fresh/stock Issyl A5 effect on exactly ONE unit.
2. While visibly active, select only that unit.
3. Open `BRZE-Hero-Effect-Clock-Reset-V26-Guarded.exe`.
4. Click `RESET ACTIVE ISSYL CLOCK` once.
5. Copy/send the report.

PASS requires:
- same A5 record remains signature-valid;
- old timestamp is nonzero;
- game repopulates +0x194 with a different nonzero timestamp;
- no crash/freeze/corruption;
- visually, Issyl lifetime restarts from the reset moment.

If PASS, next integrated semantics should be:
- selected unit already has same ability -> reset existing instance timestamp, no native replay;
- selected unit lacks same ability -> one-shot native apply;
- unrelated buffs untouched;
- duration still follows proven config+0xE0 hold rules.
