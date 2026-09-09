# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules
- GitHub source / pinned build / runtime results are authoritative.
- Distinguish static proof, runtime proof, inference, hypothesis.
- One hypothesis -> minimal patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Preserve failed experiments; do not reintroduce disproven broad patches.
- Use disposable saves and restart BRZE between invasive probes.

## Authoritative target
- `Battle_Realms_F(5).exe`
- PE32/x86, size `4,521,984`, image base `0x400000`
- SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- reference: `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only.

---

# Proven selection architecture

## Local/UI selection
- list VA `0x841708`, RVA `0x441708`
- layout: free `+0x08`, count `+0x18`, blocks `+0x1C`, first `+0x20`, growth `+0x24`
- stock first=90, growth=0
- manual admission immediate VA `0x5A7006`, RVA `0x1A7006`

## UI sort/rebuild `0x5A719F`
Hardcoded first=90 sites:
- temp A `0x5A71B1` / RVA `0x1A71B1`
- temp B `0x5A71B9` / RVA `0x1A71B9`
- active rebuild `0x5A7299` / RVA `0x1A7299`

## Simulation selection
- global pointer VA `0x841730`, RVA `0x441730`
- 10 per-player lists, stride `0x28`, same list layout
- init first=90 immediate `0x5A6C53`, RVA `0x1A6C53`
- reset first=90 immediate `0x5A6E37`, RVA `0x1A6E37`

## Selection event path
Local AddUnit `0x5A6FD8`:
1. append UI list around `0x5A70B6`
2. set `unit+0x3A8=1`
3. call event producer `0x552FFA` at `0x5A70D0`

`0x552FFA` emits a 4-byte type-0 selection-add event.

Dispatcher:
- type 0 -> `0x56016B` -> `0x5A736F(player, unit)`
- type 1 -> `0x560198` -> `0x5A73A9(player, unit)`
- type 2 -> `0x5601C5` -> `0x55437F`
- type 3 -> `0x5601E5` -> `0x5543CA`

Simulation add `0x5A736F`:
- membership check `0x4AC0BA`
- append `0x4ACB59`
- set `unit+0x3AC=1`
- refresh `0x5A7B97(player)` at `0x5A739E`

## Event queue
- used bytes VA `0x841C94`, RVA `0x441C94`
- remaining bytes VA `0x841C98`, RVA `0x441C98`
- pointer VA `0x841C9C`, RVA `0x441C9C`
- physical/native backing = 256 bytes
- `0x552FFA` writes 4 bytes per add event and requests flush via `0x561B2A` when needed
- logical remaining init immediate at RVA `0x161770`
- logical remaining post-flush reset immediate at RVA `0x161D77`

## Rectangle selection `0x5D4356`
- candidate list `0x879748` / RVA `0x479748`
- native candidate capacity first=128, growth=128
- candidate append around `0x5D44B5`
- accepted candidate AddUnit call `0x5D455A -> 0x5A6FD8`
- candidate clear `0x5D4573`
- final UI sort `0x5D457D -> 0x5A719F`

---

# Runtime experiment history

## Surgical Growth
- growth 0->90, manual cap remained 90
- stable through 90, #91 rejected

## Manual 120 — FAILED
- run `34343788268`
- #91 crashed

## First-Block 120 — FAILED/incomplete
- run `34347542420`
- #91 crashed; later found sort could restore first=90

## Sort-Pipeline 120 — FAILED
- run `34348488434`
- one large drag ~80 froze
- slow #91 visibly selected then crashed

## No-Add-Notify 120 — diagnostic only
- run `34349623960`
- UI reached 108 selected
- multi-drag degraded and move/attack broke
- proves type-0 event path is required for authoritative selection

## Simulation Pipeline 120 — PARTIAL SUCCESS
- branch `selection-sim-pipeline-120-probe`
- built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`
- run `34352052932` SUCCESS
- EXE SHA-256 `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`
- UI/sort/simulation first=120, growth=0; event type-0 LIVE
- >90 selection and normal move/attack worked when accumulated via small drags
- one-shot large rectangle drag still froze

## EventBuffer-1024 — FAILED implementation; old interpretation withdrawn
- run `34358214184`
- large drag still froze
- do not reuse unchanged; later telemetry proved the native logical boundary is central

## No-Per-Add-Refresh — FAILED
- run `34359923529`
- suppressing `0x5A7B97` did not fix large drag

