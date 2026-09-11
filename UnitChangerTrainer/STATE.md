# Unit Changer Lab — STATE

Last updated: 2026-09-11 (Asia/Tokyo)

## Isolation rule
Unit Changer remains a separate experimental trainer. Current implementations live under `UnitChangerTrainer/`, `UnitChangerTrainerV1/`, `UnitChangerTrainerV2/`, `UnitChangerTrainerV3/`, and `UnitChangerTrainerV4/`. Do not merge into the main BRZE trainer until the user explicitly asks and the runtime architecture is proven.

## CURRENT TARGET
User chose the safer architecture:
- do **not** bypass red-X/retraining eligibility for the initial Unit Changer;
- use only BRZE-native-valid training inputs;
- replace the completion output;
- final UI target is slots 1..9, independently ON/OFF, with number of emitted units equal to enabled slots;
- prove 1->1, then 1->2, then expand toward 1->9.

## Permanent safety decisions
- Never broad-patch `all units can train everywhere`.
- Never clone/copy a raw Unit struct to manufacture outputs.
- Invalid/red-X inputs stay stock.
- AI-owned training stays stock; experimental output logic is player-local.
- Extra outputs must reuse BRZE native creation/finalization and mandatory per-unit bookkeeping.
- Verify stock bytes before every hook install and restore only our own patches.

## V0 observer — RUNTIME PROVEN
V0 proved building training-state observation.
- idle: `trainType=0xFFFFFFFF`, `progress=0`
- normal training: `trainType=0x5`, progress advanced normally
- rejected retraining/red-X: building remained idle

Conclusion: red-X occurs before building enters training state. Do not solve it through `+0x488/+0x490/+0x4B8`.

Build pin:
- head `22567e97d02ad4e45ddf285746de4e0483193841`
- run `34535724003`, job `103066743201` SUCCESS
- artifact `10175347109`

## Native mapping — STATIC PROVEN
Preferred VA `0x4D69F6` / RVA `0x0D69F6` is the native Building* + UnitIn -> UnitOut mapper.
- ECX = Building*
- one stack arg = input Unit Type
- `building+0x258` = BuildingDef*
- EAX = output Unit Type or `0xFFFFFFFF`
- exact entry `55 8B EC 8B 81 58 02 00 00`
- epilogue `ret 4`
- six BuildingDef recipes at UnitIn/UnitOut offsets `+0x68/+0x6C`, `+0x78/+0x7C`, `+0x88/+0x8C`, `+0x98/+0x9C`, `+0xA8/+0xAC`, `+0xB8/+0xBC`.

## V1 eligibility trace — SUPERSEDED FOR CURRENT TARGET
V1 remains available if red-X work returns later.
- head `f2bfcd10368e0de545032ed35d0fd853dbe30e76`
- run `34538748743`, job `103076306342` SUCCESS
- artifact `10176457794`

## V2 global mapper override — RUNTIME FAIL / REJECTED
V2 globally changed mapper results for valid local UnitIn entries.
- run `34544162156` SUCCESS compile
- user runtime verdict: **GAME CRASH**

Permanent rejection:
- do not use V2 as a base;
- do not globally alter all callers of `0x4D69F6` for output replacement.

## V3 completion-only override — RUNTIME PROVEN / LOCKED BASE
Completion path:
- `0x4D5E02`: push training input `[building+0x488]`
- `0x4D5E08`: call mapper `0x4D69F6`
- `0x4D5E0D`: push mapped EAX
- `0x4D5E10`: call native create/finalize `0x4D6A88`

V3 patches only the completion call at RVA `0x0D5E08`; global mapper stays stock. Valid local completion gets Slot 1 output substituted immediately before native creation/finalization.

### V3 runtime verdict — 2026-09-11
User reported **SUKSES BESAR**.
- 1 native-valid training input -> 1 configured output works.
- Cross-clan output works.
- User specifically tested Lotus Master Warlock and Wolf Werewolf as configured outputs; both emerged correctly.
- No crash reported in these successful tests.

Therefore V3 completion-only architecture is now **runtime-proven and LOCKED**. Future versions must preserve Slot 1 behavior and must not reintroduce V2 global-mapper scope.

