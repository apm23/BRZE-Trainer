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

V10.2 remains the known-good merged fallback.

## V11 configurable duration — BUILT / CI-PROVEN, RUNTIME TEST NEXT
Source/state: `HeroEffectReplayDurationV11/STATE.md`

Purpose:
- preserve V10.2 full-lifetime-hold semantics;
- make Issyl replay duration user-selectable;
- baseline capture once, then repeated one-shot replays without recapturing baseline.

UI/config:
- `REPLAY 1X` => nominal `15000`
- `REPLAY 2X` => nominal `30000`
- `REPLAY 3X` => nominal `45000`
- `REPLAY CUSTOM` => `15000 × multiplier`
- custom UI range `0.25x..20.00x`

Runtime architecture:
1. capture original Issyl baseline/config once;
2. select exactly one clean target;
3. choose preset/custom multiplier;
4. guard A5 config and patch desired nominal value if not 1X;
5. queue one known-good Replay V2 A5 call;
6. keep chosen config resident for the ENTIRE replay lifetime;
7. read-only watcher tracks target transient roots;
8. after natural expiry stable for 4 polls, restore `15000`;
9. return to READY so another target/duration can be tested without recapturing baseline.

Current V11 initial runtime proof remains **single-target only**. Do not widen to multi-target until this passes.

Build pin:
- workflow `Hero Effect Replay Duration V11 Configurable`
- run `34797972869`
- job `103834678005`
- head `ba5f8d63dd578bbc4e2f07980c1c2ae9f256cd6f`
- artifact `10330576956`
- digest `sha256:cb7dbf423a52000ab38434f0128c5de3c136dbd86394c1c13402a287d3033444`
- standalone SHA-256 `5d5525754185d34c90313fe3215022121e5fab46c0c66e15d0cccf156800029b`
- small SHA-256 `8aba3e1246f890160ac41495c6cb5185f65c7307d26872fef0ae1c55426eab02`

CI:
- architecture invariants PASS
- compile smoke PASS
- standalone publish PASS
- small publish PASS
- artifact upload PASS

## Exact next runtime action — test V11 3X first
Use ONLY `BRZE-Hero-Effect-Replay-Duration-V11.exe`.

1. Fresh/reload BRZE.
2. Open V11 only.
3. Select exactly ONE clean target.
4. Click `1) CAPTURE ISSYL BASELINE`.
5. Cast ORIGINAL Issyl Haste ONCE on that target.
6. Wait until V11 says baseline complete / READY.
7. Select the same clean target again.
8. Click `REPLAY 3X` ONCE.
9. Do NOT manually cast Issyl the second time.
10. Wait until natural expiry and automatic restore.
11. COPY REPORT.

Expected 3X proof:
- baseline around current ~10.7 s;
- replay around ~31–32 s;
- ratio near 3.0x;
- current config returns to `15000` after expiry;
- no repeated application / no stacking.

If 3X passes, immediately test one custom value such as `4.00x` without recapturing baseline. Expected wall lifetime ~4x baseline and automatic restore afterward.

## Critical limitation
- A5 config is global/shared while the override is resident.
- Another Issyl effect created during an active extended hold may observe the modified duration.
- V11 blocks another configurable replay in its UI while hold is active, but the user should also avoid manually casting Issyl during that interval.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no lifecycle-first restore timing;
- no native-call-return restore timing;
- no claim that duration is copied at effect creation;
- keep original HeroEffectReplayV2 source/binary untouched as fallback;
- keep V10.2 merged build untouched as proven duration fallback;
- no main V18.3 integration until V11 configurable runtime proof passes.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V10.2 is RUNTIME-PROVEN: baseline 10755.0ms, Replay V2 with A5 duration held at 30000 for full effect lifetime 21115.9ms, ratio 1.963355x, then restore 15000 after expiry. V11 configurable is now BUILT/CI-PROVEN: presets 1X/2X/3X plus custom 0.25x..20x, baseline once then repeat replays, still full-lifetime hold and restore after expiry. CI run 34797972869. Next runtime test: V11 REPLAY 3X on one clean target.`
