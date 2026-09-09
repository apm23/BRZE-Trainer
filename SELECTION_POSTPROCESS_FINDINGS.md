# Selection post-process findings

Runtime after the surgical-growth probe:
- active selection can sit safely at exactly 90 with active list `first=90`, `growth=90`;
- normal manual selection still refuses additions above 90 while the native manual guard remains 90;
- after a raised-manual-cap probe, bulk drag near the old limit can crash; a later large drag can hard-freeze; single Shift-click behavior around 90 remains inconsistent and needs telemetry with the patch confirmed ON.

New static evidence from BRZE 1.60:

## Drag/preselection candidate list

There is a separate selection-candidate container at `0x8416E0`, count `0x8416F8`.

`0x5A6F45` adds candidates and contains its own native 90 guard:

```asm
5A6F55 cmp dword ptr [0x8416F8],0x5A
5A6F5C je  0x5A6F92
...
5A6F7E mov ecx,0x8416E0
5A6F83 call 0x4ACB59
```

This container is used by drag/rectangle selection paths (`0x5D461F -> 0x5A6F45`). Therefore enlarging only the authoritative active list is insufficient for large drag-select.

Its constructor in `0x5A6BA1` initializes candidate and active containers with 90/0:

```asm
5A6BC3 push 0x5A   ; candidate first=90
... ecx=0x8416E0
5A6BC5 call 0x4ABFE6

5A6BCA push 0
5A6BCB push 0x5A   ; active first=90
5A6BCF mov ecx,0x841708
5A6BD4 call 0x4ABFE6
```

## Post-selection sort/rebuild is a second hard 90/0 boundary

`0x5A719F` is called after multiple native selection paths, including:
- `0x5D41B0`
- `0x5D457D`
- `0x5D47EA`
- control-group recall `0x5A78E6`.

It reinitializes two temporary selection containers before sorting:
- `0x841784` (count `0x84179C`)
- `0x8417AC` (count `0x8417C4`)

Both are reset to first=90, growth=0 at `0x5A71AF..0x5A71BF`.

After distributing the current active selection into those temporary lists, it then reinitializes the authoritative active list itself to first=90, growth=0 at `0x5A7297..0x5A729F`, then copies entries back.

Therefore simply setting active-list `+0x24` growth before selection is NOT persistent: the same input event can enter `0x5A719F`, reset active growth back to zero, and rebuild through 90-node temporary pools. A correct >90 patch must keep all participating selection containers coherent during the native post-process, not just the active list.

## 10 x 0x28 container array

`0x841730` points to an allocated array of ten 0x28-byte linked-container objects, each initialized 90/0 (`0x5A6C50..0x5A6C6F`, and re-init at `0x5A6E26..0x5A6E52`). These are used by the separate `+0x370` grouping/team subsystem (`0x5A7A6A+`) and should not yet be conflated with the `+0x36C` hotkey-group recall path proven at `0x5A780B`.

## Next diagnostic

Use a minimal trainer only (no legacy runtime loop/hooks) and raise the complete native selection pipeline to 120 coherently:
- active AddUnit guard 90->120 (`0x5A7006` immediate);
- candidate/preselection guard 90->120 (`0x5A6F5B` immediate);
- candidate constructor first pool 90->120 (`0x5A6BC4` immediate);
- active constructor first pool 90->120 (`0x5A6BCC` immediate);
- temp A first pool 90->120 (`0x5A71B1` immediate);
- temp B first pool 90->120 (`0x5A71B9` immediate);
- active rebuild first pool 90->120 (`0x5A7299` immediate);
- rescue currently constructed candidate and active containers by setting their current growth field to 120 once while F4 is enabled.

Also expose read-only telemetry for active count, candidate count, temp A/B counts, first/growth values, and both cap bytes. If a freeze remains below 90/120 with all container states coherent, then the next suspect is a downstream selection consumer rather than allocator capacity.