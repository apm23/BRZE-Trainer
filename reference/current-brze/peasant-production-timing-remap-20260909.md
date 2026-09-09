# BRZE 1.60 — Peasant production timing remap

Date: 2026-09-09 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe` SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`

## Existing creation enable flag

The previously remapped `RVA_PEASANT_CREATION = 0x467AF4` resolves to VA `0x867AF4` at preferred base and is a 10-player creation enable/disable array. It controls whether peasant creation is allowed; it is not the production interval.

## Native timing config names

The target contains the exact configuration keys:
- `MinTimeToCreatePeasant`
- `MaxTimeToCreatePeasant`

Config loader around VA `0x61FCCC..0x61FD40` stores:
- `MinTimeToCreatePeasant` -> race/config object `+0x6C`
- `MaxTimeToCreatePeasant` -> race/config object `+0x70`

## Native schedule calculation

Function VA `0x57FFB2` is used by the automatic peasant-production update path.

At VA `0x580014`:
- `imul eax,[edx+0x70],1000` -> max-time converted to milliseconds

At VA `0x580023`:
- `imul edi,[edx+0x6C],1000` -> min-time converted to milliseconds

The function computes the next production interval from these values and finally advances the per-player schedule stored through global pointer VA `0x867AF0`.

Automatic per-player update function VA `0x57FEE9` calls `0x57FFB2` at VA `0x57FFA5` after production scheduling/spawn logic. The first argument is the player id.

## Planned local-player-only fast-production wrapper

For the next diagnostic build, patch only call site VA `0x57FFA5` to a wrapper that:
1. records the local player's old next-production timestamp from `[0x867AF0] + player*4`
2. invokes original `0x57FFB2` with unchanged arguments
3. only when `player == [0x8416D0]` and Fast Peasant is enabled, scales the newly scheduled delta by 20x
4. clamps the shortened interval to at least 1000 ms
5. writes the adjusted next-production timestamp back

This preserves native PeasantManager production/spawn semantics and avoids changing shared race config, so AI players are not intentionally accelerated.

Status: **static/remap proof; fast-production wrapper not runtime-tested yet.**
