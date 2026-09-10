# Unit Changer Lab — STATE

Last updated: 2026-09-11 (Asia/Tokyo)

## Isolation rule
This is a separate trainer/project under `UnitChangerTrainer/` / `UnitChangerTrainerV1/`. Do not merge it into the main BRZE trainer until the user explicitly asks and the runtime architecture is proven.

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

## Phase 0 — BUILT + OBSERVER RUNTIME PROVEN
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

## Observer runtime result — 2026-09-11
User supplied three screenshots in the requested sequence: idle, normal training, rejected retraining/red-X.

Observed selected building stayed local at `0x23239D6C`.

Idle:
- `trainType = 0xFFFFFFFF`
- `progress = 0x00000000`
- `gate = 0xFFFFFFFF`

Normal training active:
- `trainType = 0x5`
- `progress = 0x000A1220` (~10.1%)
- `gate = 0xFFFFFFFF`

Rejected retraining / red-X:
- `trainType = 0xFFFFFFFF`
- `progress = 0x00000000`
- `gate = 0xFFFFFFFF`

Conclusion: the red-X rejection happens **before** the building enters its training state. Do not attempt blocker A by editing building `+0x488/+0x490/+0x4B8`.

`Local active training buildings` showed baseline `1`, rose to `2` during the selected building's normal training, then returned to `1`. Therefore the aggregate scanner is useful as delta telemetry but not a unique selected-building identifier by itself.

## Native training recipe mapping — STRONG STATIC RESULT
Preferred VA `0x4D69F6` is the native Building* + UnitIn -> UnitOut mapper.
- ECX = Building*
- argument = input unit type
- `building+0x258` = BuildingDef*
- return EAX = output unit type or `0xFFFFFFFF` when no mapping exists.

BuildingDef has exactly six native recipe records:
- slot1: UnitIn `+0x68`, UnitOut `+0x6C`, Rate `+0x70`, Time `+0x74`
- slot2: UnitIn `+0x78`, UnitOut `+0x7C`, Rate `+0x80`, Time `+0x84`
- slot3: UnitIn `+0x88`, UnitOut `+0x8C`, Rate `+0x90`, Time `+0x94`
- slot4: UnitIn `+0x98`, UnitOut `+0x9C`, Rate `+0xA0`, Time `+0xA4`
- slot5: UnitIn `+0xA8`, UnitOut `+0xAC`, Rate `+0xB0`, Time `+0xB4`
- slot6: UnitIn `+0xB8`, UnitOut `+0xBC`, Rate `+0xC0`, Time `+0xC4`

The BR parser independently confirms keys `UnitIn1..6`, `UnitOut1..6`, `UnitTrainingRate1..6`, `UnitTrainingTime1..6` with the same 0x10-byte record spacing.

A strong eligibility caller is the call at preferred `0x531A50` (return `0x531A55`): it reads the selected/input unit type from `[unit+0x74]->type`, calls `0x4D69F6` on the target building, and compares EAX against `0xFFFFFFFF`.

Reference: `forensics/retraining-map-function-20260911.md`.

## Phase 1 — ELIGIBILITY TRACE BUILT / RUNTIME TEST PENDING
Separate v1 tracer under `UnitChangerTrainerV1/`.

V1 behavior:
- reads selected unit type and selected building type/def
- displays all six native UnitIn -> UnitOut recipe records from the selected BuildingDef
- installs a behavior-transparent entry hook at `RVA 0x0D69F6`
- exact displaced stock prologue is restored on close
- records total mapper calls, latest caller/building/input/BuildingDef
- separately counts candidate caller return `0x531A55` and candidate `0x55545E`
- DOES NOT alter mapper return value
- DOES NOT add recipes
- DOES NOT bypass red-X
- DOES NOT spawn units

Build pin:
- branch `instant-death-v4-hover-telemetry`
- trigger/build head `f2bfcd10368e0de545032ed35d0fd853dbe30e76`
- workflow `Unit Changer Lab v1 Eligibility Trace`
- run `34538748743` SUCCESS
- job `103076306342` SUCCESS
- artifact `10176457794` (`BRZE-Unit-Changer-Lab-v1-EligibilityTrace`)
- artifact ZIP SHA-256 `d48f06b201e842558b2230a1ba2341a2682bf38d7aa94dccb987ecad86c28c6f`
- standalone size `66,002,183` bytes, SHA-256 `96ba4eb95b069e6f83502c368808befd0f0b3f26fae8713543fc6ad782f6eae0`
- small size `147,114` bytes, SHA-256 `e6ae03342d63d68157b701bdff6f486d7ee740d50d1a64acdb246ffe3ef14dc6`

## Static multi-output lead
Training completion code around preferred VA `0x4D5DF1` reaches a native chain in which:
- `0x4D69F6` maps training input -> output unit type;
- its return is passed into `0x4D6A88` with the training building in ECX;
- `0x4D6A88` returns a non-null Unit* candidate used by subsequent building/unit bookkeeping;
- building training state is reset afterward.

This is only a static lead, not runtime proof. Reference: `forensics/training-completion-static-20260911.md`.

## Next runtime test
With v1:
1. select target training building and a normal unit that can enter it;
2. press `RESET TRACE`;
3. issue the normal training order and screenshot trace + six recipe rows;
4. select the already-trained unit (e.g. Spearman) and the same building;
5. press `RESET TRACE`;
6. issue the red-X/rejected retraining order and screenshot trace + current context.

If candidate A increments for the rejected order with the expected input type and selected building, treat `0x531A50 -> 0x4D69F6` as runtime-proven eligibility path.

After that, prefer a **temporary native recipe-slot insertion** into an unused BuildingDef slot over a broad branch bypass. Prove one extra valid recipe first, then only after that begin 1 input -> 2 output work.
