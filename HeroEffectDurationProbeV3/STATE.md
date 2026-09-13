# BRZE Hero Effect Duration Probe V3 Control Differential — STATE

Status: **RUNTIME-PROVEN OBSERVER — Unit+0x460 HIGH-CONFIDENCE CANDIDATE, MOVEMENT CONTROL REQUIRED**
Date: 2026-09-14 JST

## Why V3 exists
Recursive read-only Probe V2 completed a useful runtime pass but did not yet prove a writable duration field.

V2 runtime findings:
- the new object reached through `Unit+0x1E4 -> +0x008` contains values at `+0x028/+0x02C/+0x030` that closely track target world coordinates and clears at visible expiry; treat it as likely visual/particle/transform state, not the gameplay duration timer;
- new/container-like records beneath `Unit+0x094` remain structurally interesting but are not safe to patch yet;
- direct `Unit+0x460` became the strongest timer/accumulator candidate seen so far.

## V3 runtime result — 2026-09-14
User completed the same-unit idle CONTROL vs ORIGINAL Grayback effect pass.

Pinned `Unit+0x460` result:
- 15-second no-buff idle control: `CTRL chg 0`, `flip 0`, value stayed `345400`;
- pre-effect value: `345400`;
- during Grayback watch: `FX chg 164`, `flip 0`;
- first observed Grayback-active value: `354400`;
- natural visible expiry value: `413000`;
- total PRE -> EXP increase: `+67600`;
- therefore `Unit+0x460` is strongly effect-correlated and strictly monotonic in this pass.

Important confound discovered in the same report:
- target position fields `Unit+0x028/+0x02C/+0x030` also changed repeatedly during the Grayback watch;
- nearby transform/state fields in `0x45C..0x4A4` were active/noisy during the same interval;
- because `+0x460` sits inside/adjacent to that live movement/transform cluster, it is not yet proven to be Grayback duration-specific. It may instead be a movement/animation accumulator that happened to be inactive during the stationary control.

Other useful expiry evidence:
- `Unit+0x1E4` and `Unit+0x1E8` changed from null to a pointer during the effect and returned to zero at visible expiry;
- `Unit+0x20C/+0x210` similarly became populated during the effect and returned to zero at expiry;
- `Unit+0x214` changed during the effect and returned exactly to its pre-effect baseline pointer at expiry;
- these remain better interpreted as effect/visual/container lifecycle markers than a writable gameplay duration field until proven otherwise.

## Exact next proof
Reuse this same V3 binary for a **movement-only no-buff control** before any memory write:
1. fresh/reloaded BRZE;
2. select exactly ONE clean normal unit with no hero effect active;
3. press `1 START 15s CONTROL`;
4. DURING those 15 seconds deliberately keep the unit walking/moving (no Grayback, no other buff, no attack required);
5. when the control auto-stops, press `COPY REPORT` immediately;
6. inspect pinned `Unit+0x460`.

Decision rule:
- if movement-only control gives `CTRL chg > 0`, reject `+0x460` as Grayback-duration-specific and continue toward effect-instance/container interception;
- if movement-only control still gives `CTRL chg 0`, `+0x460` becomes a very strong Grayback-specific elapsed/duration candidate and the next test should compare its direction/scale across clean Grayback and Issyl runs before any isolated write.

## Read-only architecture
- process access: `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION` only;
- no `WriteProcessMemory`;
- no hooks;
- no `VirtualAllocEx` / `VirtualProtectEx`;
- no `CreateRemoteThread`;
- no native ability call/replay;
- original hero skills are cast manually by the user when required.

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
- no duration write is allowed until `Unit+0x460` survives the movement-only control and then cross-effect timing proof.
