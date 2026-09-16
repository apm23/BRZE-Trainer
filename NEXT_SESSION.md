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

### Premium icon
- V36 project embeds `BRZE-Trainer-V36.ico` generated from the checked-in validated 64×64 trainer icon payload.

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
- distinct V36 application icon present.
- locked UnitChangerCore / HookCore / InstantDeath wrapper / StaminaCore / HorseCore / WolfCore / original V30 feature panel hashes remained unchanged through V36 layer.

## V36 CI proof
Workflow `Final V36 Stress Hardened Four-Pack`
- run `35126740562` — **SUCCESS**
- job `104897529983` — **SUCCESS**
- head `19314539f4bb7e7d35806c199d3a951e76621d6b`
- artifact `10460000116`
- artifact digest `sha256:8ae2358c39bb55aae668a194efced783d4bf9bbd96aedc48b2092dcf921a14d8`

Hashes:
- CLEAN standalone `ec32fd352e836209ec2b8db839d5b77c6569319f260452a16a773d03458b487a`
- DIAGNOSTICS standalone `cca64f9c2469e79c75c6b74a67026efdc3d8e87e8ac5d9b633c48456c6655645`
- CLEAN small `1a95c4bde0a9e88c35fb8f5083b34a91b92355fab0b40d2359a8399773def5a7`
- DIAGNOSTICS small `187797d38b78a233d6f03b4e020b9176e2a38b72c966e0cf3e92f165962bf1a7`

Both CLEAN and DIAGNOSTICS smoke builds passed with 0 compile errors; all four publishes and artifact upload passed. Ignore unrelated legacy `fix-large-selection-freeze.yml` failures.

## EXACT NEXT ACTION — V36 USER RUNTIME STRESS GATE
Prefer `BRZE-Trainer-FINAL-V36-Diagnostics.exe` for the first pass.

1. **Pre-launch regression:** open trainer first, arm the same cheats from the V35 failure scenario, then launch BRZE and play Journey stage 1 -> 2 -> 3. Peasant 3s must stay functional and Reveal must recover after transitions.
2. **Pause Peasant stage crossing:** turn Pause Peasant ON in one stage, enter the next stage, turn it OFF. Peasants must resume normally; then Peasant 3s must still produce the fixed timing.
3. **HOME recovery:** during/after a stage transition press HOME / REFRESH TRAINER. Enabled features must rebuild and remain functional; this must repair runtime bindings rather than only blink resource values.
4. **Hero stress:** select roughly 150–300 units and test Issyl / Grayback / Both at 120 s and 200 s (420 s optional). It must apply deterministically, without stacking/freeze/crash.
5. **Fullscreen overlay Unit Changer:** Alt+W -> Unit Changer -> use filters + `‹ / ›` to change building/output heroes. BRZE must not minimize and control must stay responsive.
6. Confirm the V36 trainer icon is visibly different from the original BRZE icon.

If all pass: mark `V36_STRESS_HARDENED_STATE.md` RUNTIME-PROVEN and then promote final current. If any fail: capture the Diagnostics status around the exact failure and patch only that concrete regression.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V36 is CI-PROVEN / RUNTIME-PENDING stress-hardened candidate. CI run 35126740562 SUCCESS, artifact 10460000116. It fixes pre-launch/stage lifecycle, true SelectionCore teardown/rearm, Pause/Reveal transition recovery, Hero up to 500 targets, fullscreen-safe overlay Unit Changer, and premium icon. Next action is the exact V36 runtime stress gate; do not call FINAL before it passes.`
