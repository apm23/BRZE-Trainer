# BRZE Hero Effect Child Probe V7 — STATE

Status: **BUILD PENDING — READ-ONLY DESIGN LOCKED**
Date: 2026-09-14 JST

## Why V7 exists
V6 completed three clean ORIGINAL ISSYL passes with different human reaction speeds and still measured 10725.9 / 10713.8 / 10733.2 ms (19.4 ms total spread). Automatic timing is proven stable.

V6 also content-identified the actual Issyl effect record in every pass using:
- `parent+0x058 == 0xA5`
- `parent+0x17C == pinned Unit*`

The parent address/path changed across casts, proving path hard-coding is invalid.

No clean continuously-changing duration timer exists in the parent record. Parent `+0x194` is rejected because its terminal value increased 623600 -> 653700 -> 678400 across sequential casts while lifetime stayed ~10.72 s.

## V7 objective
After signature-locking the true A5+target parent record, discover its readable pointer-valued fields, deduplicate aliases, pin those child objects by address, and sample the child objects throughout natural Issyl lifetime.

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
5. scan the parent `0x240` bytes for readable pointer fields;
6. deduplicate aliases that point to the same child object;
7. pin up to 24 child objects;
8. sample each child `0x200` bytes every 20 ms;
9. auto-finish after lifecycle roots return to baseline for 4 consecutive samples;
10. rank child fields by change-rate and direction flips.

## Runtime objective
Find a child field that changes monotonically (ideally flip 0), with total/rate matching the ~10.72 s natural Issyl lifetime. Static duration-like child constants are reported separately.

No duration writes are allowed from V7 alone.

## Locked rejects
- repeated native reapplication
- `Unit+0x460`
- parent `+0x194` as duration-specific
- path-based effect identity
