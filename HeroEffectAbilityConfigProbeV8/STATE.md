# BRZE Hero Effect Ability Config Probe V8 — STATE

Status: **BUILT / CI-PROVEN READ-ONLY — RUNTIME COMPARISON PENDING**
Date: 2026-09-14 JST

## Why V8 exists
V7 runtime-tested ORIGINAL ISSYL successfully and found no clean continuously-changing timer in child objects. Most dynamic child fields changed only at cleanup.

The strongest structured lead was `parent+0x1F4`, which points to a child/config object that remained readable/static for the full Issyl lifecycle and contained:
- `config+0x000 = 0xA5` — exact Issyl runtime ability ID
- `config+0x0A0 = 100`
- `config+0x0E0 = 15000`
- `config+0x0E8 = 482`

V8 tests whether this is truly an ability-specific config record and whether `+0x0E0` is duration-related by comparing Issyl A5 against Grayback C0.

## Architecture
Strictly READ-ONLY:
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION`
- no writes
- no allocation/injection
- no hooks
- no native replay

Automatic flow:
1. select exactly ONE clean target and ARM;
2. cast either ORIGINAL Issyl Haste or ORIGINAL Grayback effect once on that target;
3. auto-detect transient lifecycle start;
4. find actual parent by BOTH:
   - `parent+0x058` is known ability ID (`0xA5` or `0xC0`)
   - `parent+0x17C == pinned target Unit*`
5. read `parent+0x1F4` as config pointer;
6. sample `0x200` bytes of that config every 20 ms;
7. auto-finish when lifecycle roots return to baseline;
8. report config identity, focus offsets, dynamic fields, static numeric constants, and wall/config+0x0E0 ratio.

## Runtime objective
Run two clean reports:
- A: ORIGINAL ISSYL (`0xA5`)
- B: ORIGINAL GRAYBACK (`0xC0`)

Confirmation criteria:
1. `config+0x000` must match detected ability ID in each pass (`A5` vs `C0`);
2. `config+0x0E0` should differ between abilities;
3. the difference should be plausibly consistent with their very different natural lifetimes;
4. only after this may an isolated write experiment be considered.

No writes are allowed from V8 alone.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Ability Config Probe V8 Read Only`
Run: `34794119559` — SUCCESS
Job: `103823808659` — SUCCESS
Head: `0e319752b3533d933d15ffba332f0859356be4f2`
Artifact: `10329446128`
Artifact digest: `sha256:ce98a0e10b9b975d57b039e31dbf97ec18a0fe849f51636a891057dc1489a3fb`

Binaries:
- Standalone: 151,067,880 bytes — SHA-256 `0628d47fd75ab9403c8366822a487ba3210b5fbe5c1121f0115b3acba123aa4b`
- Small: 154,890 bytes — SHA-256 `44d17a8bf358e3b1823b6dac1e6c57489d8fb9fd64a2c0c3ede560f61c5d3d75`

## Exact runtime flow
### Test A — Issyl
1. fresh/reload BRZE;
2. select exactly ONE clean normal target;
3. click `ARM TARGET + AUTO DETECT`;
4. select Issyl;
5. cast ORIGINAL Haste once on armed target;
6. do nothing until `COMPLETE`;
7. `COPY REPORT`.

### Test B — Grayback
1. RESET or fresh/reload BRZE;
2. select exactly ONE clean normal target;
3. click `ARM TARGET + AUTO DETECT`;
4. select Grayback;
5. cast ORIGINAL Grayback effect once on armed target;
6. wait for natural expiry; no manual timing needed;
7. `COPY REPORT`.

Return both reports together if possible.

## Locked rejects
- repeated native reapplication / refresh
- `Unit+0x460`
- parent `+0x194` as duration-specific
- path-based effect identity
- V7 cleanup-only dynamic child fields
- low-address/vtable static `10272` as duration merely by numeric coincidence
