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

## Active/UI selection container — static proof

Selected-unit list: VA `0x841708`, RVA `0x441708`.

Container layout:

- `+0x00` head
- `+0x04` tail
- `+0x08` free-node head
- `+0x18` selected count
- `+0x1C` allocated block count
- `+0x20` first block size
- `+0x24` growth block size

Node layout: next `+0x00`, previous `+0x04`, unit pointer `+0x08`.

Allocator:

- `0x4ACB2D` consumes free node and calls `0x4AC4C1` if empty.
- `0x4AC4C1` uses `+0x20` for first allocation, `+0x24` for later allocations.
- stock active list = `first=90, growth=0`.

Manual admission guard:

- `0x5A7000: cmp dword ptr [0x841720], 0x5A`
- immediate VA `0x5A7006`, RVA `0x1A7006`
- stock cap 90.

Manual AddUnit `0x5A6FD8`:

1. append to `0x841708` around `0x5A70B6` via `0x4ACB59`
2. set `unit+0x3A8 = 1`
3. call selection-add event producer `0x552FFA` from VA `0x5A70D0` / RVA `0x1A70D0`.

---

## Native UI sort/rebuild — static proof

`0x5A719F` rebuilds active selection through:

- temp A `0x841784`
- temp B `0x8417AC`

Hardcoded first=90 immediates:

- temp A `0x5A71B1` / RVA `0x1A71B1`
- temp B `0x5A71B9` / RVA `0x1A71B9`
- active rebuild `0x5A7299` / RVA `0x1A7299`

Therefore a one-time active `+0x20=120` is not persistent unless these reset sites are also handled.

---

## Selection-add event / authoritative path — static + runtime proof

`0x552FFA` emits a **4-byte type-0 selection-add event** containing the unit ID.

Consumer around `0x5543CA` resolves the unit and calls simulation-side selection logic around `0x5A75B2(player, unit)`.

Runtime proof from No-Add-Notify probe:

- suppressing this call allowed **108 UI-selected units**
- but rectangle group selection degraded to one unit
- right-click move/attack stopped working even for selected units
- autonomous reactions still worked

Therefore the event is required for authoritative/simulation selection. Permanent NOP is invalid.

---

## Simulation-side per-player selection containers — critical static proof

Global pointer: VA `0x841730`, RVA `0x441730`.

- 10 per-player selection containers
- stride `0x28`
- local simulation list = `[0x841730] + localPlayerId * 0x28`
- local player ID = VA `0x8416D0`, RVA `0x4416D0`

Same container layout as UI list.

Stock simulation list configuration: `first=90, growth=0`.

Hardcoded 90 sites:

- init immediate VA `0x5A6C53`, RVA `0x1A6C53`
- reset immediate VA `0x5A6E37`, RVA `0x1A6E37`

This explains the old #91 sequence: UI accepts #91, event is emitted, simulation consumes it, simulation list still capped at 90, then game crashes.

Reference: `reference/current-brze/no-add-notify-runtime-and-sim-selection-20260909.md`.

---

# Runtime experiment history

## 1. Surgical Growth Probe

- active growth `0 -> 90`
- manual cap remained 90
- stable at selected=90
- #91 rejected

Conclusion: growth write itself is harmless before overflow; second allocation not tested.

## 2. Manual 120 — FAILED

- branch `selection-capacity-manual-120`
- source `d3363c0c649920755c5f152f6a8fe2147bef8866`
- run `34343788268`
- EXE SHA-256 `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`
- growth 0->90 + manual cap 120
- runtime: #91 crashes.

## 3. First-Block 120 / no growth — FAILED, later found incomplete

- branch `selection-firstblock-120-probe`
- run `34347542420`
- EXE SHA-256 `aa412ad2ad132d03220dc9ae9e04507a31712b09a0719eb72a4ace20fde1f441`
- runtime: #91 crashes.
- correction: native sort/rebuild could restore active first=90, so this was not a persistent 120 pipeline.

