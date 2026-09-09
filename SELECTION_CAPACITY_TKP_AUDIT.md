# Selection Capacity TKP Audit

## Runtime evidence

Specimen B with only F1-F4 and F7 enabled still crashes when attempting the large ~100-unit control-group selection. F5/F6 are disabled. Manual box/Shift selection does not crash; instead additional units eventually refuse selection, and deaths free selection slots.

## PROVEN BRZE 1.60 native selection cap

Static disassembly of authoritative BRZE 1.60 proves normal AddUnitToSelection at VA `0x5A6FD8` checks selected count:

```asm
5A6FF4 cmp dword ptr [esi+0x3A8],0
5A6FFA jne 0x5A70D5
5A7000 cmp dword ptr [0x841720],0x5A
5A7007 je  0x5A70D5
```

`0x5A = 90`. Native guarded/manual selection cap is 90.

## Authoritative selected container

Container object VA `0x841708`; count DWORD VA `0x841720` (`+0x18`). Add routine `0x4ACB59` stores unit pointer in a linked node and increments `+0x18`.

Further allocator audit proves 90 is NOT a physical one-block ceiling. `0x4ABFE6` initializes the container with node-allocation block size at object `+0x20`. Selection setup at `0x5A7297` passes `0x5A` (90) as that block size. When the freelist is empty, `0x4AC4C1` allocates another block of `block_size * 12` bytes and links its nodes into the free pool. Therefore the linked container can physically grow beyond 90 nodes. This removes the fixed-90-pointer-array hypothesis.

## PROVEN control-group recall bypass

The control-group selection/recall routine beginning at VA `0x5A780B` first clears the active selected container and selected flags, then enumerates local-player units. For units whose group field `unit+0x36C` equals the requested group ID, it performs:

```asm
5A78B0 cmp dword ptr [esi+0x3BC],0
5A78B7 jne 0x5A78D6
5A78B9 cmp dword ptr [esi+0x36C],edi
5A78BF jne 0x5A78D6
5A78C1 push esi
5A78C2 mov  ecx,0x841708
5A78C7 call 0x4ACB59
5A78CC mov  dword ptr [esi+0x3A8],1
```

This path calls the generic linked-list insertion routine `0x4ACB59` DIRECTLY. It does NOT call guarded AddUnitToSelection `0x5A6FD8` and contains no `count == 90` guard before insertion. Therefore a control group containing 100 units can create an authoritative selected count of 100, while manual/Shift selection is stopped at 90.

This exactly explains the observed behavioral split at the selection layer:
- manual/Shift: guarded at 90, extra units refused safely;
- control-group recall: bypasses guard and can populate >90.

The crash itself is not yet assigned to a specific downstream consumer. Because the linked container can allocate additional node blocks, the crash is now more likely to be a downstream subsystem that was designed/tested with `selected_count <= 90`, rather than immediate overflow of the central linked list.

## Related native group operations

VA `0x5A7694` operates on unit `+0x36C` group IDs across the active selection/player-unit enumeration. The group subsystem supports ten slots (0-9). This is separate from the active selection count.

## Existing trainer hooks

Trainer event sites `0x5A70C6` and `0x5A78CC` touch per-unit selected flag `+0x3A8`; they are not authoritative count/storage. F5/F6 remain excluded from this investigation.

## Next mandatory trace

1. Audit downstream consumers reached immediately after `0x5A780B` / `0x5A78E6` when count is >90.
2. Audit all xrefs to `0x841720`, especially UI, order dispatch, pathing-group creation, battle gear, and selection post-processing.
3. Audit hard-coded 90 (`0x5A`) constants in selection-related code. Distinguish policy guards from allocator block-size constants and unrelated numeric 90s.
4. Determine exact crash consumer before raising manual cap.
5. Build F4 companion patch only after >90 consumer safety/fixes are known. Target 200 simultaneous selected units.

## Population status

Keep max-pop unchanged during isolation. Existing max-pop audit has not shown max-pop sizing this selection container.

## Acceptance gate

- native manual cap: PROVEN = 90
- central selected count: PROVEN = VA `0x841720`
- central selected container: PROVEN = VA `0x841708`, linked node container
- allocator can grow beyond first 90 nodes: PROVEN
- control-group recall bypasses normal 90 guard: PROVEN at `0x5A78C7`
- exact downstream >90 crash consumer: NOT YET PROVEN
- raised-cap patch safe: NOT YET PROVEN
- 100/150/200 runtime stress: NOT YET RUN
