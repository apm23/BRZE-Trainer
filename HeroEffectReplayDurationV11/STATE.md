# BRZE Hero Effect Replay + Duration V11 — STATE

Status: **RUNTIME-PROVEN / LAUNCH-READY — CONFIGURABLE DURATION CONFIRMED**
Date: 2026-09-14 JST

## Proven base
V11 layers above locked V10.2 runtime proof. V10.2 remains untouched as fallback.

V10.2 runtime proof:
- baseline ORIGINAL Issyl `10755.0 ms`
- Replay V2 with nominal duration `30000`: `21115.9 ms`
- ratio `1.963355x`
- config restored `30000 -> 15000` only after natural expiry
- no repeated application / no refresh loop

Locked rule: any modified Issyl duration must stay resident for the ENTIRE active replay lifetime, then restore only after natural expiry.

## V11 runtime result — PASS
User runtime report:
- Stage after replay: `Ready`
- pinned target remained valid
- captured A5 config `0x1CCFB694`
- config identity remained `0xA5`
- baseline ORIGINAL Issyl wall lifetime: `17694.9 ms`
- selected preset: nominal `45000` (`ISSYL 3X`)
- replay wall lifetime: `31642.9 ms`
- observed wall replay/baseline ratio: `1.788250x`
- restore attempted: `True`
- restore OK: `True`
- last write: `RESTORED config+0x0E0 45000 -> 15000`
- current config after replay: `15000`
- hold inactive after completion
- no repeated native application / no refresh loop

User additionally reported running a custom-duration replay successfully and observing a longer effect. No further duration-runtime testing is required for launch.

## Important interpretation
`1X / 2X / 3X / CUSTOM` are **nominal config multipliers**, not a guarantee of exact wall-clock multiplication.

The authoritative controlled parameter is:
- original nominal duration `15000`
- 2X nominal `30000`
- 3X nominal `45000`
- custom `15000 × chosen multiplier`

Observed wall-clock lifetime varies with BRZE game-time/tick scaling and therefore may not equal the nominal multiplier exactly. This does NOT invalidate the duration mechanism: V9, V10.2 and V11 together prove that increasing `config+0x0E0` causally extends the effect and that the value can be safely restored after natural expiry.

## Launch decision
**GO. V11 standalone is launch-ready.**

Reasoning:
- known-good Replay V2 remains the only native replay path;
- arbitrary configurable duration has runtime evidence beyond the fixed 2X proof;
- automatic full-lifetime hold works;
- automatic restore to `15000` works;
- custom replay also worked in user runtime;
- no stacking/reapply loop was introduced;
- CI architecture/compile/publish all passed;
- further multiplier tests would add little confidence relative to cost and are not required before launch.

Do not churn the proven binary solely to rename cosmetic multiplier labels. Document that multiplier labels refer to nominal config values, not exact wall time.

## V11 behavior
After one baseline capture, the user can replay repeatedly without recapturing baseline:
- `1X` = nominal config `15000`
- `2X` = `30000`
- `3X` = `45000`
- `CUSTOM` = `15000 × multiplier`
- custom UI range `0.25x .. 20.00x`

Current release remains single-target for configurable-duration lifecycle tracking. Multi-target duration tracking is separate future work and is NOT required for V11 standalone launch.

## Architecture
- `HeroEffectReplayV2/ReplayCore.cs` linked unchanged; it remains the only native replay path.
- Issyl A5 config discovered through proven content-signature path.
- Duration field: `parent+0x1F4 -> config+0x0E0`.
- Original nominal duration: `15000`.
- guarded config write only after A5 identity/value checks.
- read-only watcher tracks pinned target transient roots.
- non-1X desired value remains resident for full replay lifetime.
- after natural expiry stable for 4 polls, restore to `15000`.
- no repeated native application / refresh.

## Safety / guardrails
- configurable-duration lifecycle tracking is one clean target at a time;
- baseline capture required once per fresh trainer/game session;
- replay blocked unless captured config identity remains A5 and current duration is exactly `15000`;
- nominal duration hard guard `1000..600000`;
- UI custom range `0.25x..20.00x` (`3750..300000`);
- while an extended replay is active, starting another configurable replay is blocked;
- normal replay buttons are blocked while an extended hold is active;
- manual RESTORE 15000 available;
- RESET and normal close attempt restore;
- avoid manually casting Issyl during an extended hold because A5 config is global/shared while the override is resident.

## Release build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Replay Duration V11 Configurable`
Run: `34797972869` — SUCCESS
Job: `103834678005` — SUCCESS
Head: `ba5f8d63dd578bbc4e2f07980c1c2ae9f256cd6f`
Artifact: `10330576956`
Artifact digest: `sha256:cb7dbf423a52000ab38434f0128c5de3c136dbd86394c1c13402a287d3033444`

CI:
- architecture invariants PASS
- compile smoke PASS
- standalone publish PASS
- small publish PASS
- artifact upload PASS

Release binaries:
- Standalone: 151,088,336 bytes — SHA-256 `5d5525754185d34c90313fe3215022121e5fab46c0c66e15d0cccf156800029b`
- Small: 174,322 bytes — SHA-256 `8aba3e1246f890160ac41495c6cb5185f65c7307d26872fef0ae1c55426eab02`

## Locked rejects
- no repeated native reapplication/refresh;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no lifecycle-first restore timing;
- no native-call-return restore timing;
- no claim that duration latches/copies at creation;
- keep original HeroEffectReplayV2 untouched as fallback;
- keep V10.2 merged runtime-proven build untouched as duration fallback.
