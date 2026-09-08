# Full legacy trainer -> BRZE 1.60 port plan

Method is locked to the v49 success pattern: decode the old trainer operation, identify the equivalent BRZE 1.60 game routine/field, then implement against the current module base. Never assume a uniform address delta.

## Runtime-proven/current
- Resources: current player +D8/+DC; current max +E0/+E4.
- Yin/Yang: +2E8/+2EC.
- Population: module RVA 467B90 indexed by local player id.
- F7 instant training: dedicated old-F4-style hook remapped to RVA 0D5DDB; write building +490 = 0064B540 before current game's own progress read. Runtime proven successful in v49.
- F8 selected building completion: current selected object globals RVA 4417D4/4417D8 and legacy unaligned writes +8E=5000, +492=100, +4BE=100. Runtime proven to finish both new construction and current unit training when pushed.

## Port queue
1. F5/F6 large-selection HP/stamina: preserve event-driven central delta hooks; external 100ms selected-unit RPM/WPM walker is removed. Test 56+ selection. If semantics are insufficient, remap old PageUp game-side selected-list hook rather than restoring external polling. Never force +6A4 as the production solution.
2. Infinite Watchtowers: old trainer clears four clan counters from a resource/player path. Find current four clan-specific counter globals from BRZE references; hook only after exact mapping.
3. Pause Peasant Production: locate current equivalents of old four clan globals and preserve 0/1 toggle semantics.
4. Demolition Mode: locate current equivalent routine of old 4BA729 / object +8C state and validate field semantics before enabling.
5. Instant Build/Repair/Research/BattleGear: retain proven selected-building pair and exact legacy high-word completion writes; split/test each semantic if BRZE routes them differently.
6. Maximum Wolves: current selected building +250 stock mapping for building type 44 is static-proven; current push writes byte 250. Runtime test pending.
7. Maximum Horses: do not fake a stock counter. Preferred BRZE-native remap is HorseRespawnTime config: cfg base RVA 43FF2C; current config-index pointer RVA 440034; record stride 98; field +48. Save original, set zero while enabled, restore on disable/detach. Runtime pending.
8. Instant Death: remap through current native selection list RVA 441708; direct selected-unit action only, no 2000-unit scan.

## Safety/performance rules
- No 1-5ms external memory polling.
- No full unit/building pool scan for persistent cheats when a game event/hook or native selection list exists.
- Changed-only direct writes where possible.
- Validate original bytes before installing each code hook.
- Restore original code/config on disable/detach.
- A feature is labelled WORK only after runtime test in the supplied BRZE 1.60 executable.
