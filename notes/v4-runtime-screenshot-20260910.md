# Instant Death V4 runtime telemetry screenshot

User runtime result after enabling V4 and hovering an enemy in live BRZE:

`DEATH V4: ARMED | qcalls:0 | last:0x00000000 | owner:-- writes:0 | hpNow:0x00000000`

Result: V4 FAILED. The detour was armed but the chosen `0x135F27` native query call did not execute during the live hover test. Do not reuse V3/V4 query-hook architecture.
