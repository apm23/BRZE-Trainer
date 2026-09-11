# Unit Changer Lab — STATE

Last updated: 2026-09-11 (Asia/Tokyo)

## Isolation / target
Unit Changer remains a separate trainer and is NOT merged into the main BRZE trainer.

Current architecture chosen by user:
- no red-X/retraining bypass for the initial feature;
- use only BRZE-native-valid training input;
- override output only at the real training-completion path;
- final target: Slots 1..9 independently ON/OFF; emitted unit count equals active slots;
- cross-clan regular-unit output is allowed and already runtime-proven.

## Permanent safety rules
- Never reintroduce V2 global mapper override.
- Never broad-patch all units to train everywhere.
- Never clone/copy raw Unit structs for extra outputs.
- Invalid/red-X inputs remain stock.
- AI-owned training remains stock.
- Extra outputs use BRZE native create/finalize + full observed per-unit bookkeeping.
- Verify exact stock bytes before hooks; restore only our own patches on close.

## Core native mappings
- native UnitIn -> UnitOut mapper: preferred VA `0x4D69F6`, RVA `0x0D69F6`
  - ECX = Building*
  - stack arg = input Unit Type
  - EAX = output Unit Type or `0xFFFFFFFF`
  - entry `55 8B EC 8B 81 58 02 00 00`
  - epilogue `ret 4`
- completion mapper call: preferred `0x4D5E08`, RVA `0x0D5E08`
  - stock bytes `E8 E9 0B 00 00`
- native create/finalize: `0x4D6A88`, RVA `0x0D6A88`
- building attach/rally: `0x4D231C`, RVA `0x0D231C`
- first-unit register call site: preferred `0x4D5EA4`, RVA `0x0D5EA4`
  - stock bytes `E8 09 78 FD FF`
  - callee `0x4AD6B2`, RVA `0x0AD6B2`
- local unit notification: `0x5B605D`, RVA `0x1B605D`
- local player ID RVA `0x4416D0`
- player base RVA `0x4416A4`, owner stride `0xA0`, unit count field `+0x28`

Observed per-unit completion bookkeeping:
1. `0x4D6A88` create/finalize -> Unit*
2. `0x4D231C` building attach/rally
3. building `+0x4A8 -> unit+0x36C`
4. building `+0x4AC -> unit+0x370`
5. increment owner/player unit counter
6. local unit notification `0x5B605D`
7. building `+0x4A0 -> unit+0x798`
8. registration `0x4AD6B2`
9. building training reset is performed only once by original stock completion code after outputs are handled.

## V0 observer — RUNTIME PROVEN
Runtime observation showed:
- idle: `trainType=0xFFFFFFFF`, progress 0
- normal training enters active state and progress advances
- rejected red-X/retraining remains idle

Conclusion: red-X occurs before building training state. Do not attempt eligibility through `+0x488/+0x490/+0x4B8`.

Build: run `34535724003`, artifact `10175347109`.

## V1 eligibility tracer — superseded
Kept only if red-X work is revisited later.
Build: run `34538748743`, artifact `10176457794`.

## V2 global mapper override — RUNTIME FAIL / PERMANENTLY REJECTED
V2 modified mapper result globally at RVA `0x0D69F6`.
User runtime verdict: **GAME CRASH**.
Do not reuse this architecture.

Build: run `34544162156`, artifact `10178411232`.

## V3 completion-only output override — RUNTIME PROVEN / LOCKED
V3 leaves the global mapper stock and redirects only the completion mapper call at RVA `0x0D5E08`.
For valid local completion, mapper runs natively, then EAX output type is replaced immediately before stock native creation/finalization.

User runtime verdict on 2026-09-11: **SUKSES BESAR**.
- 1 valid input -> 1 configured output works.
- cross-clan output works.
- Lotus Master Warlock and Wolf Werewolf specifically tested successfully.
- no crash reported.

Build pin:
- head `ac324b493a0dc771bb7623f6fcc9bfa6abd1cd8e`
- run `34545809935`
- artifact `10178983173`
- standalone SHA-256 `1e220754a33b54a50afc739f99eb29602a9995fdb535664a3c29319b190fe309`
- small SHA-256 `bcfc4b172021722d6ce1c2a5809c1adaa6d3dcf64f074aa350a69670d0329fb6`

