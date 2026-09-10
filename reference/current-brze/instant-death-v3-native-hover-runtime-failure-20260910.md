# Instant Death v3 — native hover — RUNTIME FAILURE — 2026-09-10

Branch/build tested: `instant-death-v3-native-hover`, workflow run `34437008955`, artifact `10136556551`.

User runtime result:
- enabling Instant Death v3 and hovering enemy units produced no visible effect;
- hovering local/own units also produced no effect (local safety therefore did not distinguish whether the hook was actually seeing a Unit*);
- no runtime proof exists that the wrapper's native query returned the hovered unit or that a death write executed.

Conclusion:
- V3 is **FAILED / NOT PROVEN** and must not be treated as a working Instant Death implementation.
- The static `0x535F27 -> 0x5D4888` mouse-query call remains only a candidate runtime path.
- The V3 alliance helper guard at `0x5848E8` was never runtime-proven and may have filtered the target incorrectly.

Next single-hypothesis probe: V4 keeps the same native query call but adds in-process telemetry (`query call count`, `last Unit*`, `last owner`, `death-write count`) visible in the trainer status and removes only the uncertain alliance guard. Local-owner protection remains. This will distinguish: dead call-site vs null hit-test vs guard failure vs successful write that does not kill.
