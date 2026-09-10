# BRZE Unit Changer Lab

Separate experimental trainer for Battle Realms: Zen Edition 1.60.

This project is intentionally isolated from the main BRZE trainer. The main trainer must not import or call this project while Unit Changer work is experimental.

## Target concept
One already-trained unit is sent into a training building again. When the training completes, the Unit Changer will eventually replace the normal single output with configurable multi-output slots.

The UI has 9 output slots. Each slot can be independently enabled/disabled and assigned an output unit. Therefore one training event can eventually produce 1..9 configured output units.

## Known blockers

### Blocker A — retraining eligibility
BRZE normally rejects invalid unit/building combinations. Example target behavior: a Spearman that normally cannot enter a Dojo again should become eligible only while Unit Changer is active.

Do NOT solve this with a broad "all units can enter all buildings" patch. We need the narrow native eligibility path and must preserve ordinary building interaction when Unit Changer is off.

### Blocker B — one input to many outputs
The native training completion pipeline expects one result. Blindly duplicating a Unit* or changing a count is high crash risk because unit allocation, ownership, world insertion, pathing, selection, AI bookkeeping and event queues may all need native initialization.

The safe design target is to let BRZE complete one native training result, then call/reuse BRZE's native unit creation/finalization path for each additional configured output. Never clone raw unit structs.

### Additional blockers to watch
- population accounting and cap handling
- spawn position collision around the training building
- ownership/faction validity
- training building state reset after completion
- Journey stage/map transitions and stale pointers
- heroes/unique units and other one-instance restrictions
- invalid cross-clan unit definitions
- selection/event queue pressure when several outputs appear in one frame
- save/load consistency

## Phase 0 — current build
READ-ONLY observer only.

- Separate executable/project.
- Nine configurable UI slots.
- BRZE process opened with VM_READ + QUERY_INFORMATION only.
- No WriteProcessMemory.
- No VirtualProtectEx.
- No remote thread.
- No hooks.
- No spawning.
- Observes selected training building and known training fields.

Known current BRZE observer mappings:
- local player id RVA `0x4416D0`
- selected building A RVA `0x4417D4`
- selected building B RVA `0x4417D8`
- building pool RVA `0x4814E0`
- building stride `0x6A4`, count `500`
- building owner `+0x84`
- training type `+0x488`
- training progress `+0x490`
- training gate/state `+0x4B8`
- completion fixed-point threshold `0x00640000`

These mappings are observation inputs only and are not proof of the output transformation path.

## Next forensic order
1. Capture the exact reject/accept branch when an already-trained unit is ordered into a training building it normally cannot enter.
2. Identify the native training-result selection / output UnitDef path.
3. Identify the native "create/finalize unit from UnitDef" call that can safely be reused for extra outputs.
4. Prove one extra output (1 input -> 2 valid units) before expanding to all 9 slots.
5. Test population, pathing, stage transition, save/load and unique-unit restrictions.

No step should move to broad multi-output until the previous one is runtime-proven.