## V4 two-output proof — RUNTIME PROVEN / LOCKED
V4 preserves V3 output #1 path. It wraps the stock first-unit registration call, runs that original registration first, then creates Slot 2 with the native `0x4D6A88` path and repeats the full per-unit bookkeeping block. The building training reset is left to stock code and happens once.

User runtime verdict on 2026-09-11: **SUKSES BESAR — two units emerged successfully**.
Therefore the native extra-unit path is runtime-proven for one additional output and becomes the locked base for scaling.

Build pin:
- head `a45111949257e1d3b78956a210e974a2e1ef2513`
- run `34549070694`
- job `103107925539`
- artifact `10180134935`
- artifact ZIP SHA-256 `e98edafcaa7821ce5b7dba348f5eb0507ab9e560ab52d414b9644df7656ad615`
- standalone SHA-256 `f317f78955e7ecd3394f32cfc5158e87f20bcb3b2de4bdb7e3a8e3774ee0f6eb`
- small SHA-256 `0c8062954844758c7d5b5302f02346857dbe8954f13fa9ea20c2649204611567`

## V5 full Slots 1..9 — RUNTIME PROVEN / LOCKED FULL BASE
Project: `UnitChangerTrainerV5/`.

Architecture:
- all 9 slots are independently configurable ON/OFF;
- master `MULTI OUTPUT` switch arms the feature;
- at each valid local training completion, the **first active slot** becomes the primary output through the V3-proven completion-only path;
- every other active slot is processed sequentially as an extra output through the V4-proven native create/finalize + per-unit bookkeeping path;
- primary slot can be any of Slot 1..9; Slot 1 is not mandatory;
- inactive slots are skipped;
- if an extra `0x4D6A88` create returns null, that slot records failure and later slots continue instead of aborting the whole sequence;
- pending token is bound to the exact training Building* and consumed once at the stock post-register site;
- caller GPR + XMM0..3 are preserved while extra outputs run;
- mapper/eligibility remain stock;
- telemetry reports active slot count, primary slot, total extra attempts/success/fail, last extra Unit*, and per-slot success counters S1..S9.

Build pin:
- branch `instant-death-v4-hover-telemetry`
- head `35e6c6330665808034da7d846df9b70e856ce1b9`
- workflow `Unit Changer Lab v5 Full Nine Output`
- run `34550087750` SUCCESS
- job `103110964921` SUCCESS
- artifact `10180487723` (`BRZE-Unit-Changer-Lab-v5-FullNine`)
- artifact ZIP SHA-256 `0d367fe942fd38a19527b932d6e8a06c37dfbd24e9e911456da0b9eea99a97dd`
- standalone size `66,007,332` bytes, SHA-256 `ab8e14e9dbfe7297dd28eb6fc053a91b61f1c157979f97c0fddf8c75af63e50b`
- small size `158,378` bytes, SHA-256 `308110aaf7abe6d634838736bdd345d8863d168c3a77450af8bb60942a1c534e`
- CI: architecture guard PASS, compile PASS, publish PASS.

### V5 runtime verdict — 2026-09-11
User reported **SUKSES BESAR** and confirmed all of the following in live gameplay:
- all 9 active slots produced **exactly 9 units** from one native-valid training completion;
- no crash/freeze reported during the 1->9 stress test;
- multi-building use also works correctly;
- sparse-slot configurations also work correctly;
- therefore output count follows active slot count, not fixed slot position/count.

V5 is therefore the current **runtime-proven full 1->9 base** and must be preserved as the authoritative Unit Changer implementation for further polish/finalization.

## Remaining hardening before optional final merge
Core functionality is considered proven. Further work is polish/hardening only unless the user reports a regression:
- repeated long-session training cycles;
- population-cap/accounting edge behavior;
- save/load behavior with trainer active;
- Journey/map transition behavior;
- optional heroes/unique-unit output policy;
- optional UI cleanup/preset recipes;
- merge into main trainer only if the user explicitly requests it.
