# BRZE Trainer — AUTHORITATIVE FINAL CURRENT

**CURRENT FINAL: V18.3 — PERFECT FINAL / RUNTIME-ACCEPTED**  
Locked: **2026-09-13 (Asia/Tokyo)**

> **START HERE FOR EVERY FUTURE EDIT.**  
> Treat this file as the first source of truth before modifying the trainer.

## User final verdict
The user accepted **V18.3 Clean** as the final perfect UI/base after direct visual/runtime use.

Confirmed in the final V18.x sequence:
- V14 building-profile core works at runtime with simultaneous independent building profiles.
- 9 outputs from one valid training completion work.
- multi-building use works.
- sparse output slots work.
- Dragon Dojo and Dragon Target Range can each keep separate 9-output configurations without profile leakage.
- WotW heroes/units are intentionally selectable normally; no red block/warning policy remains.
- quick unit filters work: `ALL`, `DRAGON`, `SERPENT`, `LOTUS`, `WOLF`, `HEROES`.
- compact `1-9 ON` action renders correctly and works.
- V18.3 separates bulk actions from clan tabs and adds per-building-profile `ALL OFF`.
- final V18.3 layout was explicitly accepted by the user as **"joss"** and requested to be locked as **"final sempurna"**.

## FINAL BUILD PIN — DO NOT LOSE
Repository: `apm23/BRZE-Trainer`  
Branch: `instant-death-v4-hover-telemetry`

V18.3 source/build head:
- `878076e0345ab01f518c45f1a059543b11043fdc`

Workflow:
- `Final V18.3 UI Polish Clean Diagnostics`
- file: `.github/workflows/final-v18-3-ui-polish.yml`
- run: `34697494189` — **SUCCESS**
- job: `103563377794` — **SUCCESS**
- artifact: `10299740407`
- artifact name: `BRZE-Trainer-FINAL-V18.3-UI-Polish`
- artifact digest: `sha256:b7647822d9258bbd1d247b80c6977ecb68c9914efb87145bcded34d512443e15`

Published binaries:
- Clean standalone — 66,044,391 bytes — SHA-256 `0770e1143d5cad98639f0e284414718f80291ec18c0196b18855b92d0645f4e2`
- Diagnostics standalone — 66,044,625 bytes — SHA-256 `6a113496b6cfb744e2090efec83546970af446aa946c957390471b9ab4b373af`
- Clean small — 256,170 bytes — SHA-256 `33dc21c4eba0e796a46689b5f157c686f57680f6c61376889e2cd53b12b7c488`
- Diagnostics small — 257,194 bytes — SHA-256 `23f5a7c6df11ffc6f7420fe43caa64fb38242123655f3cc382c3845c01d63d96`

## LOCKED GAME-SIDE BASE
**Do not rewrite these simply for cleanup/refactoring.** Change only if the user reports a concrete regression or explicitly starts a new experimental branch.

### Unit Changer
Authoritative game-side line:
- V3 completion-only primary-output override — runtime proven.
- V4 native second-output path — runtime proven.
- V5 generalized Slots 1..9 — runtime proven.
- V14 12-building profile architecture — runtime proven.

Permanent rules:
- global mapper at RVA `0x0D69F6` stays stock.
- never reintroduce rejected V2 global mapper override.
- extra units use native create/finalize and observed per-unit bookkeeping; never raw-clone Unit structs.
- AI-owned training stays stock.
- invalid/red-X eligibility stays stock in this production base.
- preserve exact stock-byte validation before hooks.

### Other main trainer cores
Preserve the already runtime-proven implementations for:
- Selection120 + Event Headroom160.
- Instant Death V6 targeting + V7 Burst/Single controls.
- Reveal/Journey-safe architecture currently used by the integrated trainer.
- Unlimited Wolves V8 Wolves Den stock path.
- Pause Peasant separate from Peasant 3s.

## V18.3 UI / CATALOG — FINAL ACCEPTED SHAPE
Top layout:
- left: compact main-cheat panel.
- right: compact `UNIT CHANGER // BUILDING` panel.
- below: full-width scrollable `SYSTEM STATUS`.
- bottom: main action bar.

Unit Changer:
- one compact Building dropdown for the 12 basic training-building profiles.
- each building owns an independent 1..9 output configuration.
- 9 compact output rows in a 3x3 grid.
- output count follows checked slots automatically; no separate master Multi Output checkbox.
- quick filters: `ALL`, `DRAGON`, `SERPENT`, `LOTUS`, `WOLF`, `HEROES`.
- special/unique non-heroes are folded into their clan.
- classic + WotW hero/story variants live under `HEROES`.
- no unit is red/blocked/warned solely for being WotW.
- right-side bulk action group is visually separated from filter tabs:
  - `1-9 ON` enables all nine slots for the currently edited building profile.
  - `ALL OFF` disables all nine slots for the currently edited building profile only.
- main trainer `ALL OFF` remains a separate bottom action and still means all trainer cheats OFF.

## REFRESH TRAINER — PRESERVE
The current Refresh implementation was audited before V18 and should remain intact unless a real bug is observed.

Expected behavior:
1. stop timer;
2. reset Unit Changer;
3. stop Pause Peasant, Wolf, Instant Death, Stamina, Horse, Hook, Selection;
4. drop Reveal hook without heavy FOW restore;
5. `Native.Detach()`;
6. clear ready state;
7. force `GameGate.Probe(true)`;
8. restart timer;
9. next normal timer tick rebinds preserved toggles/configurations.

Important: Unit Changer `Reset()` clears process handle/module base/cave/install state, so next attach reads `process.MainModule.BaseAddress` again. Do not replace Refresh with a cosmetic UI-only refresh.

## FILES TO TOUCH FOR THE NEXT EDIT
Prefer **additive version layering** rather than editing old proven generators in place.

Current V18.3 UI layer:
- `tools/finalize_v18_3_bulk_actions.py`
- `.github/workflows/final-v18-3-ui-polish.yml`
- `MergedTrainerV18_3.csproj`

Generator chain that reconstructs the final trainer:
1. `tools/prepare_merged_v3_wolfstock.py`
2. `tools/finalize_v10_ui_safe.py`
3. `tools/finalize_v11_transition_safe.py`
4. `tools/finalize_v13_integrated.py`
5. `tools/finalize_v14_building_profiles.py`
6. `tools/v14_generator_repair.py`
7. `tools/v14_ui_compile_repair.py`
8. `tools/finalize_v15_compact_ui.py`
9. hash-lock `UnitChangerCore.cs`
10. `tools/finalize_v16_unit_catalog.py`
11. `tools/finalize_v17_wotw_warning.py`
12. `tools/finalize_v18_quick_tabs.py`
13. `tools/finalize_v18_3_bulk_actions.py`

For a future V18.4/V19 UI-only change:
- create a NEW finalizer layer on top of V18.3;
- create a NEW workflow/project name;
- hash `UnitChangerCore.cs` before/after and fail CI if it changes;
- compile Clean + Diagnostics;
- publish standalone + small;
- runtime-test only the changed behavior;
- keep this V18.3 pin as the rollback baseline.

## DEFERRED / SEPARATE FUTURE PROJECT
Arbitrary-building eligibility/retraining (for example training units through Peasant Hut/tree or bypassing the red-X eligibility gate) is **NOT part of this locked final**.

If resumed, treat it as a separate experimental phase. Do not destabilize V18.3 while investigating it.

## Continuation command
When resuming in a fresh thread, use:

`CONTINUE BRZE TRAINER — READ FINAL_CURRENT.md FIRST — V18.3 PERFECT FINAL IS AUTHORITATIVE`
