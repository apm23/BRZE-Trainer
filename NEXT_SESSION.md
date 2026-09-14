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

`HeroEffectDurationProbeV3/STATE.md`
- **RUNTIME-PROVEN OBSERVER — `Unit+0x460` REJECTED AS DURATION-SPECIFIC.**
- Movement-only no-buff control changed `+0x460` 55 times with 0 flips (`413000 -> 432800`).
- Do not patch/freeze/write it.

`HeroEffectContainerProbeV4/STATE.md`
- **RUNTIME-PROVEN LIFECYCLE OBSERVER — TIMER NOT YET ISOLATED.**
- User used ORIGINAL ISSYL instead of Grayback because Grayback lasts about one minute; this is valid because the probe is ability-agnostic.
- Clean root lifecycle:
  - `+0x1E4`: 0 -> `0x1E01B958` -> 0
  - `+0x1E8`: 0 -> `0x1E01B964` -> 0
  - `+0x20C`: 0 -> `0x1DF8EA80` -> 0
  - `+0x210`: 0 -> `0x1DF8EA80` -> 0
  - `+0x214`: `0x1DF8EA80` -> 0 -> exact baseline `0x1DF8EA80`
- `+0x1E4/+0x1E8` differ by 0x0C, strongly suggesting related views into one transient structure.
- `+0x20C/+0x210` are the exact same pointer.
- V4 ranking flooded the report with NEW static fields (`FX chg 0`, `EXP UNREADABLE`), proving lifecycle but obscuring changing timer candidates.

`HeroEffectTransientProbeV5/STATE.md`
- **BUILD/STATIC PROVEN — RUNTIME PENDING.**
- Strict read-only automatic transient-object observer.
- ARM one clean target once; after ARM user may change selection to cast the hero skill.
- Automatic start detection from proven lifecycle roots; no human timing required.
- Pins transient root objects from `+0x1E4/+0x1E8/+0x20C/+0x210` plus bounded one-level children.
- 50 ms sampling.
- Automatic natural expiry after lifecycle roots equal baseline for 3 consecutive samples.
- Dynamic/changing fields are ranked first; static duration-like constants are separated so they cannot bury timers.
- Observed effect wall time is reported for scale correlation.
- Run `34792424502` SUCCESS; job `103819020397` SUCCESS.
- Artifact `10327958166`, digest `sha256:1092ef001f920934a2ee0a1c6b5c861fbd4d72b03d4bf9d696965af424a95f6a`.
- Standalone SHA-256 `528e112990cccb86ec52953e8f93ddd8bd00b136915e9bc3d650ff693a9908be`.
- Small SHA-256 `daeae8b8ddf06ae967f54fa1a43428f04ee849bcab2c907c0e01968c93c9e691`.

## Exact next action
Runtime-test `HeroEffectTransientProbeV5` using ORIGINAL ISSYL:
1. fresh/reload BRZE and use a clean target with no hero buff active;
2. select exactly ONE normal unit that will receive Issyl;
3. click `ARM TARGET + AUTO WATCH`;
4. after ARM, selection may change if needed to operate Issyl;
5. cast ORIGINAL ISSYL exactly once on the armed target;
6. do NOT click anything for timing — wait for V5 itself to show `COMPLETE`;
7. click `COPY REPORT` and return the full report.

Analysis priority:
- `Observed effect wall time`;
- automatic root lifecycle should reproduce V4 pattern;
- highest-ranked `DYNAMIC FIELDS` with high change rate and `flip 0` or very low flips;
- compare total int/float change against wall-time to infer milliseconds/ticks/seconds;
- inspect `STATIC EFFECT-OBJECT CONSTANTS` secondarily for fixed duration or expiry constants.

If V5 produces one strong candidate, confirm it in a second clean Issyl run before any write. No duration write from one run alone.

Do not integrate duration into the main trainer yet. Never reintroduce repeated native re-application.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5); repeated refresh is permanently rejected. Unit+0x460 is rejected. V4 Issyl runtime proved transient lifecycle roots: +1E4/+1E8 and +20C/+210 appear only during effect and vanish at expiry; +214 is inverse. V5 automatic read-only transient-object timer probe is built successfully and is the exact next runtime test.`
