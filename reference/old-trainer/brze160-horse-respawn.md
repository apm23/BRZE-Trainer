# BRZE 1.60 horse respawn mapping

Static RE against exact `Battle_Realms_F.exe` SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`.

## HorseRespawnTime config

String `HorseRespawnTime` is referenced by parser code at VA `0x626ABA`. The parsed integer is stored at config-record offset `+0x48`.

The config table is allocated/loaded as records of stride `0x98` at global VA `0x83FF2C` (RVA `0x43FF2C`). A current/default config index is exposed indirectly through VA `0x840034` (RVA `0x440034`): the global contains a pointer, and the index is the dword at that pointer.

## Runtime horse respawn path

Function beginning VA `0x523CB7` reads the exact field:

```asm
523ccf  mov eax,[0x840034]
523cd9  imul ecx,[eax],0x98
523cdf  mov eax,[0x83ff2c]
523ce4  imul esi,[ecx+eax+0x48],0x3e8
```

Thus `HorseRespawnTime` is converted from seconds to milliseconds.

The same routine compares elapsed time against that threshold, then enters horse-spawn logic. In that branch it uses unit/type ID `0x51` and keeps bounded spawn-pool counters at globals `0x879470/0x879474` (and a second paired pool `0x879478/0x87947C`). It does not blindly create infinite concurrent horses; it only respawns when pool capacity permits.

The main game tick calls this routine at VA `0x5C32A7` with context `0x879470`.

## Horse capture confirmation

A separate interaction path at VA `0x5DBC4C` explicitly checks:

```asm
cmp dword ptr [edi+0x23c],0x51
```

and on a successful building interaction increments per-player stat table field `+0x2C`. Export `GetPlayerNumHorsesCollected` also reads that same `+0x2C` field. Therefore `+0x2C` is a horse-collected statistic/event counter, not the horse availability stock to freeze.

## Preferred trainer technique

For the requested F10 horse half, the safest current candidate is **instant horse replacement**, not editing the achievement/stat counter:

1. resolve config base from RVA `0x43FF2C`;
2. resolve config index through pointer at RVA `0x440034`;
3. save original `record + 0x48` value;
4. while F10 horse mode is enabled set `HorseRespawnTime` to `0`;
5. restore the original value when disabled/detached.

This preserves the game's own horse spawn-point pool/capacity logic, so it should avoid unbounded horse flooding. This mapping is statically strong but still needs runtime validation before being called final.
