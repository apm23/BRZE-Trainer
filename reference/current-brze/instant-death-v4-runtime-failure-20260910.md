# Instant Death v4 hover telemetry — runtime failure — 2026-09-10

Runtime result: FAILED.

User enabled Instant Death V4, hovered an enemy in live gameplay, then captured the trainer status. The telemetry line showed:

`DEATH V4: ARMED | qcalls:0 | last:0x00000000 | owner:-- writes:0 | hpNow:0x00000000`

Conclusion: the detour at BRZE RVA `0x135F27` installed/armed, but that call site was not executed during the tested live hover path. Therefore `0x135F27 -> 0x5D4888` must not be treated as the active gameplay unit-under-cursor resolver for Instant Death. The failure occurred before any owner guard or death-sentinel write; do not tune `0xFF000000`, HP/stamina offsets, or alliance logic based on this failure.

Next step: return to the exact legacy trainer PageDown implementation and current BRZE call/data flow, then identify the actual live hover target path before another write-enabled probe.