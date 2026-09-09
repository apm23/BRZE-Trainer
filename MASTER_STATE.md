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

## Latest runtime result — Manual 120 FAILED at the 90/91 boundary

### Tested build

**Experiment:** BRZE Selection Manual 120

- pinned source commit: `d3363c0c649920755c5f152f6a8fe2147bef8866`
- branch: `selection-capacity-manual-120`
- workflow: `.github/workflows/selection-capacity-manual-120.yml`
- successful Actions run: `34343788268`
- artifact: `BRZE-Selection-Manual-120` (artifact ID `10100855929`)
- artifact ZIP digest: `sha256:1d0aab8c6087eeb91b266b119b7f94f1cca8f891f77e25918443ca6f803665c0`
- built EXE SHA-256: `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`

Patch set:

- active selection `+0x24` growth `0 -> 90`
- manual AddUnit guard immediate at RVA `0x1A7006`: `90 -> 120`
- no constructor/temp-container/broad capacity patch
- no HP/stamina/F7/selection-event hook

### Runtime observation reported 2026-09-09

**Game crashes as selection reaches the native 90-unit boundary.**

Because the preceding Surgical Growth Probe was stable at exactly 90 while the manual guard remained 90, the new crash is tied to opening admission beyond 90. The most likely trigger is the attempt to admit unit #91 or work immediately caused by that admission.

Proof label: **runtime-proven failure of Manual 120 at the 90/91 boundary.**

Do not describe growth `0 -> 90` as usable beyond 90; that has now failed runtime testing when #91 is admitted.

## New static proof from authoritative BRZE binary

Direct disassembly of current target `Battle_Realms_F(5).exe` confirms the native selection container allocator.

### Manual admission guard

At `0x5A7000`:

```asm
cmp dword ptr [0x841720], 0x5A
je  0x5A70D5
```

The immediate byte at `0x5A7006` is the 90-unit manual selection guard.

### Active-list append

At `0x5A70B6`:

```asm
push esi
mov  ecx, 0x841708
call 0x4ACB59
```

`0x4ACB59` appends a node and increments `[container+0x18]`.

### Node allocator boundary behavior

`0x4ACB2D` pops a node from `[container+0x08]`. If the free-node list is empty it calls `0x4AC4C1`.

`0x4AC4C1` selects allocation size as follows:

- when `[container+0x1C] == 0`, use `[container+0x20]` (first block size)
- when `[container+0x1C] != 0`, use `[container+0x24]` (growth block size)

Then it allocates `count * 12` bytes worth of 12-byte nodes and increments `[container+0x1C]`.

### Active selection construction

`0x5A6BC9` constructs `0x841708` via `0x4ABFE6` with:

- first block size = `90`
- growth size = `0`

Therefore the stock active list is intentionally configured as one 90-node block with no second growth block.

This is **static-proven** from the authoritative BRZE binary.

## Current decisive experiment — FIRST BLOCK 120 / NO GROWTH

Goal: distinguish **second-block transition failure** from a **downstream consumer that cannot tolerate selected count >90**.

**Branch:** `selection-firstblock-120-probe`

Base: clean pinned Manual-120 source commit `d3363c0c649920755c5f152f6a8fe2147bef8866`, not the later broad/full-pipeline branch tip.

Source commit introducing probe logic: `89c8a2044524bd20f8fbe5cbea2eabe02af11180`

### Exact probe behavior

- growth stays **0**
- only when active selection allocator is pristine (`selected=0`, block count=0, free-list=0, first=90, growth=0):
  - active list first block `+0x20`: **90 -> 120**
- manual guard: **90 -> 120** only after the first-block-120 state is armed
- max population remains enabled
- no constructor-code patch
- no temp/auxiliary container patch
- no HP/stamina/F7/selection hooks
- status exposes selected count, block count, first size, growth size, free-list pointer and cap byte
- if allocator was already used, trainer refuses to open the cap and instructs user to restart

### Interpretation

#### If 91 -> 100+ works with first=120, growth=0, blocks=1

Strongly supports the hypothesis that the previous crash is specifically tied to the **second block allocation / growth transition**, not selected count >90 itself.

#### If it still crashes at 90/91 with one preallocated 120-node first block

Then the growth transition is exonerated. Move investigation downstream to code that consumes a selected count/list above 90: UI, formation, command dispatch, group/team processing, or fixed-size arrays.

## Proven earlier results

### Old broad selection-capacity attempts

Runtime observation: selecting a very large number of units could freeze gameplay/simulation/animations while audio, cursor, camera movement, and saving remained functional. Saving and reloading cleared the frozen state.

Inference: not a full process crash; consistent with simulation/game-state processing entering a bad state.

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

## Known BRZE 1.60 static mappings relevant to later trainer work

Reference: `reference/old-trainer/brze160-remap-20260908.md`.

- selected-unit list: absolute VA `0x841708`, RVA `0x441708`
- list layout: `+0x00` head, `+0x04` tail, `+0x08` free-node head, `+0x18` selected count, `+0x1C` allocated block count, `+0x20` first block size, `+0x24` growth block size
- node layout: `+0x00` next, `+0x04` previous, `+0x08` unit pointer
- selected/focused building pointer: absolute VA `0x8417D8`, RVA `0x4417D8`
- second building-related pointer: absolute VA `0x8417D4`, RVA `0x4417D4`
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

- constructor-global selection capacity patches
- auxiliary/temp-container capacity patches
- patching every site merely because it uses the same value 90
- broad shared-capacity changes
- per-frame/per-tick heavy scanning
- selection-event hooks during this capacity investigation
- multiple subsystem changes in one diagnostic specimen
- growth `0 -> 90` + cap >90 as if it were proven safe (it is now runtime-failed at the boundary)

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

**Current unresolved hinge:** runtime result of the **Selection First-Block 120 Probe** across selected unit #91 with `first=120`, `growth=0` and ideally `blocks=1`.
