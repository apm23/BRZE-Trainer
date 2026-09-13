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
- V2 remains the locked rollback/reference implementation for hero effects.
- Do not mutate V2 while duration work is being tested.

`HeroEffectReplayV3/STATE.md`
- New duration experiment layered above proven V2.
- BUILD/STATIC PROVEN — RUNTIME PENDING.
- Captures selected Unit* targets once and validates UnitDef `+0x74` + owner `+0x240` before each refresh.
- Re-applies the same proven native ability IDs on the game thread rather than raw-writing speed/stat values.
- Presets: 30s / 60s / 5m / INFINITE; refresh: 2s / 5s / 10s.
- Workflow run `34788874055` SUCCESS.
- Artifact `10327388613`.
- Artifact digest `sha256:604312730b0de8bad8fbc7cced242d93fc4a2589ec7332f195cc8cce75e054e4`.
- Standalone SHA-256 `d82e642ac9287ca39f17690832d5c1022e91954d337a8c81828b90d8058271d9`.
- Small SHA-256 `36dccb03b0041d300201dab20142e860002e4e212b3ea5580a4c47cfc3e6c7a0`.

## Exact next action
Runtime-test Hero Effect Replay V3 duration hold:
1. close V2, main trainer, Clone Lab, Sniffer, and other frame-hook labs;
2. select 10–30 visible normal units;
3. choose `60 seconds` + `5 seconds` refresh;
4. test Grayback and verify the effect stays active beyond its normal expiry;
5. test Issyl and verify Haste stays active beyond its normal expiry;
6. test APPLY BOTH and verify both remain active while HOLD is active;
7. after starting, deselect the units and confirm V3 keeps refreshing the captured group;
8. press STOP HOLD and confirm the effects eventually expire naturally.

If native re-application resets/extends the active effect timer, mark V3 duration RUNTIME-PROVEN and integrate Copy/Paste + Hero Effects + duration hold into a new main-trainer layer above locked V18.3/V19 cores.

If re-application does not extend an already-active effect, mark V3 refresh approach RUNTIME-REJECTED and build a dedicated effect-instance duration/timer probe. Do not guess timer offsets and do not fall back to raw speed/stat writes.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. HeroEffectReplayV2 is runtime-proven (Grayback=0xC0, Issyl=0xA5); continue from HeroEffectReplayV3 duration-hold runtime test. Copy/Paste Hero is already runtime-proven.`
