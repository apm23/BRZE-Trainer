# Max population xref classification — BRWOTW vs BRZE 1.60

## BRWOTW baseline
Old max-pop array: `0x718568`, indexed by player id.

Direct static references found in BRWOTW disassembly:
- `0x46E21E`: getter reads `[playerId*4 + 0x718568]` and returns it.
- `0x4C2149`: setter writes caller supplied DWORD to `[playerId*4 + 0x718568]` and returns.

This is consistent with the old trainer writing `99,999,999` to the engine's real max-unit value. The old setter itself performs no clamp, secondary write, allocation, or synchronization.

## BRZE 1.60 max-pop array
Current array: `0x867B90`, indexed by player id. Native SetMaxUnits at `0x4CBDAC` writes caller supplied DWORD directly.

16 direct static references were found:

1. `0x4CBDAC` — native setter; direct DWORD write.
2. `0x4E5F0C` — reads max after `0x582D5E` current-unit calculation; gameplay/UI decision path.
3. `0x5197A0` — reads max and compares a count returned by `0x5D5307`; capacity gate.
4. `0x57E262` — passes base address as data/registration pointer.
5. `0x57F368` — passes base address as data/registration pointer.
6. `0x57F46C` — writes max array during native state/config handling.
7. `0x57FF20` — compares `currentUnits = 0x582D5E(...)` against max. At/over cap resets a per-player state through pointer `0x867AF0`.
8. `0x57FF5D` — loads max. If a state is active, computes `max-current` and requires at least one free slot before progressing. Nearby arrays `0x867BB8` and `0x867BE0` cache/update current/count-related state; they are NOT proven to be alternate max-pop values.
9. `0x5800E6` — loads max, calls `0x582D5E`, computes `max-current`, converts max/current/free-space values to floating point and enters scaling/state logic. Strong candidate for population-pressure / peasant timing behavior.
10. `0x5813C1` — takes address of max value as a 4-byte serialization/network/state field; also serializes a field at max-0x9C.
11. `0x58153B` — symmetric 4-byte deserialization/state path for max and neighboring fields.
12-16. `0x5B5819`, `0x5B5915`, `0x5B59F4`, `0x5B5AE8`, `0x5B5BC0` — AI/gameplay decision paths comparing calculated unit counts/candidate operations against max capacity.

## Crash-risk result so far
No direct xref treats max population as:
- an allocation size,
- a loop bound over the physical unit pool,
- an array index into unit objects,
- a memcpy/memset length,
- or a selector-container capacity.

Therefore there is currently **no static evidence that 99,999,999 by itself explains the 86-unit freeze / 99-unit crash**.

The arithmetic at `0x5800E6` uses signed integer subtraction (`max-current`) and signed-to-floating conversion. `99,999,999` is safely below INT32_MAX, so normal `max-current` arithmetic with realistic unit counts does not overflow.

This does NOT prove runtime safety. A clean population-only specimen is still required.

## Important secondary-state hypothesis
The user's hypothesis that max-pop may require companion state was checked against direct xrefs.

Evidence:
- BRWOTW native setter is a single direct write; no companion state is updated by SetMaxUnits itself.
- BRZE native SetMaxUnits is likewise a direct write.
- BRZE does maintain neighboring runtime state (`0x867AF0`, `0x867BB8`, `0x867BE0`) in population/production paths, but current evidence shows these are derived/cache/state values, not mandatory mirrors of max-pop.

So we must **not** copy 99,999,999 into those neighboring addresses. Native logic should be allowed to update them normally unless further semantic proof says otherwise.

## Current best experiment
Build a population-only isolation trainer with no F5/F6 selection hooks:
- P0: vanilla max
- P1000: max = 1000
- P99M: max = 99,999,999 (old trainer exact value)

Then test mass selection at the same 80-100+ unit counts. This directly separates population from HP/selection-hook crashes.

## Peasant-speed direction
`0x5800E6` remains the strongest current target for tracing population pressure because it consumes max, current and free-space as floating-point values. Do not globally hook `0x582D5E`: it is widely used for real unit accounting. Prefer a local hook/override inside the peasant-production path once its output state/timer is fully identified.
