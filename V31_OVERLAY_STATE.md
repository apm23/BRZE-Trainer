# BRZE Trainer V31 — Alt+W Non-Activating Overlay Four-Pack

Status: **BUILT / CI-PROVEN — OVERLAY RUNTIME SMOKE NEXT**
Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Locked gameplay base
V31 is UI/packaging-only on top of **V30 runtime-proven gameplay**.

The V31 workflow byte-locks these generated/runtime sources before applying the V31 finalizer:
- `UnitChangerCore.cs`
- `HookCore.cs`
- `InstantDeathCore.cs`
- `IntegratedFrameDispatcherCore.cs`
- `HeroEffectDirectCoreV30.cs`
- `IntegratedFeaturePanelsV30.cs`

V31 does not alter Hero Effect reset, custom duration, Copy Unit, Instant Death, Unit Changer, or the shared frame dispatcher.

## Overlay behavior
New source: `OverlayHotkeyController.cs`.

Hotkey: **Alt+W**.

First Alt+W while the trainer is running:
- turns the existing trainer window into overlay mode;
- keeps the full V30 trainer UI;
- borderless dark layout;
- TopMost;
- opacity `0.93`;
- centered over the BRZE window when BRZE has a valid main window;
- removes the trainer taskbar presence in overlay mode;
- uses `WS_EX_NOACTIVATE` + `SW_SHOWNOACTIVATE`;
- handles `WM_MOUSEACTIVATE` with `MA_NOACTIVATE` so mouse interaction does not foreground the trainer;
- explicitly returns foreground to `Battle_Realms_F` after showing/hiding.

Subsequent Alt+W presses toggle overlay visibility.

Design intent: BRZE keeps foreground focus and continues simulation while the trainer is displayed. Mouse buttons/toggles remain usable; keyboard focus intentionally stays with BRZE. NumericUpDown values are intended to be adjusted by mouse while in overlay mode.

## Safety / invariants
- global hotkey is registered as `ALT + W` with `MOD_NOREPEAT`;
- no second game hook;
- no remote thread creation;
- no mutation of V30 gameplay sources by the V31 UI finalizer;
- one shared frame-hook owner remains at RVA `0x135C43`.

## V31 CI pin
Workflow: `Final V31 Alt-W Overlay Four-Pack`
- run `34837413187` — SUCCESS
- job `103954293809` — SUCCESS
- head `8cf6fe7a138eedda3c60ac159c2a9811b143ed54`
- artifact `10344892769`
- artifact digest `sha256:a4b3a8abf7043d23a5474b5b26b592e20e3ba22b25892b3e1d05f29f5e2d2372`

Outputs:
- CLEAN standalone SHA256 `dcdeb446843a149c75c2580a9c23c8b822a19775ba4d4c82d27e543cb8822b3f`
- DIAGNOSTICS standalone SHA256 `278af5063393b41dabef8b01fdca177bf1f32069e170171d33fb4cde5951fbe8`
- CLEAN small SHA256 `443a250836a2c49ac531cc8cf8c32e03297039fd9d720c9dcea4a7d175e1b460`
- DIAGNOSTICS small SHA256 `ee0dc1f5cf807f01bd9ed74c4414ccdf7fd9b1f9f6a60a45882fe1fa5579978d`

CI PASS:
- V20 base generation;
- V22 explicit-target dispatcher layer;
- V30 proven Hero reset layer;
- gameplay hash locks before V31;
- V31 overlay architecture verifier;
- CLEAN compile smoke;
- DIAGNOSTICS compile smoke;
- all four publishes;
- artifact upload.

## Exact overlay runtime smoke
Use `BRZE-Trainer-FINAL-V31-Diagnostics.exe` first.

1. Start trainer and BRZE; click BRZE so the game is foreground and visibly running.
2. Press **Alt+W**.
   - trainer should appear centered above BRZE;
   - trainer should be slightly transparent and borderless;
   - BRZE animation/simulation should continue rather than pause.
3. Click one trainer toggle/button while watching BRZE.
   - BRZE should remain foreground/running;
   - trainer action should still register.
4. Press **Alt+W** again.
   - overlay should hide;
   - press again to show it without activating/pausing BRZE.
5. Quick Hero Effect APPLY/RESET smoke only to confirm the UI layer did not regress V30 gameplay.

If this passes, V31 is the final overlay distribution candidate.
