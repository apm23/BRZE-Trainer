# Selection Capacity TKP Audit

## Runtime evidence

Specimen B with only F1-F4 and F7 enabled crashes when recalling a large ~100-unit control group. F5/F6 are disabled. Manual box/Shift selection does not crash; additional units are refused after the native cap.

The first raised-cap interrogation specimen changed the manual cap to 120 and repeatedly wrote 120 into selection growth fields. Runtime result: the original crash changed into a **hard simulation freeze around a 64-unit selection**. Audio/UI input still accepted commands, but simulation/animation stopped. This is a new failure mode and must NOT be interpreted as proof that 64 is a native selection cap.

## PROVEN native manual cap = 90

Normal AddUnitToSelection at VA `0x5A6FD8` contains:

```asm
5A7000 cmp dword ptr [0x841720],0x5A
5A7007 je  0x5A70D5
```

`0x5A = 90`.

## PROVEN authoritative selection container

Container object VA `0x841708`; selected count DWORD VA `0x841720` (`+0x18`). `0x4ACB59` inserts a unit into a linked node and increments count.

The container allocator parameters are critical:
- object `+0x20` = first allocation block size
- object `+0x24` = subsequent/growth block size

`0x4ABFE6` writes those two constructor arguments directly into `+0x20` and `+0x24`.

## PROVEN control-group recall bypass

Control-group recall at `0x5A780B` enumerates matching units and inserts each one directly:

```asm
5A78B9 cmp dword ptr [esi+0x36C],edi
5A78BF jne 0x5A78D6
5A78C1 push esi
5A78C2 mov  ecx,0x841708
5A78C7 call 0x4ACB59
5A78CC mov  dword ptr [esi+0x3A8],1
```

It bypasses guarded `0x5A6FD8`, so a group with >90 members attempts insertion #91.

## EXACT >90 CRASH MECHANISM — PROVEN

Selection-related containers are initialized with **first block = 90, growth block = 0**.

Selection post-processing `0x5A719F` reinitializes temporary and active selection containers with:

```asm
5A71AF push 0
5A71B0 push 0x5A
... call 0x4ABFE6

5A71B7 push 0
5A71B8 push 0x5A
... call 0x4ABFE6

...
5A7297 push 0
5A7298 push 0x5A
5A729A mov ecx,0x841708
5A729F call 0x4ABFE6
```

`0x4ABFE6` stores arg1 into `+0x20` and arg2 into `+0x24`. Therefore those calls configure 90/0.

When the first 90 nodes are consumed, insertion #91 enters `0x4AC4C1`. Since block count is already nonzero it selects object `+0x24`, which is zero. No usable freelist nodes are produced. `0x4ACB2D` then reaches:

```asm
4ACB3D mov ecx,[esi+0x8]    ; NULL freelist
4ACB40 mov eax,[ecx]        ; NULL dereference
```

This matches the original ~100-unit Team recall crash.

## Why the FIRST raised-cap interrogation specimen is rejected

Static review after the 64-unit freeze found two confounders in that specimen:

1. **Timer-race/incomplete allocator patch.** F4 wrote active/temp `+0x24 = 120` every 16 ms, but native post-selection code at `0x5A71B0`, `0x5A71B8`, and `0x5A7298` re-runs `0x4ABFE6` with 90/0. Therefore the trainer and the game were racing over allocator configuration instead of changing the native construction path coherently.
2. **Selection hooks were still installable through F7.** `EnsureHooks()` installs HP, stamina, training, and both selection-event hooks as one bundle. Therefore running with F7 active can install `0x5A70C6` / `0x5A78CC` hooks even while F5/F6 are OFF. This contaminates a test intended to isolate selection capacity.

The 64-unit freeze is therefore evidence that the first diagnostic patch is invalid for root-cause isolation. It is **not** evidence that the proven 90 guard or #91 NULL-dereference findings were wrong.

## Clean-room diagnostic design

Branch `selection-capacity-cleanroom` removes both confounders:

- F5/F6/F7 are disabled; `SetHooks(false,false,false)` guarantees no HP/stamina/training/selection-event cave is installed.
- F4 changes the native manual cap immediate `0x5A -> 0x78` (120).
- It patches the native selection-manager constructor/rebuild immediate values to create **120-node first pools** instead of depending on repeated external growth-field writes:
  - `0x5A6BCC` match/init capacity source
  - `0x5A71B1` temp selection A
  - `0x5A71B9` temp selection B
  - `0x5A7299` active-selection rebuild
- `+0x24 = 120` is written only once when enabling F4, solely so an already-constructed 90-node current pool can grow before the next native rebuild.

Run `34340574604` compiled and published this clean-room specimen successfully.

## Safe patch direction

Do NOT only change the manual `cmp 90` guard. A correct Large Selection companion for F4 must coherently enlarge the allocator initialization/rebuild path as well.

The next runtime gate is intentionally 120 rather than 200. First prove 100-unit Team recall and manual selection without freeze/crash in the clean-room specimen. Only then audit/raise to 150/200.

## Population status

Max-pop remains independent of the original #91 crash. F4 will eventually bundle Max Population + Large Selection Capacity, but population value itself is not the identified selection crash mechanism.

## Acceptance gate

- native manual cap: PROVEN = 90
- central selected count: PROVEN = `0x841720`
- central selected container: PROVEN = `0x841708`
- control-group recall bypass: PROVEN = `0x5A78C7`
- initial selection node pool: PROVEN = 90
- growth block size in native selection rebuild: PROVEN = 0
- exact insertion #91 failure: PROVEN = NULL freelist dereference at `0x4ACB40`
- first 120-cap interrogation specimen: REJECTED (64-unit simulation freeze; contaminated by timer writes + bundled selection hooks)
- clean-room 120-cap specimen: BUILT, awaiting runtime test
- downstream >90 consumers safe: NOT YET PROVEN
- 100/150/200 runtime stress: NOT YET PASSED