## Rectangle Sleep(1) throttle — FAILED
- fixed run `34363669046`
- large drag still froze
- only proved wall-clock delay inside same rectangle invocation does not help

---

# DECISIVE FAILURE TELEMETRY

Read-only observer:
- branch `selection-bulk-telemetry-observer`
- run `34365054121` SUCCESS
- artifact `10109550140`
- EXE SHA-256 `4f73f3e23b77f954fe424c8d48dd8a2b8e4a39b85b959e849b79028303cd761c`

With clean Sim-Pipeline-120, one-shot large drag froze at:
- `ACTIVE=64`
- `SIM=0`
- `EVENT used=258`
- `EVENT remain=0xFFFFFFFE` (-2)

Raw log/reference:
- `reference/current-brze/bulk-telemetry-freeze-64-20260909.md`
- `reference/current-brze/bulk-telemetry-freeze-64-log-20260909.txt`

Exact boundary explanation:
- rectangle clear-selection contributes 2 event bytes
- `2 + 63*4 = 254`, leaving 2 bytes in the native 256 logical window
- next 4-byte add requests native flush too late
- failing path continues with invalid counters -> `used=258`, `remain=-2`

This is runtime proof that the remaining bulk-drag freeze is an event-queue logical-window/flush-headroom failure, not selection-list capacity.

---

# RUNTIME-PROVEN FIX — EVENT HEADROOM 160

Probe:
- branch `selection-event-headroom-160-probe`
- built head `84026dd163f85f1121e4d5607a24b54abdeb5611`
- run `34367473165` SUCCESS
- artifact ID `10110502418`
- EXE SHA-256 `c25e9ac2962c5659d851e7db3bb722d06a36cd6d03d4023ad259643c0792002a`

Exact change:
- physical event backing remains native 256 bytes
- logical remaining/reset window changed from 256 to 160
- live remain armed to 160 while queue is empty
- init/reset immediate at `0x161770` -> 160
- post-flush reset immediate at `0x161D77` -> 160
- no event suppression, no buffer replacement, no throttle, no extra selection-capacity patch

Runtime success with Sim-Pipeline-120 + Headroom160 + observer:
- **one-shot large rectangle drag no longer froze**
- MAX ACTIVE reached `103`
- MAX SIM reached `103`
- event queue repeatedly flushed and returned to `used=0 / remain=160`
- SIM repeatedly caught up to ACTIVE (`39 -> 75`, later `39 -> 79 -> 103`)
- no underflow
- companion status: `ARMED headroom160`, reset `A0/A0`, physical backing 256
- network status in successful run: state=3, mode=2, maxPayload=256

Reference:
- `reference/current-brze/event-headroom160-runtime-success-20260909.md`

**Conclusion: logical event headroom 160 is runtime-proven to fix the bulk-drag freeze in the tested configuration while preserving authoritative simulation selection.**

---

# CURRENT NEXT CANDIDATE — SINGLE INTEGRATED EXE

Branch:
- `selection-120-headroom160-integrated`

Purpose:
- merge the proven Sim-Pipeline-120 selection changes and runtime-proven Headroom160 event fix into one F4-controlled trainer
- no 1024 buffer, no event NOP, no refresh NOP, no throttle

Source/project:
- `Integrated120Headroom160.cs`
- `Integrated120Headroom160.csproj`

Workflow:
- `.github/workflows/selection-120-headroom160-integrated.yml`
- head at workflow trigger: `b623167036bed91299f4cc8184bb2a2fd821ca34`
- Actions run `34368982513` currently building at time of this ledger update

Required regression after build:
1. fresh BRZE, integrated F4 before any selection
2. one-shot large drag 80-110
3. verify no freeze and ACTIVE/SIM sync
4. right-click move
5. right-click attack
6. cross 90 and test around 100-110

---

## Do not reintroduce without new evidence
- global/shared list constructor patching
- blanket every-90 patching
- candidate-list capacity patch
- permanent event type-0 suppression
- old EventBuffer-1024 implementation
- permanent `0x5A7B97` suppression
- in-function Sleep throttle
- heavy selected-unit polling scans

## Handoff
`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

**Current hinge:** finish Actions run `34368982513`, download the integrated EXE, then runtime-regression one-shot bulk drag + >90 move/attack. The two-process Sim120 + Headroom160 combination is already runtime-proven successful.