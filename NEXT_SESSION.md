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
- Same pinned target, Issyl controlled write:
  - baseline nominal `15000`
  - test nominal `30000`
  - baseline natural wall `10738.0 ms`
  - patched natural wall `21222.5 ms`
  - observed ratio `1.976390x`
  - test parent `0x28C03890` via a different transient path, but same A5 config verified
  - automatic restore `30000 -> 15000` succeeded
  - final current config back at `15000`
- Locked conclusion: `parent+0x1F4 -> config+0x0E0` is the hero-effect nominal duration parameter.
- Critical integration fact: restoring the config after effect creation does NOT shorten the already-created effect. Duration is copied/consumed at effect creation.

## Exact next engineering objective
Build **V10 Duration Replay Integration Lab** as a separate layer above known-good Replay V2. Do NOT mutate V2.

Desired mechanism:
1. preserve V2 game-thread one-shot native replay architecture;
2. resolve/validate the ability-specific config record before replay;
3. immediately before each native apply call, temporarily patch only `config+0x0E0` to requested nominal duration;
4. call the native target-helper once;
5. immediately restore the original nominal duration after that call;
6. no periodic refresh, no stacking, no lingering global config patch;
7. retain V2 max-120 / max-4-calls-per-frame guardrails;
8. runtime-test standalone V10 before any main-trainer integration.

Initial safe V10 target: prove Issyl replay at 2X duration first. Grayback can follow after the same mechanism is proven in replay context.

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
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. Replay V2 one-shot is runtime-proven; V3 repeated refresh is permanently rejected. V8 proved ability config at parent+0x1F4: Issyl A5 duration nominal 15000, Grayback C0 60000. V9 CAUSALLY proved config+0x0E0 is the duration field: Issyl 15000->30000 changed natural wall lifetime 10738.0ms->21222.5ms (1.976390x) and auto-restored to 15000. Next build V10 as a separate Replay-V2-based duration integration lab using temporary per-call config patch + immediate restore.`
