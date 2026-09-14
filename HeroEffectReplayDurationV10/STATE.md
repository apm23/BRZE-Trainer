# BRZE Hero Effect Replay Duration V10 — STATE

Status: **BUILT / CI-PROVEN COMPANION — RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V10 exists
V9 causally proved `parent+0x1F4 -> config+0x0E0` is the nominal hero-effect duration parameter:
- Issyl baseline `15000` -> `10738.0 ms`
- guarded test `30000` -> `21222.5 ms`
- ratio `1.976390x`
- automatic restore `30000 -> 15000` succeeded

The next question is whether the already runtime-proven `HeroEffectReplayV2` one-shot native replay consumes the same duration field at creation time.

## Architecture
V10 is deliberately a **companion** to the untouched known-good `HeroEffectReplayV2`.

V10 itself:
- does NOT install a hook;
- does NOT allocate executable memory;
- does NOT call the native ability helper;
- does NOT replay/refresh effects;
- only reads lifecycle/config state and performs the already-proven guarded `config+0x0E0` write/restore.

Replay V2 remains responsible for the one-shot native A5 call.

CI explicitly forbids V10 source from containing:
- `VirtualAllocEx`
- `VirtualProtectEx`
- `CreateRemoteThread`
- `FlushInstructionCache`
- frame-hook RVA `0x135C43`
- target-helper RVA `0x1F0C32`

## Runtime flow
1. Select exactly ONE clean target.
2. Click `1) ARM BASELINE / CAPTURE` in V10.
3. Cast ORIGINAL Issyl Haste once on that target.
4. V10 captures the A5 config via content signature and measures baseline until natural expiry.
5. After baseline is complete, select the same now-clean target again (or another clean target).
6. Click `2) ARM REPLAY 2X` in V10. This guarded-write patches A5 `config+0x0E0: 15000 -> 30000`.
7. In the known-good `HeroEffectReplayV2` window, click `ISSYL 0xA5` exactly once for the selected target.
8. V10 detects the newly-created A5 lifecycle and immediately restores `30000 -> 15000`.
9. V10 verifies the replay parent resolves to the same captured A5 config and measures natural expiry.
10. Expected replay lifetime is approximately 2x baseline.

## Guardrails
- exactly one clean target for this proof;
- config identity must remain `config+0x000 == A5`;
- patch refuses unless current duration is exactly `15000`;
- restore only writes if current duration is exactly `30000`;
- automatic restore occurs immediately when replay lifecycle appears;
- manual `RESTORE NOW` available;
- RESET and normal close attempt restore;
- no repeated native application/refresh;
- no hook/injection/native helper in V10.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Replay Duration V10 Companion`
Run: `34795909466` — SUCCESS
Job: `103828838973` — SUCCESS
Head: `2a140fc395cbb9c76e0c8fca173a24933f00d147`
Artifact: `10329941650`
Artifact digest: `sha256:89779875f2dd9f73770c7153aa53ec3132f8a73aa90292ccaae88a95d1e4f733`

Binaries:
- Standalone: 151,067,856 bytes — SHA-256 `2933018dd7888598f2d9380bc9155a42885f62ba23a03efe150ef3ae4351bc57`
- Small: 153,330 bytes — SHA-256 `bb0eebcd330674949115f3991ee171e0000b63bbd364c34a411e010511ff5c1e`

Known-good Replay V2 fallback hashes:
- standalone SHA-256 `2b5671e9a4d13768a2e3adccd12a82b0137419cbd926d2a3cbb6870fe930a702`
- small SHA-256 `c0e5f4b3ca847612c9fff3094bb5bbc43639a1720aaf341356034277595e0ca1`

## Proof condition
Runtime success requires all of:
- baseline ~10.7 s under current time scale;
- Replay V2-created Issyl effect ~21 s;
- replay/baseline ratio near 2.0x;
- replay parent resolves to same captured A5 config;
- config is already restored to 15000 while replay effect remains active;
- no stacking/compounded gameplay behavior.

Known-good `HeroEffectReplayV2` remains untouched as fallback.
