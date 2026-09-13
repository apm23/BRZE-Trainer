# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Authoritative stable base
- Main trainer rollback/final stable base remains V18.3 as documented in `FINAL_CURRENT.md`.
- Do not mutate the locked V18.3 baseline directly; layer new work above it.

## Proven side feature
`UnitCloneLab/STATE.md`
- Native Copy/Paste of currently selected units is RUNTIME-PROVEN.
- Hero / unique / story units are allowed and successfully duplicated.
- Uses native coordinate spawn path; never raw-clones Unit structs.

## Hero Effect research status
`HeroEffectLab/STATE.md`
- V1 static BattleGear-derived replay is RUNTIME-REJECTED. Do not reuse V1-derived IDs.

`HeroEffectSniffer/STATE.md`
- RUNTIME-PROVEN.
- Grayback runtime ability `0xC0`; Issyl runtime ability `0xA5`.
- Target helper RVA `0x1F0C32`; magic-create helper RVA `0x13FFD1`.

`HeroEffectReplayV2/STATE.md`
- **RUNTIME-PROVEN** one-shot effect replay.
- Keep as locked reference/fallback.

`HeroEffectReplayV3/STATE.md`
- **RUNTIME-REJECTED — NEVER REUSE REFRESH/STACK APPROACH.**
- Repeated native application stacks/compounds effect instances and corrupts combat behavior (extreme movement speed, building one-hit behavior, unit-damage/invulnerability anomalies, slower-feeling attack cadence).

`HeroEffectDurationProbe/STATE.md`
- V1 read-only observer completed; no trustworthy duration field isolated.

`HeroEffectDurationProbeV2/STATE.md`
- **RUNTIME-PROVEN OBSERVER — TIMER NOT YET PROVEN.**
- Recursive depth-3 read-only graph pass completed using ORIGINAL Grayback and natural expiry.
- `Unit+0x1E4 -> +0x008` produced XYZ-like values at `+0x028/+0x02C/+0x030` and clears at expiry: likely visual/particle/transform object, not a proven gameplay timer.
- structures beneath `Unit+0x094` remain interesting effect/container candidates but are not safe to patch.
- direct `Unit+0x460` is the strongest direct timer/accumulator candidate: starts from zero, changes frequently, and showed zero direction flips in repeated runs; latest run ended at integer 178500 with 89 changes / 0 flips.
- because earlier run ended at a different total (102000), `Unit+0x460` may be a generic unit clock. NO WRITE is allowed yet.

`HeroEffectDurationProbeV3/STATE.md`
- **BUILD/STATIC PROVEN — RUNTIME PENDING.**
- Strictly read-only same-unit CONTROL-vs-EFFECT differential.
- 15-second no-buff idle control sampled every 250 ms, then pre-effect capture, ORIGINAL Grayback once, effect watch, natural-expiry mark.
- Direct Unit scan only (`0x500`) with UnitDef/owner identity validation.
- `Unit+0x460` is pinned at top of report.
- Run `34790956827` SUCCESS; job `103814953856` SUCCESS.
- Artifact `10327249644`, digest `sha256:a7eeb16a1bccd51bd7f449f9ac55913989e42c46f8ef4e7536451ae650d08ac9`.
- Standalone SHA-256 `d3902d905d7787dd85f0b11b54befb62f7081b5ee8c9fa465812ad0058bdee9f`.
- Small SHA-256 `a4006557ae4a98c1a0fdb7502536ea48d773535c39b6ff63efaea7b91e1b3235`.

## Exact next action
Runtime-test `HeroEffectDurationProbeV3`:
1. restart/reload BRZE clean if needed;
2. select exactly ONE normal clean target unit and keep it completely idle;
3. press `1 START 15s CONTROL`; do not move, attack, or cast anything until it auto-stops;
4. press `2 CAPTURE PRE-EFFECT`;
5. cast ORIGINAL Grayback exactly once on the same target;
6. immediately press `3 START EFFECT WATCH`;
7. leave the target completely idle; do not move, attack, or recast;
8. the instant the visible Grayback effect disappears naturally, press `4 MARK EXPIRED`;
9. press `COPY REPORT` and return it.

Interpretation:
- if pinned `Unit+0x460` shows `CTRL chg > 0`, reject it as Grayback-specific duration and continue toward the effect-instance/container path;
- if `CTRL chg = 0` while FX shows many smooth changes with low/zero flips, promote `+0x460` to a high-confidence candidate, but first determine direction/scale before any isolated write test.

Do not integrate duration into the main trainer until runtime proof. Never reintroduce repeated native re-application.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5); repeated refresh V3 is permanently rejected because effects stack/corrupt combat. Recursive Duration Probe V2 found likely visual object under Unit+0x1E4 and a still-unproven direct candidate Unit+0x460. Continue from HeroEffectDurationProbeV3 CONTROL-vs-EFFECT runtime test. Copy/Paste Hero is already runtime-proven.`
