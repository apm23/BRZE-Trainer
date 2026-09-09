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

User-supplied target:

- filename: `Battle_Realms_F(5).exe`
- PE32 / x86
- size: `4,521,984` bytes
- preferred image base: `0x400000`
- SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- fingerprint: `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only:

- old trainer SHA-256: `41571fc8cd83e296a60a04d934440b5a01d22ce45d111acdc31b13f16e8aa24d`
- matching legacy WOTW SHA-256: `6217a30325c4f84ba3c44965051979d891d468e9b2374686be6b7aab82403666`

Never copy legacy addresses blindly into current BRZE.

---

## Active/UI selection container — static proof

Selected-unit list:

- VA `0x841708`
- RVA `0x441708`

Container layout:

- `+0x00` head
- `+0x04` tail
- `+0x08` free-node head
- `+0x18` selected count
- `+0x1C` allocated block count
- `+0x20` first block size
- `+0x24` growth block size

Node layout:

- `+0x00` next
- `+0x04` previous
- `+0x08` unit pointer

Allocator:

- `0x4ACB2D` consumes a free node and calls `0x4AC4C1` if free list is empty
- `0x4AC4C1` uses container `+0x20` for the first allocation and `+0x24` for later allocations
- stock active list is configured `first=90, growth=0`

Manual admission guard:

- `0x5A7000: cmp dword ptr [0x841720], 0x5A`
- immediate byte: VA `0x5A7006`, RVA `0x1A7006`
- stock cap = 90

Manual AddUnit path `0x5A6FD8`:

1. append to active/UI list `0x841708` around `0x5A70B6` via `call 0x4ACB59`
2. set unit selected flag (`unit+0x3A8 = 1`)
3. call selection-add event producer `0x552FFA` from VA `0x5A70D0`, RVA `0x1A70D0`

---

## Native UI selection sort/rebuild — static proof

Function `0x5A719F` rebuilds active selection through two temporary lists:

- temp A: `0x841784`
- temp B: `0x8417AC`

Hardcoded first-block 90 sites:

- temp A immediate: VA `0x5A71B1`, RVA `0x1A71B1`
- temp B immediate: VA `0x5A71B9`, RVA `0x1A71B9`
- active rebuild immediate: VA `0x5A7299`, RVA `0x1A7299`

Therefore changing active `+0x20` only once is not persistent; `0x5A719F` can reconstruct it back to first=90/growth=0.

---

## Selection-add event / authoritative path — static + runtime proof

`0x552FFA` emits a 4-byte type-0 selection-add event containing the unit ID into the game's command/event buffer.

Event type-0 consumer is around `0x5543CA`:

- reads unit ID from event payload `+2`
- resolves unit pointer
- calls simulation-side selection logic around `0x5A75B2(player, unit)`

This event is **not cosmetic**. Runtime testing proved that suppressing it allows local/UI selection to rise above 90, but player movement/attack commands cease to work because authoritative simulation selection is no longer updated.

Command/event buffer notes:

- producer event size: 4 bytes
- buffer startup allocation around `0x550C98`: `0x100` bytes
- producer checks remaining space and uses `0x561B2A` for rollover/flush

The event buffer remains a secondary area to inspect if needed, but the current primary boundary finding is the simulation-side selection list below.

---

## Simulation-side per-player selection containers — CRITICAL STATIC FINDING

Global pointer:

- VA `0x841730`
- RVA `0x441730`

Structure:

- **10 selection containers**
- stride: `0x28`
- local player's simulation selection list: `[0x841730] + localPlayerId * 0x28`
- local player ID: VA `0x8416D0`, RVA `0x4416D0`

These containers use the same list layout (`+0x18` count, `+0x1C` blocks, `+0x20` first, `+0x24` growth).

### Initialization cap

BRZE initializes each simulation selection container with:

- first block = 90
- growth = 0

Hardcoded first-block immediate:

- VA `0x5A6C53`
- RVA `0x1A6C53`
- stock byte `0x5A`

### Reset cap

A reset loop reconstructs all 10 containers with first=90/growth=0.

Hardcoded reset immediate:

- VA `0x5A6E37`
- RVA `0x1A6E37`
- stock byte `0x5A`

### Current best explanation of #91 crash

Runtime + static evidence fit this sequence:

1. local/UI AddUnit accepts #91 and it becomes visibly selected
2. `0x552FFA` sends the selection-add event
3. simulation consumes event type 0 and appends #91 to the per-player simulation selection list
4. that simulation list is still native first=90/growth=0
5. BRZE crashes at/after simulation-side boundary

This explains why No-Add-Notify can display 108 selected units yet right-click orders do not work: UI list exceeds 90, but simulation selection never receives those events.

Detailed reference:

- `reference/current-brze/no-add-notify-runtime-and-sim-selection-20260909.md`

---

# Runtime experiment history

## 1. Surgical Growth Probe

Patch:

- active `+0x24` growth `0 -> 90`
- manual guard remained 90

Runtime:

- selected=90
- first=90
- growth=90
- no crash/freeze
- #91 rejected by manual cap

Conclusion: growth write itself is harmless before overflow. Did not test #91.

## 2. Manual 120 — FAILED

- branch: `selection-capacity-manual-120`
- source: `d3363c0c649920755c5f152f6a8fe2147bef8866`
- run: `34343788268`
- artifact: `BRZE-Selection-Manual-120`
- EXE SHA-256: `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`

Patch:

- active growth `0 -> 90`
- manual admission `90 -> 120`

Runtime:

- up to 90 works
- #91 crashes BRZE

## 3. First-Block 120 / no growth — FAILED, experiment later found incomplete

- branch: `selection-firstblock-120-probe`
- run: `34347542420`
- EXE SHA-256: `aa412ad2ad132d03220dc9ae9e04507a31712b09a0719eb72a4ace20fde1f441`

Runtime:

- #91 crashes

Correction:

- active first=120 was not maintained through native `0x5A719F` sort/rebuild, so this test did not fully test a persistent first-block 120 pipeline.

## 4. Sort-Pipeline 120 / no growth — FAILED IN TWO DISTINCT MODES

- branch: `selection-sort-pipeline-120-probe`
- source logic: `0b38f804d54b4374bc774cd05468f256efc729ef`
- built head: `3123b0eb32490f582ef2ddd4ef0c8676bf6840aa`
- run: `34348488434`
- artifact: `BRZE-Selection-Sort-Pipeline-120-Probe`
- EXE SHA-256: `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`

Patch:

- UI active first=120 while pristine
- growth=0
- manual cap=120
- temp A/B + active rebuild first-block immediates=120
- add-notify event LIVE

Runtime:

1. rectangle/shift-drag around ~80 units -> gameplay freeze even below 90
2. slow selection 1..90 works; #91 becomes visibly selected for ~0.3 s, then BRZE crashes

Important proof:

- local append of #91 succeeds before fatal downstream processing
- drag has an additional bulk-selection failure path

## 5. No-Add-Notify 120 — DIAGNOSTIC SUCCESS ABOVE 90, BUT COMMAND SEMANTICS BROKEN

- branch: `selection-no-add-notify-120-probe`
- source differential: `d9e9f374deaebe6bf6d26d63051de37600478336`
- built head: `832815431be30ef3ec6ac8a6bc4a4909e1657df9`
- run: `34349623960`
- artifact: `BRZE-Selection-No-Add-Notify-120-Probe`
- EXE SHA-256: `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`

Differential:

- same UI/sort 120 setup
- `call 0x552FFA` at `0x5A70D0` NOPed

Runtime reported by user:

- **108 units successfully selected**
- drag no longer selects a group; large rectangle selects only 1
- selected units cannot receive right-click move or attack orders
- units still behave autonomously/react to enemies

Runtime proof:

- local/UI list can exceed 90
- selection-add event is required for authoritative/simulation selection and normal orders
- permanent event suppression is not a valid final solution

---

# CURRENT DECISIVE EXPERIMENT — SIMULATION PIPELINE 120

Goal: keep the selection-add event **LIVE** while extending the actual simulation-side selection list that remained capped at 90.

Branch:

- `selection-sim-pipeline-120-probe`

Base:

- clean Sort-Pipeline-120 branch with event LIVE
- base commit `3123b0eb32490f582ef2ddd4ef0c8676bf6840aa`

Commits:

- source logic: `cb3a73eab81e289d521ea7a1b47b69dc6da1fd04`
- workflow update: `297813e9048bffd72196d79e29c8b09bdeecf393`
- built head/trigger: `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`

Workflow:

- run: `34352052932`
- conclusion: **SUCCESS**
- compile smoke: success
- x86 single-file publish: success
- artifact upload: success

Artifact:

- name: `BRZE-Selection-Sim-Pipeline-120-Probe`
- artifact ID: `10104163056`
- ZIP SHA-256: `0a15006d2970c30c3c662c79fc56dda2518db03ac929a581021c6935c21dd444`
- EXE SHA-256: `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`

## Exact patch set

UI/local pipeline:

- active first block `90 -> 120` only while allocator pristine
- active growth remains `0`
- manual cap `0x5A7006`: `90 -> 120`
- sort temp A `0x5A71B1`: `90 -> 120`
- sort temp B `0x5A71B9`: `90 -> 120`
- active rebuild `0x5A7299`: `90 -> 120`

Simulation pipeline:

- simulation 10-list init hardcode `0x5A6C53`: `90 -> 120`
- simulation 10-list reset hardcode `0x5A6E37`: `90 -> 120`
- local player's currently instantiated simulation container `+0x20`: `90 -> 120` only while pristine
- simulation growth remains `0`

Event semantics:

- post-add call `0x5A70D0 -> 0x552FFA` stays **LIVE**
- no NOP here

Deliberately absent:

- no global/shared container constructor patch
- no blanket patch of every constant 90
- no HP/stamina/F7 hooks
- no per-frame selected-unit scans
- no selection-event replacement/hook

## Required runtime test

Fresh BRZE process. Enable F4 **before selecting anything**.

Before testing, status should show:

- `ARMED sim-pipeline-120`
- `cap:0x78`
- sort `78/78/78`
- sim-code `78/78`
- `add-notify:LIVE`
- ACTIVE first=120, grow=0
- SIM first=120, grow=0

### Test A — slow selection first

Select gradually:

`89 -> 90 -> 91 -> 92 -> 100 -> 108`

If stable, immediately test:

- right-click move
- right-click attack

Watch status:

- `ACTIVE n` should rise
- `SIM n` should follow the authoritative selection state

### Test B — only after Test A succeeds

Use a separate fresh BRZE process.

- enable F4 before any selection
- rectangle/shift-drag a large group around 80 units

If slow >90 + commands work but drag still freezes, treat drag as a separate rectangle-path bug rather than a general selection-capacity failure.

---

## Rectangle/drag path — current secondary investigation

Path around `0x5D4356`:

- scans candidates
- appends candidate pointers to global temp list `0x879748` around `0x5D44B5`
- iterates candidates
- calls same AddUnit `0x5A6FD8` around `0x5D455A`
- clears candidate list around `0x5D4573`
- then sort/rebuild

`0x879748` is currently a **secondary suspect** for the drag-only freeze. Its exact effective capacity has not yet been runtime/static-proven sufficiently to patch it.

Do not touch it until Simulation-Pipeline-120 slow-selection result is known.

---

## Do not reintroduce without evidence

- shared/global constructor capacity patching
- patching every value 90 merely because it matches
- broad auxiliary-container changes
- permanent suppression of `0x552FFA`
- heavy per-frame/per-tick scans
- multiple unrelated subsystem changes in a single diagnostic build
- treating build/CI green as runtime proof

Use disposable test saves and restart BRZE fully between invasive selection probes.

## New-chat handoff

Use:

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Then read `MASTER_STATE.md` from `apm23/BRZE-Trainer` before taking action.

**Current unresolved hinge:** runtime result of `BRZE-Selection-Sim-Pipeline-120-Probe`, first on slow `90 -> 91 -> 108` with right-click command verification, then separately rectangle/shift-drag if the slow path succeeds.
