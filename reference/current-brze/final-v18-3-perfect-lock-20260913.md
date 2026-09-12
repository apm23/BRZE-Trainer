# BRZE Trainer V18.3 — PERFECT FINAL LOCK

Date: 2026-09-13 Asia/Tokyo  
Status: **USER-ACCEPTED PERFECT FINAL / LOCKED ROLLBACK BASELINE**

## Runtime/UI verdict
The user visually tested V18.3 Clean and accepted the final layout with the message equivalent to **"joss"**, then explicitly requested that this exact version be locked in the repository as the **perfect final** for easier future editing.

The accepted V18.3 UI includes:
- compact main cheat panel on the left;
- compact Unit Changer / Building panel on the right;
- full-width scrollable System Status below;
- quick unit filters `ALL / DRAGON / SERPENT / LOTUS / WOLF / HEROES`;
- separated bulk-action area on the right of the filter row;
- `1-9 ON` bulk enable for the currently edited building profile;
- `ALL OFF` bulk disable for the currently edited building profile;
- no WotW red blocking/warnings;
- 12 independent basic training-building profiles;
- per-building 1..9 output configuration.

## Proven functional base carried into V18.3
- V5 full 1->9 native extra-output path: runtime proven.
- multi-building output use: runtime proven.
- sparse slots: runtime proven.
- V14 building profiles: runtime proven.
- simultaneous Dragon Dojo + Target Range with separate 9-output setups: runtime proven.
- classic/WotW heroes and WotW units are intentionally selectable in the final catalog.

## Build pin
Repository: `apm23/BRZE-Trainer`  
Branch: `instant-death-v4-hover-telemetry`

Source/build head: `878076e0345ab01f518c45f1a059543b11043fdc`

Workflow: `Final V18.3 UI Polish Clean Diagnostics`  
Workflow file: `.github/workflows/final-v18-3-ui-polish.yml`  
Run: `34697494189` SUCCESS  
Job: `103563377794` SUCCESS  
Artifact: `10299740407`  
Artifact name: `BRZE-Trainer-FINAL-V18.3-UI-Polish`  
Artifact digest: `sha256:b7647822d9258bbd1d247b80c6977ecb68c9914efb87145bcded34d512443e15`

## Binary hashes
- `BRZE-Trainer-FINAL-V18.3-Clean.exe`
  - size: 66,044,391 bytes
  - SHA-256: `0770e1143d5cad98639f0e284414718f80291ec18c0196b18855b92d0645f4e2`
- `BRZE-Trainer-FINAL-V18.3-Diagnostics.exe`
  - size: 66,044,625 bytes
  - SHA-256: `6a113496b6cfb744e2090efec83546970af446aa946c957390471b9ab4b373af`
- `BRZE-Trainer-FINAL-V18.3-Clean-Small.exe`
  - size: 256,170 bytes
  - SHA-256: `33dc21c4eba0e796a46689b5f157c686f57680f6c61376889e2cd53b12b7c488`
- `BRZE-Trainer-FINAL-V18.3-Diagnostics-Small.exe`
  - size: 257,194 bytes
  - SHA-256: `23f5a7c6df11ffc6f7420fe43caa64fb38242123655f3cc382c3845c01d63d96`

## Authoritative edit points
- UI delta: `tools/finalize_v18_3_bulk_actions.py`
- workflow: `.github/workflows/final-v18-3-ui-polish.yml`
- project: `MergedTrainerV18_3.csproj`
- root continuity pointer: `FINAL_CURRENT.md`
- Unit Changer historical ledger: `UnitChangerTrainer/STATE.md`

## Lock rules
1. Preserve V18.3 as the rollback baseline.
2. Do not edit proven game-side core simply to refactor or beautify code.
3. Future UI/catalog changes should be a new additive finalizer layer (V18.4/V19), not destructive edits to old proven layers.
4. Hash-lock `UnitChangerCore.cs` before/after UI-only generation.
5. Do not reintroduce the rejected global UnitIn->UnitOut mapper override.
6. Arbitrary-building/red-X bypass remains a separate future experiment.
7. Refresh Trainer must remain a true detach/re-resolve/rebind path, not just UI refresh.

## Fresh-thread handoff
`CONTINUE BRZE TRAINER — READ FINAL_CURRENT.md FIRST — V18.3 PERFECT FINAL IS AUTHORITATIVE`