Build pin:
- head `ac324b493a0dc771bb7623f6fcc9bfa6abd1cd8e`
- run `34545809935`, job `103098037229` SUCCESS
- artifact `10178983173`
- standalone SHA-256 `1e220754a33b54a50afc739f99eb29602a9995fdb535664a3c29319b190fe309`
- small SHA-256 `bcfc4b172021722d6ce1c2a5809c1adaa6d3dcf64f074aa350a69670d0329fb6`

## Per-unit completion bookkeeping — STATIC RESULT FOR V4
Main V3 path around `0x4D5E10..0x4D5EA9` shows the first created unit receives this per-unit sequence before building reset:
1. native create/finalize `0x4D6A88` -> Unit*
2. building attach/rally logic `0x4D231C`
3. copy building `+0x4A8 -> unit+0x36C`
4. copy building `+0x4AC -> unit+0x370`
5. increment owner/player unit counter through global RVA `0x4416A4`, owner stride `0xA0`, field `+0x28`
6. local-player unit-type notification `0x5B605D`
7. copy building `+0x4A0 -> unit+0x798`
8. player/controller registration `0x4AD6B2`
9. only after that does stock code reset the building training state.

The second output must repeat this per-unit block but must **not** independently reset the training building.

## V4 two-output proof — BUILT / RUNTIME PENDING
Separate project `UnitChangerTrainerV4/`.

### V4 architecture
- Slot 1 keeps the V3 completion-only hook at RVA `0x0D5E08` unchanged in concept.
- Slot 2 does not run until the first unit reaches the native registration call site at RVA `0x0D5EA4` (`004D5EA4: E8 09 78 FD FF -> 0x4AD6B2`).
- V4 wraps that exact call site, executes the original registration for unit #1 first, then conditionally creates unit #2.
- A `pending` token is cleared at every completion attempt, set only for a valid local V3 override, checked against the exact training Building* (`EBX`), and consumed exactly once.
- Slot 2 creation calls native `0x4D6A88` with configured output type, original building owner, and native flag `1`.
- If native creation returns null, V4 skips extra bookkeeping and records failure telemetry.
- If creation succeeds, V4 repeats the observed per-unit sequence: `0x4D231C`, fields `+0x36C/+0x370`, owner count, `0x5B605D`, `+0x798`, `0x4AD6B2`.
- Caller GPR and XMM0..3 are preserved around the extra-unit sequence.
- Stock mapper remains untouched.
- Slots 3..9 remain locked.

### V4 build pin
- branch `instant-death-v4-hover-telemetry`
- head `a45111949257e1d3b78956a210e974a2e1ef2513`
- workflow `Unit Changer Lab v4 Two Output Proof`
- run `34549070694` SUCCESS
- job `103107925539` SUCCESS
- artifact `10180134935` (`BRZE-Unit-Changer-Lab-v4-TwoOutputProof`)
- artifact ZIP SHA-256 `e98edafcaa7821ce5b7dba348f5eb0507ab9e560ab52d414b9644df7656ad615`
- standalone: 66,006,686 bytes, SHA-256 `f317f78955e7ecd3394f32cfc5158e87f20bcb3b2de4bdb7e3a8e3774ee0f6eb`
- small: 156,842 bytes, SHA-256 `0c8062954844758c7d5b5302f02346857dbe8954f13fa9ea20c2649204611567`
- compile: 0 warnings, 0 errors.

Status: **compile/static candidate only; 1->2 runtime behavior pending user test.**

## V4 runtime test
Safest first test:
1. fully close old Unit Changer builds and restart BRZE fresh;
2. enter a normal Dragon match;
3. run V4;
4. Slot 1 = `Dragon Archer`, ON;
5. Slot 2 = `Lotus Master Warlock`, ON;
6. enable `MULTI OUTPUT`;
7. send one normal Dragon Peasant into Dragon Dojo and let training complete;
8. target PASS: **two units emerge**: Dragon Archer + Lotus Master Warlock, with no crash;
9. monitor target: `valid local completions` +1, `second attempts` +1, `second success` +1, and non-zero `unit2`.

If this crashes, do not open Slot 3. Record whether the crash occurs exactly when training completes or after one/two units visibly appear. If unit #1 appears but unit #2 crashes, V3 remains locked-good and only the extra per-unit block is investigated.
