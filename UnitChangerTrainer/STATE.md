# Unit Changer Lab — STATE

Last updated: 2026-09-11 (Asia/Tokyo)

## Isolation rule
Unit Changer remains a separate experimental trainer. Current implementations live under `UnitChangerTrainer/`, `UnitChangerTrainerV1/`, and `UnitChangerTrainerV2/`. Do not merge into the main BRZE trainer until the user explicitly asks and the runtime architecture is proven.

## CURRENT TARGET — architecture pivot
The original idea allowed already-trained units to retrain in normally-invalid buildings, but the user chose the safer architecture on 2026-09-11:

- **Do not bypass red-X/retraining eligibility for the initial Unit Changer.**
- Use only a training input that BRZE already considers valid, e.g. Dragon Peasant -> Dragon Dojo.
- Intercept the native valid training result and replace its UnitOut with the configured Unit Changer output.
- First prove **1 valid input -> 1 different output**.
- Then prove **1 -> 2**, and only after that expand toward slots 1..9.
- Red-X bypass can be revisited later as an optional feature, not a foundation.

Final intended UI still has slots 1..9; each slot can be ON/OFF and choose an output unit. Number of emitted units eventually equals number of enabled slots.

## Safety decisions
- Never broad-patch "all units can train everywhere".
- Never clone/copy a raw Unit struct to manufacture outputs.
- Invalid/red-X inputs must continue through stock BRZE logic.
- Player-local isolation: experimental output override must not alter AI-owned training buildings.
- Extra outputs in later phases must reuse BRZE native allocation/create/finalize and mandatory per-unit bookkeeping.
- Prove 1->1, then 1->2, before 1->9.
- Keep hooks reversible; verify stock bytes before install and restore only our own patch on close.
- Heroes/unique units are intentionally excluded from the first 1->1 proof dropdown.

## Phase 0 — OBSERVER RUNTIME PROVEN
V0 read-only observer proved selected-building training state transitions.

Build pin:
- head `22567e97d02ad4e45ddf285746de4e0483193841`
- run `34535724003`, job `103066743201` SUCCESS
- artifact `10175347109`

User runtime screenshots:
- idle: `trainType=0xFFFFFFFF`, `progress=0`, `gate=0xFFFFFFFF`
- normal training: `trainType=0x5`, `progress=0x000A1220` (~10.1%)
- rejected retraining/red-X: `trainType=0xFFFFFFFF`, `progress=0`, `gate=0xFFFFFFFF`

Conclusion: red-X occurs before building enters training state. Do not solve it through building `+0x488/+0x490/+0x4B8`.

## Native training mapping — STRONG STATIC RESULT
Preferred VA `0x4D69F6` / RVA `0x0D69F6` is the native Building* + UnitIn -> UnitOut mapper.

Calling convention / behavior:
- ECX = Building*
- stack argument = input unit type ID
- `building+0x258` = BuildingDef*
- EAX = mapped output unit type or `0xFFFFFFFF`
- exact entry bytes: `55 8B EC 8B 81 58 02 00 00`
- exact epilogue at preferred `0x4D6A59`: `5D C2 04 00` (`pop ebp; ret 4`)

BuildingDef has six native recipes:
- #1 UnitIn `+0x68`, UnitOut `+0x6C`, rate `+0x70`, time `+0x74`
- #2 `+0x78/+0x7C/+0x80/+0x84`
- #3 `+0x88/+0x8C/+0x90/+0x94`
- #4 `+0x98/+0x9C/+0xA0/+0xA4`
- #5 `+0xA8/+0xAC/+0xB0/+0xB4`
- #6 `+0xB8/+0xBC/+0xC0/+0xC4`

The Data_Buildings parser independently exposes `UnitIn1..6`, `UnitOut1..6`, `UnitTrainingRate1..6`, and `UnitTrainingTime1..6`.

Reference: `forensics/retraining-map-function-20260911.md`.

## Phase 1 — ELIGIBILITY TRACE BUILT, now superseded for current target
V1 passive trace exists under `UnitChangerTrainerV1/` and can still be used if red-X work returns later.

