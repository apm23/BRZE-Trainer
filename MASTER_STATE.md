# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules

- **GitHub source / pinned build / runtime results are authoritative.**
- This file is the cross-chat forensic ledger.
- Distinguish observation, static proof, runtime proof, inference, and hypothesis.
- One hypothesis -> smallest patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Do not reintroduce disproven/broad patches without new evidence.
- Use disposable saves and restart BRZE between invasive probes.

## Authoritative target

- `Battle_Realms_F(5).exe`
- PE32/x86, size `4,521,984`
- image base `0x400000`
- SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- fingerprint: `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only.

---

## Proven selection architecture

### Local/UI selection

- list VA `0x841708`, RVA `0x441708`
- layout: head +0x00, tail +0x04, free +0x08, count +0x18, blocks +0x1C, first +0x20, growth +0x24
- stock first=90, growth=0
- manual cap immediate VA `0x5A7006`, RVA `0x1A7006`

### UI sort/rebuild `0x5A719F`

Hardcoded first=90:

- temp A `0x5A71B1` / RVA `0x1A71B1`
- temp B `0x5A71B9` / RVA `0x1A71B9`
- active rebuild `0x5A7299` / RVA `0x1A7299`

### Simulation selection

- global pointer VA `0x841730`, RVA `0x441730`
- 10 per-player lists, stride `0x28`
- same list layout
- init first=90 immediate `0x5A6C53`, RVA `0x1A6C53`
- reset first=90 immediate `0x5A6E37`, RVA `0x1A6E37`

### Local AddUnit / event producer

`0x5A6FD8`:

1. append UI list around `0x5A70B6`
2. set `unit+0x3A8=1`
3. call selection event producer `0x552FFA` at `0x5A70D0`

`0x552FFA` emits a 4-byte event type **0** containing unit ID.

### Corrected event dispatcher mapping

Jump table VA `0x5609C2`:

- type 0 -> `0x56016B` -> resolve unit -> `0x5A736F(player, unit)`
- type 1 -> `0x560198` -> `0x5A73A9(player, unit)`
- type 2 -> `0x5601C5` -> `0x55437F`
- type 3 -> `0x5601E5` -> `0x5543CA`

Earlier notes that treated `0x5543CA -> 0x5A75B2` as type-0 were wrong.

### Simulation add `0x5A736F`

For each type-0 event:

1. calculate `[0x841730] + player*0x28`
2. membership check via `0x4AC0BA`
3. append if absent via `0x4ACB59`
4. set `unit+0x3AC=1`
5. call `0x5A7B97(player)` at VA `0x5A739E`

Call bytes at `0x5A739E`: `E8 F4 07 00 00`.

---

## Rectangle selection path — static proof

Function `0x5D4356`:

- candidate list `0x879748`
- candidate append around `0x5D44B5`
- candidate list default constructor gives first=128, growth=128
- accepted candidates call the same AddUnit at `0x5D455A`
- candidate list clear `0x5D4573`
- UI sort/rebuild `0x5A719F` at `0x5D457D`

Important: candidate capacity is not the ~80 freeze cause. Small drags that eventually accumulate >100 also run the same final sort, so final UI sort itself is not the leading cause.

The rectangle-only accepted-unit call site is:

- VA `0x5D455A`, RVA `0x1D455A`
- original bytes `E8 79 2A FD FF` -> call `0x5A6FD8`
- immediately preceded by `push esi` at `0x5D4559`

This site is useful for rectangle-only throttling without touching manual single-click AddUnit.

---

# Runtime experiment history

## Surgical Growth

- active growth 0->90, manual cap stayed 90
- stable at 90, #91 rejected

## Manual 120 — FAILED

- branch `selection-capacity-manual-120`
- run `34343788268`
- EXE `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`
- #91 crashes

## First-Block 120 — FAILED / incomplete

- run `34347542420`
- #91 crashes; later found UI sort can restore first=90

## Sort-Pipeline 120 — FAILED in two modes

- run `34348488434`
- EXE `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`
- large drag ~80 freezes
- slow #91 visibly selects then crashes

## No-Add-Notify 120 — diagnostic >90 success, semantics broken

- run `34349623960`
- EXE `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`
- reached 108 UI-selected
- group drag degraded to one unit
- right-click move/attack broken
- proves event type-0 path is authoritative; permanent suppression invalid

## Simulation Pipeline 120 — ~70% SUCCESS

- branch `selection-sim-pipeline-120-probe`
- built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`
- run `34352052932` SUCCESS
- EXE `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`

