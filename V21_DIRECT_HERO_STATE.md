# BRZE Trainer V21 — DIRECT HERO EFFECT STATE

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
Status: **BUILT / CI-PROVEN — RUNTIME SMOKE PENDING**

## Why V21 exists
V20 integrated successfully but user runtime visual inspection found the Hero Effect row clipped (`ISSYL CUSTOM`) and, more importantly, the Hero Effect panel still exposed research workflow (`CAPTURE ISSYL BASELINE`, baseline/capture/manual restore concepts).

V21 converts Hero Effect into final trainer UX:
- select one or more clean units;
- enter desired duration in seconds (`1–420`);
- click `APPLY ISSYL`, `APPLY GRAYBACK`, or `APPLY BOTH`;
- no original cast, no baseline capture, no research UI, no manual restore step.

## Locked duration behavior
Do NOT regress to native-helper-return restore.
Authoritative V11 proof remains:
- Issyl A5 base config duration `15000`;
- Grayback C0 base config duration `60000`;
- duration config must remain resident through the active effect lifetime;
- restore after natural expiry, not immediately after the native helper returns;
- repeated native reapply/refresh remains rejected.

V21 therefore:
1. resolves the ability config with strict ID/base-duration guards;
2. patches requested duration before the one-shot native replay;
3. captures selected clean-unit lifecycle roots;
4. tracks each selected target automatically;
5. keeps config held while tracked effects are active;
6. restores original config only after natural lifecycle expiry;
7. restores defensively on shutdown/abort.

## Direct config resolver
Known runtime-proven addresses are used only after strict validation:
- Issyl A5 config candidate `0x227F4664`, must satisfy `+0x000 == 0xA5`, `+0x0E0 == 15000`;
- Grayback C0 config candidate `0x227F9470`, must satisfy `+0x000 == 0xC0`, `+0x0E0 == 60000`.

If a known address does not validate, V21 performs guarded readable-memory scanning for the exact ability ID + baseline-duration signature and selects the candidate nearest the known runtime-proven record. No write occurs without identity/value guards.

## Duration input
UI uses seconds, calibrated from runtime-proven natural baselines:
- Issyl: `15000` nominal ~= `10.7271 s` observed wall time;
- Grayback: `60000` nominal ~= `42.2221 s` observed wall time.

Requested seconds are converted to nominal config duration independently for each ability. `APPLY BOTH` therefore targets approximately the same wall duration for A5 and C0 even though their native base durations differ.

## Layout
V20 geometry is preserved:
- `1640x900` client area;
- full-width centered `SYSTEM STATUS` section;
- top cheats + Unit Changer unchanged;
- middle-right Copy Unit unchanged.

Hero Effect panel now uses a bounded 6-column TableLayout rather than the old one-line FlowLayout, specifically to prevent the clipping seen in V20.

## Shared hook architecture
Unchanged from V20:
- exactly one owner of render-frame hook RVA `0x135C43`: `IntegratedFrameDispatcherCore.cs`;
- shared by Instant Death, one-shot Hero Effect native replay, and Copy Unit native spawn;
- no CreateRemoteThread;
- no raw Unit struct clone;
- native replay/copy queue remains serialized;
- max 120 selected/copied units.

## V21 CI pin
Workflow: `Final V21 Direct Hero Effect`
Run: `34821655613` — SUCCESS
Job: `103904381017` — SUCCESS
Head: `15c8213ba19d3af567584139e99545cdc89f1019`
Artifact: `10338672153`
Artifact digest: `sha256:8490e294fcf3ac9333f11c77518b36de0f99c6f7ffb2fe98cf68de9cce877d42`

Build hashes:
- CLEAN standalone: `a933409004138e606358b27ccc697ee702447f9faca64f91d5c7cdef6d56b0b9`
- DIAGNOSTICS standalone: `f074cec5c0aa75efb56d9df4f2644dc7dbde17fe5cc3994bfc8cbe622a52198a`
- CLEAN small: `21893d232bc70ef1d031cc1fa42ed3ce3a092291f82bc6a36eb4785f24669604`
- DIAGNOSTICS small: `d97468c525a6864895636f35619981483aefdce378c16f0b868b0df796a11ffc`

CI passed:
- V20 base generation;
- V21 direct Hero Effect finalizer;
- V20 shared dispatcher unchanged;
- UnitChanger/HookCore unchanged;
- no baseline/research UI in compiled V21 panel;
- natural-expiry hold invariants;
- CLEAN compile;
- DIAGNOSTICS compile;
- four publish outputs;
- hash + artifact upload.

## Exact runtime smoke next
Use V21 Diagnostics.
1. Start BRZE and V21 Diagnostics.
2. Select one clean unit.
3. Set a short obvious duration such as `30.0` seconds.
4. Click `APPLY ISSYL`; verify effect appears immediately without any original Issyl cast/capture step and stays roughly requested duration.
5. Repeat on a clean unit with `APPLY GRAYBACK`.
6. Optionally test `APPLY BOTH` once.
7. Confirm status ends with config restored after natural expiry.
8. Confirm no Hero Effect text/control clipping.

If runtime passes, V21 becomes the integrated main-trainer candidate.
