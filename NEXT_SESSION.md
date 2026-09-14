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

`HeroEffectDurationWriteProbeV9/STATE.md`
- **RUNTIME-PROVEN — DURATION FIELD CONFIRMED.**
- Issyl controlled write:
  - `15000 -> 10738.0 ms`
  - `30000 -> 21222.5 ms`
  - ratio `1.976390x`
  - restore `30000 -> 15000` succeeded.
- V9 held modified duration for the ENTIRE effect lifetime and restored only after natural expiry.

## Merged V2 + duration history
`HeroEffectReplayDurationMerged/STATE.md`

Rejected restore timings:
- first lifecycle visibility: ratio `0.988283x`
- after V2 native helper returned: ratio `0.990176x`

Never reuse either restore timing.

V10.2 runtime-proven timing:
- baseline `10755.0 ms`
- nominal `30000` replay `21115.9 ms`
- ratio `1.963355x`
- restore after natural expiry succeeded
- no repeated replay/refresh loop.

V10.2 remains known-good merged fallback.

## V11 configurable duration — RUNTIME-PROVEN / LAUNCH-READY
Source/state: `HeroEffectReplayDurationV11/STATE.md`

Runtime result:
- Stage after replay: `Ready`
- baseline ORIGINAL Issyl: `17694.9 ms`
- selected nominal duration: `45000` (`ISSYL 3X` preset)
- replay lifetime: `31642.9 ms`
- observed wall ratio: `1.788250x`
- config restored `45000 -> 15000`
- restore OK
- current config after replay `15000`
- no repeated application / no stacking loop.

User also reported a custom-duration replay succeeded and lasted longer.

### Locked interpretation
`1X / 2X / 3X / CUSTOM` are **nominal config multipliers**, not guaranteed exact wall-clock multipliers.

Controlled values:
- 1X nominal = `15000`
- 2X nominal = `30000`
- 3X nominal = `45000`
- custom = `15000 × selected multiplier`

Wall-clock lifetime can differ because BRZE game-time/tick scaling varies. Do not reject the feature simply because wall ratio does not equal nominal multiplier exactly.

### Launch decision
**GO. V11 standalone is launch-ready. No more duration-runtime multiplier tests are required before launch.**

Do not churn the runtime-proven V11 binary merely to rename cosmetic multiplier labels. Document that X means nominal config multiplier.

## V11 release build pin
- workflow `Hero Effect Replay Duration V11 Configurable`
- run `34797972869` SUCCESS
- job `103834678005` SUCCESS
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

## Current release limitations
- configurable-duration lifecycle tracking remains one clean target at a time;
- baseline capture once per fresh trainer/game session;
- A5 config is global/shared while an extended override is resident;
- avoid manually casting Issyl or starting another Issyl replay during an active extended hold;
- V11 UI blocks another configurable replay while hold is active;
- manual restore/reset/close paths attempt restore to `15000`.

These limitations are accepted for V11 standalone launch and are documented, not blockers.

## Next engineering step
Duration research/testing is DONE.

Next work may proceed directly to either:
1. package/publish V11 standalone as the released hero-effect replay-duration tool; or
2. integrate the runtime-proven V11 capability above locked main trainer V18.3 using a single shared dispatcher for the frame-hook site.

Do NOT reopen multiplier testing unless a real runtime regression is reported.

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
- V11 runtime-proven binary is the launch candidate/final standalone for configurable duration.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V11 configurable duration is RUNTIME-PROVEN / LAUNCH-READY. User runtime: baseline 17694.9ms, nominal 45000 replay 31642.9ms, restore 45000->15000 OK; custom duration also runtime-successful. X labels are nominal config multipliers, not exact wall-clock guarantees. No more duration multiplier tests. V11 standalone SHA256 5d5525754185d34c90313fe3215022121e5fab46c0c66e15d0cccf156800029b. Next: package/release standalone or integrate above locked V18.3 using one shared dispatcher.`
