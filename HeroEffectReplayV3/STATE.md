# BRZE Hero Effect Replay V3 Duration — STATE

Status: **RUNTIME-REJECTED — DO NOT REUSE REFRESH/STACK APPROACH**
Date: 2026-09-14 JST

## Runtime verdict
User runtime-tested V3 Duration Hold. Scheduled native re-application does **not** behave as a safe timer refresh. It creates/accumulates effect state strongly consistent with stacking modifiers.

Observed user-visible failure after repeated refresh:
- damage against units became effectively zero / target units stopped losing HP;
- damage against buildings became extreme, approximately one-hit;
- movement speed became extremely fast;
- attack cadence felt slower than the runtime-proven V2 single-application behavior.

This is a hard rejection of periodic native re-application as a duration mechanism. Do not tune the interval, lower the refresh rate, or reuse this architecture for duration extension.

## Proven baseline preserved
Hero Effect Replay V2 remains the runtime-proven baseline for a **single native application** of the runtime-sniffed ability IDs:
- Grayback native runtime ability: `0xC0`
- Issyl native runtime ability: `0xA5`
- Native target ability helper RVA: `0x1F0C32`
- Selection list RVA: `0x441708`
- Frame/game-thread hook RVA: `0x135C43`

V2 must remain untouched as rollback/proven baseline.

## Why V3 failed
The V2 target helper signature exposes `Unit* + ability ID`; no duration argument is present on that proven call path. V3 attempted to extend effective duration by repeatedly invoking that same native helper. Runtime evidence shows this is not an idempotent timer reset. Re-application can stack or otherwise compound modifier/effect state and corrupt combat behavior.

Therefore:
- NEVER use repeated `0xC0` / `0xA5` application as a timer extender.
- NEVER implement INFINITE by periodic re-application.
- STOP HOLD only stops future calls and cannot reliably remove already-created stacked instances.
- For a contaminated runtime session, safest recovery is to stop V3 and reload a clean save / restart the game process.

## Build pin — rejected specimen only
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `0f0d0c34cccff6df6f8e31c1c98436ba3a717fcc`
Workflow: `Hero Effect Replay V3 Duration`
Run: `34788874055` — SUCCESS (build only; runtime rejected)
Job: `103809270556` — SUCCESS
Artifact: `10327388613`
Artifact digest: `sha256:604312730b0de8bad8fbc7cced242d93fc4a2589ec7332f195cc8cce75e054e4`

Binaries — **DO NOT USE FOR NORMAL PLAY**:
- Standalone SHA-256 `d82e642ac9287ca39f17690832d5c1022e91954d337a8c81828b90d8058271d9`
- Small SHA-256 `36dccb03b0041d300201dab20142e860002e4e212b3ea5580a4c47cfc3e6c7a0`

## Next safe research direction
Build a dedicated **effect-instance duration probe**. The probe must be observational first: identify the runtime effect instance/timer created by ONE proven V2 application, then determine whether its remaining-duration field can be extended directly without re-applying the ability or changing raw movement/attack/damage stats.

Requirements for the next experiment:
1. Start from a clean game runtime.
2. Apply each ability only ONCE using the V2-proven native path.
3. Capture before/after effect-container or effect-instance state for the selected Unit*.
4. Identify a timer/expiry value by watching it change naturally over time.
5. Only after a timer candidate is proven should a separate isolated test write that timer.
6. Do not integrate duration into the main trainer until this is runtime-proven.
