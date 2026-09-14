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
`HeroEffectReplayV2/STATE.md`
- **RUNTIME-PROVEN one-shot replay.** Keep untouched as known-good fallback.
- Issyl runtime ability `0xA5`; Grayback `0xC0`.
- Uses game-thread frame hook RVA `0x135C43`.
- Max 4 native calls/frame, max 120 selected.
- Standalone SHA-256 `2b5671e9a4d13768a2e3adccd12a82b0137419cbd926d2a3cbb6870fe930a702`.

`HeroEffectDurationWriteProbeV9/STATE.md`
- **RUNTIME-PROVEN duration field:** `parent+0x1F4 -> config+0x0E0`.
- Original Issyl nominal duration `15000`.
- Modified duration must stay resident for the entire effect lifetime and restore only after natural expiry.

Rejected restore timings:
- restore on first lifecycle visibility;
- restore after V2 native helper return.
Never reuse either.

## V10.2 fallback — RUNTIME-PROVEN
- baseline `10755.0 ms`
- nominal `30000` replay `21115.9 ms`
- ratio `1.963355x`
- restore `30000 -> 15000` after natural expiry
- no repeated replay/refresh loop.

Keep V10.2 untouched as proven fallback.

## V11 configurable duration — PROVEN FINAL / LAUNCH-READY
Source/state: `HeroEffectReplayDurationV11/STATE.md`

Final authoritative runtime proof:
- Stage: `Ready`
- baseline ORIGINAL Issyl: `10742.1 ms`
- `ISSYL 3X`, nominal duration `45000`
- replay lifetime `31612.4 ms`
- observed replay/baseline ratio `2.942838x`
- restore attempted `True`
- restore OK `True`
- restored `45000 -> 15000`
- current config after replay `15000`
- hold inactive after completion
- no repeated application / refresh loop.

Custom-duration replay had also already succeeded and produced a longer effect.

### Launch decision
**GO. V11 is final. No more duration multiplier testing.**

UI/config:
- 1X = `15000`
- 2X = `30000`
- 3X = `45000`
- CUSTOM = `15000 × chosen multiplier`
- custom range `0.25x..20.00x`

Release build pin:
- workflow `Hero Effect Replay Duration V11 Configurable`
- run `34797972869` SUCCESS
- job `103834678005` SUCCESS
- head `ba5f8d63dd578bbc4e2f07980c1c2ae9f256cd6f`
- artifact `10330576956`
- digest `sha256:cb7dbf423a52000ab38434f0128c5de3c136dbd86394c1c13402a287d3033444`
- standalone SHA-256 `5d5525754185d34c90313fe3215022121e5fab46c0c66e15d0cccf156800029b`
- small SHA-256 `8aba3e1246f890160ac41495c6cb5185f65c7307d26872fef0ae1c55426eab02`

## Accepted release limitations
- configurable-duration lifecycle tracking is one clean target at a time;
- baseline capture once per fresh trainer/game session;
- A5 config is global/shared while extended override is resident;
- do not manually cast Issyl or start another Issyl replay during an active extended hold;
- V11 UI blocks another configurable replay while hold is active;
- manual restore/reset/close paths restore/attempt restore to `15000`.

These are documented limitations, not blockers.

## Exact next engineering step
Duration research/testing is DONE.

Next task: **integrate V11 above locked main trainer V18.3**.

Integration rules:
1. Keep V18.3 byte/source baseline recoverable as fallback.
2. Do not introduce a second competing hook at `0x135C43`.
3. Merge frame-hook consumers through one shared dispatcher.
4. Preserve known-good Replay V2 behavior and V11 full-lifetime duration hold semantics.
5. Add Issyl configurable-duration UI into next main-trainer version.
6. Regression-check all existing V18.3 features before replacing the stable base.
7. Only after integration CI passes, perform one final main-trainer smoke test rather than reopening duration research.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no lifecycle-first restore timing;
- no native-call-return restore timing;
- no claim that duration copies/latches at creation;
- keep Replay V2, V10.2, and V11 release binaries available as fallbacks.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V11 configurable duration is PROVEN FINAL. Final 3X runtime: baseline 10742.1ms, nominal 45000 replay 31612.4ms, ratio 2.942838x, restore 45000->15000 OK; custom replay also succeeded. No more duration testing. Next task: integrate V11 above locked V18.3 using one shared dispatcher for frame-hook RVA 0x135C43, preserve V18.3 fallback and Replay V2/V11 semantics.`
