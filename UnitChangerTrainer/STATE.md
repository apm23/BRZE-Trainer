# Unit Changer Lab — STATE

Last updated: 2026-09-11 (Asia/Tokyo)

## Isolation rule
This is a separate trainer/project under `UnitChangerTrainer/`. Do not merge it into the main BRZE trainer until the user explicitly asks and the runtime architecture is proven.

## User target
- An already-trained unit can be sent into a training building again while Unit Changer is active.
- One completed training event can emit multiple configured output units.
- UI exposes slots 1..9.
- Each slot can be ON/OFF and select an output unit.
- Number of emitted units equals number of enabled slots.

Example target: Spearman enters Dojo again; slots 1..3 are enabled and configured; completion emits three configured units. Slots 4..9 OFF means they do nothing.

## Known hard blockers
1. Native retraining eligibility rejects combinations BR data does not permit (red X / cannot enter building).
2. Native completion is designed around one training result; blind 1->N manipulation has high crash risk.
3. Unknown secondary constraints may include population, unique-unit rules, ownership, placement collision, event queues, save/load, and stage transitions.

## Safety decisions
- Never broad-patch "all units can train everywhere".
- Never clone/copy a raw Unit struct to manufacture outputs.
- Extra outputs must eventually reuse BRZE native unit allocation/create/finalize paths.
- Prove 1->2 first before 1->9.
- Keep all hooks reversible and stage-safe.

## Phase 0
Separate read-only observer trainer.
- 9 output slot UI exists.
- Output names are configuration placeholders until UnitDef IDs are mapped.
- Process access is READ + QUERY only.
- No memory writes, code patch, allocation, remote thread, or spawn.
- Training observer reads selected-building/training context.

## Known observer mappings
- target process: `Battle_Realms_F.exe`
- local ID RVA `0x4416D0`
- selected building A RVA `0x4417D4`
- selected building B RVA `0x4417D8`
- building pool RVA `0x4814E0`
- building count 500, stride `0x6A4`
- owner `+0x84`
- training type `+0x488`
- progress `+0x490`
- gate/state `+0x4B8`
- completion threshold `0x00640000`

## Next forensic milestone
Find the exact native eligibility decision for a retraining order that currently shows the red X. Instrument/observe first, then make the smallest conditional bypass restricted to Unit Changer mode and intended training context.
