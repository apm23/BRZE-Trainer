# Population / Peasant Production redesign

## User intent
The old multi-million max-population value was not the real desired mechanic. The desired behavior is:

1. Native maximum population: **1000**.
2. Real units continue to exist normally and remain visible to selection, AI, combat, save/load, entity management, etc.
3. The peasant-production slowdown logic should behave as though population pressure/used population is approximately **1**, even when hundreds of units exist.

## Important separation
Do **not** globally overwrite a real unit-count or population-used field to 1 until every consumer is known. Such a write could corrupt selection, AI, save/load, unit limits, achievements, or other systems.

Preferred implementation: identify the BRZE 1.60 read/calculation that converts current population into peasant spawn delay/production pressure and spoof only that input/result for the local player.

## Current source audit
Current Program.cs writes `9_999_999` to `RVA_MAX_UNITS + localPlayerId*4` on every 16 ms UI timer tick while F4 is enabled. This is excessive and is no longer accepted as the final design.

Stage 1 changes the candidate cap to 1000. Stage 2 must find the pressure/slowdown calculation through Old Trainer -> BRWOTW -> BRZE structural comparison. Stage 3 should make writes changed-only / restore-safe rather than unconditional 16 ms writes.

## Runtime isolation
Mass-selection stability must be tested independently from HP/stamina selection hooks. A population diagnostic specimen should contain no HP/stamina selection-event hooks. This lets us distinguish:
- extreme max-population side effects,
- selection-hook side effects,
- an engine/native high-unit-count limitation.

## Acceptance criteria
- max population reads/behaves as 1000;
- peasant production does not progressively slow merely because real unit count is high;
- real unit count is not globally falsified;
- 100+ unit selection does not freeze/crash because of trainer work;
- disabling the feature restores native behavior without requiring process restart;
- no high-frequency unit-pool scanning.
