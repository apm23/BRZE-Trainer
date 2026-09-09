# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules
- GitHub source, pinned builds, and runtime results are authoritative.
- One hypothesis -> minimal patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Preserve failed experiments; do not reintroduce disproven broad patches.
- New thread handoff: `CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`.

## Authoritative target
`Battle_Realms_F(5).exe`
- PE32/x86, size `4,521,984`, image base `0x400000`
- SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- reference `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only.

---

# Selection architecture — proven

## Local/UI
- list VA `0x841708`, RVA `0x441708`
- free `+0x08`, count `+0x18`, blocks `+0x1C`, first `+0x20`, growth `+0x24`
- stock first=90, growth=0
- manual admission immediate `0x5A7006` / RVA `0x1A7006`

## Sort/rebuild `0x5A719F`
Hardcoded first=90 sites:
- temp A `0x5A71B1` / RVA `0x1A71B1`
- temp B `0x5A71B9` / RVA `0x1A71B9`
- active `0x5A7299` / RVA `0x1A7299`

## Simulation selection
- base pointer VA `0x841730`, RVA `0x441730`
- 10 per-player lists, stride `0x28`
- init first immediate `0x5A6C53` / RVA `0x1A6C53`
- reset first immediate `0x5A6E37` / RVA `0x1A6E37`

## Selection event path
Local AddUnit `0x5A6FD8`:
1. append UI list
2. set `unit+0x3A8=1`
3. call event producer `0x552FFA`

`0x552FFA` emits a 4-byte type-0 selection-add event.

Type-0 dispatcher -> `0x56016B` -> `0x5A736F(player, unit)`.
Simulation add checks membership, appends to per-player SIM list, sets `unit+0x3AC=1`, then calls `0x5A7B97(player)`.

## Rectangle selection `0x5D4356`
- candidate list `0x879748`, native first=128/growth=128
- accepted candidate AddUnit call `0x5D455A -> 0x5A6FD8`
- candidate clear `0x5D4573`
- final sort `0x5D457D -> 0x5A719F`

## Event queue
- used `0x841C94`
- remaining `0x841C98`
- pointer `0x841C9C`
- physical/native backing = 256 bytes
- logical remaining init immediate RVA `0x161770`
- logical remaining post-flush reset immediate RVA `0x161D77`
- producer requests native flush via `0x561B2A`

---

# Runtime history

## Failed/partial selection probes
- Surgical Growth: stable to 90, #91 rejected.
- Manual120 run `34343788268`: #91 crash.
- FirstBlock120 run `34347542420`: #91 crash; later found sort reset issue.
- SortPipeline120 run `34348488434`: large drag freeze; slow #91 selected briefly then crash.
- NoAddNotify120 run `34349623960`: UI reached 108 but drag semantics and move/attack broke; event path is required.

## Sim-Pipeline-120 — partial success
- branch `selection-sim-pipeline-120-probe`
- run `34352052932` SUCCESS
- EXE `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`
- UI/sort/SIM first=120, growth=0, event LIVE
- multiple small drags can build >90 working selection; move/attack works
- one-shot large drag still froze

## Disproven bulk-drag theories
- EventBuffer1024 run `34358214184`: failed; old implementation must not be reused.
- NoPerAddRefresh run `34359923529`: suppressing `0x5A7B97` did not fix freeze.
- Rectangle Sleep(1) fixed run `34363669046`: did not fix freeze.

---

# Decisive failure telemetry

Read-only observer:
- run `34365054121` SUCCESS
- artifact `10109550140`
- EXE `4f73f3e23b77f954fe424c8d48dd8a2b8e4a39b85b959e849b79028303cd761c`

With clean Sim120, one large drag froze at:
- ACTIVE=64
- SIM=0
- EVENT used=258
- EVENT remain=`0xFFFFFFFE` (-2)

References:
- `reference/current-brze/bulk-telemetry-freeze-64-20260909.md`
- `reference/current-brze/bulk-telemetry-freeze-64-log-20260909.txt`

Boundary explanation:
- rectangle clear-selection contributes 2 event bytes
- `2 + 63*4 = 254`, leaving 2 bytes in logical 256
- next 4-byte add reaches the flush boundary too late
- failed reset + continued producer write yields `258 / -2`

This is runtime proof that the one-shot bulk freeze is an event-queue logical-window/flush-headroom failure, not candidate capacity or SIM capacity.

---

# Runtime-proven fix — logical headroom 160

Probe:
- branch `selection-event-headroom-160-probe`
- run `34367473165` SUCCESS
- artifact `10110502418`
- EXE `c25e9ac2962c5659d851e7db3bb722d06a36cd6d03d4023ad259643c0792002a`

Exact change:
- physical event backing remains native 256
- logical remaining/reset window becomes 160
- live remain armed to 160 only while queue empty
- RVA `0x161770` -> 160
- RVA `0x161D77` -> 160
- no 1024 allocation, no event NOP, no refresh NOP, no throttle

Runtime success using Sim120 + Headroom160 + observer:
- **large one-shot rectangle selection no longer froze**
- MAX ACTIVE = 103
- MAX SIM = 103
- queue repeatedly flushed back to `used=0 / remain=160`
- SIM repeatedly caught up to ACTIVE, including final 103
- no underflow
- headroom status `A0/A0`, native physical 256
- NET state=3, mode=2, maxPayload=256

Reference:
- `reference/current-brze/event-headroom160-runtime-success-20260909.md`

**This two-process combination is runtime-proven successful.**

---

# Current candidate — SINGLE integrated EXE

Branch:
- `selection-120-headroom160-integrated`

Files:
- `Integrated120Headroom160.cs`
- `Integrated120Headroom160.csproj`
- `.github/workflows/selection-120-headroom160-integrated.yml`

Build pin:
- head `b623167036bed91299f4cc8184bb2a2fd821ca34`
- Actions run `34368982513` — SUCCESS
- compile smoke SUCCESS
- x86 single-file publish SUCCESS
- artifact upload SUCCESS
- artifact name `BRZE-Selection-120-Headroom160-Integrated`
- artifact ID `10111151679`
- ZIP SHA-256 `c47e2bdf4aa36d4996b8336efb1839cdb1ac14b875ab3688018e0704b9730456`
- EXE SHA-256 `5a13f24b7bc6b6dafbea9bf837e5037358c265f01594ea1c6bd934f23e85790c`
- EXE size `151,055,598` bytes

Reference:
- `reference/current-brze/selection120-headroom160-integrated-build-20260909.md`

Integrated behavior:
- one F4 controls max population + UI/SORT/SIM selection120 + logical headroom160
- event type-0 stays LIVE
- physical event backing stays native 256
- no 1024 buffer, event suppression, refresh suppression, or throttle

Required regression:
1. fresh BRZE
2. integrated F4 before selecting anything
3. one-shot large drag ~80-110
4. verify no freeze
5. verify ACTIVE/SIM converge
6. right-click move
7. right-click attack
8. verify stable >90 around 100-110

---

## Do not reintroduce without evidence
- global/shared list constructor patching
- blanket every-90 patching
- candidate capacity patch
- permanent type-0 suppression
- old EventBuffer1024 implementation
- permanent `0x5A7B97` suppression
- in-function Sleep throttle
- heavy per-frame selected-unit scans

## Current hinge
Runtime-regression the **single integrated EXE**. The underlying two-process Sim120 + Headroom160 combination is already runtime-proven successful.