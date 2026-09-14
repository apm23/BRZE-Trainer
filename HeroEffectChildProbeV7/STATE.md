# BRZE Hero Effect Child Probe V7 — STATE

Status: **RUNTIME-PROVEN OBSERVER — CHILD CONFIG CANDIDATE FOUND**
Date: 2026-09-14 JST

## Runtime result
One clean ORIGINAL ISSYL pass completed successfully:
- observed natural lifetime: `10749.4 ms`
- effect polls / child sample cycles: `344`
- signature parent discovery delay: `32.6 ms`
- signature parent: `0x28C01ACC`
- discovery path this pass: `Unit+0x1E4->+0x014`
- 11 readable/deduplicated child objects pinned
- zero child read failures in the pointer map

Root lifecycle again matched the established effect pattern:
- `Unit+0x1E4`: zero -> transient -> zero
- `Unit+0x1E8`: zero -> transient -> zero
- `Unit+0x20C`: zero -> transient -> zero
- `Unit+0x210`: zero -> transient -> zero
- `Unit+0x214`: baseline pointer -> transient pointer -> exact baseline pointer

## Important negative result
No child field behaved like a clean continuously-changing countdown/elapsed timer. Most apparent high-score fields changed only once at cleanup and are pointer/list/object teardown noise.

Therefore V7 does NOT justify writing/freezing any dynamic child field.

## Strong new lead: parent+0x1F4 ability/config object
One child is especially structured:
- `parent+0x1F4 -> 0x227F4664`
- child `+0x000 = 0xA5` — exact Issyl runtime ability ID
- child `+0x0A0 = 100`
- child `+0x0E0 = 15000`
- child `+0x0E8 = 482`
- the object stayed readable/static for all 344 samples

This strongly suggests `parent+0x1F4` is an ability/config descriptor rather than a transient countdown instance.

`config+0x0E0 = 15000` is now the strongest duration-parameter candidate, but it is NOT proven yet. Its scale does not directly equal the measured `10749.4 ms`, so a cross-ability comparison is required before any write.

## Rejected false lead
`PARENT[+0x000,+0x1FC]` pointed to low address `0x00F738EC` and exposed a static integer `10272` at `+0x158`, numerically close to Issyl lifetime. This child shape is more consistent with vtable/type/static data and must not be promoted merely because the number is close to wall time.

## Architecture
Strictly READ-ONLY:
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION`
- no writes
- no allocation/injection
- no hooks
- no native ability replay

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

## Exact next action
Use V8 ability-config comparison, READ ONLY:
1. test ORIGINAL ISSYL A5 once and capture `parent+0x1F4` config values;
2. test ORIGINAL GRAYBACK C0 once and capture the same config offsets;
3. confirm config `+0x000` follows the ability ID (`A5` vs `C0`);
4. compare config `+0x0E0` against their very different natural lifetimes;
5. only if the field behaves ability-specifically and duration-plausibly may an isolated write experiment be considered.

## Locked rejects
- repeated native reapplication / refresh
- `Unit+0x460`
- parent `+0x194` as duration-specific
- path-based effect identity
- any V7 dynamic child field as duration without further proof
- low-address/vtable static `10272` as duration merely by numeric coincidence
