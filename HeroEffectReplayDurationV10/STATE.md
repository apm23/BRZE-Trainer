# BRZE Hero Effect Replay Duration V10 — STATE

Status: **IMPLEMENTED — CI/RUNTIME PENDING**
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

## Proof condition
Runtime success requires all of:
- baseline ~10.7 s under current time scale;
- Replay V2-created Issyl effect ~21 s;
- replay/baseline ratio near 2.0x;
- replay parent resolves to same captured A5 config;
- config is already restored to 15000 while replay effect remains active;
- no stacking/compounded gameplay behavior.

Known-good `HeroEffectReplayV2` must remain untouched as fallback.
