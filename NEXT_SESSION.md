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
- Repeated native application stacks/compounds effect instances and corrupts combat behavior.

`HeroEffectDurationProbe/STATE.md`
- V1 read-only observer completed; no trustworthy duration field isolated.

`HeroEffectDurationProbeV2/STATE.md`
- **RUNTIME-PROVEN OBSERVER — TIMER NOT YET PROVEN.**
- Recursive graph observation identified likely effect/visual lifecycle objects under `Unit+0x1E4` and interesting structures beneath `Unit+0x094`.

`HeroEffectDurationProbeV3/STATE.md`
- **RUNTIME-PROVEN OBSERVER — `Unit+0x460` HIGH-CONFIDENCE CANDIDATE, MOVEMENT CONTROL REQUIRED.**
- Idle no-buff 15s control: `+0x460` stayed exactly `345400`, `CTRL chg 0`, `flip 0`.
- Grayback-active watch: `FX chg 164`, `flip 0`, first observed active value `354400`, natural-expiry value `413000`.
- PRE -> EXP increase = `+67600`, strictly one-directional.
- This is strong effect correlation, but target position fields also changed continuously during the effect and `+0x460` sits next to the noisy movement/transform region `0x45C..0x4A4`.
- Therefore `+0x460` is NOT yet approved for writing; it may be a movement/animation accumulator.
- `Unit+0x1E4/+0x1E8` and `+0x20C/+0x210` populated during the effect and returned to zero at visible expiry; `+0x214` returned to its pre-effect pointer. Treat these as lifecycle markers until deeper proof.

## Exact next action
Reuse the existing `HeroEffectDurationProbeV3` binary for a movement-only no-buff control:
1. restart/reload BRZE clean;
2. select exactly ONE normal clean unit with no hero buff active;
3. press `1 START 15s CONTROL`;
4. during those 15 seconds deliberately keep the selected unit walking/moving; do NOT cast Grayback/Issyl or any other buff;
5. when the control auto-stops, press `COPY REPORT` immediately and return it;
6. the only decisive line needed first is pinned `Unit+0x460`.

Decision:
- movement-only `CTRL chg > 0` at `Unit+0x460` => reject it as Grayback-duration-specific and move to effect-instance/container interception;
- movement-only `CTRL chg 0` => promote `+0x460` to very strong Grayback-specific elapsed/duration candidate, then compare clean Grayback vs Issyl timing/direction before any isolated write.

Do not integrate duration into the main trainer yet. Never reintroduce repeated native re-application.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5); repeated refresh V3 is permanently rejected. Duration Probe V3 runtime result made Unit+0x460 a high-confidence candidate: idle control chg0, Grayback FX chg164 flip0, PRE345400 -> EXP413000. But movement fields also changed, so the exact next test is movement-only no-buff control using the same V3 binary before any write.`
