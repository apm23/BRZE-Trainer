# Selection500 always-on + fixed 1s Fast Peasant — design

Date: 2026-09-10 (Asia/Tokyo)

Requested behavior after runtime result where Fast Peasant stopped around total unit 167:

- Selection500 + safe bulk-drag headroom160 is always armed while trainer is attached.
- F4 controls only local Max Population 500 and can be toggled independently.
- Fast Peasant becomes fixed 1.0 second, not a multiplier.
- Fast Peasant bypasses only the two native PeasantManager population-stop checks for the local player so automatic peasant production does not stop merely because population-used reached the max-pop value.
- AI/non-local players retain native behavior.

Implementation plan:

- retain the proven narrow selection500 wrappers and event headroom160;
- auto-set the selection wrapper flag immediately after attach and arm ACTIVE/SIM first=500/growth=0 before use;
- save the original local max-pop value and restore it when F4 is OFF; F4 ON writes exactly 500;
- replace the old 20x scheduler wrapper with one that first invokes native scheduler, then for local Fast Peasant sets next-production timestamp to caller current-game-time + 1000 ms;
- wrap population-used calls at RVAs `0x17FF1B` and `0x17FF71`; preserve native result normally, but return 0 only for local player + Fast Peasant so those two PeasantManager stop comparisons do not block the automatic production loop;
- do not globally patch population counting or the shared race config.

Runtime validation still required.
