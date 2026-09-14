# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Authoritative stable base
- Main trainer rollback/final stable base remains V18.3 as documented in `FINAL_CURRENT.md`.
- Do not mutate locked V18.3 directly; layer new work above it.

## Proven side feature
`UnitCloneLab/STATE.md`
- Native Copy/Paste selected units is RUNTIME-PROVEN, including hero / unique / story units.
- Never raw-clone Unit structs.

## Hero Effect locked facts
`HeroEffectSniffer/STATE.md`
- Grayback runtime ability `0xC0`; Issyl runtime ability `0xA5`.
- Target helper RVA `0x1F0C32`; magic-create helper RVA `0x13FFD1`.

`HeroEffectReplayV2/STATE.md`
- **RUNTIME-PROVEN one-shot replay.** Keep untouched as known-good fallback.
- Uses game-thread frame hook RVA `0x135C43`, max 4 native calls/frame, max 120 selected.
- Proven standalone SHA-256 `2b5671e9a4d13768a2e3adccd12a82b0137419cbd926d2a3cbb6870fe930a702`.

`HeroEffectReplayV3/STATE.md`
- **RUNTIME-REJECTED.** Repeated native replay stacks/compounds effects. NEVER REUSE.

`HeroEffectDurationProbeV3/STATE.md`
- `Unit+0x460` hard-rejected as duration-specific.

`HeroEffectSignatureProbeV6/STATE.md`
- Original Issyl lifetime stable around 10.72–10.73 s.
- Effect parent identity: `parent+0x058 == ability ID` AND `parent+0x17C == pinned Unit*`.
- Parent paths move between casts; never hard-code transient path.
- Parent `+0x194` rejected as duration-specific.

`HeroEffectChildProbeV7/STATE.md`
- Ability config discovered at `parent+0x1F4`.
- Issyl config contains `config+0x000=A5`, `config+0x0E0=15000`.

`HeroEffectAbilityConfigProbeV8/STATE.md`
- Issyl A5: nominal `+0x0E0=15000`, natural wall `10727.1 ms`.
- Grayback C0: nominal `+0x0E0=60000`, natural wall `42222.1 ms`.
- Config ratio exactly 4x; wall ratio ~3.936x.

`HeroEffectDurationWriteProbeV9/STATE.md`
- **RUNTIME-PROVEN — DURATION FIELD CONFIRMED.**
- Issyl controlled write:
  - `15000 -> 10738.0 ms`
  - `30000 -> 21222.5 ms`
  - ratio `1.976390x`
  - restore `30000 -> 15000` succeeded.
- IMPORTANT: V9 held `30000` for the ENTIRE effect lifetime and restored only after natural expiry. Do NOT claim duration was copied/latching at creation.

## Merged V2 + duration history
`HeroEffectReplayDurationMerged/STATE.md`

### Attempt #1 — REJECTED
Restore on first lifecycle visibility:
- baseline `10749.7 ms`
- replay `10623.7 ms`
- ratio `0.988283x`
- same A5 config verified
- restore OK

### Attempt #2 / V10.1 — REJECTED
Restore after V2 native helper returned (`native calls > 0`):
- baseline `10717.2 ms`
- replay `10611.9 ms`
- ratio `0.990176x`
- same A5 config verified
- restore OK

Locked conclusion:
- neither lifecycle appearance nor native-helper return is late enough to restore the global A5 config;
- never reuse either restore timing.

## V10.2 — RUNTIME-PROVEN SUCCESS
Design mirrors V9 exactly: keep `config+0x0E0=30000` for the entire Replay V2 effect lifetime and restore only after natural expiry.

Runtime result:
- baseline ORIGINAL Issyl: `10755.0 ms`
- merged Replay V2 2X: `21115.9 ms`
- ratio: `1.963355x`
- restore after expiry succeeded: `30000 -> 15000`
- current config after test: `15000`
- hold active after completion: `False`
- no repeated replay/refresh loop.

IMPORTANT report interpretation:
- the old companion block may still show `Stage: ReadyForReplay`, replay `0.0 ms`, ratio `0.0x` because V10.2 intentionally bypasses the old V10 lifecycle state machine during its full-lifetime hold;
- the authoritative V10.2 runtime section is `MERGED V10.2 FULL-LIFETIME HOLD`.

Build pin:
- workflow `Hero Effect Replay Duration Merged`
- run `34797240949` SUCCESS
- job `103832611033` SUCCESS
- head `0062f32fa0d3d903f85ef613eb71dd8034de95a3`
- artifact `10330421265`
- digest `sha256:4dcdf829c606197fb631257248753befa5d6d00757a38e32c3281b33675a7b77`
- standalone SHA-256 `d522c611f48e84b1a9b0259938e8847f87e9c968ec07bbb2cfb2c10f58f51c29`
- small SHA-256 `6a22bc4611edc41daa174a024d3334c6b152b7481838887d4edc1c270892c17d`

## Exact next engineering action
Build the next merged version ABOVE V10.2 with **user-configurable Issyl duration** while preserving V10.2 semantics:
1. capture/validate A5 config;
2. user chooses desired nominal duration instead of fixed 30000;
3. patch before one-shot Replay V2;
4. keep patched value for the entire active replay lifetime;
5. restore original `15000` after natural expiry;
6. never refresh/reapply the effect.

Recommended first configurable proof values:
- `15000` = normal
- `30000` = 2x proven
- optionally `45000` = 3x experimental after 2x path remains locked.

Critical limitation to surface in final integration:
- A5 config object is global/shared. While an override is resident, another Issyl effect created during that interval may also inherit/observe the modified duration.
- Do not hide this limitation; final design should minimize or control concurrent Issyl casts.

Do NOT integrate duration into locked V18.3 main trainer until the configurable merged version is runtime-proven.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no lifecycle-first restore timing;
- no native-call-return restore timing;
- no claim that duration is copied at effect creation;
- keep original HeroEffectReplayV2 source/binary untouched as fallback.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V10.2 is now RUNTIME-PROVEN: original Issyl baseline 10755.0ms, Replay V2 with A5 config held at 30000 for the full effect lifetime 21115.9ms, ratio 1.963355x, then automatic restore to 15000 after natural expiry succeeded. Lifecycle-first restore and native-call-return restore are permanently rejected. Next: configurable Issyl duration above V10.2, still one-shot Replay V2 + full-lifetime hold + restore after expiry.`
