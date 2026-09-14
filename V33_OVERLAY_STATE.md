# V33 FINAL PREMIUM OVERLAY STATE

Status: BUILT / CI-PROVEN / RUNTIME PENDING

## Runtime base
- Gameplay base remains V30.
- V30 Hero reset mechanics remain untouched.
- IntegratedFrameDispatcherCore remains untouched.
- UnitChangerCore remains untouched.
- V33 is UI/hotkey/bridge only.

## User-approved V32 baseline
- Alt+W overlay works and visual direction is approved.
- V32 premium deck layout is the visual base for V33.

## V33 requested final polish
- Overlay opacity: 91%.
- Hero duration shows the exact seconds that will be applied.
- COPY UNIT shows and allows changing base +X offset before paste.
- Overlay direct SINGLE KILL action removed.
- Stale Death Burst toggle removed; tap/hold behavior remains the authoritative single/burst path.
- One target-policy switch: ENEMY ONLY vs ALL UNITS.
- Compact Unit Changer is included without removing current Unit Changer capability.

## Unit Changer bridge design
- V33 does not duplicate UnitChangerCore or write its config independently.
- It mirrors and drives the existing V18.3 MainForm controls by reflection:
  - `profileSelect` for all 12 native training-building profiles,
  - `slotOn[0..8]` for all nine output slots,
  - `slotOut[0..8]` for the full current catalog.
- Local compact Unit Changer page includes:
  - all 12 buildings,
  - nine slot ON/OFF controls,
  - full unit catalog copied from the authoritative main controls,
  - ALL / DRAGON / SERPENT / LOTUS / WOLF / HEROES filters,
  - 1-9 ON and ALL OFF.
- Because the bridge manipulates the existing controls, the normal trainer and overlay share one authoritative Unit Changer state instead of fighting each other.

## Safety / invariants
- V33 finalizer changes only FinalV18MainForm UI metadata/attach hook.
- CI byte-locked UnitChangerCore, HookCore, InstantDeathCore, IntegratedFrameDispatcherCore, HeroEffectDirectCoreV30, and IntegratedFeaturePanelsV30 before applying V33.
- No CreateRemoteThread.
- No new native gameplay helper.
- No duplicated frame hook.
- Alt+W remains HWND RegisterHotKey + GetAsyncKeyState edge fallback.
- Overlay remains WS_EX_NOACTIVATE / ShowWithoutActivation / TopMost.

## CI proof
- Workflow: `Final V33 Final Premium Overlay Four-Pack`
- Run: `34842216589`
- Job: `103969580217`
- Head: `5d33adfea082e84a6935775c71b299a8cbfcd737`
- Result: SUCCESS
- Architecture verifier: PASS
- CLEAN compile smoke: PASS
- DIAGNOSTICS compile smoke: PASS
- CLEAN standalone publish: PASS
- DIAGNOSTICS standalone publish: PASS
- CLEAN small publish: PASS
- DIAGNOSTICS small publish: PASS
- Artifact: `10346841656`
- Artifact digest: `sha256:dc309e75f1bb98c3a3627c6ce8c9a213d84dfdfc21eebfde13a2dbd312655f0f`

## Output hashes
- BRZE-Trainer-FINAL-V33-Clean.exe
  - SHA256 `f1c1718d92028949757fcdad34b83f4855286bf251c62a56c62469b1ef6b760f`
- BRZE-Trainer-FINAL-V33-Diagnostics.exe
  - SHA256 `a996b33c8ff67d463c79f69fc3743f8ee18e34fe71a4a59535d7d134598d1928`
- BRZE-Trainer-FINAL-V33-Clean-Small.exe
  - SHA256 `c32ec4fb0ebcdba72d47fcadbebd458e778ea34434e1d55e9bf0f6e714d68b6d`
- BRZE-Trainer-FINAL-V33-Diagnostics-Small.exe
  - SHA256 `c9fa9f636363c5afe7fd43173ae92138baed61d3a7356b7e3c33a2382d57791a`

## Next action
Runtime-test `BRZE-Trainer-FINAL-V33-Diagnostics.exe` first. Confirm:
1. Alt+W still toggles reliably over BRZE.
2. Overlay is 91% opaque and visually acceptable.
3. Hero duration value is visible and changes with -5/+5/reset.
4. COPY UNIT base +X offset is visible, adjustable, and Paste uses the selected base offset.
5. Death policy switch mirrors ENEMY ONLY / ALL UNITS correctly while tap/hold behavior remains unchanged.
6. Unit Changer mini page changes the same authoritative 12-profile / 9-slot / full-catalog state as the normal trainer.
