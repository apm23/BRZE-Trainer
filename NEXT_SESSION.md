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
- **BUILT / CI-PROVEN COMPANION — RUNTIME TEST NEXT.**
- V10 deliberately does NOT hook or call native helpers; known-good Replay V2 stays untouched and performs the one-shot replay.
- CI forbids V10 from containing executable-memory/hook/native-helper machinery.
- V10 captures A5 config from one original Issyl baseline, then guarded-patches `15000 -> 30000` before a Replay V2 click.
- When V10 detects the V2-created lifecycle, it immediately restores `30000 -> 15000` and measures the replay effect until natural expiry.
- Workflow `Hero Effect Replay Duration V10 Companion`.
- Run `34795909466` SUCCESS; job `103828838973` SUCCESS.
- Head `2a140fc395cbb9c76e0c8fca173a24933f00d147`.
- Artifact `10329941650`, digest `sha256:89779875f2dd9f73770c7153aa53ec3132f8a73aa90292ccaae88a95d1e4f733`.
- Standalone SHA-256 `2933018dd7888598f2d9380bc9155a42885f62ba23a03efe150ef3ae4351bc57`.
- Small SHA-256 `bb0eebcd330674949115f3991ee171e0000b63bbd364c34a411e010511ff5c1e`.

## Exact next action — V10 Replay V2 duration proof
Run BOTH the V10 companion and the known-good Replay V2.

1. Fresh/reload BRZE.
2. Open V10 companion.
3. Open known-good `HeroEffectReplayV2`; do NOT press its buttons yet.
4. Select exactly ONE clean target in BRZE.
5. In V10 click `1) ARM BASELINE / CAPTURE`.
6. Select Issyl in BRZE and cast ORIGINAL Haste ONCE on that target.
7. Wait until V10 says baseline complete / ready for replay (~10.7 s).
8. Select the same target again after Haste is fully gone.
9. In V10 click `2) ARM REPLAY 2X`.
10. NOW switch to Replay V2 and click `ISSYL 0xA5` exactly once. Do NOT manually cast Issyl this second time.
11. Return to V10 and do nothing until it shows COMPLETE (~21 s expected).
12. COPY REPORT from V10 and return it.

Expected proof:
- baseline ~10.7 s;
- Replay V2 2X ~21 s;
- ratio near 2.0x;
- replay parent resolves to same A5 config;
- config already restored to 15000 while effect remains active;
- no stacking/compounded behavior.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no V7 cleanup-only child writes;
- no low-address/vtable `10272` assumption;
- keep `HeroEffectReplayV2` untouched as fallback;
- no main V18.3 integration until V10 runtime proof passes.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V9 causally proved config+0x0E0 duration: Issyl 15000->30000 changed natural lifetime 10738.0ms->21222.5ms (1.976390x) and restored safely. Replay V2 one-shot remains runtime-proven and untouched. V10 companion built SUCCESS (run 34795909466, artifact 10329941650): capture A5 config with one original Issyl baseline, arm 30000, then click ISSYL once in known-good Replay V2; V10 restores 15000 immediately on lifecycle detection and measures replay lifetime. Runtime-test V10 next.`
