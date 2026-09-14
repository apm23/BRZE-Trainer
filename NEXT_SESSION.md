# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks / current
- V18.3 = locked main stable fallback (`FINAL_CURRENT.md`).
- V29 = runtime-proven standalone Hero reset primitive.
- V30 = **runtime-proven integrated gameplay base** and must remain locked.
- V31 overlay = **runtime-rejected** because Alt+W did not show.
- V32 = latest overlay/UI/packaging candidate; CI PASS, runtime overlay smoke next.

Do not reopen Hero reset research unless a real gameplay regression appears.

## Locked V30 gameplay
Hero reset primitive:
- exact same-effect record signature uses `record+0x58` ability ID and `record+0x17C` target Unit*;
- current BRZE tick source is module RVA `0x440A3C`;
- same effect already active -> write current tick to `record+0x194` -> restart same instance without stacking;
- missing effect -> one-shot native Replay V2 apply;
- APPLY BOTH partitions mixed selections so existing same effects are never replayed over themselves;
- custom duration retains proven full-lifetime config hold/restore.

V30 integrated runtime result from user: **WORK / PASS**.

Never reintroduce:
- repeated native replay/refresh on already-active same effect;
- `record+0x194 = 0` reset;
- Unit+0x460 duration writes;
- CreateRemoteThread;
- hard-coded transient effect path.

## BRZE minimize behavior
When BRZE is minimized, simulation/game time pauses. Do not interpret a static clock sample under minimize as a bad clock source.

## V31 — RUNTIME REJECTED
User test: **Alt+W did not make the overlay appear.**
Rejected architecture: `RegisterHotKey(IntPtr.Zero, ...)` plus `Application.AddMessageFilter` for the thread-level WM_HOTKEY. Do not restore this approach.

## V32 — PREMIUM ALT+W OVERLAY
State: `V32_PREMIUM_OVERLAY_STATE.md`.

Hotkey repair:
- dedicated message-only `NativeWindow` HWND owns `RegisterHotKey` for Alt+W;
- edge-triggered `GetAsyncKeyState` fallback every 45 ms;
- shared 320 ms debounce prevents double-toggle.

Overlay design:
- separate compact `PremiumOverlayForm`; normal full trainer remains intact;
- 820x510 control deck near top-right of BRZE;
- borderless / TopMost / no-activate;
- opacity **0.87**;
- dark premium/glass presentation with distinct overlay-only design;
- Quick Cheats compact toggle grid mirrors normal trainer UI state;
- Hero Issyl / Grayback / Both buttons use V30 core;
- COPY SELECTED / PASTE BESIDE / SINGLE KILL use existing shared dispatcher;
- BRZE foreground is restored after showing the overlay and after mouse actions.

V32 CI byte-locks all V30 gameplay sources before/after overlay finalization:
- `UnitChangerCore.cs`
- `HookCore.cs`
- `InstantDeathCore.cs`
- `IntegratedFrameDispatcherCore.cs`
- `HeroEffectDirectCoreV30.cs`
- `IntegratedFeaturePanelsV30.cs`

V32 CI:
- workflow `Final V32 Premium Alt-W Overlay Four-Pack`
- run `34839252243` — SUCCESS
- job `103960124211` — SUCCESS
- head `e107794db4c88429be6cbd4d3c497345a07bd87c`
- artifact `10345003617`
- digest `sha256:48e9b48dc0d2b1e2cf60f5cf828cd24c6320a2b0d51e13fb996b0cb5b3e7cebf`
- CLEAN standalone SHA256 `fb393ed27a5362d35c229194724ca755701570c881024186592ea0516f6c6d4a`
- DIAGNOSTICS standalone SHA256 `08aeefc8ba1c0ae436483291421a020d21de75ddbd0c7a8501c95657fe90861b`
- CLEAN small SHA256 `a53fec8b6ca2c0f982aca705e07cbed53905073c23d560ea1057bec87fa97c83`
- DIAGNOSTICS small SHA256 `4a854bcd33eb55ff422e58a1c731dc773fba8cb12b83f91393c0ec2ee571da28`

## EXACT NEXT ACTION — V32 RUNTIME OVERLAY SMOKE
Use `BRZE-Trainer-FINAL-V32-Diagnostics.exe` first.

1. Start trainer + BRZE and return foreground to BRZE.
2. Press Alt+W while BRZE is running. Premium V32 control deck must appear top-right at 87% opacity.
3. BRZE must remain running and must not be minimized/paused.
4. Alt+W hides; Alt+W again shows.
5. Click one harmless Quick Cheat toggle and, if convenient, one Hero/COPY action. Overlay must not regress V30 gameplay.

If Alt+W still fails, do not resume a long probe chain; make one direct hotkey runtime diagnostic/fallback repair based on the V32 dual-path implementation.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V30 integrated gameplay is RUNTIME-PROVEN PASS and locked. V31 overlay is runtime-rejected because Alt+W did not show. V32 replaces it with a dedicated HWND RegisterHotKey plus GetAsyncKeyState fallback and a separate premium 87% no-activate control deck. V32 CI run 34839252243 SUCCESS, artifact 10345003617. Next: one runtime smoke: BRZE foreground -> Alt+W show premium overlay -> game keeps running -> Alt+W hide/show -> one harmless button action.`
