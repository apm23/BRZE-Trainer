# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules

- **GitHub source / pinned build commit / runtime test results are authoritative.**
- This file is the forensic ledger for cross-chat continuity.
- Chat messages are discussion context, not the sole source of truth.
- After every meaningful runtime test or patch milestone, update this file.
- Distinguish **observation**, **static proof**, **runtime proof**, **inference**, and **hypothesis**.
- Do not reintroduce disproven/broad patches without new evidence.

## Current BRZE target binary

Authoritative current target specimen supplied by user:

- filename: `Battle_Realms_F(5).exe`
- PE32 / x86
- size: `4,521,984` bytes
- preferred image base: `0x400000`
- SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- fingerprint note: `reference/current-brze/target-binary-20260909.md`

All current offsets/patches must be verified against this specimen. Legacy WOTW binaries are reference-only.

## Runtime result history — selection boundary

### Surgical Growth Probe

Patch set:

- active selection `+0x24` growth `0 -> 90`
- manual selection guard remained 90
- no constructor/temp-container/broad patches

Runtime result:

- `selected = 90`
- `first = 90`
- `growth = 90`
- no crash
- no freeze
- unit #91 could not be selected because manual guard remained 90

Conclusion: writing growth=90 is harmless while the stock first block has not overflowed. It did **not** prove the second allocation path.

### Manual 120 Probe — FAILED

Pinned build:

- source commit: `d3363c0c649920755c5f152f6a8fe2147bef8866`
- branch: `selection-capacity-manual-120`
- Actions run: `34343788268`
- artifact: `BRZE-Selection-Manual-120`
- EXE SHA-256: `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`

Patch set:

- active growth `0 -> 90`
- manual AddUnit guard `90 -> 120`

Runtime observation:

- selection up through 90 works
- attempting unit #91 crashes BRZE

Proof label: **runtime-proven failure at the 90/91 boundary**.

### First-Block 120 / No-Growth Probe — ALSO CRASHED AT #91

Branch: `selection-firstblock-120-probe`

Actions run: `34347542420`

Artifact: `BRZE-Selection-FirstBlock-120-Probe`

Probe behavior:

- active `+0x20` first block changed `90 -> 120` only before first allocation
- growth kept `0`
- manual guard `90 -> 120`
- no shared constructor patch
- no temp/auxiliary patch

Runtime observation reported by user:

- selection through 90 remains alive
- clicking/selecting unit #91 causes an immediate game crash

Important correction: this result does **not** yet prove that a downstream consumer cannot tolerate count >90, because static follow-up found that BRZE's own selection sort/rebuild routine can reset active selection back to `first=90, growth=0` after the one-time `+0x20=120` mutation.

Reference notes:

- `reference/current-brze/runtime-result-firstblock120-20260909.md`
- `reference/current-brze/selection-sort-pipeline-20260909.md`

## Static proof — native selection container

Selected-unit list:

- absolute VA `0x841708`
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

Manual admission guard at `0x5A7000`:

```asm
cmp dword ptr [0x841720], 0x5A
je  0x5A70D5
```

Immediate byte at `0x5A7006` / RVA `0x1A7006` is the 90-unit manual guard.

Active append at `0x5A70B6` calls `0x4ACB59` on container `0x841708`.

Allocator `0x4AC4C1` uses:

- `+0x20` for first allocation while block count is 0
- `+0x24` for later allocations once block count is nonzero

Stock active list is initialized as `first=90, growth=0`.

## Critical new static finding — selection sort/rebuild resets capacity to 90

Function: absolute VA `0x5A719F`, RVA `0x1A719F`.

This routine sorts/rebuilds active selection through two temporary lists and contains three hardcoded first-block 90 initializations:

1. `0x5A71B0: push 0x5A` for temp sort list `0x841784`
   - immediate byte `0x5A71B1`, RVA `0x1A71B1`
2. `0x5A71B8: push 0x5A` for temp sort list `0x8417AC`
   - immediate byte `0x5A71B9`, RVA `0x1A71B9`
3. `0x5A7298: push 0x5A` before reinitializing active list `0x841708`
   - immediate byte `0x5A7299`, RVA `0x1A7299`

The routine walks active selection, partitions/sorts units into `0x841784` and `0x8417AC`, then reinitializes active selection and copies units back.

**Key implication:** the previous First-Block-120 probe changed active `+0x20` only once. Native routine `0x5A719F` can overwrite that state back to `first=90, growth=0`, so the #91 crash is still compatible with hitting a 90-node list again.

