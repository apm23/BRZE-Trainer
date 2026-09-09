# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules

- **GitHub source / pinned build / runtime results are authoritative.**
- This file is the cross-chat forensic ledger.
- Distinguish observation, static proof, runtime proof, inference, and hypothesis.
- One hypothesis -> smallest patch -> runtime test -> record result.
- Do not reintroduce broad/disproven patches without new evidence.

## Authoritative BRZE target

User-supplied current target:

- filename: `Battle_Realms_F(5).exe`
- PE32 / x86
- size: `4,521,984` bytes
- preferred image base: `0x400000`
- SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- note: `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only.

## Selection container — static proof

Active selected-unit list:

- VA `0x841708`, RVA `0x441708`
- `+0x00` head
- `+0x04` tail
- `+0x08` free-node head
- `+0x18` selected count
- `+0x1C` allocated block count
- `+0x20` first block size
- `+0x24` growth block size

Node:

- `+0x00` next
- `+0x04` previous
- `+0x08` unit pointer

Manual admission guard:

- `0x5A7000: cmp dword ptr [0x841720], 0x5A`
- immediate byte at VA `0x5A7006`, RVA `0x1A7006`
- stock cap = 90

Manual AddUnit path `0x5A6FD8`:

- append active list at `0x5A70B6` / `call 0x4ACB59`
- set unit selected flag `unit+0x3A8 = 1`
- **then** call post-add notification/event routine `0x552FFA` from VA `0x5A70D0` (RVA `0x1A70D0`)

Allocator:

- `0x4ACB2D` consumes free node; if empty calls `0x4AC4C1`
- `0x4AC4C1` uses `+0x20` for first allocation and `+0x24` for later blocks
- stock active list is first=90, growth=0

## Selection sort/rebuild — static proof

Function `0x5A719F` sorts/rebuilds active selection through two temp lists:

- temp A `0x841784`
- temp B `0x8417AC`

Hardcoded first-block 90 sites:

- `0x5A71B1` temp A immediate
- `0x5A71B9` temp B immediate
- `0x5A7299` active rebuild immediate

Therefore changing active `+0x20` once is not persistent; native sort/rebuild can restore capacity 90.

## Runtime history

### Surgical Growth Probe

- active growth `0 -> 90`
- manual guard stayed 90
- result: stable at selected=90; #91 rejected
- conclusion: growth write alone is harmless before overflow; second allocation not tested.

### Manual 120 — FAILED

- branch `selection-capacity-manual-120`
- pinned source `d3363c0c649920755c5f152f6a8fe2147bef8866`
- run `34343788268`
- EXE SHA-256 `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`
- patch: growth 0->90 + manual guard 90->120
- runtime: #91 crashes BRZE

### First-Block 120 / no growth — FAILED

- branch `selection-firstblock-120-probe`
- run `34347542420`
- runtime: #91 crashes
- correction: this did not persist through native sort/rebuild, so it did not exonerate all capacity paths.

### Sort-Pipeline 120 / no growth — FAILED WITH TWO DISTINCT MODES

Build:

- branch `selection-sort-pipeline-120-probe`
- built head `3123b0eb32490f582ef2ddd4ef0c8676bf6840aa`
- run `34348488434`
- artifact `BRZE-Selection-Sort-Pipeline-120-Probe`
- EXE SHA-256 `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`

Patch set:

- active first block 120 before first allocation
- growth 0
- manual cap 120
- sort temp A/B + active rebuild first-block immediates 120
- no shared constructor/global broad patch
- no HP/stamina/F7 hooks

Runtime observations from user:

1. **Bulk rectangle/shift-drag ~80 units:** gameplay freezes immediately, even though selection is below 90.
2. **Slow selection:** 1..90 works; clicking #91 makes unit #91 visibly selected for ~0.3 seconds, then BRZE process crashes.

Proof labels:

- bulk drag has a failure path below the 90 boundary
- #91 is visibly admitted before crash, so active append/selected flag likely complete before the fatal downstream action
- allocator/list capacity is no longer the only or primary suspect for the #91 crash

## Critical path split — static proof after latest runtime test

### Single-click / manual AddUnit

`0x5A6FD8` ultimately:

1. appends node to `0x841708`
2. sets selected flag
3. calls `0x552FFA` at `0x5A70D0`

`0x552FFA` emits a small selection-add event into the game's global event queue when `[0x841194] != 0`.

### Rectangle / drag selection

Rectangle path around `0x5D4356`:

- scans candidates
- appends candidate pointers to global temp list `0x879748` via `0x4ACB59` around `0x5D44B5`
- iterates candidates
- calls the **same AddUnit function `0x5A6FD8`** for each accepted unit around `0x5D455A`
- clears candidate list and later runs sort/rebuild

Therefore both observed failure modes share the per-unit post-add `0x552FFA` notification path. Rectangle selection also has an additional candidate-list path (`0x879748`) that remains a secondary suspect if suppressing add-notify does not remove the bulk freeze.

Do not claim the capacity of `0x879748`; it is not yet proven.

## CURRENT DECISIVE PROBE — NO ADD-NOTIFY 120

Goal: isolate the common post-add event/notification path without introducing another broad capacity change.

Branch:

- `selection-no-add-notify-120-probe`

Source differential commit:

- `d9e9f374deaebe6bf6d26d63051de37600478336`

Built head / trigger commit:

- `832815431be30ef3ec6ac8a6bc4a4909e1657df9`

Workflow run:

- `34349623960`
- conclusion: **success**
- compile smoke: success
- x86 single-file publish: success
- artifact upload: success

Artifact:

- `BRZE-Selection-No-Add-Notify-120-Probe`
- artifact ID `10103170720`
- ZIP SHA-256 `f5ecfa61476bd50efec7b34765d9fbea30645856d402719fffbee8d7a9933e5d`
- EXE SHA-256 `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`

Exact differential from Sort-Pipeline-120:

- preserves sort-pipeline-120 setup
- **one new behavioral change:** suppress call at VA `0x5A70D0` / RVA `0x1A70D0`
- expected original bytes: `E8 25 BF FA FF`
- probe bytes: `90 90 90 90 90`
- this suppresses only `call 0x552FFA` after append + selected flag
- expected-byte validation is required before patching
- F4 off / trainer stop restores the original call bytes
- status reports `add-notify:NOP`

### Required runtime tests

Fresh BRZE process for each test. Enable F4 before any selection. Confirm:

- `ARMED no-add-notify-120`
- `cap:0x78`
- sort bytes `78/78/78`
- `add-notify:NOP`

Test A — slow boundary:

- select gradually 89 -> 90 -> 91 -> 92 -> 100 if stable

Test B — separate fresh process:

- shift/rectangle-drag a large group around ~80 units

### Interpretation

#### A and B both become stable

Strongly implicates `0x552FFA` / selection-add event emission or its downstream queue consumers as the common failure source. Next step: inspect event semantics/queue limits and preserve necessary notification behavior safely rather than permanently dropping it.

#### #91 stable but drag still freezes

Post-add event was involved in #91 crash, while rectangle candidate list `0x879748` or another batch-only path remains a separate bulk-selection problem.

#### #91 still crashes with add-notify NOP

Move to the next post-append synchronous/asynchronous consumer: sort/UI/selection state readers or delayed frame processing. Because #91 visibly appears first, instrument state immediately after append rather than changing more capacity values.

#### Drag still freezes and slow path unchanged

Investigate `0x879748` candidate-list construction/clear path and rectangle-only processing separately.

## Other BRZE mappings for later trainer work

- selected/focused building `0x8417D8`, RVA `0x4417D8`
- second building pointer `0x8417D4`, RVA `0x4417D4`
- training progress `building+0x490`, 16.16 threshold `0x00640000`
- another progress channel `building+0x4BC`

Reference: `reference/old-trainer/brze160-remap-20260908.md`.

## Do not reintroduce without evidence

- shared constructor/global capacity patching
- patching every constant 90 merely because it matches
- broad auxiliary-list changes
- per-frame heavy scanning
- multiple new subsystems in one probe
- treating compile success as runtime proof

Use disposable test saves. Restart BRZE fully between invasive selection probes.

## New-chat handoff

Use:

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Then read this file before taking action.

**Current unresolved hinge:** runtime behavior of `BRZE-Selection-No-Add-Notify-120-Probe` for (A) slow #91 crossing and (B) rectangle/shift-drag ~80 selection.