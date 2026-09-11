# V14 Building Profiles — Runtime PASS

Date: 2026-09-11 (Asia/Tokyo)

User runtime report:
- Dragon Dojo configured with 9 outputs: PASS.
- Dragon Target Range configured with 9 outputs: PASS.
- Dojo and Target Range profiles enabled simultaneously: PASS.
- Each building emitted its own configured outputs correctly; no profile cross-talk reported.
- Building-profile selector core is therefore runtime-proven for simultaneous independent profiles at least across these two Dragon base training buildings.

Locked conclusion:
- Preserve the V14 `UnitChangerCore` 12-profile completion-only architecture in subsequent UI revisions.
- V15 is UI-only cleanup: do not change the profiled Unit Changer runtime core unless a new runtime regression is reported.
- Mapper remains stock; red-X/arbitrary non-training-building bypass remains out of scope.

UI feedback that motivates V15:
- V14 Unit Changer panel is visually too large.
- 12 building buttons consume excessive height.
- slot cards are too tall, `Use` text wraps/clips, and long unit names are truncated.
- desired: Unit Changer visual footprint no larger than the main-cheat panel, simpler controls, full Clean + Diagnostics variants retained.
