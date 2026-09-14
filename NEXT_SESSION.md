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
- Proven binary standalone SHA-256 `2b5671e9a4d13768a2e3adccd12a82b0137419cbd926d2a3cbb6870fe930a702`.

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
- Restoring config after effect creation does NOT shorten the already-created effect; duration is copied/consumed at effect creation.

`HeroEffectReplayDurationV10/STATE.md`
- CI-proven companion architecture; preserved as intermediate proof tool.
- Known-good Replay V2 performs replay; V10 performs guarded duration patch/restore + lifecycle measurement.

`HeroEffectReplayDurationMerged/STATE.md`
- **BUILT / CI-PROVEN — RUNTIME MERGED TEST NEXT.**
- Single EXE/window that links the proven V2 ReplayCore and V10 ReplayDurationCore directly into one assembly.
- User no longer needs two programs.
- Normal V2 buttons preserved: Grayback, Issyl, Both.
- Merged `REPLAY ISSYL 2X` button automatically:
  1. arms V10 duration controller;
  2. patches A5 duration 15000 -> 30000 under V10 guards;
  3. queues the proven V2 native Issyl replay in the same click;
  4. V10 restores A5 config to 15000 when replay lifecycle appears;
  5. measures replay effect to natural expiry.
- Workflow `Hero Effect Replay Duration Merged`.
- Run `34796236065` SUCCESS; job `103829774919` SUCCESS.
- Head `c873a613801fb681485ad0f50fdd1bd9a0d08f04`.
- Artifact `10330070782`, digest `sha256:640421a5825bacfe735d626a24c16a9eaac4395989c4404e0b31aaca82e2ff7d`.
- Standalone SHA-256 `bc57c6689b6526230aad79f78e32725752bd4e8e69f80d8ddf7f8ceef84ae9d7`.
- Small SHA-256 `ea97594995ba047b7f8fcf12e0743a2f3e5974be8e3f9b975d0625a26e5ff8cd`.

## Exact next action — MERGED runtime proof
Use ONLY `BRZE-Hero-Effect-Replay-Duration-Merged.exe`.

1. Fresh/reload BRZE.
2. Open the merged EXE only.
3. Select exactly ONE clean target.
4. Click `1) CAPTURE ISSYL BASELINE`.
5. Select Issyl in BRZE and cast ORIGINAL Haste ONCE on that target.
6. Wait until baseline is complete / ready for replay (~10.7 s).
7. Select the same target again after Haste is fully gone.
8. Click `2) REPLAY ISSYL 2X` ONCE.
9. Do not manually cast Issyl the second time; the same merged EXE queues V2 replay automatically.
10. Do nothing until COMPLETE (~21 s expected).
11. Click COPY REPORT and return the report.

Expected proof:
- baseline ~10.7 s;
- merged V2 replay ~21 s;
- ratio near 2.0x;
- replay parent resolves to same A5 config;
- current A5 config is restored to 15000 while replay effect remains active;
- no stacking/compounded behavior.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no V7 cleanup-only child writes;
- no low-address/vtable `10272` assumption;
- keep original HeroEffectReplayV2 source/binary untouched as fallback;
- no main V18.3 integration until merged runtime proof passes.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V9 causally proved config+0x0E0 duration: Issyl 15000->30000 changed natural lifetime 10738.0ms->21222.5ms (1.976390x) and restored safely. Replay V2 one-shot remains runtime-proven. A single merged V2+V10 EXE is now built CI-successfully (run 34796236065, artifact 10330070782). Runtime-test the merged EXE next: capture one original Issyl baseline, then click REPLAY ISSYL 2X once; no second app/window.`
