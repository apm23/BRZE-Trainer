# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules

- **GitHub source / pinned build / runtime results are authoritative.**
- This file is the cross-chat forensic ledger.
- Distinguish observation, static proof, runtime proof, inference, and hypothesis.
- One hypothesis -> smallest patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Do not reintroduce disproven/broad patches without new evidence.

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

### Local AddUnit/event producer

`0x5A6FD8`:

1. append UI list around `0x5A70B6`
2. set `unit+0x3A8=1`
3. call `0x552FFA` at `0x5A70D0`

`0x552FFA` emits a 4-byte event type **0** containing unit ID.

### Corrected event dispatcher mapping

Jump table VA `0x5609C2`.

Relevant cases:

- type 0 -> `0x56016B` -> resolve unit -> `0x5A736F(player, unit)`
- type 1 -> `0x560198` -> `0x5A73A9(player, unit)`
- type 2 -> `0x5601C5` -> `0x55437F`
- type 3 -> `0x5601E5` -> `0x5543CA`

**Correction:** earlier notes treated `0x5543CA -> 0x5A75B2` as type-0. That was wrong. Real selection-add type-0 consumer is `0x56016B -> 0x5A736F`.

### Simulation add `0x5A736F`

For each type-0 event:

1. calculate `[0x841730] + player*0x28`
2. membership check via `0x4AC0BA`
3. append if absent via `0x4ACB59`
4. set `unit+0x3AC=1`
5. always call `0x5A7B97(player)` at VA `0x5A739E`

Call bytes at `0x5A739E`: `E8 F4 07 00 00`.

`0x5A7B97` performs non-trivial downstream selection/group synchronization and iterates state; calling it once per unit is the leading bulk-burst suspect.

References:

- `reference/current-brze/bulk-drag-event0-refresh-analysis-20260909.md`
- `reference/current-brze/no-add-notify-runtime-and-sim-selection-20260909.md`

---

# Runtime history

## Surgical Growth

- growth 0->90, manual cap 90
- stable at 90, #91 rejected

## Manual 120 — FAILED

- branch `selection-capacity-manual-120`
- run `34343788268`
- EXE `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`
- #91 crashes

## First-Block 120 — FAILED/incomplete

- run `34347542420`
- #91 crashes; later found UI sort could restore first=90

## Sort-Pipeline 120 — FAILED in two modes

- run `34348488434`
- EXE `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`
- large drag ~80 freezes
- slow #91 visibly selects then crashes

## No-Add-Notify 120 — diagnostic success >90, semantics broken

- run `34349623960`
- EXE `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`
- reached 108 UI-selected
- group drag degraded to one unit
- right-click move/attack broken
- proves event path is authoritative; permanent NOP invalid

## Simulation Pipeline 120 — ~70% SUCCESS

- branch `selection-sim-pipeline-120-probe`
- built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`
- run `34352052932` SUCCESS
- EXE `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`

Patch: UI/sort/simulation first=120, growth=0, type-0 event LIVE.

Runtime:

- multiple small rectangle drags can build a large working selection
- >90 + normal orders materially work
- one large rectangle drag still immediately freezes

Conclusion: 90-boundary problem materially solved; remaining bug is one-shot bulk/burst path.

## EventBuffer-1024 + Sim120 — FAILED TO FIX BULK DRAG

- branch `selection-event-buffer-1024-probe`
- built head `1f3dc34285ee415a3d5cb8132b2a9f19ec27e210`
- run `34358214184` SUCCESS
- EXE `c797463a44a7c283747a36f0af2827c409536a83f386bd9a4b6cd9a00d45803b`
- real event backing buffer widened 256 -> 1024
- runtime: large drag still immediately freezes

Proof: 256-byte rollover is not sufficient to explain freeze. Do not carry buffer-1024 into next probe.

Reference: `reference/current-brze/eventbuffer1024-runtime-failure-20260909.md`.

---

## Rectangle path static proof

Function around `0x5D4356`:

- candidate list `0x879748`
- candidate append `0x5D44B5`
- candidate constructor first=128, growth=128
- iterate candidates and call AddUnit `0x5A6FD8` at `0x5D455A`
- clear candidate list `0x5D4573`
- UI sort/rebuild `0x5A719F` at `0x5D457D`

Candidate capacity is not the ~80 freeze cause. Small drags reaching >100 also call the same final sort, so final UI sort itself is not leading suspect.

---

# CURRENT DECISIVE PROBE — NO PER-ADD REFRESH

Goal: keep clean working Sim120 architecture and all authoritative selection-add events/list appends, while testing whether repeated `0x5A7B97` refresh is the one-shot burst freeze.

Branch:

- `selection-no-per-add-refresh-probe`

Base:

- exact clean Sim120 built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`

Commits:

- source logic `d9bcc0290c7687a3edc662a5beaa60e4a4a73946`
- workflow `328a7e7981f15496fcf01009fcdea4d409f7b209`
- built head/trigger `13b857c92c7fcf8af166d3dee8a4cd842c540a45`

Actions:

- run `34359923529` — **SUCCESS**
- compile smoke success
- x86 publish success
- artifact upload success

Artifact:

- `BRZE-Selection-No-Per-Add-Refresh-Probe`
- artifact ID `10107440081`
- ZIP SHA-256 `a9cc2e8c1d2396cfa87e1b21faf3776fd510f63e9ae3a1213cd60959fdfcec05`
- EXE SHA-256 `853ec688772da02fbd244a848803588dbfed4bda114eb340247f356b0e3a2e98`

Exact differential from clean Sim120:

- type-0 event remains LIVE
- simulation membership lookup remains LIVE
- simulation list append remains LIVE
- simulation selected flag remains LIVE
- UI/sort/simulation capacities remain 120/growth0
- **only call at VA `0x5A739E` to `0x5A7B97` is NOPed** (`E8 F4 07 00 00` -> five NOPs)
- remove-side refresh untouched
- no EventBuffer-1024

Status target:

- `ARMED no-per-add-refresh`
- `event0:LIVE`
- `sim-add-refresh:NOP`
- ACTIVE/SIM first=120 grow=0

Runtime test:

1. fresh BRZE, enable F4 before selection
2. one large rectangle drag (~80-100) immediately
3. if stable, test right-click move and attack
4. inspect whether ACTIVE and SIM counts follow

Interpretation:

- stable + commands work -> per-add refresh storm confirmed; next implementation must call refresh once per batch/deferred rather than suppress permanently
- stable but some UI/group state stale -> same cause confirmed; add one batch-end refresh
- still freezes -> investigate type-0 membership lookup/append burst or dispatcher scheduling, not capacity/buffer rollover

---

## Do not reintroduce without evidence

- global/shared list constructor patching
- patch every constant 90
- candidate list capacity patch
- permanent event type-0 suppression
- EventBuffer-1024 by default
- heavy per-frame selected-unit scans
- multiple unrelated changes per probe

Use disposable saves and restart BRZE between invasive probes.

## Handoff

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Read this file first.

**Current unresolved hinge:** runtime behavior of `BRZE-Selection-No-Per-Add-Refresh-Probe` on one large rectangle drag and subsequent move/attack.