# Wand / WeMod Unlimited Horses — runtime diff (2026-09-10)

Target: authoritative BRZE 1.60 `Battle_Realms_F(5).exe`.

User captured OFF -> ON -> OFF with the read-only observer while the same Stable remained selected.

Proven code delta while Wand Unlimited Horses is ON:
- RVA `0x0D5482`, 6 bytes
- stock: `83 3E 00 8D 41 01`
- ON: `E9 <rel32> 90` (runtime capture jumped to an external code cave)
- patch is reversible when toggled OFF.

Static disassembly of the authoritative target maps this site to the six-slot Stable horse-count loop in the function at VA `0x4D5465`:
- prior setup adds `0x5AC` to the building pointer
- loop count = 6
- slot stride = `0x1C`
- stock body begins `cmp dword ptr [esi],0 ; lea eax,[ecx+1]`

Runtime data delta on the selected Stable while Wand was ON:
- `building+0x5AC`: `0 -> 1 -> 1`
- `building+0x5C8`: `0 -> 1 -> 1`
- `building+0x5E4`: `0 -> 1 -> 1`
- `building+0x600`: `0 -> 1 -> 1`
- `building+0x61C`: `0 -> 1 -> 1`
- `building+0x638`: `0 -> 1 -> 1`

These are exactly six entries separated by `0x1C`, matching the native loop. The values remain 1 after the Wand cheat is toggled OFF, which strongly supports a slot-fill mechanism rather than the old `HorseRespawnTime` config hypothesis.

Do not reintroduce `HorseRespawnTime=0` as Unlimited Horses. Next port should act only on this proven six-slot Stable mechanism and preserve non-local/AI behavior where possible.
