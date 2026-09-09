# Max Population Deep Audit — Old Trainer / BRWOTW / BRZE 1.60

## Scope
Determine whether the extreme population cap itself can plausibly explain the 86-unit freeze / 99-unit crash, whether BRZE has companion state that must stay coherent, and locate the safest peasant-speed bypass.

## Old trainer / BRWOTW baseline
- Old trainer F2 writes `0x05F5E0FF` = **99,999,999**.
- BRWOTW max-pop storage is an indexed dword array at VA `0x718568`.
- BRWOTW getter at `0x46E21B`: loads player id from `[ecx+0x6C]`, then returns `[eax*4+0x718568]`.
- BRWOTW setter at `0x4C2141`: writes caller supplied value to `[playerId*4+0x718568]`.
- Therefore the old trainer's extreme value is not merely a UI value: it replaces the same native max-pop datum exposed by the old game.

## BRZE native equivalent
- BRZE `SetMaxUnits` at `0x4CBDA3` writes caller supplied dword to `[playerId*4+0x867B90]`.
- This is the semantic successor of BRWOTW `0x718568`, not an address-delta guess.
- Current trainer's `RVA_MAX_UNITS=0x467B90` is therefore statically correct.

## All direct BRZE references found to `0x867B90`
Direct code xrefs in the current executable:
`0x4CBDAC`, `0x4E5F0C`, `0x5197A0`, `0x57E262`, `0x57F368`, `0x57F46C`, `0x57FF20`, `0x57FF5D`, `0x5800E6`, `0x5813C1`, `0x58153B`, `0x5B5819`, `0x5B5915`, `0x5B59F4`, `0x5B5AE8`, `0x5B5BC0`.

### Classification
1. `0x4CBDAC` — native setter.
2. `0x57E262`, `0x57F368` — array initialization/reset helper calls; these pass the array address, not a max-sized allocation.
3. `0x57F46C` — initialization/write path.
4. `0x57FF20..0x57FF81` — gameplay cap / peasant-production state. Calls `0x582D5E` to obtain current unit count, compares current count with max, and computes `max-current` before deciding whether another production step is allowed.
5. `0x5800E6..0x580251` — **population-pressure / timing calculation**. It loads max population, calls `0x582D5E` for current units, computes `max-current`, converts max/current/free-slot values to floating point, performs interpolation/scaling, then updates per-player state through the array rooted at `0x867AF0`. This is the strongest current candidate for the peasant slowdown / birth interval logic.
6. `0x4E5F0C`, `0x5197A0`, `0x5B5819`, `0x5B5915`, `0x5B59F4`, `0x5B5AE8`, `0x5B5BC0` — assorted cap/AI/gameplay checks. Several compare native current-unit count against max. They show max-pop is shared gameplay state, not merely peasant UI state.
7. `0x5813C1`, `0x58153B` — take the address of the per-player max-pop slot for update/management logic.

## Does 99,999,999 itself obviously crash BRZE?
Static answer: **not proven, and there is currently no direct evidence that the value alone causes the selection crash.**

Important observations:
- The field is a 32-bit integer and `99,999,999` is far below signed `INT32_MAX` (`2,147,483,647`).
- The most obvious production path uses comparisons and `max-current` subtraction. With real unit counts in the hundreds, `99,999,999-current` does not overflow a signed 32-bit integer.
- The timing path converts these values to floating point. 99,999,999 is representable as a finite float (with reduced integer precision, but no Inf/NaN merely from conversion).
- No direct xref found so far uses max-pop as an allocation size or as an index into the unit pool.

Therefore we must NOT claim that extreme max-pop caused the 86/99-unit failure. The previous A/B crashes remain confounded by HP/selection hooks and need a population-isolation specimen.

## User hypothesis: companion state may need updating
This is plausible enough to test, but not yet proven. BRZE keeps nearby per-player arrays around the population subsystem (`0x867AF0`, `0x867BB8`, `0x867BE0`, etc.). The `0x57FFxx` / `0x5800xx` logic updates these alongside current/max population. They appear to be production/timing state rather than a second copy of max-pop.

Crucially, this means blindly forcing those arrays to 99,999,999 would be unjustified and dangerous. If an extreme cap needs companion handling, it should be derived from the native function path, not guessed.

## Peasant speed candidate
The block beginning around `0x5800E6` is now the primary target:
- `max = [playerId*4 + 0x867B90]`
- `current = call 0x582D5E`
- `free = max-current`
- max/current/free are converted to floating point
- the result feeds an interpolation/scaling block and ultimately updates per-player production state rooted at `0x867AF0`.

This is a better target than globally spoofing `0x582D5E`, because `0x582D5E` has many callers and globally lying about current unit count could corrupt unrelated AI/cap logic.

## Required runtime specimens
To isolate cause rather than guess:
- **P0 control:** no HP/stamina selection hooks, native/default population.
- **P1000:** no HP/stamina selection hooks, max-pop = 1000.
- **P99M:** no HP/stamina selection hooks, max-pop = 99,999,999 exactly like old trainer.
- Same save / same mass-selection test for all three.

If P99M survives while A/B crash, max-pop is exonerated and the selection/HP hooks become the leading suspect. If P99M alone fails, continue tracing the nearby population state before touching any companion value.

## Current policy
- Do not globally spoof used population.
- Do not write guessed companion counters.
- Keep old trainer's 99,999,999 as a legitimate diagnostic specimen because BRWOTW demonstrably accepted that native max-pop value.
- Prefer a narrow hook in the `0x5800E6` population-pressure/timing path for fast peasants once exact semantics are sufficiently proven.