Build pin:
- head `f2bfcd10368e0de545032ed35d0fd853dbe30e76`
- run `34538748743`, job `103076306342` SUCCESS
- artifact `10176457794`

V1 does not change mapper results. Current project direction no longer requires this blocker for the first implementation.

## Training completion — STATIC LEAD
Completion around preferred `0x4D5DF1` calls the native input->output mapper and passes the mapped type into the creation chain around `0x4D6A88`, which returns a Unit* candidate used by subsequent building/unit bookkeeping. This is a lead for the later 1->N phase; it is not runtime-proven yet.

Reference: `forensics/training-completion-static-20260911.md`.

## Phase 2 — V2 SINGLE OUTPUT PROOF BUILT / RUNTIME PENDING
Separate trainer under `UnitChangerTrainerV2/`.

### V2 architecture
V2 hooks only the native mapper entry at RVA `0x0D69F6`.

When override is OFF:
- executes the displaced stock 9-byte prologue and returns to stock mapper.

When override is ON, fast override is allowed only when ALL guards pass:
1. training building owner `[building+0x84]` equals local player ID at RVA `0x4416D0`;
2. `BuildingDef*` at `[building+0x258]` is non-null;
3. input unit type matches one of the building's existing native `UnitIn1..6` entries.

If any guard fails, execution falls back to the stock mapper unchanged. Therefore invalid/red-X input combinations are **not made valid**.

If all guards pass:
- records last building/input/BuildingDef + intercept count;
- returns configured Unit Type in EAX;
- uses the native mapper calling convention `ret 4`.

This deliberately overrides the mapping consistently anywhere BRZE asks for a valid training result, including completion. It does not mutate BuildingDef recipe data.

### V2 UI
- master `OUTPUT OVERRIDE` switch
- Slot 1 usable with Unit Type dropdown
- Slots 2..9 visible but hard-locked until V2 1->1 runtime PASS
- first dropdown contains regular Dragon/Serpent/Lotus/Wolf units only; heroes/unique units intentionally excluded
- monitor shows hook state, configured output, intercept count, last valid input, native output, and override output
- selected building native six recipes remain visible
- corrected default layout uses 1080x940 and a dedicated recipe row so status is not clipped

### Corrected build pin
- branch `instant-death-v4-hover-telemetry`
- build head `05624020919ff10ef7d49098fe1946f8a04068d7`
- workflow `Unit Changer Lab v2 Single Output Proof`
- run `34544162156` SUCCESS
- job `103093060639` SUCCESS
- artifact `10178411232` (`BRZE-Unit-Changer-Lab-v2-SingleOutputProof`)
- artifact ZIP SHA-256 `f6f0ee90868aa355b8b9d2aa00624ad105487f7fd7c91a2697a8ba992bf6ae7c`
- standalone: 66,004,581 bytes, SHA-256 `a3e0af00d757c055173dc09ea560b9497b0286bdb200f87426a76624bfee8bae`
- small: 152,746 bytes, SHA-256 `4db2b2faedf3d353c23954bd76c0e125a2fab38ff897e5df93dc929ce966e5cd`

Status: **compile/static candidate only; runtime output behavior pending user test.**

## V2 first runtime proof
Preferred safest test:
1. close V0/V1 or any other Unit Changer build;
2. start BRZE and enter a normal controllable match;
3. start V2;
4. in Slot 1 choose `Dragon Archer (type 0)`;
5. enable Slot 1 and master `OUTPUT OVERRIDE`;
6. use a normal Dragon Peasant (type 5) and send it into a Dragon Dojo through the normal valid training interaction;
7. let training complete normally;
8. PASS target: the unit exiting is Dragon Archer instead of native Dragon Spearman;
9. monitor should show an intercept with input `0x5`, native output `0x8`, override `0x0`.

If this passes without crash, lock V2 1->1 and begin the 1->2 native creation/finalization experiment. If it crashes or output remains native, do not scale to multi-output; diagnose V2 mapper timing/consumer semantics first.
