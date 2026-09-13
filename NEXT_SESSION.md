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
- V1 static BattleGear-derived replay is RUNTIME-REJECTED: calls executed but no visible Grayback/Issyl effect.
- Do not reuse V1-derived target IDs.

`HeroEffectSniffer/STATE.md`
- Runtime sniffer is RUNTIME-PROVEN.
- Captured Grayback runtime ability `0xC0` and Issyl runtime ability `0xA5` from original skills.
- Locked mapping for this executable:
  - Grayback buff = `0xC0`
  - Issyl Haste = `0xA5`

`HeroEffectReplayV2/STATE.md`
- **RUNTIME-PROVEN** from user report on 2026-09-14 JST.
- Runtime-sniffed IDs successfully replay through native target helper RVA `0x1F0C32` on BRZE game/render thread.
- V2 is the locked rollback/reference implementation for hero effects.
- One native application is proven-good; do not mutate this baseline.

`HeroEffectReplayV3/STATE.md`
- **RUNTIME-REJECTED — DO NOT REUSE REFRESH/STACK APPROACH.**
- Scheduled native re-application did not safely refresh an existing timer; runtime behavior became strongly consistent with stacked/compounded effect state.
- User-observed failure:
  - damage against units became effectively zero / units stopped losing HP;
  - buildings became approximately one-hit;
  - movement speed became extremely fast;
  - attack cadence felt slower than V2.
- Do not tune the refresh interval or retry INFINITE via periodic re-application.
- STOP HOLD cannot reliably undo already-created stacks; safest recovery is reload a clean save / restart BRZE.
- Rejected build pin only: run `34788874055`, artifact `10327388613`.

## Exact next action
Build a dedicated **effect-instance duration/timer probe**, observation-first.

Requirements:
1. start from a clean BRZE runtime;
2. apply Grayback `0xC0` or Issyl `0xA5` only ONCE using the V2-proven path;
3. identify the effect instance/container created for the target Unit*;
4. observe candidate timer/expiry values over time without writing them;
5. prove a candidate by showing it changes monotonically / expires with the visible buff;
6. only then build a separate isolated timer-write experiment;
7. never extend duration by repeated ability application and never substitute raw movement/attack/damage stat writes.

Do not integrate duration into the main trainer until the timer path is runtime-proven.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5). V3 duration refresh is runtime-rejected because native re-apply stacks/corrupts combat. Continue with an observation-only effect-instance duration/timer probe. Copy/Paste Hero is already runtime-proven.`
