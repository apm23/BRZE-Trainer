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

## Phase 0 — BUILT
Separate read-only observer trainer.
- 9 output slot UI exists.
- Output names are configuration placeholders until UnitDef IDs are mapped.
- Process access is READ + QUERY only.
- No memory writes, code patch, allocation, remote thread, or spawn.
- Training observer reads selected-building/training context.

Build pin:
- branch `instant-death-v4-hover-telemetry`
- trigger/build head `22567e97d02ad4e45ddf285746de4e0483193841`
- workflow `Unit Changer Lab v0`
- run `34535724003` SUCCESS
- job `103066743201` SUCCESS
- artifact `10175347109` (`BRZE-Unit-Changer-Lab-v0-Observer`)
- artifact ZIP SHA-256 `19cefdaee6a1e7a9f62a1ebaf720218e565e7f04b5e2ffe100e593c3c33a4a77`
- standalone size `66,001,071` bytes, SHA-256 `3449abf307d95683a5436eab5dfa2c8e317be88b6139f6f00265b0baa57e8bf9`
- small size `144,536` bytes, SHA-256 `ea9e417e5d169293ffdec343784a7eb098b052ff2f372d17746e5e5ac68d2e88`

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

## Static multi-output lead
Training completion code around preferred VA `0x4D5DF1` reaches a native chain in which:
- `0x4D69F6` receives training type / owner / completion context;
- its return is passed into `0x4D6A88` with the training building in ECX;
- `0x4D6A88` returns a non-null Unit* candidate used by subsequent building/unit bookkeeping;
- building training state is reset afterward.

This is only a static lead, not runtime proof. Reference: `forensics/training-completion-static-20260911.md`.

## Next forensic milestone
Find the exact native eligibility decision for a retraining order that currently shows the red X. Instrument/observe first, then make the smallest conditional bypass restricted to Unit Changer mode and intended training context.

After that, runtime-prove the output creation chain with 1 input -> 2 outputs before expanding toward slots 1..9.