Do not treat first-block-120 as having been persistently maintained until these three reset sites are patched.

## Current decisive experiment — SORT PIPELINE 120 / NO GROWTH

Branch: `selection-sort-pipeline-120-probe`

Built head commit: `3123b0eb32490f582ef2ddd4ef0c8676bf6840aa`

Source logic commit: `0b38f804d54b4374bc774cd05468f256efc729ef`

Workflow: `.github/workflows/selection-sort-pipeline-120-probe.yml`

Successful Actions run: `34348488434`

Artifact: `BRZE-Selection-Sort-Pipeline-120-Probe`

Artifact ID: `10102715805`

Artifact ZIP SHA-256: `bc855de50ed9988abda3ae8815373dbd0867de9b430e50f6e24ab4e367bcb004`

Built EXE SHA-256: `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`

### Exact probe behavior

- active list first block starts `90 -> 120` only while allocator is pristine
- growth remains `0`
- manual admission guard `0x5A7006`: `90 -> 120`
- sort temp A first-block immediate `0x5A71B1`: `90 -> 120`
- sort temp B first-block immediate `0x5A71B9`: `90 -> 120`
- active rebuild first-block immediate `0x5A7299`: `90 -> 120`
- **shared constructor at `0x5A6BCB` is NOT patched**
- unrelated auxiliary lists remain untouched
- no HP/stamina/F7/selection-event hooks
- status displays active + sort temp list counts/block sizes and the three sort patch bytes

### Required test sequence

1. Fully restart BRZE.
2. Start trainer and enable F4 **before selecting anything**.
3. Confirm status says `ARMED sort-pipeline-120`.
4. Confirm `cap:0x78` and sort bytes `78/78/78`.
5. Select across 89 -> 90 -> 91 -> 92 -> 100.
6. Then test Team 1 ~100 units.

### Interpretation

#### If #91 now works

The previous crash was caused by the native sort/rebuild pipeline silently restoring one or more selection containers to 90. Continue testing to 100/120 while keeping growth zero.

#### If #91 still crashes with active + both sort temp lists genuinely at first=120 and growth=0

Then move to the next synchronous/downstream consumer after `0x5A70B6` append, including post-add notification/event dispatch and other fixed-size selected-unit consumers.

## Other known BRZE mappings for later trainer work

Reference: `reference/old-trainer/brze160-remap-20260908.md`.

- selected/focused building pointer: `0x8417D8`, RVA `0x4417D8`
- second building-related pointer: `0x8417D4`, RVA `0x4417D4`
- training progress: building `+0x490`, 16.16 fixed-point, threshold `0x00640000`
- another progress channel: building `+0x4BC`, 16.16 fixed-point

Preserve proof labels; not every mapping is runtime-proven cheat behavior.

## Historical reference binaries

Reference-only, not current target:

### Legacy trainer

- `BattleRealmsTrainer OLD.exe`
- PE32 / x86 GUI
- UPX-compressed
- SHA-256: `41571fc8cd83e296a60a04d934440b5a01d22ce45d111acdc31b13f16e8aa24d`

### Matching legacy BR WOTW executable

- `Battle_Realms_F(4).exe`
- PE32 / x86 GUI
- SHA-256: `6217a30325c4f84ba3c44965051979d891d468e9b2374686be6b7aab82403666`

Use only for semantic/pattern recovery. Do not copy WOTW addresses blindly into current BRZE.

## Do not reintroduce without evidence

- shared constructor/global selection-capacity patching
- patching every 90 merely because the value matches
- broad auxiliary-container changes
- per-frame heavy scans
- selection-event hooks while the current sort-pipeline capacity hypothesis remains untested
- assuming first-block=120 persists without accounting for `0x5A719F`

## Forensic discipline

1. One hypothesis -> smallest possible patch -> runtime test -> record result.
2. Preserve original bytes before code patching.
3. Pin every test artifact to commit, workflow run and hash when possible.
4. Keep failed tests/builds; they are evidence.
5. Compile success is not runtime proof.
6. Use disposable save slots for risky tests.
7. On a new ChatGPT thread, read this file before proposing the next specimen.

## New-chat handoff

Use:

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Then read `MASTER_STATE.md` from `apm23/BRZE-Trainer` before taking action.

**Current unresolved hinge:** runtime result of `BRZE-Selection-Sort-Pipeline-120-Probe` at selected unit #91 with `cap=0x78`, sort bytes `78/78/78`, and growth kept at 0.
