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

`HeroEffectReplayV2/STATE.md`
- **RUNTIME-PROVEN**.
- Grayback `0xC0` and Issyl `0xA5` successfully replay through native target helper RVA `0x1F0C32` on the BRZE game/render thread.
- V2 is the locked rollback/reference implementation for one-shot hero effects.

`HeroEffectReplayV3/STATE.md`
- **RUNTIME-REJECTED — DO NOT REUSE REFRESH/STACK APPROACH.**
- Repeated native re-application stacked/compounded effect state and corrupted combat behavior.
- User-observed failure included effectively zero damage to units, approximately one-hit building damage, extreme movement speed, and slower-feeling attack cadence.
- Recover contaminated runtime with clean save/restart.

`HeroEffectDurationProbe/STATE.md`
- V1 read-only runtime probe completed.
- Direct Unit* and one-level pointer scan did NOT isolate a trustworthy duration field.
- High-scoring direct Unit fields were dominated by movement/transform noise.
- Pointer structures around Unit+0x1E4 / +0x1F4 / +0x20C / +0x21C remain interesting; V1 showed repeated node-like chains under Unit+0x1E4.

`HeroEffectDurationProbeV2/STATE.md`
- **BUILD/STATIC PROVEN — RUNTIME PENDING.**
- Strictly read-only recursive pointer graph observer, depth <= 3.
- Root Unit scan 0x500, child node scan 0x200, max 700 nodes, max 3000 candidates.
- Adds `MARK EFFECT EXPIRED` ranking to boost fields/objects that return to baseline, hit zero, or disappear exactly when the visible buff ends.
- Run `34790442232` SUCCESS; job `103813532429` SUCCESS.
- Artifact `10327766605`, digest `sha256:83bcfda67467a3d0b2c85f0e13b9644a02a399f7c5fd38cacea5dcd9617a0901`.
- Standalone SHA-256 `acb1b4f87266eab44d760ab0d6ac18257ff60ffd5696f3bdc9d08eebed1ea825`.
- Small SHA-256 `2ef44674881ebf6b0d2d5f0aa9c0b9250ab405f9c2d02cc6eecb4270c5633991`.

## Exact next action
Runtime-test Hero Effect Duration Probe V2 using the ORIGINAL Grayback hero skill, not V2 replay:
1. restart/reload BRZE clean if needed;
2. launch the V2 recursive read-only probe;
3. select exactly ONE clean normal target unit;
4. press `1 CAPTURE BASELINE`;
5. cast Grayback's original hero buff exactly once on that same target;
6. immediately press `2 CAPTURE EFFECT DIFF`;
7. press `3 START WATCH` and leave the unit/effect alone;
8. the instant the visible buff disappears naturally, press `4 MARK EFFECT EXPIRED`;
9. press `COPY REPORT` and return the report for analysis;
10. do not use HeroEffectReplayV3 and do not recast during the observation window.

Only after a timer/expiry candidate is correlated with natural visible expiry should a separate isolated timer-write experiment be built. Do not integrate duration into the main trainer before that proof.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5). V3 duration refresh is runtime-rejected because native re-apply stacks/corrupts combat. Duration Probe V1 did not isolate the timer; continue from HeroEffectDurationProbeV2 recursive read-only runtime test using original Grayback skill. Copy/Paste Hero is already runtime-proven.`
