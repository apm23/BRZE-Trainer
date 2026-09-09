# Selection500 ALWAYS + Fixed1s Peasant + Pop9M — build pin

Date: 2026-09-10 (Asia/Tokyo)

Runtime-proven predecessor:
- `BRZE-Selection-500-Always-Fixed1s-Peasant`
- user confirmed Selection500 always-on, F4-independent selection, fixed 1s local peasant production, and continued production beyond the prior ~167-unit stop.

Requested delta only:
- F4 max population changed from 500 to `9,999,999`.
- Selection limit remains exactly 500.
- Selection500 + headroom160 remains always-on.
- Fast Peasant remains fixed 1.0 second and local-player only with the proven PeasantManager population-stop bypass design.

Build:
- branch `selection-500-always-fixed1s-pop9m`
- head `b68e4f295944f058aa226c97cff2db622f64976c`
- Actions run `34377039566` SUCCESS
- exact Pop9M replacement verification SUCCESS
- compile smoke SUCCESS
- x86 single-file publish SUCCESS
- artifact upload SUCCESS
- artifact ID `10114328832`
- ZIP SHA-256 `7f763704fc5b964b94994e1b143179560f9ea4b054e837741c9c39bdb0edb0e1`
- EXE SHA-256 `f47da9c55315fb89d2cc1b1e287ac61e68fcc5a87be35baee1e0a43a8cb3a436`

Runtime status: build/compile proven; delta is minimal from a runtime-proven predecessor.