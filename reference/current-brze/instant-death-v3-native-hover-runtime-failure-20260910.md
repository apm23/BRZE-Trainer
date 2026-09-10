# Instant Death v3 — native hover — RUNTIME FAILURE — 2026-09-10

Tested build: branch `instant-death-v3-native-hover`, workflow run `34437008955`, artifact `10136556551`.

User runtime result:
- enemy units: no effect while hovered;
- local/own units: no effect;
- therefore V3 did not provide runtime proof that the candidate native hover call returned the live hovered Unit* or that the death write fired.

Conclusion:
- V3 is FAILED / NOT PROVEN and must not be treated as a working Instant Death implementation.
- Candidate call `RVA 0x135F27 -> RVA 0x1D4888` remains static-only until telemetry proves runtime activity.
- V3 alliance helper guard was not runtime-proven.

Next probe: `instant-death-v4-hover-telemetry` keeps the same single call-site but exposes query-call count, last Unit*, owner and death-write count directly in the trainer status, while removing only the uncertain alliance guard and retaining local-owner protection.
