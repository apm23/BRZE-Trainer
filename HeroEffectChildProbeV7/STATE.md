# BRZE Hero Effect Child Probe V7 — STATE

Status: **BUILT / CI-PROVEN READ-ONLY — RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V7 exists
V6 completed three clean ORIGINAL ISSYL passes with different human reaction speeds and still measured 10725.9 / 10713.8 / 10733.2 ms (19.4 ms total spread). Automatic timing is proven stable.

V6 content-identified the actual Issyl effect parent in every pass using BOTH:
- `parent+0x058 == 0xA5`
- `parent+0x17C == pinned Unit*`

The parent address/path changed across casts, proving path hard-coding is invalid.

No clean continuously-changing duration timer exists in the parent record. Parent `+0x194` is rejected because its terminal value increased `623600 -> 653700 -> 678400` across sequential casts while lifetime stayed ~10.72 s.

## V7 objective
After signature-locking the true A5+target parent, discover its readable pointer-valued fields, deduplicate aliases, pin those child objects by address, and sample them throughout natural Issyl lifetime.

## Architecture
Strictly READ-ONLY:
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION`
- no `WriteProcessMemory`
- no `VirtualAllocEx`
- no `VirtualProtectEx`
- no `CreateRemoteThread`
- no hooks
- no native ability replay

Flow:
1. arm exactly one clean target;
2. cast ORIGINAL Issyl once;
3. auto-detect lifecycle start;
4. locate parent by `A5 + target Unit*` signature;
5. scan parent `0x240` bytes for readable pointer fields;
6. deduplicate aliases pointing to same child;
7. pin up to 24 child objects;
8. sample each child `0x200` bytes every 20 ms;
9. auto-finish after lifecycle roots equal baseline for 4 consecutive samples;
10. rank child fields by change-rate and direction flips.

## Runtime objective
Find a child field that changes monotonically (ideally flip 0), with total/rate matching the ~10.72 s natural Issyl lifetime. Static duration-like child constants are reported separately.

No duration writes are allowed from V7 alone.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Child Probe V7 Read Only`
Run: `34793489888` — SUCCESS
Job: `103822036340` — SUCCESS
Head: `18d22cbaa5f1b6da1f0d017f665e978c3486b95d`
Artifact: `10329090887`
Artifact digest: `sha256:83096a67245a1cc1d2163c071f8163cdcda2f0c4fe4706e1fe054021b7d92294`

Binaries:
- Standalone: 151,067,826 bytes — SHA-256 `ee30cc76f54f91aa93d4f7e26f01d7ed6db3070ca1a53d2c785d8d452c5843d2`
- Small: 155,348 bytes — SHA-256 `46a252b206a8dc78e04ffbf1144aa0a76ff1970f208f66834637e5a6ca582492`

## Exact runtime flow
1. fresh/reload BRZE;
2. select exactly ONE clean normal target;
3. click `ARM TARGET + AUTO WATCH`;
4. select Issyl;
5. cast ORIGINAL Haste once on armed target;
6. do nothing until `COMPLETE`;
7. click `COPY REPORT` and return full report.

## Locked rejects
- repeated native reapplication
- `Unit+0x460`
- parent `+0x194` as duration-specific
- path-based effect identity
