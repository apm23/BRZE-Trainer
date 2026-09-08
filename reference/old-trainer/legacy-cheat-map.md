# Legacy trainer cheat map (Battle Realms/Winter of the Wolf v1.50q)

Source: exact supplied trainer binary after successful UPX unpack. This is a technique/reference map only; addresses belong to the old v1.50q game and must be remapped for BRZE 1.60.

## Trainer architecture

- Hotkeys are polled with `GetAsyncKeyState` from a 100 ms timer.
- Code cheats use a remote trampoline/code-cave pattern:
  1. find `Battle_Realms_F.exe`
  2. `OpenProcess`
  3. `VirtualAllocEx` remote executable cave
  4. write custom code to the cave
  5. patch the game site with `JMP cave` and one NOP (6-byte overwrite)
  6. custom code executes the displaced original instruction and jumps back
- Direct pointer/value cheats use `ReadProcessMemory` / `WriteProcessMemory` with temporary `VirtualProtectEx` protection changes.

## F1 - Maximum Resources

Old hook site: `0x46E72B`

Injected body:
```asm
cmp ecx, dword ptr [0x73E890]   ; local player pointer check
jne original
mov eax, 99999
mov [ecx+0xD4], eax
mov [ecx+0xD8], eax
mov [ecx+0xDC], eax
mov [ecx+0xE0], eax
original:
mov eax, [ecx+0xD4]
```

Technique: event hook in player-resource getter/update path, not an external high-frequency freeze.

Observed BRZE 1.60 relationship: these player resource fields shifted by +4 in our current build (`+D8/+DC/+E0/+E4`).

## F2 - Maximum Population

Old hook site: `0x46E732`

Injected body:
```asm
mov dword ptr [0x718568], 99999999
mov eax, [ecx+0xD8]
```

Technique: event hook + write of old global max-population value.

## F3 - Maximum Yin/Yang

Old hook site: `0x46E739`

Injected body:
```asm
mov eax, [0x73E890]             ; local player pointer
mov dword ptr [eax+0x2E4], 10
mov dword ptr [eax+0x2E8], 10
mov eax, [ecx+0xDC]
```

Observed BRZE 1.60 relationship: Yin/Yang offsets shifted by +4 (`+2E8/+2EC`).

## F4 - Instant Unit Training

Old hook site: `0x41BECB`

Injected body (exact important pattern):
```asm
cmp ecx, [edi]                  ; local-player/owner gate in old routine
jne original
mov dword ptr [ebx+0x490], 0x0064B540
original:
mov esi, [ebx+0x490]
```

Technique: code-cave hook in the training-progress read/update path. It does **not** scan all buildings and does **not** repeatedly freeze the field externally. It writes progress immediately before the game's own progress read, then lets the original instruction continue.

Critical discovery for BRZE 1.60:
- training progress offset is still `+0x490`
- current completion threshold in code is `0x00640000`
- old trainer deliberately writes slightly above it: `0x0064B540`
- current equivalent training update routine is around `0x4D6573`; progress is read/added at `0x4D65DC` and stored at `0x4D65E2`.

This is now the preferred pattern for our F7 remap.

## F5 - Infinite Watchtowers

Old hook site: `0x46E740`

Injected body:
```asm
xor eax, eax
mov [0x6F9448], eax
mov [0x6F95E8], eax
mov [0x6F96C8], eax
mov [0x6F9798], eax
mov eax, [ecx+0xE0]
```

Technique: event hook that clears four clan/global watchtower counters.

## F8 - Demolition Mode

Old hook site: `0x4BA729`

Injected body:
```asm
mov dword ptr [eax+0x8C], 0
push dword ptr [eax+0x8C]
```

Technique: code-cave hook that forces a mode/state field to zero before the original push.

## F9 - Pause Peasant Production

Direct writes (no code cave):
- `0x6F93D8`
- `0x6F9578`
- `0x6F9658`
- `0x6F9728`

ON writes `0` to all four. OFF restores `1` to all four.

## F10 - Maximum Wolves

Old trainer reads a player pointer from old global `0x73E810`, then writes one byte:

```text
[player + 0x250] = 250
```

Technique: direct pointer-chain write, not a hook.

This gives us a strong field/signature target for BRZE 1.60, but the current offset must be proven rather than assumed.

## Delete - Instant Build / Repair / Research / BattleGear

This is a PUSH action, not a persistent toggle.

The old trainer resolves object pointers from old globals `0x73E810` and `0x73E80C`, then writes the same three fields on each resolved object:

- `+0x8E`  <- value from constant `0x1388` (5000)
- `+0x492` <- value from constant `0x64` (100)
- `+0x4BE` <- value from constant `0x64` (100)

Technique: direct object writes on keypress. This is the most useful old reference for our future instant building/research/BattleGear implementation.

## Page Up - Infinite Health/Stamina

Old hook site: `0x4A4D54`

The old trainer does **not** use an invincibility flag. It hooks a unit field-read path and iterates the old game's selected-unit list:

- selected count global: `0x73E758`
- selection list base global: `0x73E74C`
- entry spacing: `0x0C`

For each selected unit pointer it writes:

```asm
mov dword ptr [unit+0x404], 0x01FFFFFF
mov dword ptr [unit+0x400], 0x7D00FFFF
```

The displaced original instruction is:
```asm
mov eax, [esi+0x404]
```

Technique: event hook + selected-unit list iteration. No `UnitSetInvincible` style flag.

For BRZE 1.60 the current unit layout is different (`HP +0x404`, stamina `+0x408`), so only the selection-list technique should be reused, not the old offsets/values.

## Page Down - Instant Death

The old trainer resolves selected-unit pointers through the same selection infrastructure and performs direct field writes. This remains a lower-priority reference because it is not part of our requested final hotkeys.

## Priority remap order for BRZE 1.60

1. F7 training: reproduce old F4's progress-read hook at current `0x4D65DC`, local-owner only, write `0x0064B540`, execute displaced `add eax,[esi+0x490]`.
2. F5/F6: map current selection-list globals so selected units can be handled without scanning all 2000 slots.
3. F8/instant building/research: remap the old Delete object's `+0x8E/+0x492/+0x4BE` semantics to current structures.
4. F10 wolves: trace current equivalent of old player `+0x250` field from `UnitGiveWolfToUnit` and related code.
