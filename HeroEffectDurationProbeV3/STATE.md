# BRZE Hero Effect Duration Probe V3 Control Differential — STATE

Status: **RUNTIME-PROVEN OBSERVER — Unit+0x460 REJECTED AS DURATION-SPECIFIC**
Date: 2026-09-14 JST

## Why V3 exists
Recursive read-only Probe V2 completed a useful runtime pass but did not prove a writable duration field. V3 compared direct Unit* fields against no-buff controls before any write experiment.

## Idle-control + Grayback result
Initial same-unit idle control made `Unit+0x460` look highly promising:
- idle no-buff 15-second control: `CTRL chg 0`, `flip 0`, value stayed `345400`;
- pre-effect value `345400`;
- Grayback watch: `FX chg 164`, `flip 0`;
- first observed active value `354400`;
- natural visible expiry value `413000`;
- PRE -> EXP increase `+67600`.

However the same Grayback report showed movement/transform activity nearby, so a movement-only control was required before any write.

## Decisive movement-only no-buff result — 2026-09-14
User reused V3 with NO Grayback/Issyl/other buff and deliberately kept the selected unit moving during the 15-second control.

Pinned `Unit+0x460`:
- `CTRL chg 55`;
- `flip 0`;
- value `413000 -> 432800`;
- total increase `+19800` during movement-only control.

Therefore `Unit+0x460` is **REJECTED as a Grayback-duration-specific timer**. Its monotonic behavior is reproducible from ordinary movement/activity and is consistent with a generic movement/animation/activity accumulator or neighboring state clock.

Do NOT patch, freeze, scale, or otherwise write `Unit+0x460` for hero-effect duration.

Additional movement-only evidence:
- position/transform fields including `Unit+0x028/+0x02C/+0x030`, `+0x45C..+0x4A4`, and several world-coordinate-like fields changed during the no-buff movement control;
- this confirms the direct movement/transform cluster cannot be used as effect duration evidence solely because it changes monotonically during a buff pass.

## Remaining useful V2/V3 lifecycle evidence
- `Unit+0x1E4/+0x1E8` populated during Grayback and returned to zero at visible expiry;
- `Unit+0x20C/+0x210` likewise populated during Grayback and returned to zero at visible expiry;
- `Unit+0x214` changed during the effect and returned to its pre-effect pointer;
- structures beneath `Unit+0x094` remain interesting container candidates;
- object(s) under `Unit+0x1E4` carried XYZ-like values and are likely visual/particle/transform state, so they must not be assumed to contain the gameplay timer.

## Next direction
Stop scanning direct Unit fields for the timer. Move to a targeted **effect-instance/container differential**:
- compare recursive object/container paths during movement-only control versus one original Grayback cast;
- prioritize paths absent/stable during movement but created/changed only during Grayback;
- correlate candidate field lifetime with natural visible expiry;
- remain observation-only before any write.

If recursive container differential still fails to isolate a coherent timer, intercept the runtime effect-creation path (`RVA 0x13FFD1`, already runtime-proven by HeroEffectSniffer) to capture the actual created effect/object pointer or downstream container insertion, then observe that object read-only.

## Read-only architecture
- process access: `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION` only;
- no `WriteProcessMemory`;
- no hooks in V3;
- no `VirtualAllocEx` / `VirtualProtectEx`;
- no `CreateRemoteThread`;
- no native ability call/replay.

Known runtime anchors:
- selection list RVA `0x441708`;
- UnitDef pointer `Unit+0x74`;
- owner `Unit+0x240`;
- direct Unit scan size `0x500`.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `01a2e03d74f3f8c2983d98febf4a70785703a67c`
Workflow: `Hero Effect Duration Probe V3 Control Diff Read Only`
Run: `34790956827` — SUCCESS
Job: `103814953856` — SUCCESS
Artifact: `10327249644`
Artifact digest: `sha256:a7eeb16a1bccd51bd7f449f9ac55913989e42c46f8ef4e7536451ae650d08ac9`

Binaries:
- Standalone: 66,003,520 bytes — SHA-256 `d3902d905d7787dd85f0b11b54befb62f7081b5ee8c9fa465812ad0058bdee9f`
- Small: 149,806 bytes — SHA-256 `a4006557ae4a98c1a0fdb7502536ea48d773535c39b6ff63efaea7b91e1b3235`

## Locked safety rules
- `HeroEffectReplayV2` one-shot native replay remains runtime-proven and untouched.
- `HeroEffectReplayV3` repeated re-apply/refresh is runtime-rejected and must never be reused.
- `Unit+0x460` is a hard reject for duration-specific writing.
- no duration write until an effect-instance/container field is correlated against movement control and natural expiry.