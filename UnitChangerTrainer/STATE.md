# Unit Changer Lab — STATE

Last updated: 2026-09-11 (Asia/Tokyo)

## Isolation rule
Unit Changer remains a separate experimental trainer. Current implementations live under `UnitChangerTrainer/`, `UnitChangerTrainerV1/`, `UnitChangerTrainerV2/`, and `UnitChangerTrainerV3/`. Do not merge into the main BRZE trainer until the user explicitly asks and the runtime architecture is proven.

## CURRENT TARGET — architecture pivot
The original idea allowed already-trained units to retrain in normally-invalid buildings, but the user chose the safer architecture on 2026-09-11:

- **Do not bypass red-X/retraining eligibility for the initial Unit Changer.**
- Use only a training input BRZE already considers valid, e.g. Dragon Peasant -> Dragon Dojo.
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

## Phase 1 — ELIGIBILITY TRACE BUILT, superseded for current target
V1 passive trace exists under `UnitChangerTrainerV1/` and can still be used if red-X work returns later.

Build pin:
- head `f2bfcd10368e0de545032ed35d0fd853dbe30e76`
- run `34538748743`, job `103076306342` SUCCESS
- artifact `10176457794`

## Training completion — STATIC LEAD NOW CONFIRMED IN DISASSEMBLY
Completion path around preferred VA `0x4D5DF1` does:
- `0x4D5E02`: pushes training input `[building+0x488]`
- `0x4D5E08`: calls native mapper `0x4D69F6`
- `0x4D5E0D`: pushes mapper EAX result
- `0x4D5E10`: calls native creation/finalization entry `0x4D6A88`

Therefore `0x4D5E08` is a much narrower interception point than globally patching the mapper itself.

Reference: `forensics/training-completion-static-20260911.md`.

## Phase 2 — V2 GLOBAL-MAPPER OUTPUT OVERRIDE — RUNTIME FAIL / REJECTED
V2 under `UnitChangerTrainerV2/` patched the native mapper entry at RVA `0x0D69F6` and returned the configured output for local valid UnitIn matches.

Build pin:
- head `05624020919ff10ef7d49098fe1946f8a04068d7`
- run `34544162156`, job `103093060639` SUCCESS
- artifact `10178411232`
- standalone SHA-256 `a3e0af00d757c055173dc09ea560b9497b0286bdb200f87426a76624bfee8bae`
- small SHA-256 `4db2b2faedf3d353c23954bd76c0e125a2fab38ff897e5df93dc929ce966e5cd`

Runtime verdict from user on 2026-09-11: **GAME CRASH** during V2 test. Exact crash phase was not specified.

Decision:
- V2 is **rejected as an active base**.
- Do not scale V2 to 1->2.
- Do not globally alter every caller of `0x4D69F6` for output replacement.

Static review after the crash showed `0x4D69F6` itself is a pure six-slot lookup, so the main V2 risk is scope: changing mapper results for every caller can affect eligibility/UI/setup and other consumers before actual completion.

## Phase 3 — V3 COMPLETION-ONLY OUTPUT OVERRIDE BUILT / RUNTIME PENDING
Separate trainer under `UnitChangerTrainerV3/`.

### V3 architecture
- The global mapper at RVA `0x0D69F6` is left **100% stock** and V3 verifies its stock 9-byte entry before installing anything.
- V3 patches only the exact completion CALL at RVA `0x0D5E08` (`E8 E9 0B 00 00`).
- Replacement stub calls the untouched native mapper first with the original training input.
- If mapper returns `0xFFFFFFFF`, result stays native.
- If training building owner is not local player, result stays native.
- Only after a valid local completion mapping does V3 replace EAX with configured Unit Type.
- Stack convention remains equivalent to original call: original mapper copy is cleaned by mapper `ret 4`, then V3 stub itself `ret 4` cleans the caller's original input.
- EDX is preserved; ECX is restored to the mapper-equivalent final input value before returning.
- Native creation/finalization at `0x4D6A88` then receives the replacement type through the original stock completion flow.
- V3 does **not** change eligibility, red-X, training setup, training time, UI mapper calls, or AI-owned buildings.

### V3 UI
- Slot 1 only for 1->1 runtime proof.
- Slots 2..9 remain conceptually locked until this path passes.
- Regular Dragon/Serpent/Lotus/Wolf units only; hero/unique outputs still excluded.
- Monitor counts only actual completion-call interceptions and records input/nativeOut/override.

### Build pin
- branch `instant-death-v4-hover-telemetry`
- build head `ac324b493a0dc771bb7623f6fcc9bfa6abd1cd8e`
- workflow `Unit Changer Lab v3 Completion Only`
- run `34545809935` SUCCESS
- job `103098037229` SUCCESS
- artifact `10178983173` (`BRZE-Unit-Changer-Lab-v3-CompletionOnly`)
- artifact ZIP SHA-256 `429eccdfa3f23e4224e0b266b37e395a3fca7496dd7a65004ed73db2680df115`
- standalone: 66,003,755 bytes, SHA-256 `1e220754a33b54a50afc739f99eb29602a9995fdb535664a3c29319b190fe309`
- small: 150,698 bytes, SHA-256 `bcfc4b172021722d6ce1c2a5809c1adaa6d3dcf64f074aa350a69670d0329fb6`

Status: **compile/static candidate only; runtime output behavior pending user test.**

## V3 runtime test
1. Fully close V2/V1/V0 and BRZE.
2. Start BRZE fresh and enter a normal match.
3. Start V3.
4. Slot 1: choose `Dragon Archer (type 0)`.
5. Enable Slot 1 + `OUTPUT OVERRIDE`.
6. Send a normal Dragon Peasant into Dragon Dojo using the normal valid training action.
7. Let training complete.
8. PASS target: unit exits as Dragon Archer instead of native Dragon Spearman; game remains stable; completion counter increments once with input `0x5`, nativeOut `0x8`, override `0x0`.

If V3 still crashes, record whether crash occurs immediately on enabling V3, when issuing the training order, during progress, or exactly at completion; next step is then to instrument the native creation/finalization consumer around `0x4D6A88` rather than broadening the hook.
