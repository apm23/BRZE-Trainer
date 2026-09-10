# Exact old trainer PageDown / Instant Death mechanism — 2026-09-10

Source specimen: user-supplied legacy trainer, PE32 x86, 17,920 bytes, SHA-256 `41571fc8cd83e296a60a04d934440b5a01d22ce45d111acdc31b13f16e8aa24d`. Static UPX unpack only; specimen was not executed.

## PageDown dispatch
Unpacked code at `0x401E61` calls `GetAsyncKeyState(0x22)` (VK_NEXT / PageDown). If active, the trainer reads game-internal memory rather than Win32 cursor APIs.

Primary path:
- read byte from old game absolute `0x73E758`;
- read dword from old game absolute `0x73E71C`;
- if nonzero, read target pointer from `[value+0x08]`;
- if target nonzero, write DWORD sentinel `0xFF000000` to target `+0x400` and target `+0x404`.

Secondary list path when byte count at `0x73E758` is >0:
- read dword base from old game absolute `0x73E74C`;
- for each entry, stride `0x0C`, read candidate target from `[base + 0x434 - i*0x0C]`;
- for every non-null target, write the same DWORD sentinel `0xFF000000` to `+0x400` and `+0x404`.

The sentinel is stored in the trainer at VA `0x401168` as bytes `00 00 00 FF`.

## Important architecture result
The legacy trainer does NOT import/use `GetCursorPos`, `ScreenToClient`, `WindowFromPoint`, or related Win32 cursor APIs for PageDown. It obtains current/candidate targets from game-internal state. This invalidates the V3/V4 assumption that current BRZE RVA `0x135F27 -> 0x5D4888` is necessarily the live Instant Death hover path.

## Current BRZE correspondence lead
Current BRZE global absolute `0x7DD858` / RVA `0x3DD858` is statically proven to hold a `Unit*` in its active native subsystem:
- current code compares it against unit pool slots built from `*(0x8796A0) + index*0x818`;
- current code dereferences fields including `+0x98`, `+0x240` (owner), `+0x5E8`, `+0x60C`, `+0x610`;
- current code writes this global at `0x5E4451` from a Unit* held in EDI before invoking subsystem `0x57C0E6`.

Earlier user hover-observer snapshots also showed this global changing between two different non-local units. Therefore RVA `0x3DD858` is a strong single-pointer V5 candidate, but still requires runtime proof before being locked as the modern equivalent of the legacy Instant Death target source.
