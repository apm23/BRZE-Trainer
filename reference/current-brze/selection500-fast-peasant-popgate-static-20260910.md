# PeasantManager population-stop gate static note

Target: `Battle_Realms_F(5).exe` SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`

In `0x57FEE9` automatic peasant update:

- `0x57FF1B` calls `0x582D5E`, then `0x57FF20` compares returned population-used against `[0x867B90 + player*4]`. If at/above max, next-production timestamp is zeroed.
- `0x57FF71` calls `0x582D5E`, then `0x57FF76` repeats the max-pop test and exits before scheduling/spawning if at/above max.

This confirms a native PeasantManager path that can stop production independently of the fast-timer wrapper. The next probe will bypass these two comparisons indirectly by wrapping only those two calls for local Fast Peasant, leaving the global population counter and AI behavior unchanged.
