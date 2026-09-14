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
- **RUNTIME-PROVEN OBSERVER — `Unit+0x460` REJECTED AS DURATION-SPECIFIC.**
- Earlier idle control + Grayback pass looked promising: idle `CTRL chg 0`, Grayback `FX chg 164`, `flip 0`, PRE `345400` -> EXP `413000`.
- Decisive movement-only no-buff control: `Unit+0x460 CTRL chg 55`, `flip 0`, `413000 -> 432800`.
- Therefore `+0x460` is ordinary movement/activity-correlated state, not a Grayback-specific duration timer. DO NOT PATCH/FREEZE/WRITE IT.
- Direct movement/transform cluster is no longer the duration target.

`HeroEffectContainerProbeV4/STATE.md`
- **BUILD/STATIC PROVEN — RUNTIME PENDING.**
- Strict read-only movement-subtracted recursive container observer.
- Target roots: `Unit+0x094/+0x098/+0x1E4/+0x1E8/+0x1F4/+0x20C/+0x210/+0x214/+0x21C`.
- Recursive depth <= 4; node scan 0x180; max 260 nodes; 250 ms sample interval.
- Movement-only changes are penalized; effect-only fields/objects that return to pre-effect state, hit zero, or disappear at natural expiry are boosted.
- Static fields inside NEW effect objects are retained because duration may be an expiry timestamp/constant rather than a countdown.
- Run `34791583601` SUCCESS; job `103816698699` SUCCESS.
- Artifact `10328387975`, digest `sha256:9170fbf96b3b922ab411932dc9d2f6d0f6dafef7bed2e166e9ab13a00af4133a`.
- Standalone SHA-256 `2294592d2844c05a00a72dd06f306e22b0308d99cc7cfeb9a06b66368925a422`.
- Small SHA-256 `8b8b98155acce0e8690a9c09c5e56a84bae58d30e2669cb1c3ff0cd0542ea7f4`.

## Exact next action
Runtime-test `HeroEffectContainerProbeV4`:
1. fresh/reloaded BRZE;
2. select exactly ONE clean normal unit;
3. press `1 START 15s MOVE CONTROL` and keep it walking continuously with NO buff;
4. after auto-stop, stop the unit;
5. press `2 CAPTURE PRE-EFFECT`;
6. cast ORIGINAL Grayback exactly once on that unit;
7. immediately press `3 START EFFECT WATCH`;
8. do not recast;
9. the instant the visible Grayback effect ends naturally, press `4 MARK EXPIRED`;
10. press `COPY REPORT` and return the full report.

Analysis priority for returned V4 report:
- top-ranked `NEW` fields with `CTRL unseen` or `CTRL chg 0`;
- low/zero FX flips;
- fields that become `EXP UNREADABLE`, zero, or return exactly to PRE at natural expiry;
- inspect root lifecycle block first to see which Unit root owns the effect-created object.

If V4 still fails to isolate a coherent timer, stop recursive guessing and build an effect-creation pointer interceptor from runtime-proven magic-create RVA `0x13FFD1`, capturing the created effect/object pointer or downstream insertion before any write.

Do not integrate duration into the main trainer yet. Never reintroduce repeated native re-application.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5); repeated refresh is permanently rejected. Duration Probe V3 decisively rejected Unit+0x460 because movement-only no-buff control changed it 55 times (413000->432800). Continue from HeroEffectContainerProbeV4 movement-subtracted read-only runtime test; if it cannot isolate timer, intercept the proven magic-create path RVA 0x13FFD1 for actual effect-object pointer telemetry.`
