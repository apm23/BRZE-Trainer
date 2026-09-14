# V33 FINAL PREMIUM OVERLAY STATE

Status: SOURCE READY / CI PENDING / RUNTIME PENDING

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
- Hero duration must show the exact seconds that will be applied.
- COPY UNIT must show and allow changing base +X offset before paste.
- Remove overlay direct SINGLE KILL action.
- Remove stale Death Burst toggle; tap/hold behavior already supplies single/burst.
- Add one target-policy switch: ENEMY ONLY vs ALL UNITS (same-side units included when ALL is selected).
- Add compact Unit Changer inside the overlay without removing current Unit Changer capability.

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
- V33 finalizer may change only FinalV18MainForm UI metadata/attach hook.
- CI hashes and byte-locks UnitChangerCore, HookCore, InstantDeathCore, IntegratedFrameDispatcherCore, HeroEffectDirectCoreV30, and IntegratedFeaturePanelsV30 before applying V33.
- No CreateRemoteThread.
- No new native gameplay helper.
- No duplicated frame hook.
- Alt+W remains HWND RegisterHotKey + GetAsyncKeyState edge fallback.
- Overlay remains WS_EX_NOACTIVATE / ShowWithoutActivation / TopMost.

## Build outputs expected
- BRZE-Trainer-FINAL-V33-Clean.exe
- BRZE-Trainer-FINAL-V33-Diagnostics.exe
- BRZE-Trainer-FINAL-V33-Clean-Small.exe
- BRZE-Trainer-FINAL-V33-Diagnostics-Small.exe

## Next action
Trigger `Final V33 Final Premium Overlay Four-Pack`, fix compile/UI-only issues if any, then runtime-test the Diagnostics standalone first.
