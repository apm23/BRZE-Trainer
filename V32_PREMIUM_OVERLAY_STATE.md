# V32 PREMIUM OVERLAY STATE

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Purpose
V32 is an overlay/UI-only repair above runtime-proven V30 gameplay.

## V31 result — REJECTED
User runtime test: **Alt+W did not show the V31 overlay.** V31 depended on `RegisterHotKey(IntPtr.Zero, ...)` plus an application message filter for a thread-level `WM_HOTKEY`. Do not reuse that hotkey architecture.

## V32 hotkey architecture
- dedicated `NativeWindow` message-only HWND owns `RegisterHotKey` for Alt+W;
- `MOD_ALT | MOD_NOREPEAT`, VK_W;
- independent edge-triggered `GetAsyncKeyState` fallback every 45 ms;
- 320 ms shared debounce prevents double-toggle if both paths fire;
- intended to work while BRZE owns foreground focus and when the normal trainer is behind/minimized.

## V32 overlay architecture
- separate `PremiumOverlayForm`; normal 1640x900 trainer layout remains intact;
- borderless, TopMost, tool-window, no-activate;
- `ShowWithoutActivation = true` and `WM_MOUSEACTIVATE -> MA_NOACTIVATE`;
- opacity exactly **0.87**;
- compact 820x510 premium dark/glass control-deck design, positioned top-right over BRZE when possible;
- BRZE foreground explicitly restored after show and overlay actions;
- Quick Cheats grid mirrors existing normal-trainer toggles through UI reflection only;
- Hero buttons call runtime-proven V30 Hero Reset core;
- COPY UNIT / PASTE / single-kill use existing shared-dispatcher facades;
- no duplicate gameplay hook or game-memory implementation added.

## Gameplay lock
CI byte-locks these generated V30 gameplay sources before/after V32 finalization:
- `UnitChangerCore.cs`
- `HookCore.cs`
- `InstantDeathCore.cs`
- `IntegratedFrameDispatcherCore.cs`
- `HeroEffectDirectCoreV30.cs`
- `IntegratedFeaturePanelsV30.cs`

## V32 CI — SUCCESS
Workflow: `Final V32 Premium Alt-W Overlay Four-Pack`
- run: `34839252243` — SUCCESS
- job: `103960124211` — SUCCESS
- head: `e107794db4c88429be6cbd4d3c497345a07bd87c`
- artifact: `10345003617`
- artifact digest: `sha256:48e9b48dc0d2b1e2cf60f5cf828cd24c6320a2b0d51e13fb996b0cb5b3e7cebf`

All PASS:
- V30 gameplay byte-lock verification
- V32 HWND hotkey + async fallback architecture verification
- CLEAN compile smoke
- DIAGNOSTICS compile smoke
- CLEAN standalone publish
- DIAGNOSTICS standalone publish
- CLEAN small publish
- DIAGNOSTICS small publish

Output hashes:
- CLEAN standalone: `fb393ed27a5362d35c229194724ca755701570c881024186592ea0516f6c6d4a`
- DIAGNOSTICS standalone: `08aeefc8ba1c0ae436483291421a020d21de75ddbd0c7a8501c95657fe90861b`
- CLEAN small: `a53fec8b6ca2c0f982aca705e07cbed53905073c23d560ea1057bec87fa97c83`
- DIAGNOSTICS small: `4a854bcd33eb55ff422e58a1c731dc773fba8cb12b83f91393c0ec2ee571da28`

## Runtime status
V32 is **CI-PROVEN / RUNTIME OVERLAY SMOKE PENDING**.

Required runtime smoke:
1. Run V32 Diagnostics and return focus to BRZE.
2. Press Alt+W while BRZE is foreground.
3. Premium overlay should appear top-right at 87% opacity without pausing/minimizing BRZE.
4. Click one harmless toggle and one Hero/Copy action if convenient; BRZE should retain foreground behavior.
5. Alt+W again should hide the overlay; Alt+W again should show it.
