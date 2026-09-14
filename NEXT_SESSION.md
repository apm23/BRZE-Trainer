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
- RUNTIME-PROVEN one-shot replay. Keep untouched as known-good fallback.

`HeroEffectReplayV3/STATE.md`
- RUNTIME-REJECTED. Repeated native replay stacks/compounds effects. NEVER REUSE.

`HeroEffectDurationProbeV3/STATE.md`
- `Unit+0x460` hard-rejected as duration-specific.

`HeroEffectContainerProbeV4/STATE.md`
- Issyl transient lifecycle roots proven: `+0x1E4/+0x1E8/+0x20C/+0x210`; `+0x214` returns to baseline.

`HeroEffectSignatureProbeV6/STATE.md`
- Three original Issyl passes with deliberately different human timing: `10725.9 / 10713.8 / 10733.2 ms`; spread only `19.4 ms`.
- Human click speed is irrelevant.
- Effect parent identity is content-based:
  - `parent+0x058 == ability ID`
  - `parent+0x17C == pinned Unit*`
- Parent paths/addresses move between casts; NEVER hard-code transient path.
- Parent `+0x194` rejected as duration-specific.

`HeroEffectChildProbeV7/STATE.md`
- Runtime-proven child observer.
- No clean dynamic countdown found.
- Strong structured config object found at `parent+0x1F4`.
- Issyl config contained `config+0x000=A5`, `+0x0E0=15000`, remained static/readable for full lifecycle.
- Low-address static `10272` rejected as vtable/type coincidence.

`HeroEffectAbilityConfigProbeV8/STATE.md`
- **RUNTIME-PROVEN ABILITY CONFIG.**
- ORIGINAL ISSYL:
  - natural wall lifetime `10727.1 ms`
  - `config+0x000 = 0xA5`
  - `config+0x0E0 = 15000`
  - wall/config ratio `0.715138`
- ORIGINAL GRAYBACK:
  - natural wall lifetime `42222.1 ms`
  - `config+0x000 = 0xC0`
  - `config+0x0E0 = 60000`
  - wall/config ratio `0.703701`
- Both config records remained static for full lifecycle with zero read failures.
- Config ratio is exactly `4.0x`; observed wall-lifetime ratio is `~3.936x`.
- `parent+0x1F4` is strongly established as ability-specific config and `config+0x0E0` as nominal duration parameter.

`HeroEffectDurationWriteProbeV9/STATE.md`
- **BUILT / CI-PROVEN GUARDED WRITE — RUNTIME 2X TEST NEXT.**
- Issyl-only isolated write experiment; main trainer untouched.
- Baseline first; must discover A5 config through content signature.
- Before any write require BOTH:
  - `config+0x000 == 0xA5`
  - `config+0x0E0 == 15000`
- Test patch writes ONLY `config+0x0E0: 15000 -> 30000`.
- Next original Issyl cast is measured to natural expiry.
- Automatic restore `30000 -> 15000` after test; also manual `RESTORE NOW`, reset restore, normal-close restore.
- Restore refuses unexpected values.
- No hooks, injection, remote thread, native replay, or repeated application.
- CI guarded-write invariant PASS + compile smoke PASS.
- Workflow `Hero Effect Duration Write Probe V9 Guarded`.
- Run `34794886552` SUCCESS; job `103825945080` SUCCESS.
- Head `47058aa56524fbd6581d9003903ac252b9b4d854`.
- Artifact `10328933404`, digest `sha256:9e755bb48a0383ccdf484479648fd720f5205c83a597b029de7c8088c4fc499a`.
- Standalone SHA-256 `9dae8e6c07c66417c268fc5226e8273109de4187b0915bc24bce8a3121ebdd72`.
- Small SHA-256 `b11092f3a7d30b205fe3d59279ba76899d5952a4120d678ca74c1649131ef453`.

## Exact next action — V9 2X proof
1. Fresh/reload BRZE.
2. Select exactly ONE clean normal target.
3. Click `1) ARM BASELINE`.
4. Select Issyl and cast ORIGINAL Haste once on that target.
5. Wait until V9 says baseline is complete / ready for patch.
6. Select exactly ONE clean target for the test cast.
7. Click `2) PATCH 30000 + ARM TEST`.
8. Select Issyl and cast ORIGINAL Haste once on that target.
9. Do nothing until natural expiry and V9 says TEST COMPLETE.
10. Confirm report says restore attempted/OK and current config is back to `15000`.
11. Click `COPY REPORT` and return full report.

Expected proof condition: second lifetime approximately `2.0x` baseline (~21 s versus ~10.7 s under current game time scale). Do not integrate if result is not clean.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient path;
- no V7 cleanup-only child writes;
- no low-address/vtable `10272` assumption;
- no main-trainer duration integration until V9 2X write proof passes.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot replay is proven; repeated refresh, Unit+0x460 and parent+0x194 are rejected. V8 runtime proved parent+0x1F4 ability config: Issyl A5 has config+0x0E0=15000 and ~10.727 s lifetime; Grayback C0 has +0x0E0=60000 and ~42.222 s lifetime (exact 4x config, ~3.936x wall). V9 guarded Issyl 15000->30000 controlled write probe built SUCCESS (run 34794886552, artifact 10328933404) with automatic restore. Runtime-test V9 next.`
