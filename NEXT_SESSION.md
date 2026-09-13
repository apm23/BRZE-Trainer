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
- User used original Grayback skill first, then cleared log, then original Issyl Haste.
- Captured Grayback: TARGET calls 8, MAGIC calls 8, runtime ability `0xC0`.
- Captured Issyl: TARGET calls 8, MAGIC calls 8, runtime ability `0xA5`.
- Locked mapping for this executable:
  - Grayback buff = `0xC0`
  - Issyl Haste = `0xA5`

`HeroEffectReplayV2/STATE.md`
- Replay V2 directly applies the runtime-sniffed IDs through native target helper RVA `0x1F0C32` to current selection (up to 120).
- Modes: `GRAYBACK 0xC0`, `ISSYL 0xA5`, `APPLY BOTH`.
- BUILD/STATIC PROVEN, RUNTIME PENDING.
- Workflow run `34787689041` SUCCESS.
- Artifact `10327416796`.
- Standalone SHA-256 `2b5671e9a4d13768a2e3adccd12a82b0137419cbd926d2a3cbb6870fe930a702`.
- Small SHA-256 `c0e5f4b3ca847612c9fff3094bb5bbc43639a1720aaf341356034277595e0ca1`.

## Exact next action
Runtime-test Hero Effect Replay V2:
1. close main trainer, Clone Lab, old Hero Effect Lab, and Sniffer;
2. select 20–30 visible normal units such as Spearmen;
3. test `GRAYBACK 0xC0` and verify visible/behavioral buff;
4. test `ISSYL 0xA5` and verify Haste;
5. test `APPLY BOTH` and verify both stack;
6. check queue reaches DONE and call count matches expected.

If all three pass, mark Replay V2 RUNTIME-PROVEN, then integrate Copy/Paste + Hero Effects into a new main trainer layer above the locked stable baseline.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. Continue from HeroEffectReplayV2 runtime test; Grayback=0xC0, Issyl=0xA5, Copy/Paste Hero already runtime-proven.`