## 4. Sort-Pipeline 120 — FAILED IN TWO MODES

- branch `selection-sort-pipeline-120-probe`
- built head `3123b0eb32490f582ef2ddd4ef0c8676bf6840aa`
- run `34348488434`
- EXE SHA-256 `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`

Runtime:

1. one large rectangle/shift-drag around ~80 units -> gameplay freeze below 90
2. slow 1..90 works; #91 visibly selected ~0.3 s, then process crash

Proof: local append succeeds before downstream fatal processing; rectangle path has an additional bulk-only problem.

## 5. No-Add-Notify 120 — DIAGNOSTIC SUCCESS ABOVE 90, COMMANDS BROKEN

- branch `selection-no-add-notify-120-probe`
- built head `832815431be30ef3ec6ac8a6bc4a4909e1657df9`
- run `34349623960`
- EXE SHA-256 `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`

Runtime:

- 108 units successfully UI-selected
- rectangle group selection effectively became one unit
- right-click move/attack unavailable

Proof: event path is required for simulation/command semantics.

## 6. Simulation Pipeline 120 — ~70% RUNTIME SUCCESS

Build:

- branch `selection-sim-pipeline-120-probe`
- source logic `cb3a73eab81e289d521ea7a1b47b69dc6da1fd04`
- built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`
- run `34352052932` — SUCCESS
- artifact `BRZE-Selection-Sim-Pipeline-120-Probe`, ID `10104163056`
- ZIP SHA-256 `0a15006d2970c30c3c662c79fc56dda2518db03ac929a581021c6935c21dd444`
- EXE SHA-256 `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`

Patch set:

- UI active first 120, growth 0
- manual cap 120
- sort temp A/B + active rebuild first 120
- simulation init/reset first 120
- local instantiated simulation list first 120 while pristine
- simulation growth 0
- selection-add event remains LIVE

Latest runtime result reported by user:

- building a large selection via **multiple small rectangle drags works**
- this is approximately **70% successful** relative to desired behavior
- one **large rectangle drag in a single batch still freezes gameplay**

Interpretation:

- UI/sort/simulation 90-limit problem is materially solved
- remaining failure is isolated to bulk rectangle/batched event handling, not general >90 selection

Reference: `reference/current-brze/runtime-result-sim-pipeline120-20260909.md`.

---

## Rectangle/drag path — static proof

Path around `0x5D4356`:

- candidate global list `0x879748`
- append candidate around `0x5D44B5`
- iterate candidates
- call same AddUnit `0x5A6FD8` around `0x5D455A`
- clear candidate list around `0x5D4573`
- then sort/rebuild.

Important correction: candidate list `0x879748` uses default constructor `0x4ABFBF`, giving:

- first block = `0x80` = **128**
- growth block = `0x80` = **128**

Therefore a freeze around ~80 is **not explained by candidate-list capacity**, and that list should not be patched merely because drag freezes.

---

## Event buffer — current bulk-drag hypothesis

Global event-buffer state:

- used bytes `0x841C94`
- remaining bytes `0x841C98`
- buffer pointer `0x841C9C`
- event gate `0x841194`

Native allocation is **0x100 = 256 bytes** around `0x550CCB`.

Selection-add event size = **4 bytes** per admitted unit.

Therefore one batch crosses the buffer boundary after roughly **64 selection-add events**.

`0x552FFA` checks remaining space and calls `0x561B2A` when fewer than 4 bytes remain. Known native 0x100 size/reset constants:

- constructor size byte VA `0x550CCD`, RVA `0x150CCD`
- initialization remaining-size byte VA `0x561771`, RVA `0x161771`
- post-flush/reset size byte VA `0x561D78`, RVA `0x161D78`

Small-area drags can stay below this rollover point and let processing occur between batches; one large drag necessarily forces a flush/rollover in the middle of the rectangle AddUnit loop. This matches the current runtime split and is the present hypothesis.

Reference: `reference/current-brze/event-buffer-bulk-drag-20260909.md`.

---

# CURRENT DECISIVE EXPERIMENT — EVENT BUFFER 1024 + SIM 120

Goal: preserve the now-working UI/sort/simulation 120 pipeline and **LIVE selection events**, while preventing the native 256-byte event queue from rolling over in the middle of one large rectangle drag.

Branch:

- `selection-event-buffer-1024-probe`

Commits/build:

- source logic `4c96ace28aa33d52bfe9fbd526af19d4a541f2ed`
- workflow update `29a677eeabd2189c38830a9b1fa6d69ade8c844d`
- built head/trigger `1f3dc34285ee415a3d5cb8132b2a9f19ec27e210`
- Actions run `34358214184` — **SUCCESS**
- compile smoke success
- x86 publish success
- artifact upload success

Artifact:

- `BRZE-Selection-EventBuffer-1024-Probe`
- artifact ID `10106732130`
- ZIP SHA-256 `e22b552c29009479483ff4bbde5c8a3ae8455a39bbec7c5cf49d4e3cdd57ec3a`
- EXE SHA-256 `c797463a44a7c283747a36f0af2827c409536a83f386bd9a4b6cd9a00d45803b`

Exact differential from Simulation-Pipeline-120:

- retains UI active/sort/simulation selection 120
- retains growth=0 for active and simulation lists
- retains post-add event `0x552FFA` LIVE
- does **not** patch rectangle candidate-list capacity
- replaces the 256-byte event backing buffer with a **real 1024-byte allocation** obtained from BRZE's own allocator `0x72FC28`
- buffer swap only occurs while event queue `used=0`
- event emission is temporarily gated during pointer/state swap
- then updates pointer, used=0, remaining=1024
- patches all three known native 0x100 size/reset constants consistently from 0x100 to 0x400
- old 256-byte block is deliberately left alone for this diagnostic; do not free it from outside BRZE while native ownership is uncertain

Status should report:

- `ARMED eventbuf1024+sim120`
- cap `0x78`
- sort `78/78/78`
- sim-code `78/78`
- `add-notify:LIVE`
- EVENT target `1024`, free near `1024` while idle
- ACTIVE first=120 grow=0
- SIM first=120 grow=0
- DRAG-CAND first=128 grow=128

### Required runtime test

Fresh BRZE process. Enable F4 before selecting anything.

**Primary test:** immediately use one large rectangle/shift-drag covering roughly 80–100 units in a single operation.

If stable:

1. right-click move the whole group
2. issue right-click attack
3. verify ACTIVE and SIM counts follow
4. continue toward ~108/120 if desired

### Interpretation

- **Large one-shot drag becomes stable:** strongly supports mid-loop 256-byte event-buffer rollover/flush as the remaining bulk-selection freeze cause. This becomes candidate basis for final selection fix.
- **Large drag still freezes:** do not enlarge candidate capacity; move investigation into `0x561B2A` flush semantics or another synchronous batch-only consumer while keeping the proven UI/simulation 120 pipeline intact.

---

## Do not reintroduce without evidence

- shared/global list constructor capacity patching
- patching every constant 90 merely because it matches
- patching `0x879748` capacity when it is already static-proven 128/128
- permanent suppression of `0x552FFA`
- heavy per-frame/per-tick selected-unit scans
- multiple unrelated subsystem changes in one diagnostic build
- treating CI/build success as runtime proof

Use disposable test saves and restart BRZE fully between invasive selection probes.

## New-chat handoff

Use:

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Then read `MASTER_STATE.md` from `apm23/BRZE-Trainer` before taking action.

**Current unresolved hinge:** runtime result of `BRZE-Selection-EventBuffer-1024-Probe` on one large rectangle/shift-drag (~80–100 units) with normal right-click move/attack afterward.
