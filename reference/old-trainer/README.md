# Old Battle Realms Trainer — reverse-engineering reference

This directory documents the user-supplied legacy trainer used only as a behavioral/architecture reference for BRZE 1.60.

## Sample

- File: `BattleRealmsTrainer.exe`
- SHA-256: `41571fc8cd83e296a60a04d934440b5a01d22ce45d111acdc31b13f16e8aa24d`
- Format: PE32 / Intel i386 / Windows GUI
- Timestamp in PE header: 2009-01-17 10:18:38
- Image base: `0x00400000`
- Packed entry point: RVA `0x9120` / VA `0x409120`
- Packer signature: UPX 3.03 (`UPX0`, `UPX1`, `.rsrc`, `UPX!`)
- Packed size: ~17.5 KiB

## Packed-image observations

The visible import table is the UPX loader/stub rather than the trainer's real import set. Visible imports include:

- `KERNEL32!LoadLibraryA`
- `KERNEL32!GetProcAddress`
- `KERNEL32!VirtualProtect`
- `KERNEL32!VirtualAlloc`
- `KERNEL32!VirtualFree`
- `KERNEL32!ExitProcess`
- `USER32!SetTimer`
- `MSVCR90!exit`
- `COMCTL32.dll` ordinal 17

Do **not** infer the trainer's cheat implementation from these packed imports alone.

## Goal

Recover and classify the legacy trainer's actual implementation, especially:

1. HP / god-mode mechanism
2. stamina mechanism
3. instant unit training
4. instant building / research if present
5. resource and population cheats
6. horse / wolf handling if present
7. hotkey/timer architecture
8. whether cheats use direct data writes, code patches, injected code caves, or process-memory polling

The useful output is the **mechanism/pattern**, not stale addresses from a different Battle Realms version. Any mechanism adopted by BRZE Trainer must be independently mapped and verified against the exact BRZE 1.60 executable.

## Current blocker

The sample is still UPX-packed. Static disassembly currently exposes the unpacking stub, not trustworthy cheat logic. Next step is to obtain an unpacked image (prefer normal UPX decompression; otherwise controlled runtime dump/rebuild), then inventory its real imports, strings, hotkey dispatch, memory-write sites, and patch constants.

## Safety rule for our trainer

The legacy sample may use an invincibility flag. Even if it does, BRZE Trainer must **not** reintroduce the known-bad BRZE 1.60 `UnitSetInvincible` / `unit+0x6A4` mechanism. We only borrow architecture that survives independent verification.
