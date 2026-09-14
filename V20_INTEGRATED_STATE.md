# BRZE Trainer V20 — INTEGRATED STATE

Status: **BUILT / CI-PROVEN — RUNTIME SMOKE TEST NEXT**
Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Purpose
Integrate the locked V19 trainer UI/mechanics with two already runtime-proven side features:
- V11 configurable Issyl duration/replay;
- Unit Clone Lab native Copy Unit / Paste Beside.

V18.3/V19 remain recoverable fallbacks.

## Final layout
- top-left: existing game cheat toggles;
- top-right: existing `UNIT CHANGER // BUILDING`;
- middle-left: `HERO EFFECT // ISSYL DURATION`;
- middle-right: `COPY UNIT // UNIT CLONE LAB`;
- bottom content row: `SYSTEM STATUS` full-width, aligned to the same left/right outer edges as the top layout, with centered heading;
- bottom action bar unchanged;
- client size raised to `1640x900` so labels/buttons are not intentionally compressed or clipped.

The feature is named **COPY UNIT**, never Copy Hero.

## Shared hook architecture
V20 has exactly ONE compiled owner for render-frame hook RVA `0x135C43`: `IntegratedFrameDispatcherCore.cs`.

It serializes the three consumers that previously competed for the same site:
1. V19 Instant Death cursor action;
2. Replay V2 target-ability calls;
3. Unit Clone Lab native position-spawn calls.

Locked native RVAs:
- frame hook `0x135C43`;
- replay target helper `0x1F0C32`;
- clone native position-spawn wrapper `0x0C4A1C`;
- cursor unit query `0x135EFB`.

Other invariants:
- FXSAVE/FXRSTOR + pushfd/pushad around shared native dispatcher;
- max 120 replay selected targets;
- max 120 copied units;
- clone/replay shared queue prevents simultaneous native queue ownership;
- no `CreateRemoteThread` path;
- original InstantDeathCore becomes a thin facade delegating to the shared dispatcher.

## V11 semantics preserved
`HeroEffectReplayDurationV11/ConfigurableDurationCore.cs` is linked into V20 unchanged.
- original nominal Issyl duration `15000`;
- preset 2X `30000`;
- preset 3X `45000`;
- custom `15000 × multiplier`, UI range `0.25x..20x`;
- modified duration remains resident for the entire replay lifetime;
- restore to `15000` only after natural expiry;
- baseline capture once per fresh session.

## Copy Unit semantics preserved
- read selected unit snapshot from selection list RVA `0x441708`;
- hero / unique / story types are allowed;
- native paste uses position-spawn wrapper `0x0C4A1C`;
- formation shifts on +X by configurable side offset;
- max four native spawns per render frame;
- no raw Unit struct clone.

## CI proof
Workflow: `Final V20 Integrated Copy Unit Hero Effect`
Run: `34819704044` — SUCCESS
Job: `103898203112` — SUCCESS
Head: `51d18424c8bbb02ac962dce2a33a0a77da374627`
Artifact: `10338255531`
Artifact digest: `sha256:7088f5ef16d945e4cb4705dfaa6ac4ba09e2a73262a531965887aa463c896e6f`

CI passed:
- V18.3/V19 generator chain;
- V20 layout generation;
- locked non-target core hash checks;
- full-width/no-clipping layout invariants;
- Copy Unit naming invariants;
- exactly one compiled frame-hook owner;
- V11 duration invariants;
- CLEAN compile smoke;
- DIAGNOSTICS compile smoke;
- CLEAN/DIAGNOSTICS standalone publish;
- CLEAN/DIAGNOSTICS small publish;
- hashes;
- artifact upload.

## Build hashes
- CLEAN standalone: `7ac953345ccd2a6774da798b17f484abead475e77b9d291cdfd77c405a93b686`
- DIAGNOSTICS standalone: `50327732e173d5f5adcfeaa8b03e4ab7f71a0df439caf36c14de521213c974ec`
- CLEAN small: `5f6a36ee5734da3903ed5f127af2d832d592af979f5606d5ac13ef34039b825a`
- DIAGNOSTICS small: `f70b8939f29e7e814e5ed4cec247b3f44ebafe1196b97d119c085b2b7af0963d`

## Exact next action
One integrated runtime smoke test only; do not reopen duration research:
1. launch V20 Diagnostics first;
2. verify existing V19 trainer attaches and one ordinary old feature still works;
3. Copy Unit: select unit(s) -> `COPY SELECTED UNITS` -> `PASTE BESIDE` once;
4. Hero Effect: capture one Issyl baseline -> run one preset/custom replay -> wait for restore to `15000`;
5. press Instant Death once after the above to prove the shared dispatcher still serves the V19 path;
6. inspect layout for any actual clipping at the user's Windows scaling.

If this smoke passes, promote V20 to the main trainer candidate. Do not repeat multiplier research.
