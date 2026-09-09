# Selection500 + Fast Peasant — runtime result: peasant stops at total unit 167

Date: 2026-09-10 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe`
Build under test: `BRZE-Selection-500-Fast-Peasant-Probe.exe`
EXE SHA-256: `295e9a4c65d9e63092189b6bb96c3c22898ec7ef1d90da457ad570af7203babf`

## User runtime result

- Fast Peasant initially produced peasants extremely quickly / in a burst.
- When total unit count reached about **167**, automatic peasant production stopped completely.
- User additionally requested that Selection500 remain always enabled independently from Max Population, and that Fast Peasant use a fixed 1–2 second interval instead of a multiplicative speed factor.

## Static correlation found after runtime result

Native PeasantManager update `0x57FEE9` contains two population-used checks against per-player max population (`0x867B90[player]`):

- `0x57FF1B -> 0x582D5E`, compare at `0x57FF20`
- `0x57FF71 -> 0x582D5E`, compare at `0x57FF76`

When population-used is at/above the max, the update clears/avoids the production timestamp and exits before scheduling/spawning another peasant. This is a concrete native stop path consistent with the observed halt.

The next probe should therefore:

1. keep Selection500 + headroom160 always armed while trainer is attached, independent from Max Population toggle;
2. make F4 control only the local max-pop value 500;
3. replace the old 20x scheduler shortening with a fixed **1000 ms** local-player schedule;
4. bypass only the two PeasantManager population-stop checks for the local player while Fast Peasant is enabled; AI/non-local players remain native.

Runtime proof of the new design is still required.
