# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules
- GitHub source, pinned builds, and runtime results are authoritative.
- One hypothesis -> minimal patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Preserve failed experiments; do not reintroduce disproven broad patches.
- Use fresh BRZE/restart between invasive selection probes unless a procedure explicitly says otherwise.
- New thread handoff: `CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`.

## Authoritative target
`Battle_Realms_F(5).exe`
- PE32/x86, size `4,521,984`, preferred image base `0x400000`
- SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- ASLR is active in runtime; patchers must use moduleBase+RVA for absolute operands.
- reference: `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only.

---

# Selection architecture — proven

## Local/UI list
- RVA `0x441708`
- layout: free `+0x08`, count `+0x18`, blocks `+0x1C`, first `+0x20`, growth `+0x24`
- stock first=90, growth=0

## Local admission gate
Function `0x5A6FD8`:
- stock gate at VA `0x5A7000`: compare active count against 90, reject path `0x5A70D5`
- continue path after gate `0x5A700D`
- then append UI list, set `unit+0x3A8=1`, emit selection event

## Sort/rebuild `0x5A719F`
Selection-only list reset calls use stock first=90/growth=0:
- sort temp A call `0x5A71B2 -> 0x4ABFE6`
- sort temp B call `0x5A71BF -> 0x4ABFE6`
- active rebuild call `0x5A729F -> 0x4ABFE6`

## Simulation selection
- base pointer RVA `0x441730`
- 10 per-player lists, stride `0x28`, same list layout
- init reset call `0x5A6C57 -> 0x4ABFE6`
- runtime reset call `0x5A6E3B -> 0x4ABFE6`

## Selection event path
- local AddUnit emits 4-byte type-0 event through `0x552FFA`
- type 0 -> dispatcher `0x56016B` -> simulation add `0x5A736F(player,unit)`
- simulation add checks membership, appends, sets `unit+0x3AC=1`, calls refresh `0x5A7B97`

## Rectangle selection `0x5D4356`
- candidate list RVA `0x479748`
- native candidate first=128, growth=128
- accepted candidate call `0x5D455A -> 0x5A6FD8`
- candidate clear `0x5D4573`
- final UI sort `0x5D457D -> 0x5A719F`

## Event queue
- used RVA `0x441C94`
- remaining RVA `0x441C98`
- pointer RVA `0x441C9C`
- physical/native backing = 256 bytes
- logical remaining init immediate RVA `0x161770`
- post-flush reset immediate RVA `0x161D77`
- native flush helper `0x561B2A`

---

# Selection runtime history

## Failed/partial probes
- Surgical Growth: stable to 90; #91 rejected.
- Manual120 run `34343788268`: #91 crash.
- FirstBlock120 run `34347542420`: #91 crash; later found sort reset issue.
- SortPipeline120 run `34348488434`: large drag freeze; slow #91 briefly selected then crash.
- NoAddNotify120 run `34349623960`: UI reached 108 but multi-drag + move/attack semantics broke; type-0 events are required.
- EventBuffer1024 run `34358214184`: large drag still froze; old 1024 implementation must not be reused.
- NoPerAddRefresh run `34359923529`: suppressing `0x5A7B97` did not fix freeze.
- Rectangle Sleep(1) fixed run `34363669046`: did not fix freeze; same-invocation delay is not cross-tick throttling.

## Sim-Pipeline-120 — partial success
- branch `selection-sim-pipeline-120-probe`
- run `34352052932` SUCCESS
- EXE `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`
- >90 selection + normal move/attack worked when accumulated via smaller drags
- one-shot large drag still froze

## Decisive failure telemetry
Read-only observer run `34365054121`, EXE `4f73f3e23b77f954fe424c8d48dd8a2b8e4a39b85b959e849b79028303cd761c`.

Known large-drag freeze:
- ACTIVE=64
- SIM=0
- EVENT used=258
- EVENT remain=`0xFFFFFFFE` (-2)

Exact boundary:
- rectangle clear-selection contributes 2 event bytes
- `2 + 63*4 = 254`, leaving 2 bytes in logical 256
- next 4-byte add reaches native flush too late
- failed reset + continued write yields `258 / -2`

References:
- `reference/current-brze/bulk-telemetry-freeze-64-20260909.md`
- `reference/current-brze/bulk-telemetry-freeze-64-log-20260909.txt`

## Runtime-proven bulk fix — logical headroom160
Probe branch `selection-event-headroom-160-probe`, run `34367473165`, EXE `c25e9ac2962c5659d851e7db3bb722d06a36cd6d03d4023ad259643c0792002a`.

Exact fix:
- physical event backing stays 256
- logical remaining/reset becomes 160
- live remain set 160 while queue empty
- event type-0 remains LIVE

Runtime proof with Sim120:
- one-shot large rectangle no freeze
- MAX ACTIVE 103
- MAX SIM 103
- repeated successful flushes `used=0/remain=160`
- SIM catches ACTIVE; no underflow

Reference: `reference/current-brze/event-headroom160-runtime-success-20260909.md`.

---

# LOCKED BASELINE — Integrated Selection120 + Headroom160

Branch `selection-120-headroom160-integrated`.

Build:
- head `b623167036bed91299f4cc8184bb2a2fd821ca34`
- run `34368982513` SUCCESS
- artifact `10111151679`
- EXE SHA-256 `5a13f24b7bc6b6dafbea9bf837e5037358c265f01594ea1c6bd934f23e85790c`

User runtime regression after fresh restart:
- single integrated EXE passed again
- large one-shot drag passed
- >90 remained stable
- move + attack remained functional/authoritative

**Treat this as the solved/runtime-proven baseline for further selection work.**

Reference: `reference/current-brze/selection120-headroom160-integrated-runtime-success-20260909.md`.

---

# Peasant production timing — static/remap proof

Existing creation enable array:
- RVA `0x467AF4` (`Enable/DisablePeasantCreation` semantics)
- this is NOT the timing value

Target strings/config loader prove:
- `MinTimeToCreatePeasant` -> race config `+0x6C`
- `MaxTimeToCreatePeasant` -> race config `+0x70`

Native scheduler `0x57FFB2`:
- `0x580014`: max time * 1000
- `0x580023`: min time * 1000
- advances per-player next-production timestamp through global pointer RVA `0x467AF0`

Automatic update `0x57FEE9` calls scheduler at `0x57FFA5` with player id as first argument.

Reference: `reference/current-brze/peasant-production-timing-remap-20260909.md`.

---

# CURRENT RUNTIME CANDIDATE — Selection500 + Fast Peasant

Branch: `selection-500-fast-peasant-probe`

Files:
- `Selection500FastPeasant.cs`
- `Selection500FastPeasant.csproj`
- `.github/workflows/selection-500-fast-peasant-probe.yml`

Build pin:
- head `7b1a8832075ace66db8bc2e17953fbb4675d15f9`
- Actions run `34371976218` — SUCCESS
- compile smoke SUCCESS
- x86 single-file publish SUCCESS
- artifact upload SUCCESS
- artifact `BRZE-Selection-500-Fast-Peasant-Probe`
- artifact ID `10112324559`
- ZIP SHA-256 `097cbe040623d636fc88303c6db4a8a12b67b27f4e60747819391219e0a52059`
- EXE SHA-256 `295e9a4c65d9e63092189b6bb96c3c22898ec7ef1d90da457ad570af7203babf`

## Selection/population500 design
- local max population is exactly 500
- local admission gate is replaced by a narrow cave wrapper rejecting count >=500
- active local list is armed first=500/growth=0 before allocation
- only five proven selection-specific reset call sites are redirected through one wrapper that substitutes first=500 while latched:
  - sim init `0x5A6C57`
  - sim reset `0x5A6E3B`
  - sort A `0x5A71B2`
  - sort B `0x5A71BF`
  - active rebuild `0x5A729F`
- shared `0x4ABFE6` is NOT globally patched
- event type-0 LIVE; physical buffer 256; logical headroom160 retained
- rectangle candidate list unchanged (native 128/128)

500 requires wrappers because the old 90->120 immediate patch sites cannot safely encode 500.

## Fast Peasant design
- separate toggle in this probe
- patch only automatic scheduler call `0x57FFA5`
- wrapper invokes original `0x57FFB2` first
- only for local player + enabled flag, shorten the newly scheduled positive interval by 20x
- minimum shortened interval = 1000 ms
- non-local/AI schedule remains native
- no fake unit spawning, no shared race-config mutation, creation-enable flag is not forced

Reference: `reference/current-brze/selection500-fast-peasant-probe-build-20260909.md`.

Status: **compile/build proven only; runtime not yet proven.**

## Required next runtime test
1. fully restart BRZE
2. use ONLY `BRZE-Selection-500-Fast-Peasant-Probe.exe`
3. F4 before any selection
4. verify status: `ARMED selection500+headroom160`, popLimit 500, ACTIVE/SIM `first:500 grow:0`, event remain 160
5. first prove crossing old 120 boundary: 121+, then 150-200 if available
6. test one-shot large drag + right-click move + attack
7. enable Fast Peasant; verify native Peasant Hut production becomes materially faster; designed ~20x, min 1s
8. reaching/testing full 500 is optional later; do not block first runtime decision on having 500 units immediately

---

## Do not reintroduce without new evidence
- global/shared list constructor patching
- blanket every-90 patching
- candidate capacity patch
- permanent type-0 suppression
- old EventBuffer1024 implementation
- permanent `0x5A7B97` suppression
- in-function Sleep throttle
- heavy selected-unit polling scans

## Current hinge
Runtime-test the pinned **Selection500 + Fast Peasant** build. The Selection120 + Headroom160 integrated predecessor is locked as runtime-proven baseline.