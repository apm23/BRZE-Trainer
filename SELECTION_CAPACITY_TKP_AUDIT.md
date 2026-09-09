# Selection Capacity TKP Audit

## Runtime evidence

Specimen B with only F1-F4 and F7 enabled crashes when recalling a large ~100-unit control group. F5/F6 are disabled. Manual box/Shift selection does not crash; additional units are refused after the native cap.

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

The previous audit incorrectly treated the linked allocator as safely growable beyond 90. Full allocator inspection proves the important missing detail: the selection container is initialized with **first block = 90, growth block = 0**.

Selection post-processing `0x5A719F` initializes/reinitializes its selection-related containers with:

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

Because of cdecl/thiscall stack order, `0x4ABFE6` receives arg1=`0x5A` and arg2=`0`. It stores:

```asm
4AC013 mov [esi+0x20],eax   ; first block size = 90
4AC016 mov eax,[ebp+0x0C]
4AC019 mov [esi+0x24],eax   ; growth block size = 0
```

The insertion allocator `0x4ACB2D` checks the freelist at object `+0x8`. When the first 90 nodes are consumed, insertion #91 sees an empty freelist and calls `0x4AC4C1`.

`0x4AC4C1` chooses allocation size as follows:

```asm
4AC4D3 cmp dword ptr [ecx+0x1C],0
4AC4D8 cmovne eax,edx
4AC4DB mov ebx,[eax+ecx]
```

For the first allocation it reads `+0x20` = 90. For every later allocation it reads `+0x24` = **0**.

On insertion #91, `0x4AC4C1` therefore allocates a block with zero nodes and cannot populate the freelist. Control returns to `0x4ACB2D`, which immediately assumes a node exists:

```asm
4ACB3D mov ecx,[esi+0x8]    ; still NULL after zero-node growth
4ACB40 mov eax,[ecx]        ; NULL dereference -> crash
```

This is the exact native crash mechanism matching the user's runtime observation.

### Why manual selection is safe

Manual/Shift selection never reaches insertion #91 because `0x5A7000` rejects additions once count is 90.

### Why a 100-unit Team recall crashes

Control-group recall bypasses the 90 guard and calls `0x4ACB59` directly. Inserts 1..90 consume the initial 90-node pool. Insert #91 requests a growth block, but growth size is configured as zero, leaving the freelist NULL; `0x4ACB2D` then dereferences NULL.

This means the crash is not presently evidence that UI/pathing/order consumers cannot handle >90. The game crashes **before a valid 91st selection node is created**. Downstream consumers still need auditing before declaring 200 selections safe, but the immediate crash culprit is now proven.

## Safe patch direction

Do NOT only change the manual `cmp 90` guard. A correct Large Selection companion for F4 must at minimum change both parts coherently:

1. raise the manual policy cap at `0x5A7000`;
2. ensure selection container allocation can supply enough nodes by changing/replacing the `first=90, growth=0` initialization used for `0x841708`.

For a target of 200 selected units, a conservative first diagnostic specimen should configure the active selection container for at least 200 nodes before permitting the manual guard above 90. Temporary selection containers at `0x841784` and `0x8417AC` created in `0x5A719F` must also be audited because the same 90/0 constructor pattern is used there.

After allocator capacity is fixed, audit post-selection/UI/order/pathing consumers and runtime stress at 100/150/200 before merging into production F4.

## Population status

Max-pop remains independent of this root cause. F4 will eventually bundle Max Population + Large Selection Capacity, but the population value itself is not the selection crash mechanism.

## Acceptance gate

- native manual cap: PROVEN = 90
- central selected count: PROVEN = `0x841720`
- central selected container: PROVEN = `0x841708`
- control-group recall bypass: PROVEN = `0x5A78C7`
- initial selection node pool: PROVEN = 90
- growth block size: PROVEN = 0
- exact insertion #91 failure: PROVEN = NULL freelist dereference at `0x4ACB40`
- downstream >90 consumers safe: NOT YET PROVEN
- raised-cap diagnostic patch: NOT YET RUNTIME TESTED
- 100/150/200 runtime stress: NOT YET RUN
