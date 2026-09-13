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
- V2 is the locked rollback/reference implementation for hero effects.
- One native application is proven-good; do not mutate this baseline.

`HeroEffectReplayV3/STATE.md`
- **RUNTIME-REJECTED — DO NOT REUSE REFRESH/STACK APPROACH.**
- Repeated native re-application stacked/compounded effect state and corrupted combat behavior.
- User-observed failure included effectively zero damage to units, approximately one-hit building damage, extreme movement speed, and slower-feeling attack cadence.
- STOP HOLD cannot reliably undo already-created stacks; recover with a clean save/restart.

`HeroEffectDurationProbe/STATE.md`
- **BUILD/STATIC PROVEN — READ-ONLY RUNTIME PROBE PENDING.**
- Observation-only probe: `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION` + `ReadProcessMemory` only.
- No `WriteProcessMemory`, no allocation/protection patch APIs, no hooks, no native ability calls.
- Captures direct Unit memory and readable first-level pointer regions before/after ONE V2 effect, then ranks smoothly changing dwords as timer candidates.
- Run `34789742039` SUCCESS; job `103811637139` SUCCESS.
- Artifact `10328450262`, digest `sha256:17ed088b97c58bdd74d881cab07da420cef718662d77b3852b88b67c294417e7`.
- Standalone SHA-256 `28a83ed1fa601e9c1406b207bbdaceec80620e33a5a7078fb48c5fc293686b1f`.
- Small SHA-256 `1a17d8ed2c6bb018f2f02cd99675019cebbe09c231684a9bb64a3fbfa40a2122`.

## Exact next action
Runtime-test the read-only duration probe:
1. fully close V3 and restart/reload BRZE to clear stacked contamination;
2. start the read-only Duration Probe;
3. select exactly ONE clean normal unit;
4. press `1 CAPTURE BASELINE`;
5. use proven V2 to apply exactly ONE effect to that same unit: Grayback `0xC0` OR Issyl `0xA5`;
6. immediately return to the probe and press `2 CAPTURE EFFECT DIFF`;
7. press `3 START WATCH` and leave the effect/unit alone until the visible buff expires naturally;
8. press `COPY REPORT` near/just after expiry and return the report for candidate analysis;
9. do not use V3 and do not re-apply the ability during this test.

Only after a timer/expiry candidate is correlated with natural visible expiry should a separate isolated timer-write experiment be built. Do not integrate duration into the main trainer before that proof.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5). V3 duration refresh is runtime-rejected because native re-apply stacks/corrupts combat. Read-only HeroEffectDurationProbe build is ready; continue from its runtime timer-candidate test. Copy/Paste Hero is already runtime-proven.`
