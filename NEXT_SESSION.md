# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Authoritative stable base
- Main trainer rollback/final stable base remains V18.3 as documented in `FINAL_CURRENT.md`.
- Do not mutate the locked V18.3 baseline directly; layer new work above it.

## Proven side feature
`UnitCloneLab/STATE.md`
- Native Copy/Paste selected units is RUNTIME-PROVEN.
- Hero / unique / story units work.
- Never raw-clone Unit structs.

## Hero Effect locked facts
`HeroEffectSniffer/STATE.md`
- Grayback runtime ability `0xC0`; Issyl runtime ability `0xA5`.
- Target helper RVA `0x1F0C32`; magic-create helper RVA `0x13FFD1`.

`HeroEffectReplayV2/STATE.md`
- RUNTIME-PROVEN one-shot replay. Keep untouched as known-good fallback.

`HeroEffectReplayV3/STATE.md`
- RUNTIME-REJECTED. Repeated native replay stacks/compounds effects. NEVER REUSE.

`HeroEffectDurationProbeV3/STATE.md`
- `Unit+0x460` hard-rejected as duration-specific.

`HeroEffectContainerProbeV4/STATE.md`
- Issyl proved transient lifecycle roots `+0x1E4/+0x1E8/+0x20C/+0x210`; `+0x214` returns to baseline at expiry.

`HeroEffectTransientProbeV5/STATE.md`
- Two automatic Issyl passes proved lifecycle timing and showed transient paths swap between casts.

`HeroEffectSignatureProbeV6/STATE.md`
- Three ORIGINAL ISSYL passes with deliberately different human timing measured `10725.9 / 10713.8 / 10733.2 ms`; total spread only `19.4 ms`.
- Human click speed is irrelevant to measured lifecycle.
- Every pass found actual effect parent by content signature:
  - `parent+0x058 == 0xA5`
  - `parent+0x17C == pinned Unit*`
- Effect parent paths/addresses move between casts; NEVER hard-code path.
- Parent `+0x194` rejected as duration-specific.

`HeroEffectChildProbeV7/STATE.md`
- **RUNTIME-PROVEN OBSERVER — CHILD CONFIG CANDIDATE FOUND.**
- One clean ORIGINAL ISSYL pass:
  - natural lifetime `10749.4 ms`
  - parent discovery `32.6 ms`
  - 11 readable/deduplicated child objects
  - no clean continuously-changing child timer
- Strong structured child/config lead:
  - `parent+0x1F4 -> config object`
  - `config+0x000 = 0xA5` exact Issyl ID
  - `config+0x0A0 = 100`
  - `config+0x0E0 = 15000`
  - `config+0x0E8 = 482`
  - config stayed readable/static for full lifecycle
- `config+0x0E0 = 15000` is the strongest duration-parameter candidate but NOT proven yet.
- Low-address `PARENT[+0x000,+0x1FC]` static `10272` is rejected as a likely vtable/type/static coincidence, not promoted by numeric proximity alone.

`HeroEffectAbilityConfigProbeV8/STATE.md`
- **BUILT / CI-PROVEN READ-ONLY — RUNTIME COMPARISON NEXT.**
- Auto-detects either Issyl A5 or Grayback C0 using ability ID + pinned target signature.
- Reads `parent+0x1F4` as the ability/config pointer.
- Samples config `0x200` bytes every 20 ms through natural expiry.
- Main comparison target: `config+0x0E0`.
- Workflow `Hero Effect Ability Config Probe V8 Read Only`.
- Run `34794119559` SUCCESS; job `103823808659` SUCCESS.
- Head `0e319752b3533d933d15ffba332f0859356be4f2`.
- Artifact `10329446128`, digest `sha256:ce98a0e10b9b975d57b039e31dbf97ec18a0fe849f51636a891057dc1489a3fb`.
- Standalone SHA-256 `0628d47fd75ab9403c8366822a487ba3210b5fbe5c1121f0115b3acba123aa4b`.
- Small SHA-256 `44d17a8bf358e3b1823b6dac1e6c57489d8fb9fd64a2c0c3ede560f61c5d3d75`.

## Exact next action
Runtime-test V8 twice and return both reports.

### Test A — ORIGINAL ISSYL
1. fresh/reload BRZE;
2. select exactly ONE clean normal target;
3. click `ARM TARGET + AUTO DETECT`;
4. select Issyl;
5. cast ORIGINAL Haste once on the armed target;
6. do nothing until `COMPLETE`;
7. `COPY REPORT`.

### Test B — ORIGINAL GRAYBACK
1. RESET or fresh/reload BRZE;
2. select exactly ONE clean normal target;
3. click `ARM TARGET + AUTO DETECT`;
4. select Grayback;
5. cast ORIGINAL Grayback effect once on the armed target;
6. wait for natural expiry; no manual timing needed;
7. `COPY REPORT`.

## Analysis priority for V8
- confirm detected ability is A5 for Issyl and C0 for Grayback;
- confirm `config+0x000` follows ability ID;
- compare `config+0x0E0` across A5 vs C0;
- compare wall-time/config ratio;
- if +0x0E0 changes in a duration-plausible way between the abilities, repeat/confirm before an isolated write experiment;
- no duration write from V8 alone.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient path;
- no V7 cleanup-only child field writes;
- no low-address/vtable `10272` duration assumption;
- no integration into main trainer until a real duration parameter is proven.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot replay is proven; repeated refresh, Unit+0x460 and parent+0x194 are rejected. V7 runtime found no dynamic child timer but found parent+0x1F4 ability/config object: Issyl config+0=A5 and config+0x0E0=15000. V8 read-only A5-vs-C0 config comparator is built SUCCESS (run 34794119559, artifact 10329446128). Runtime-test V8 once with ORIGINAL Issyl and once with ORIGINAL Grayback next.`
