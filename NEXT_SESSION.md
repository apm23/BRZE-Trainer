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
- Three original Issyl passes with deliberately different human timing: `10725.9 / 10713.8 / 10733.2 ms`; spread only `19.4 ms`.
- Effect parent identity is content-based: `parent+0x058 == ability ID` AND `parent+0x17C == pinned Unit*`.
- Parent paths move between casts; never hard-code transient path.
- Parent `+0x194` rejected as duration-specific.

`HeroEffectChildProbeV7/STATE.md`
- Runtime-proven child observer.
- Strong ability config object discovered at `parent+0x1F4`.
- Issyl config contains `config+0x000=A5`, `config+0x0E0=15000`.

`HeroEffectAbilityConfigProbeV8/STATE.md`
- **RUNTIME-PROVEN ABILITY CONFIG.**
- Issyl A5: nominal `+0x0E0=15000`, natural wall `10727.1 ms`.
- Grayback C0: nominal `+0x0E0=60000`, natural wall `42222.1 ms`.
- Config ratio exactly `4x`; wall ratio ~`3.936x`.

`HeroEffectDurationWriteProbeV9/STATE.md`
- **RUNTIME-PROVEN — DURATION FIELD CONFIRMED.**
- Issyl controlled write:
  - baseline nominal `15000`
  - test nominal `30000`
  - baseline natural wall `10738.0 ms`
  - patched natural wall `21222.5 ms`
  - observed ratio `1.976390x`
  - automatic restore `30000 -> 15000` succeeded
- Locked conclusion: `parent+0x1F4 -> config+0x0E0` is the hero-effect nominal duration parameter.
- Restoring config after the original cast has consumed the duration does NOT shorten the already-created effect.

## Merged V2 + V10 status
`HeroEffectReplayDurationMerged/STATE.md`

### First merged runtime attempt — timing rejected
Report:
- baseline original Issyl: `10749.7 ms`
- Replay V2 2X attempt: `10623.7 ms`
- ratio `0.988283x`
- same A5 config verified
- restore succeeded

Conclusion:
- restoring when lifecycle first appears is TOO EARLY;
- lifecycle visibility happens before Replay V2's native apply helper has finished consuming/copying duration;
- this does NOT invalidate the proven duration field from V9;
- NEVER restore on lifecycle-first timing again.

### V10.1 fix — BUILT / CI-PROVEN, RUNTIME RETEST NEXT
Merged UI now holds A5 duration at 30000 while V2 reports `native calls:0`.
Replay V2 increments CALLS only after the native helper returns.
Only after `native calls > 0` does V10 resume ticking and restore 30000 -> 15000.

This fixes timing without modifying the proven V2 replay core/stub.

Build pin:
- workflow `Hero Effect Replay Duration Merged`
- run `34796740941` SUCCESS
- job `103831200764` SUCCESS
- head `71f7c1f048be4db14a30a70141e09856e4116e63`
- artifact `10330240927`
- digest `sha256:f1235fd559129d18201faca31ae84beb37933128f4f67cf93a9a6338ee618503`
- standalone V10.1 SHA-256 `30423192bcf813d9f7d0a285755291e15a2b4ae01e54078a84e31e7d9d22f64f`
- small V10.1 SHA-256 `0a908dc8ce91991168b064bc0109eac2748ea2ea9276d1191ceec8e5d387ac82`

## Exact next action — V10.1 runtime retest
Use ONLY `BRZE-Hero-Effect-Replay-Duration-Merged-V10.1.exe`.

1. Fresh/reload BRZE.
2. Open V10.1 merged EXE only.
3. Select exactly ONE clean target.
4. Click `1) CAPTURE ISSYL BASELINE`.
5. Select Issyl and cast ORIGINAL Haste ONCE on that target.
6. Wait until baseline completes (~10.7 s).
7. Select the same target again after Haste fully expires.
8. Click `2) REPLAY ISSYL 2X` ONCE.
9. Do NOT manually cast Issyl the second time.
10. Wait until COMPLETE (~21 s expected).
11. COPY REPORT and inspect:
   - replay lifetime ~21 s
   - ratio near 2.0x
   - same A5 config verified
   - current config 15000
   - restore OK

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no V7 cleanup-only child writes;
- no low-address/vtable `10272` assumption;
- no lifecycle-first restore timing;
- keep original HeroEffectReplayV2 source/binary behavior untouched as fallback;
- no main V18.3 integration until merged runtime proof passes.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V9 proved Issyl config+0x0E0 duration causally: 15000->30000 changed 10738.0ms->21222.5ms (1.976390x). First merged replay failed timing: baseline 10749.7ms, replay 10623.7ms (0.988283x) because V10 restored on lifecycle visibility before V2 native helper finished. V10.1 now holds 30000 until Replay V2 native calls > 0 (helper returned), then restores. CI run 34796740941 success; runtime-test V10.1 next.`
