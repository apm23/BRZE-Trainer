# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules

- **GitHub source / pinned build / runtime results are authoritative.**
- This file is the cross-chat forensic ledger.
- Chat is discussion context, not the sole source of truth.
- Distinguish observation, static proof, runtime proof, inference, and hypothesis.
- One hypothesis -> smallest patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Do not reintroduce broad/disproven patches without new evidence.

## Authoritative current BRZE target

- filename: `Battle_Realms_F(5).exe`
- PE32 / x86
- size: `4,521,984` bytes
- preferred image base: `0x400000`
- SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- fingerprint: `reference/current-brze/target-binary-20260909.md`

Legacy references only:

- old trainer SHA-256: `41571fc8cd83e296a60a04d934440b5a01d22ce45d111acdc31b13f16e8aa24d`
- matching WOTW SHA-256: `6217a30325c4f84ba3c44965051979d891d468e9b2374686be6b7aab82403666`

Never copy legacy addresses blindly into current BRZE.

---

## Selection architecture — proven map

### Local/UI list

- VA `0x841708`, RVA `0x441708`
- list layout: head +0x00, tail +0x04, free +0x08, count +0x18, blocks +0x1C, first +0x20, growth +0x24
- stock first=90, growth=0
- manual cap immediate: VA `0x5A7006`, RVA `0x1A7006`

### UI sort/rebuild

Function `0x5A719F`, hardcoded first=90 sites:

- temp A `0x5A71B1` / RVA `0x1A71B1`
- temp B `0x5A71B9` / RVA `0x1A71B9`
- active rebuild `0x5A7299` / RVA `0x1A7299`

### Per-player simulation selection lists

- global pointer VA `0x841730`, RVA `0x441730`
- 10 lists, stride `0x28`
- same container layout
- init first=90 immediate `0x5A6C53`, RVA `0x1A6C53`
- reset first=90 immediate `0x5A6E37`, RVA `0x1A6E37`

### Local AddUnit / event producer

`0x5A6FD8`:

1. append local/UI list around `0x5A70B6`
2. set `unit+0x3A8 = 1`
3. call event producer `0x552FFA` at `0x5A70D0`

`0x552FFA` emits event type **0**, size 4 bytes: type word + unit ID word.

### Corrected event dispatch mapping — IMPORTANT

The event consumer dispatcher uses jump table at VA `0x5609C2` for event types 0..0x33.

Relevant proven cases:

- **type 0 -> `0x56016B` -> resolves unit -> calls `0x5A736F(player, unit)`**
- **type 1 -> `0x560198` -> calls `0x5A73A9(player, unit)`** (remove)
- **type 2 -> `0x5601C5` -> calls handler `0x55437F`** (clear/reset selection)
- **type 3 -> `0x5601E5` -> calls `0x5543CA`** (different selection-related event; previous notes incorrectly treated this as type 0)

Correction to earlier interpretation: `0x5543CA -> 0x5A75B2` is **not** the event-type-0 add-selected-unit handler. The real type-0 consumer is `0x56016B -> 0x5A736F`.

### Simulation add routine `0x5A736F` — critical bulk-drag finding

For each event-type-0 selected unit:

1. computes per-player list = `[0x841730] + player*0x28`
2. checks membership via `0x4AC0BA`
3. if absent, appends via `0x4ACB59`
4. sets `unit+0x3AC = 1`
5. **always calls `0x5A7B97(player)` at VA `0x5A739E`**

The call instruction at `0x5A739E` is `E8 F4 07 00 00` (call `0x5A7B97`).

`0x5A7B97` performs non-trivial downstream selection/group synchronization and itself iterates selection/group state. Repeating it once per added unit creates a strong burst-cost candidate for a one-shot rectangle selection.

---

# Runtime experiment history

## 1. Surgical Growth Probe

- active growth 0->90
- manual cap stayed 90
- stable at 90; #91 rejected

## 2. Manual 120 — FAILED

- branch `selection-capacity-manual-120`
- run `34343788268`
- EXE `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`
- #91 crashes

## 3. First-Block 120 — FAILED / incomplete

- branch `selection-firstblock-120-probe`
- run `34347542420`
- #91 crashes; later found UI sort could restore first=90

## 4. Sort-Pipeline 120 — FAILED in two modes

- branch `selection-sort-pipeline-120-probe`
- run `34348488434`
- EXE `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`
- large drag ~80 freezes
- slow #91 visibly selects then crashes

