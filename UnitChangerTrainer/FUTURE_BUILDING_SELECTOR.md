# FUTURE PHASE — Building Selector / Per-Building Recipes

Deferred on 2026-09-11. Do **not** destabilize the temporary-final V5 base for this phase yet.

## User target
A future Unit Changer should support multiple independently configured target buildings. Each selected building/rule can have its own output slots. Example:

- Building A -> Kenji + Grayback + Arah
- Building B -> a different configured output set
- more than one building/rule active at the same time

Long-term dream includes applying training behavior to structures that are not normally training buildings (for example a Peasant Hut or even tree-like structures).

## Current blocker
BRZE native eligibility/red-X rejection occurs before building training state begins. The current V5 architecture intentionally does not bypass that eligibility layer. Therefore arbitrary normally-invalid structures must **not** be enabled by broad patching or by faking building state.

## Future architecture requirement
First solve a narrow, reversible, player-only eligibility path. Only after that is runtime-proven should per-building recipe selection be layered on top of the V5 completion/output engine.

Until then, V5 is considered temporarily finished and authoritative for output selection on native-valid training interactions.
