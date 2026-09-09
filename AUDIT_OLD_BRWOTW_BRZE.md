# Full old trainer -> BRWOTW -> BRZE 1.60 audit

This branch is the clean-room audit baseline before another runtime specimen is produced.

## Evidence rule
A BRZE cheat is not accepted merely because it compiles or an address looks plausible. For each feature:
1. recover the old trainer operation;
2. identify the exact BRWOTW instruction/state it targets;
3. identify the semantic homolog in BRZE 1.60;
4. use byte guards / native structure semantics;
5. require runtime confirmation before marking proven.

## Current matrix

| Old cheat | Old/BRWOTW evidence | Current BRZE candidate | Status / action |
|---|---|---|---|
| F1 Maximum Resources | player resource fields D4/D8/DC/E0 in BRWOTW | player D8/DC/E0/E4; current UI splits rice/water | runtime proven rice/water; re-audit max-resource semantics |
| F2 Maximum Population | old trainer global 0x718568 = 0x05F5E0FF | indexed max-unit array RVA 0x467B90; current trainer used 9,999,999 | **REOPENED**: value/range and engine side effects must be audited; do not use extreme cap in clean specimen |
| F3 Maximum Yin/Yang | BRWOTW +2E4/+2E8 = 10 | BRZE +2E8/+2EC | runtime proven; retain after byte/struct check |
| F4 Instant Unit Training | old hook 0x41BECB; force progress 0x0064B540 before original read | BRZE RVA 0x0D5DDB v49 native hook | **runtime proven baseline**; preserve v49 |
| F5 Infinite Watchtowers | old zeroes four faction globals 0x6F9448/0x6F95E8/0x6F96C8/0x6F9798 | unresolved | do not ship guessed implementation; trace watchtower counters/cap checks |
| Delete Instant Build/Repair/Research/BattleGear | old writes selected object +0x8E=5000, +0x492=100, +0x4BE=100 | current selected-building completion writes | partially runtime proven for training/building; research/repair/battle gear still require semantic audit |
| F10 Maximum Wolves | old selected object +0x250 byte = 0xFA | BRZE selected building +0x250 byte = 250 candidate | static candidate only; validate homolog and runtime |
| F8 Demolition Mode | old hook at 0x4BA729 and state mutation | BRZE native EnableDemolish flag RVA 0x3D7A1C | strong native candidate; compare exported BRWOTW behavior then runtime test |
| F9 Pause Peasant Production | four BRWOTW faction globals toggle 0/1 | BRZE native indexed array RVA 0x467AF4 | strong native mapping; runtime test required |
| PageDown Instant Death | old writes 0xFF000000 to old +0x400/+0x404 | current HP +0x404 = 0 implementation | reopen: determine exact BRZE death/stamina homolog rather than assuming HP=0 is equivalent |
| PageUp Infinite Health/Stamina | old injected game-side selection-list writes huge sentinel values | A/B current selection-event/delta experiments | **A/B rejected**: mass-selection freeze/crash. Clean specimen must have zero work on selection; no polling, no +0x6A4, no huge sentinel |
| Horse cheat | old behavior still to be matched exactly | BRZE HorseRespawnTime config +0x48 candidate | static candidate only; compare old trainer operation before shipping |

## Crash evidence
- Specimen A: selected HP/stamina suppression worked, but selecting ~86 units froze simulation.
- Specimen B: selecting 99 units crashed.
- Population 9,999,999 remains enabled in these builds and is now an independent suspect. It must be isolated before attributing all mass-selection failures to selection hooks.

## Clean next specimen requirements
- no 9,999,999 population cap;
- no selection-event refill hooks at RVA 0x1A70C6 / 0x1A78CC;
- no external 2000-unit polling;
- preserve v49 instant-training hook unchanged;
- experimental/unverified cheats excluded from Enable All;
- each hook guarded by expected original bytes.
