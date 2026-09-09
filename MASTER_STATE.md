# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules

- **GitHub source / pinned build commit / runtime test results are authoritative.**
- This file is the forensic ledger for cross-chat continuity.
- Chat messages are discussion context, not the sole source of truth.
- After every meaningful runtime test or patch milestone, update this file.
- Distinguish **observation**, **static proof**, **runtime proof**, **inference**, and **hypothesis**.
- Do not reintroduce disproven/broad patches without new evidence.

## Current base — narrow selection experiment

**Experiment:** BRZE Selection Manual 120

**Pinned build source commit:** `d3363c0c649920755c5f152f6a8fe2147bef8866`

**Branch:** `selection-capacity-manual-120`

**Workflow:** `.github/workflows/selection-capacity-manual-120.yml`

**Successful Actions run:** `34343788268`

**Artifact:** `BRZE-Selection-Manual-120` (artifact ID `10100855929`)

**Artifact ZIP digest reported by GitHub:** `sha256:1d0aab8c6087eeb91b266b119b7f94f1cca8f891f77e25918443ca6f803665c0`

**Built EXE SHA-256 after extracting artifact:** `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`

### Exact narrow patch set

From `Program.cs`:

- `RVA_SELECTION_LIST = 0x441708` (absolute VA `0x841708` at preferred image base `0x400000`)
- selected count offset: `+0x18`
- first block offset: `+0x20`
- growth block offset: `+0x24`
- active selection growth: **`0 -> 90`**
- `RVA_MANUAL_CAP_IMM = 0x1A7006` (absolute VA `0x5A7006`)
- proven manual AddUnit cap immediate: **`0x5A (90) -> 0x78 (120)`**
- max population path remains enabled under F4 via `RVA_MAX_UNITS = 0x467B90` for local player ID at `RVA_LOCAL_ID = 0x4416D0`

### Deliberately absent from this specimen

- no HP hook
- no stamina hook
- no F7
- no selection-event hook
- no selection polling/top-up loop
- no constructor capacity patch
- no temp/auxiliary selection-container patch
- no broad shared-capacity patch

The current diagnostic must remain narrow until the >90 runtime behavior is known.

## Proven results before current test

### Old broad selection-capacity attempts

**Runtime observation:** selecting a very large number of units could freeze gameplay/simulation/animations while audio, cursor, camera movement, and saving remained functional. Saving and reloading cleared the frozen state.

**Inference:** this is not a full process crash; it is consistent with simulation/game-state processing entering a bad state.

### Surgical Growth Probe

Patch set:

- active selection `+0x24` growth `0 -> 90`
- manual selection guard remained 90
- no constructor/temp-container/broad patches

**Runtime result:**

- `selected = 90`
- `first = 90`
- `growth = 90`
- no crash
- no freeze
- unit #91 could not be selected because the manual guard was still 90

**Conclusion:** merely setting native active-list growth to 90 is stable through the first 90 selections. It did not yet prove second-block allocation because the manual guard prevented #91.

## Current decisive runtime test — PENDING

Use only the pinned **BRZE Selection Manual 120** artifact.

Test sequence:

1. Fully restart BRZE.
2. Load the known old test save.
3. Start the narrow trainer.
4. Enable **F4 only**.
5. Manually select across the boundary: 89 -> 90 -> 91 -> 92 -> 100+.
6. Test Team 1 containing about 100 units.
7. Record exact selected count and behavior at the first anomaly.

Record:

- maximum selected count
- whether 91 is admitted
- whether Team 1 ~100 selects completely
- freeze yes/no
- crash yes/no
- exact count at anomaly
- animation/simulation behavior
- audio behavior
- camera behavior
- cursor behavior
- whether deselect recovers
- whether save/reload recovers
- trainer status line (`selected / first / growth / manual cap byte`)

## Decision tree after Manual 120 test

### A — 90 -> 100+ stable

Supports:

- growth 90 is usable for the active-selection list
- the manual 90 guard was a real blocker
- older broad patches likely destabilized unrelated/shared containers

Next work: increase the target in controlled increments while keeping the patch narrow and inspect any next fixed-size consumer before raising much further.

### B — freeze/crash at 91 or immediately after crossing 90

Primary suspects:

- second allocation block path
- pointer/block transition
- native block-linking/index logic
- a consumer assuming the first block is the only block

Do not immediately patch constructors or all containers.

### C — >90 works, then freeze near another count

Move investigation toward downstream consumers:

- formation processing
- order processing
- selected-unit iteration
- simulation-side fixed-size arrays/buffers
- group/team command processing

### D — still hard-capped at 90

There is at least one additional admission gate/clamp/compare. Find that gate before changing allocator/container construction.

## Known BRZE 1.60 static mappings relevant to later trainer work

Reference: `reference/old-trainer/brze160-remap-20260908.md`.

- selected-unit list: absolute VA `0x841708`, RVA `0x441708`
- list layout: `+0x00` head, `+0x04` tail, `+0x18` selected count
- node layout: `+0x00` next, `+0x04` previous, `+0x08` unit pointer
- selected/focused building pointer: absolute VA `0x8417D8`, RVA `0x4417D8`
- second building-related pointer: absolute VA `0x8417D4`, RVA `0x4417D4`
- training progress: building `+0x490`, 16.16 fixed-point, completion threshold `0x00640000`
- another progress channel: building `+0x4BC`, 16.16 fixed-point

These mappings have mixed proof levels. Preserve the proof labels from the reference document; do not treat all of them as runtime-proven cheats.

## Historical reference binaries supplied again by user

These are reference specimens only, not the current BRZE target.

### Legacy trainer

- supplied filename: `BattleRealmsTrainer OLD.exe`
- PE32 / x86 GUI
- UPX-compressed (`UPX0`, `UPX1`, resource section)
- SHA-256: `41571fc8cd83e296a60a04d934440b5a01d22ce45d111acdc31b13f16e8aa24d`

### Matching legacy BR WOTW executable

- supplied filename: `Battle_Realms_F(4).exe`
- PE32 / x86 GUI
- SHA-256: `6217a30325c4f84ba3c44965051979d891d468e9b2374686be6b7aab82403666`

Use these only to recover legacy semantics/patterns and cross-check the existing `reference/old-trainer/` autopsy. Current BRZE addresses must come from the BRZE remap/current binary work, not copied blindly from WOTW.

## Do not reintroduce without evidence

- constructor-global selection capacity patches
- auxiliary/temp-container capacity patches
- patching every site merely because it uses the same value 90
- broad shared-capacity changes
- per-frame/per-tick heavy scanning of selected units
- selection-event hooks during the current capacity investigation
- multiple subsystem changes in a single diagnostic specimen

## Forensic discipline

1. One hypothesis -> smallest possible patch -> runtime test -> record result.
2. Preserve original bytes before any code patch.
3. Pin every test artifact to commit, workflow run, and hash when possible.
4. Keep failed builds/tests in history; they are evidence.
5. Never call a behavior runtime-proven solely because it compiles or matches legacy structure.
6. Use a disposable/test save slot for risky experiments; do not overwrite the main save.
7. On a new ChatGPT thread, read this file before proposing or building the next specimen.

## New-chat handoff

Use:

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Then read `MASTER_STATE.md` from `apm23/BRZE-Trainer` before taking action.

**Current unresolved hinge:** runtime result of pinned `BRZE-Selection-Manual-120` across selection #91 and Team 1 ~100 units.
