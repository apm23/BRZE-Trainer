# Wand Unlimited Horses runtime diff + Horse v2 port build — 2026-09-10

Authoritative target: `Battle_Realms_F(5).exe`, BRZE 1.60.

## User runtime diff evidence
Read-only observer OFF -> ON -> OFF while the same Stable remained selected.

Wand/WeMod code patch while Unlimited Horses is ON:
- RVA `0x0D5482`, 6 bytes
- stock bytes: `83 3E 00 8D 41 01`
- ON bytes: `E9 <rel32> 90`
- runtime sample detour target resolved to an external code cave.

Static target disassembly proves this site is the six-slot Stable horse-count loop in the function at VA `0x4D5465`:
- slot0 = building + `0x5AC`
- loop count = 6
- stride = `0x1C`

Selected Stable data changed during Wand ON:
- +0x5AC: 0 -> 1 -> 1
- +0x5C8: 0 -> 1 -> 1
- +0x5E4: 0 -> 1 -> 1
- +0x600: 0 -> 1 -> 1
- +0x61C: 0 -> 1 -> 1
- +0x638: 0 -> 1 -> 1

This disproves the old HorseRespawnTime hypothesis for Stable stock and strongly supports six-slot fill semantics.

## Horse v2 port candidate
Branch: `horse-v2-wemod-slotfill-probe`
Source commit produced by CI: `1d37970e3b1a5326e0d8381146059e0111610988`
Workflow run: `34428802655` SUCCESS
Artifact: `10133691448` (`BRZE-Trainer-Peasant3s-StaminaV2-HorseV2`)
Artifact ZIP SHA-256: `60f8f29d3239e6ff70a5740fdf24670aed1a562a285f6e40420d96df38fb8d97`
EXE SHA-256 after extraction: `c73defd697ce4343afd272c9f41ec8893f7901cbd0c87cf21322a85f6f0ecb4c`

Port behavior:
- detours only proven RVA `0x0D5482`
- stock bytes are hard-pinned before install
- reconstructs the Stable base dynamically for all six loop iterations using ESI/EDX and 0x1C stride
- fills current slot to 1 only when Stable owner == local player
- then executes original cmp/lea and returns to native loop
- old HorseRespawnTime path is disabled in the merged runtime
- no external per-tick Stable scan and no manual horse spawn loop

Status: compile/publish proven only. Runtime validation still required: empty local Stable should become 6; taking a horse should replenish back to 6 without unbounded spawning. Stamina v2 in the same build is also still runtime-candidate until user test.
