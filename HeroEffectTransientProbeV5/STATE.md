# BRZE Hero Effect Transient Probe V5 — STATE

Status: **SOURCE READY — BUILD/RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V5 exists
V4 runtime with ORIGINAL ISSYL proved a clean effect lifecycle:
- `Unit+0x1E4`: 0 -> transient pointer -> 0
- `Unit+0x1E8`: 0 -> transient pointer -> 0
- `Unit+0x20C`: 0 -> transient pointer -> 0
- `Unit+0x210`: 0 -> transient pointer -> 0
- `Unit+0x214`: baseline pointer -> 0 during effect -> exact baseline pointer at expiry

But V4 ranked too many NEW static fields (`FX chg 0`) above genuinely changing fields. V5 narrows the problem to exact transient objects and makes user timing automatic.

## V5 architecture
Strictly read-only:
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION`
- no writes
- no allocation/injection
- no hooks
- no native ability replay

Workflow:
1. select exactly ONE clean target unit;
2. click ARM;
3. V5 pins Unit* + UnitDef + owner and captures baseline lifecycle roots;
4. selection may change after ARM if needed to cast the original hero ability;
5. cast ORIGINAL ISSYL (recommended) or Grayback exactly once on the armed target;
6. V5 automatically detects effect start when multiple proven lifecycle roots change together;
7. it pins transient objects from `+0x1E4/+0x1E8/+0x20C/+0x210` plus a bounded one-level child set;
8. samples every 50 ms;
9. automatically marks natural expiry only after all lifecycle roots equal baseline for 3 consecutive samples;
10. COPY REPORT.

No fast human reaction is required at effect start or expiry.

## Scan scope
- root object scan: 0x600 bytes
- one-level child discovery from first 0x180 bytes
- child scan: 0x240 bytes
- max 28 child objects
- exact transient addresses are pinned at effect start

## Ranking
Primary report contains only fields that actually changed during the effect.
Boost:
- high change rate
- zero/low direction flips
- many samples
Penalty:
- pointer-like values
- direction flipping/noise

Static effect-object constants are separated into a secondary section and filtered to duration-like numeric ranges so they cannot bury dynamic timers.

The report also records automatically observed effect wall time, enabling scale tests such as milliseconds, ticks, frames, or float seconds.

## Runtime objective
Find a field inside a proven transient Issyl/Grayback object whose behavior is monotonic and whose total/rate correlates with automatic natural lifecycle expiry.

No write is allowed from V5 alone. If a strong field appears, confirm it in a second clean run (preferably Issyl) before an isolated write experiment.

## Locked rules
- `HeroEffectReplayV2` one-shot replay remains runtime-proven and untouched.
- repeated native reapplication remains permanently rejected.
- `Unit+0x460` remains rejected for duration.
- do not merge duration control into the main trainer until a real transient-object field is runtime-proven.
