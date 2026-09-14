# V32 PREMIUM OVERLAY STATE

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Purpose
V32 is an overlay/UI-only repair above runtime-proven V30 gameplay.

## Why V31 is superseded
User runtime test: Alt+W did not show the V31 overlay. V31 registered Alt+W with `RegisterHotKey(IntPtr.Zero, ...)` and depended on an application message filter for a thread-level WM_HOTKEY. That design is rejected for the final overlay path.

## V32 hotkey architecture
- dedicated `NativeWindow` message-only HWND owns `RegisterHotKey` for Alt+W;
- `MOD_ALT | MOD_NOREPEAT`, VK_W;
- independent edge-triggered `GetAsyncKeyState` fallback runs every 45 ms;
- 320 ms shared debounce prevents a registered-hotkey event plus poll fallback from toggling twice;
- works while BRZE owns foreground focus and also when the normal trainer window is behind/minimized.

## V32 overlay architecture
- separate `PremiumOverlayForm`; it does NOT transform the normal 1640x900 trainer window;
- borderless / TopMost / tool window / no-activate;
- `ShowWithoutActivation = true` and `WM_MOUSEACTIVATE -> MA_NOACTIVATE`;
- opacity exactly **0.87**;
- positioned at the top-right of the BRZE window where possible;
- BRZE foreground is explicitly restored after show and after overlay actions;
- distinct premium dark/glass control-deck layout;
- Quick Cheats compact toggle grid mirrors the existing MainForm toggle states through UI reflection only;
- Hero actions call the proven V30 Hero core;
- COPY UNIT and single-kill actions call the existing shared dispatcher facade;
- no duplicate gameplay hooks or game-memory implementation in the overlay layer.

## Gameplay lock
V32 must leave these generated V30 gameplay sources byte-identical after the V32 finalizer:
- `UnitChangerCore.cs`
- `HookCore.cs`
- `InstantDeathCore.cs`
- `IntegratedFrameDispatcherCore.cs`
- `HeroEffectDirectCoreV30.cs`
- `IntegratedFeaturePanelsV30.cs`

## Build outputs
Target four-pack:
1. CLEAN standalone
2. DIAGNOSTICS standalone
3. CLEAN small
4. DIAGNOSTICS small

## Status
SOURCE COMMITTED — CI / runtime overlay smoke pending.
