# BRZE Trainer V30 — Runtime-Proven Hero Reset Integration

Status: **SOURCE READY / CI NEXT**
Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Runtime proof inherited from V29
V29 proved the reset primitive on an active stock Issyl A5 instance:
- active record signature remained `A5 + target Unit*`;
- old `record+0x194 = 507200`;
- proven BRZE current simulation tick at module RVA `0x440A3C` was `519700`;
- V29 wrote only `record+0x194 = 519700`;
- exact readback passed and same A5 record/signature remained valid;
- user visual proof: the selected/reset unit kept Issyl longer than Issyl himself / the comparison unit that was not reset.

Conclusion: **writing the current BRZE simulation tick into an existing same-effect record's `+0x194` restarts that instance's lifetime without native reapply or stacking.**

BRZE pauses its simulation/game clock when minimized. A static clock sample while minimized is expected.

## V30 final semantics
For each selected unit and requested ability:
- if the same effect record already exists: signature-lock it and reset `record+0x194` to module `RVA 0x440A3C` current tick;
- if the same effect is already pending from a previous native queue: do not duplicate it;
- if the same effect is absent: queue exactly one Replay V2 native apply;
- unrelated buffs are ignored and untouched.

For `APPLY BOTH`, V30 partitions targets into:
- missing both Issyl + Grayback;
- missing Grayback only;
- missing Issyl only.
This prevents reapplying an ability that is already active on a mixed selected group.

Mixed sub-batches are serialized through the existing single V20/V22 shared dispatcher. No second frame hook is introduced.

## Duration
- Issyl nominal baseline 15000; Grayback nominal baseline 60000.
- Existing proven `record+0x1F4 -> config+0xE0` full-lifetime hold remains unchanged.
- Same held duration may be reused across groups.
- If changing a held duration, V30 only retunes when no other tracked group of that ability exists outside the selected set and no pending batch uses it.
- Restore to baseline only after tracked natural expiry.

## Locked safety
- no repeated native refresh/replay on an already-active same effect;
- no V26 zero reset;
- no Unit+0x460 duration write;
- no CreateRemoteThread;
- max 120 selected targets;
- one shared frame dispatcher / hook owner at RVA `0x135C43`;
- Copy Unit / Instant Death / Unit Changer remain on the locked integrated base.

## Source
- `HeroEffectDirectCoreV30.cs`
- `IntegratedFeaturePanelsV30.cs`
- `MergedTrainerV30.csproj`
- `tools/finalize_v30_reset_hero.py`
- `.github/workflows/final-v30-reset-hero.yml`

## Next
Run V30 CI. If green, give Diagnostics standalone first and perform one compact integrated smoke:
1. one unit with active Issyl + one unit without Issyl selected together -> APPLY ISSYL;
2. active unit must restart while missing unit receives one fresh apply;
3. repeat APPLY before expiry -> both existing instances restart, no stacking;
4. quick Grayback or APPLY BOTH mixed-group smoke;
5. confirm Copy Unit and Instant Death were not regressed only if anything looks suspicious (dispatcher/base are hash-locked in CI).
