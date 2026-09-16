# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-17 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative current / fallbacks
- V18.3 = locked historical stable fallback (`FINAL_CURRENT.md`).
- V29 = runtime-proven standalone Hero reset primitive.
- V30 = runtime-proven integrated gameplay base; locked.
- V33 = runtime-proven premium overlay baseline.
- V34 = runtime-proven gameplay/collision/hard-refresh base.
- V35 = previous FINAL RC.
- V36 = **latest STRESS-HARDENED candidate: CI-PROVEN / RUNTIME-PENDING**.

Do not promote V36 to FINAL solely from CI. Exact user runtime stress gate below is mandatory.

## Locked Hero architecture retained
- exact same-effect signature: `record+0x58` ability + `record+0x17C` target Unit*;
- current BRZE tick = module RVA `0x440A3C`;
- active same effect -> write only `record+0x194 = current tick`;
- missing effect -> one-shot native Replay V2 apply;
- APPLY BOTH partitions mixed selections;
- custom duration uses proven full-lifetime config hold/restore;
- repeated native same-effect reapply remains forbidden.

## Why V36 exists
User stress-tested V35 and found three concrete runtime regressions:
1. If cheats were armed before BRZE launched, later Journey stages could break Peasant 3s / Pause Peasant / Reveal Map; HOME refresh often did not recover them.
2. Hero Effect was unreliable around selections above 100 and sometimes with long requested durations.
3. Overlay Unit Changer ComboBoxes were hard to use and could minimize fullscreen BRZE.
User also requested a premium trainer icon visually distinct from BRZE.

## V36 stress hardening
### Pre-launch / Journey stage safety
- Armed cheats wait for **1.5 seconds of continuously battle-ready state** before runtime writes/hooks start.
- `SelectionCore` detects stage/root/local-player changes and re-arms live lists/headroom.
- `SelectionCore.Stop()` now restores only trainer-owned instruction patches and frees the code cave only after proving no patched instruction still points to it.
- Pause Peasant no longer preserves a transition-time zero as the normal creation state; it waits for the new-stage creation state and restores creation enabled.
- Reveal Map can recognize/adopt its own resident transition guard after refresh/reopen and transient reapply failures become retryable instead of permanent-error state.

### Hero Effect stress capacity
- Hero selected/replay cap raised from 120 to **500**.
- `COPY UNIT` remains capped at **120**.
- effect maintenance polling throttled to 200 ms for large/long-lived groups.
- V30 no-stacking semantics remain unchanged.

### Fullscreen-safe Unit Changer overlay
- visible Unit Changer dropdown popup controls removed from the overlay.
- building/output selection is now filter + `‹ / ›` controls with label readouts.
- hidden ComboBoxes remain only as the state bridge/model; they are not added as visible overlay controls.
- overlay remains no-activate and draggable.

### Premium icon / taskbar fix
- V36 project embeds `BRZE-Trainer-V36.ico` generated from the checked-in validated 64×64 trainer icon payload.
- User runtime screenshot showed the Windows taskbar still used the generic WinForms overlapping-windows icon.
- Root cause: EXE `ApplicationIcon` existed, but the WinForms `MainForm.Icon` was never explicitly bound to it.
- Current build adds `this.Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath)` and `ShowIcon=true` at runtime.
- This icon-only repair does **not** change gameplay cores.

## V36 architecture / lifecycle audit — PASS
- 15 compiled sources scanned.
- 15 fixed module mutation sites resolved.
- no fixed-address mutation site has multiple compiled owners.
- shared render hook `0x135C43` remains owned only by `IntegratedFrameDispatcherCore`.
- pre-launch stable-ready gate present.
- SelectionCore teardown/rearm invariant present.
- Selection + Pause Peasant stage rebind safety present.
- Reveal guard adoption/retry present.
- Hero Effect supports 500 targets while Copy Unit stays 120.
- overlay Unit Changer uses non-popup previous/next controls.
- distinct V36 application icon embedded and runtime MainForm icon binding verified in CI.
- locked UnitChangerCore / HookCore / InstantDeath wrapper / StaminaCore / HorseCore / WolfCore / original V30 feature panel hashes remained unchanged through V36 layer.

## V36 current CI proof — icon-fixed build
Workflow `Final V36 Stress Hardened Four-Pack`
- run `35160650206` — **SUCCESS**
- job `105010378257` — **SUCCESS**
- head `7748625e24ce4c080901d649c0a0b23a426d90f5`
- artifact `10472971410`
- artifact digest `sha256:b1b382e5df5d05079b31704ced5b69bff16d067f16edded4e0eda1def8abd054`

Hashes:
- CLEAN standalone `4c5da5d6755a5c5209d73a67f42cc83807022cb4418bbffa62048ca43992b9b7`
- DIAGNOSTICS standalone `8afc8b66263ed42fdf3a141d4a9d16082b5710cb0ad9ec7c5fb5c98cb42af512`
- CLEAN small `a88382e97d137e6f89659e265de5cac4cc9530c6dff335c03bb1d0278ad859d5`
- DIAGNOSTICS small `8bed9ce0c60c7c7945d086c04a5b3efa4be7ceaaf1cad030870516d8896b9399`

Both CLEAN and DIAGNOSTICS smoke builds passed; runtime Form.Icon verification passed; all four publishes and artifact upload passed. Ignore unrelated legacy `fix-large-selection-freeze.yml` failures.

## EXACT NEXT ACTION — V36 USER RUNTIME STRESS GATE
Prefer `BRZE-Trainer-FINAL-V36-Diagnostics.exe` from the **icon-fixed artifact/run 35160650206** for the first pass.

1. Confirm taskbar/window icon is now the custom premium trainer icon, not the generic overlapping-windows WinForms icon.
2. **Pre-launch regression:** open trainer first, arm the same cheats from the V35 failure scenario, then launch BRZE and play Journey stage 1 -> 2 -> 3. Peasant 3s must stay functional and Reveal must recover after transitions.
3. **Pause Peasant stage crossing:** turn Pause Peasant ON in one stage, enter the next stage, turn it OFF. Peasants must resume normally; then Peasant 3s must still produce the fixed timing.
4. **HOME recovery:** during/after a stage transition press HOME / REFRESH TRAINER. Enabled features must rebuild and remain functional; this must repair runtime bindings rather than only blink resource values.
5. **Hero stress:** select roughly 150–300 units and test Issyl / Grayback / Both at 120 s and 200 s (420 s optional). It must apply deterministically, without stacking/freeze/crash.
6. **Fullscreen overlay Unit Changer:** Alt+W -> Unit Changer -> use filters + `‹ / ›` to change building/output heroes. BRZE must not minimize and control must stay responsive.

If all pass: mark V36 RUNTIME-PROVEN and promote final current. If any fail: capture the Diagnostics status around the exact failure and patch only that concrete regression.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V36 icon-fixed stress candidate is CI-PROVEN / RUNTIME-PENDING. Current CI run 35160650206 SUCCESS, artifact 10472971410. Runtime MainForm icon is explicitly bound to the embedded premium EXE icon after user observed the generic WinForms taskbar icon. Gameplay fixes remain pre-launch/stage lifecycle, Selection teardown/rearm, Pause/Reveal recovery, Hero up to 500 targets, fullscreen-safe overlay Unit Changer. Next: confirm custom icon and run exact V36 stress gate; do not call FINAL before it passes.`
