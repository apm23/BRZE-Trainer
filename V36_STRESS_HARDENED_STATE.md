# V36 STRESS HARDENED — STATE

Status: **CI-PROVEN / RUNTIME-PENDING**
Date: 2026-09-17 JST
Branch: `instant-death-v4-hover-telemetry`

## Scope
V36 is a targeted hardening layer above the runtime-proven V30/V34 gameplay base. It addresses user-reported V35 stress regressions without reopening proven Hero reset mechanics or changing locked unrelated gameplay cores.

## User-reported regressions targeted
1. Cheats armed before BRZE launch could degrade across Journey stages: Peasant 3s, Pause Peasant, Reveal Map, and HOME recovery.
2. Hero Effect became unreliable for very large selections / long durations.
3. Overlay Unit Changer dropdown interaction was difficult and could minimize fullscreen BRZE.
4. Trainer needed a distinct premium icon.

## Implemented invariants
- 1500 ms continuously-ready battle settle gate before first runtime activation.
- SelectionCore dynamically rebinds across stage/root/local-player changes.
- SelectionCore teardown restores only owned patches and never frees a code cave while live BRZE instructions still reference it.
- Pause Peasant does not restore a transient stage-loading zero as the normal creation state.
- Reveal transition guard can adopt a previously resident trainer-owned guard and retries transient native reveal failures.
- Hero Effect selection/replay cap = 500; Copy Unit cap remains 120.
- Hero maintenance interval = 200 ms.
- V30 active-same-buff timestamp reset / missing-only one-shot native replay behavior preserved.
- Overlay Unit Changer exposes no popup ComboBox controls; visible selection uses filter + previous/next buttons.
- distinct V36 application icon embedded.

## CI / audit proof
Workflow: `Final V36 Stress Hardened Four-Pack`
- run: `35126740562` SUCCESS
- job: `104897529983` SUCCESS
- HEAD: `19314539f4bb7e7d35806c199d3a951e76621d6b`
- artifact: `10460000116`
- artifact digest: `sha256:8ae2358c39bb55aae668a194efced783d4bf9bbd96aedc48b2092dcf921a14d8`

Audit PASS:
- no fixed-address mutation collisions;
- render hook RVA `0x135C43` single owner = `IntegratedFrameDispatcherCore`;
- pre-launch stable-ready gate present;
- SelectionCore restore/free/rebind invariant present;
- Selection + Pause Peasant Journey-stage rearm markers present;
- Reveal guard adoption/retry markers present;
- Hero target capacity 500; Copy 120;
- fullscreen-safe overlay Unit Changer controls present;
- V36 premium icon present;
- locked UnitChangerCore, HookCore, InstantDeath wrapper, StaminaCore, HorseCore, WolfCore and original V30 feature-panel files remained byte-identical through the V36 layer.

Build/publish PASS:
- CLEAN smoke: success, 0 errors.
- DIAGNOSTICS smoke: success, 0 errors.
- CLEAN standalone publish: success.
- DIAGNOSTICS standalone publish: success.
- CLEAN small publish: success.
- DIAGNOSTICS small publish: success.

Output SHA256:
- CLEAN standalone: `ec32fd352e836209ec2b8db839d5b77c6569319f260452a16a773d03458b487a`
- DIAGNOSTICS standalone: `cca64f9c2469e79c75c6b74a67026efdc3d8e87e8ac5d9b633c48456c6655645`
- CLEAN small: `1a95c4bde0a9e88c35fb8f5083b34a91b92355fab0b40d2359a8399773def5a7`
- DIAGNOSTICS small: `187797d38b78a233d6f03b4e020b9176e2a38b72c966e0cf3e92f165962bf1a7`

## Runtime gate — mandatory before FINAL
Test the exact failure pattern:
1. launch trainer first, arm cheats, then launch BRZE and traverse Journey stages 1 -> 2 -> 3;
2. cross a stage boundary with Pause Peasant enabled, disable it next stage, verify normal peasants resume and Peasant 3s still works;
3. press HOME during/after a transition and verify actual runtime recovery;
4. apply Issyl/Grayback/Both to ~150–300 selected units at 120 s / 200 s (420 s optional), verify deterministic no-stack behavior;
5. configure Unit Changer through Alt+W overlay in fullscreen using filter + previous/next controls, verify BRZE never minimizes;
6. verify distinct trainer icon.

Only after this user runtime gate passes may V36 be marked RUNTIME-PROVEN / FINAL candidate.
