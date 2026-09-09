# Population audit — Old Trainer -> BRWOTW -> BRZE 1.60

## Status
REOPENED / unsafe current value.

## Old trainer 2009
F2 Maximum Population does not use the BRZE trainer's 9,999,999 policy. Reverse engineering of the unpacked trainer found the old trainer writing `0x05F5E0FF` (99,999,999) to old global `0x718568`, with its trampoline anchored at old game `0x46E732`.

Important: this proves the old trainer used a very large value, but does NOT prove that the redesigned BRZE 1.60 max-unit storage safely accepts the same policy/value.

## BRWOTW bridge
The supplied BRWOTW executable exports `BattleScriptInterface::SetMaxUnits(unsigned int,unsigned int)`. Therefore population has a native game semantic and should be mapped through that semantic rather than assuming the old singleton global layout survived.

## BRZE 1.60
BRZE also exports `BattleScriptInterface::SetMaxUnits(unsigned int,unsigned int)`. Static disassembly previously mapped it to indexed storage at RVA `0x467B90`, indexed by player id. Current trainer writes 9,999,999 there.

## Risk discovered during runtime tests
Mass selection failures occurred while the unlimited-population feature remained present in specimens A/B. This does not prove population caused the crash, but it means population is a confounder and must be removed from HP/stamina mass-selection specimens until isolated.

## Required correction
1. Do not include F4 population in Set-All during isolation builds.
2. Do not use 9,999,999 as a presumed-safe production value.
3. Build a dedicated population-only specimen with HP/stamina hooks absent.
4. Test conservative values first (e.g. 200, 500, 1000), then higher values separately.
5. Final implementation should preserve BRZE's per-player indexed storage/native SetMaxUnits semantics.

## Verdict
Address/structure mapping: STRONG STATIC.
Current 9,999,999 policy: UNPROVEN / potentially unsafe.
Runtime interaction with 99-unit selection: UNRESOLVED.