Patch:

- UI/sort first=120, growth=0
- simulation init/reset/current local first=120, growth=0
- event type-0 LIVE

Runtime:

- multiple small rectangle drags can build a large working selection
- >90 + normal move/attack materially work
- one large rectangle drag still immediately freezes

Conclusion: the 90-boundary problem is materially solved. Remaining problem is one-shot rectangle/burst-specific.

## EventBuffer-1024 + Sim120 — FAILED TO FIX BULK DRAG

- branch `selection-event-buffer-1024-probe`
- built head `1f3dc34285ee415a3d5cb8132b2a9f19ec27e210`
- run `34358214184` SUCCESS
- EXE `c797463a44a7c283747a36f0af2827c409536a83f386bd9a4b6cd9a00d45803b`
- real event backing buffer widened 256 -> 1024
- runtime: one large drag still immediately freezes

Proof: native 256-byte event rollover is not sufficient to explain freeze. Do not carry EventBuffer-1024 into later probes by default.

## No-Per-Add-Refresh + Sim120 — FAILED TO FIX BULK DRAG

- branch `selection-no-per-add-refresh-probe`
- built head `13b857c92c7fcf8af166d3dee8a4cd842c540a45`
- run `34359923529` SUCCESS
- artifact `BRZE-Selection-No-Per-Add-Refresh-Probe`, ID `10107440081`
- EXE SHA-256 `853ec688772da02fbd244a848803588dbfed4bda114eb340247f356b0e3a2e98`

Differential from clean Sim120:

- event type-0 LIVE
- simulation membership check and append LIVE
- selected flag LIVE
- only `call 0x5A7B97` at `0x5A739E` NOPed

Runtime reported by user:

- **one large drag still immediately freezes**

Proof / conclusion:

- per-add `0x5A7B97` refresh is not sufficient to explain the bulk-drag freeze
- do not keep this NOP in the next specimen
- return to clean Sim120 base for further tests

---

# CURRENT NEXT PROBE — RECTANGLE ADDUNIT THROTTLE 1 MS

Branch:

- `selection-rectangle-throttle-1ms-probe`

Base:

- exact clean Sim120 built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`

Hypothesis:

- the remaining failure is caused by too many otherwise-valid AddUnit/type-0 operations produced essentially back-to-back inside one rectangle invocation
- the issue is timing/scheduling/burst pressure rather than persistent capacity, candidate capacity, event-buffer size, or the single per-add refresh call

Minimal design:

- keep original AddUnit `0x5A6FD8` semantics completely intact
- keep event type-0 LIVE
- keep simulation append/refresh LIVE
- patch only rectangle call site `0x5D455A`
- wrapper calls original AddUnit, then yields/sleeps about 1 ms before returning to the rectangle loop
- manual/single-click AddUnit path remains untouched
- no EventBuffer-1024
- no refresh NOP

Expected interpretation:

- large drag becomes stable -> burst timing/scheduling confirmed; optimize final throttle/yield frequency rather than changing selection semantics
- large drag still freezes -> continue into another rectangle-specific synchronous structure or dispatcher end-of-batch behavior; capacity/buffer/refresh hypotheses remain disproven

---

## Do not reintroduce without evidence

- global/shared list constructor patching
- blanket every-90 patching
- candidate-list capacity patch
- permanent event type-0 suppression
- EventBuffer-1024 by default
- permanent `0x5A7B97` suppression
- heavy per-frame selected-unit scans
- multiple unrelated changes in one probe

## Handoff

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Read this file first.

**Current unresolved hinge:** runtime result of clean Sim120 plus rectangle-only AddUnit throttle/yield probe.