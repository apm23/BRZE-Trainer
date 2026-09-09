# Selection Capacity TKP Audit

## Runtime evidence

Specimen B with only F1-F4 and F7 enabled still crashes when attempting the large ~100-unit control-group selection. F5/F6 are disabled. Manual box/Shift selection does not crash; instead additional units eventually refuse selection, and deaths free selection slots.

## PROVEN BRZE 1.60 native selection cap

Static disassembly of the supplied authoritative `Battle_Realms_F.exe` proves the normal AddUnitToSelection path at VA `0x5A6FD8` checks the authoritative selected count:

```asm
5A6FF4 cmp dword ptr [esi+0x3A8],0
5A6FFA jne 0x5A70D5
5A7000 cmp dword ptr [0x841720],0x5A
5A7007 je  0x5A70D5
```

`0x5A = 90` decimal. Therefore the native guarded/manual selection cap in BRZE 1.60 is **90 units**, not 80. The earlier 80 figure was only an illustrative hypothesis and is superseded by this proof.

## Authoritative selected container

The selected-unit container object begins at VA `0x841708`. Its count is the DWORD at object `+0x18`, therefore VA `0x841720`.

The add call immediately before the selected flag is set is:

```asm
5A70B6 push esi
5A70B7 mov ecx,0x841708
5A70BC call 0x4ACB59
5A70C1 movzx eax,word ptr [esi+6]
5A70C5 push eax
5A70C6 mov dword ptr [esi+0x3A8],1
```

`0x4ACB59` is a linked-list insertion routine. It obtains a node, stores the unit pointer at node `+0x8`, links the node at the tail, and executes `inc dword ptr [esi+0x18]`. The corresponding generic removal logic decrements object `+0x18`.

This is important: the active selection is **not a simple fixed 90-pointer array**. It is a node-based linked container with an explicit count. The value 90 is an engine policy guard in the normal add path. This materially lowers the probability that merely raising the normal guard would immediately overrun a 90-entry backing pointer array, but it does NOT yet prove all consumers are safe above 90.

## Why the user's control-group crash is now highly significant

The original/manual controls support Ctrl+1..9 assignment and 1..9 recall. Public documentation confirms these are native group operations. Runtime evidence says manual/Shift selection stops safely, while recalling a group containing ~100 units crashes.

Combined with the proven 90-unit guard, the leading hypothesis is now:

1. normal manual AddUnitToSelection checks `[0x841720] == 90` and refuses the next unit safely;
2. a hotkey/control-group recall path may bulk-restore or otherwise populate selection without converging through this exact guard;
3. one or more downstream consumers may assume selected count <= 90, causing failure when the group recall produces >90 selected units.

This is a hypothesis, not yet a conviction. The control-group recall path and every selected-count consumer must be traced before patching.

## Existing trainer selection hooks

Known trainer-related event sites `0x5A70C6` and `0x5A78CC` set per-unit `+0x3A8 = 1`. They are not the authoritative count. The central count is `0x841720` and central container is `0x841708`. F5/F6 remain excluded from the current large-selection isolation.

## Next mandatory trace

1. Find the exact 1..9 control-group recall routine and determine whether it calls `0x5A6FD8`, calls `0x4ACB59` directly, or uses a separate bulk-copy/list path.
2. Audit every meaningful xref to `0x841720` and selection-container iterators for assumptions of <=90.
3. Trace UI portrait/card code, order dispatch/pathing group construction, battle gear indexing, deselection/death cleanup, and control-group serialization.
4. Compare WOTW implementation structurally and determine whether 90 is legacy or ZE-specific.
5. Only after consumer safety is proven, create an F4 Max Population companion patch that raises selection capacity. Target stress tests: 100, 150, 200 simultaneous selected units.

## Population status

Keep max-pop behavior unchanged during this isolation. Existing max-pop audit has not shown the large numeric max-pop value to size this selection container.

## Acceptance gate

- native manual cap: PROVEN = 90
- central selected count: PROVEN = VA 0x841720
- central selected container: PROVEN = VA 0x841708, linked node container
- control-group overflow path: NOT YET PROVEN
- all >90 consumers safe: NOT YET PROVEN
- raised-cap patch safe: NOT YET PROVEN
- 100/150/200 runtime stress: NOT YET RUN
