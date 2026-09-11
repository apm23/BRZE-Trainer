# Unit Changer Lab — STATE

Last updated: 2026-09-11 (Asia/Tokyo)

## Isolation / target
Unit Changer began as a separate trainer and its runtime-proven core is now also integrated into the main BRZE trainer.

Current architecture:
- no red-X/retraining bypass for the current production base;
- use only BRZE-native-valid training input;
- override output only at the real training-completion path;
- Slots 1..9 independently ON/OFF; emitted unit count equals active slots;
- cross-clan output is runtime-proven;
- 12 basic training-building profiles keep independent output configurations.

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
- at each valid local training completion, the **first active slot** becomes the primary output through the V3-proven completion-only path;
- every other active slot is processed sequentially as an extra output through the V4-proven native create/finalize + per-unit bookkeeping path;
- primary slot can be any of Slot 1..9;
- inactive slots are skipped;
- if an extra create returns null, that slot records failure and later slots continue;
- pending token is bound to the exact training Building* and consumed once at the stock post-register site;
- caller GPR + XMM0..3 are preserved while extra outputs run;
- mapper/eligibility remain stock.

Build pin:
- branch `instant-death-v4-hover-telemetry`
- head `35e6c6330665808034da7d846df9b70e856ce1b9`
- workflow `Unit Changer Lab v5 Full Nine Output`
- run `34550087750` SUCCESS
- job `103110964921` SUCCESS
- artifact `10180487723`
- standalone SHA-256 `ab8e14e9dbfe7297dd28eb6fc053a91b61f1c157979f97c0fddf8c75af63e50b`
- small SHA-256 `308110aaf7abe6d634838736bdd345d8863d168c3a77450af8bb60942a1c534e`

### V5 runtime verdict — 2026-09-11
User confirmed:
- all 9 active slots produced exactly 9 units from one valid completion;
- no crash/freeze during 1->9 stress test;
- multi-building use works;
- sparse slots work.

V5 remains the authoritative game-side 1->9 implementation.

## V14 building profiles — RUNTIME PROVEN / LOCKED CORE
Twelve independent basic-training-building profiles use the stock mapper as a fingerprint rather than guessed building type IDs:
- Dragon Dojo `5 -> 8`, Target Range `5 -> 0`, Alchemist Hut `5 -> 1`
- Serpent Tavern `24 -> 29`, Sharpshooter's Guild `24 -> 21`, Alchemist Hut `24 -> 23`
- Lotus Forge `40 -> 30`, Blade Garden `40 -> 38`, Training Yard `40 -> 41`
- Wolf Combat Pit `51 -> 46`, Ballistics Grounds `51 -> 48`, Quarry `51 -> 49`

User runtime verdict on 2026-09-11:
- Dragon Dojo configured for 9 outputs and Dragon Target Range configured for 9 different outputs simultaneously;
- both buildings produced their own configured outputs correctly;
- no cross-profile leakage or crash was reported.

Therefore the per-building profile architecture is runtime-proven at least for simultaneous Dragon Dojo + Target Range under full 9-output load. Preserve this core unchanged while expanding UI/catalog.

## V15 compact UI — BUILD PROVEN
- UI-only layer over V14 core.
- Building selection uses one compact dropdown rather than 12 large buttons.
- Main cheats and Unit Changer occupy equal-height top panels.
- System Status spans full width below.
- Unit slot `Use` text removed; compact checkbox only.
- Clean + Diagnostics build succeeded in run `34563835023`, artifact `10185305510`.
- V14 game-side core unchanged.

## V16 catalog expansion — SUPERSEDED POLICY
V16 added classic heroes, special/unique units, WotW units, and WotW heroes to categorized dropdowns. Its first safety policy marked all heroes/story/special/WotW content red and used auto-OFF + modal confirmation. User rejected that policy as too broad/rumbersome. Do not restore it.

## V17 WotW-only warning policy — BUILD/STATIC PROVEN, RUNTIME UI TEST PENDING
Scope remains UI/catalog only; V14 game-side core is unchanged byte-for-byte through the V16+V17 layers.

Current catalog policy:
- Regular clan units remain normal.
- Old special/unique units such as Spirit Warrior, Lotus Brothers/Golem, and Serpent Necromancer are normal entries.
- Classic heroes/story variants such as Arah, Grayback, Kenji, Otomo, Shinja, Zymeth, etc. are normal entries.
- ONLY WotW-release-exclusive units/heroes are red/risk-marked:
  - WotW units IDs `116..127`: Chakram Maiden variants, Guardian variants, Serpent Enforcer/Witch variants, Lotus Overseer/Reaper, Wolf Digger/Dryad.
  - WotW story/hero variants IDs `136..144`: Grayback variants, Longtooth Slave, Taro, Teppo, Wildeye, Yvaine variants.
- No Yes/No modal confirmation.
- No automatic slot OFF when selecting a WotW entry.
- A small red warning line appears below the Unit Changer slot grid whenever one or more WotW-only outputs are selected, advising against use in Kenji Journey/story maps.
- Reserve placeholder IDs `145..154` remain hidden.

Build pin:
- head `2d4a644e510140c357175cd43afc39c74fac9adc`
- workflow `Final V17 WotW Only Warning Clean Diagnostics`
- run `34566489119` SUCCESS
- artifact `10186231013` (`BRZE-Trainer-FINAL-V17-WotW-Only-Warning`)
- artifact ZIP SHA-256 `174a137d4efeb70f6092401a779fa9f909eac4faa8c26afab4b06feb93c23c8c`
- Clean standalone SHA-256 `621c37764e1e1f0c497a1b5430cebb124c8abb9ab54eb14847c9d5bcda81dd39`
- Diagnostics standalone SHA-256 `3b16e8a136c8676f348509de2f2ee0213c2bb5e1d619847cdd72740f78afa875`
- Clean small SHA-256 `687b31975aa178e3dce44196a737c8c86ddbec86f3eae7a5f183e6cd16255d46`
- Diagnostics small SHA-256 `3a7d8a0e7e0df01000898fd87047cd41e8dd9f616d7bbee661c213a1e7c78a5c`

Do not call V17 runtime-proven until user visually/runtime tests the revised dropdown/warning behavior.

## Deferred phase
- Arbitrary-building training such as Peasant Hut/tree and red-X bypass remains deferred until eligibility/retraining logic is proven safely.