## 5. No-Add-Notify 120 — diagnostic >90 success, semantics broken

- branch `selection-no-add-notify-120-probe`
- run `34349623960`
- EXE `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`
- reached 108 UI-selected
- group drag degraded to one unit
- right-click move/attack broken
- proved selection event path is authoritative and cannot simply be NOPed

## 6. Simulation Pipeline 120 — ~70% SUCCESS

- branch `selection-sim-pipeline-120-probe`
- built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`
- run `34352052932` success
- EXE `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`

Patch:

- UI/sort first=120, growth=0
- simulation init/reset/current local list first=120, growth=0
- event type 0 remains LIVE

Runtime:

- multiple small rectangle drags can accumulate a large working selection
- >90 selection + normal commands materially work
- one large rectangle drag still immediately freezes

Interpretation: 90-boundary problem materially solved; remaining bug is bulk/burst-specific.

## 7. Event Buffer 1024 + Sim120 — FAILED TO FIX BULK DRAG

- branch `selection-event-buffer-1024-probe`
- built head `1f3dc34285ee415a3d5cb8132b2a9f19ec27e210`
- run `34358214184` success
- artifact `BRZE-Selection-EventBuffer-1024-Probe`
- EXE `c797463a44a7c283747a36f0af2827c409536a83f386bd9a4b6cd9a00d45803b`

Differential:

- retained proven Sim120 pipeline
- replaced native 256-byte event backing buffer with real 1024-byte allocation
- widened all known 0x100 native size/reset constants consistently

Runtime reported by user:

- **one large drag still immediately freezes**

Conclusion / proof label:

- 256-byte event-buffer rollover is **not sufficient to explain the bulk-drag freeze**
- do not carry EventBuffer-1024 into the next specimen unless new evidence requires it
- return to clean Simulation-Pipeline-120 base for subsequent probes

---

## Rectangle/drag path — static proof

Function around `0x5D4356`:

- candidate list `0x879748`
- candidate append around `0x5D44B5`
- candidate list uses default constructor first=128, growth=128
- iterates candidate list and calls AddUnit `0x5A6FD8` around `0x5D455A`
- clears candidate list around `0x5D4573`
- calls UI sort/rebuild `0x5A719F` around `0x5D457D`

Because small drags can accumulate >100 and each drag still invokes the same final sort/rebuild, the sort function itself is not the leading explanation for the one-shot freeze.

Large one-shot drag differs mainly by producing many type-0 events in one simulation update. Each event invokes `0x5A736F`, and every `0x5A736F` invokes heavy refresh `0x5A7B97`.

---

# CURRENT NEXT PROBE — SUPPRESS PER-ADD `0x5A7B97` REFRESH

Base: clean `selection-sim-pipeline-120-probe` / built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`.

Hypothesis:

- large drag freezes because event type 0 is processed in a burst and `0x5A7B97(player)` is redundantly executed once for every selected unit in that burst
- small drags work because the same expensive work is distributed over multiple ticks

Minimal differential to test:

- keep event type 0 LIVE
- keep simulation list append in `0x5A736F` LIVE
- keep selected flag update LIVE
- NOP only the 5-byte call at VA `0x5A739E` (`E8 F4 07 00 00`) that invokes `0x5A7B97` after each add
- do not touch remove-side refresh yet
- do not keep EventBuffer-1024 in this probe

Expected diagnostic outcomes:

- if large drag becomes stable and commands still work: per-add refresh storm is the bulk-freeze cause; next step is to restore refresh once per batch instead of once per unit
- if large drag becomes stable but some selection/group UI state is stale: same cause confirmed; implement one deferred/batch-end refresh
- if large drag still freezes: move deeper into event type-0 membership lookup/append burst or dispatcher processing, not capacity and not buffer rollover

---

## Do not reintroduce without evidence

- global/shared list constructor patching
- patching every constant 90
- candidate-list capacity patch (already 128/128)
- permanent suppression of event type 0
- EventBuffer-1024 as default after runtime failure
- heavy per-frame selected-unit scans
- multiple unrelated subsystem changes per probe

Use disposable saves and restart BRZE fully between invasive probes.

## New-chat handoff

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Read this file first.

**Current unresolved hinge:** runtime result of a clean Sim120-based specimen with only the per-add `call 0x5A7B97` at `0x5A739E` suppressed.