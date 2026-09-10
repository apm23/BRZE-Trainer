# Instant Death v3 — native hover probe — 2026-09-10

Status: BUILD/STATIC PROVEN ONLY. Runtime result pending.

## User observer result
User captured `BRZE-HOVER-FORENSIC-20260910-130437.txt` but BRZE did not deliver RegisterHotKey F9 while focused, so each capture required Ctrl+Esc before recording. Six dynamic `.data` candidates were produced. `RVA 0x3DD858` superficially looked strongest because captures A/B decoded as non-local units, but static xrefs show it is simulation/reference scratch state used by spatial/unit queries. It is NOT accepted as a hover pointer and must not be patched.

## Native mouse path identified statically in target BRZE 1.60
Target SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`.

InterfaceMouse helper at preferred VA `0x535EFB` (RVA `0x135EFB`) converts the live InterfaceMouse cursor position and calls preferred VA `0x5D4888` (RVA `0x1D4888`) at preferred VA `0x535F27` (RVA `0x135F27`). That query returns a native Unit* in EAX and uses unit ownership/alliance filtering internally. Original call bytes at the wrapper site are:

`E8 5C E9 09 00`  (`call 0x5D4888`)

Player relation/alliance helper is preferred VA `0x5848E8` (RVA `0x1848E8`), where nonzero means same/allied under the observed call pattern.

## Probe architecture
`InstantDeathCore.cs` replaces only the 5-byte query call at RVA `0x135F27` with a call to a remote x86 wrapper. The wrapper:
1. forwards the original three stdcall arguments to native `0x5D4888` unchanged;
2. preserves the returned Unit*;
3. requires non-null Unit*, non-null `unit+0x74` definition, and owner <= 10;
4. calls native relation helper with local player ID and target owner; same/allied targets are skipped;
5. enemy-only target receives legacy death sentinel `0xFF000000` at current BRZE `unit+0x404` HP and `unit+0x408` stamina;
6. returns the original Unit* and preserves the native stdcall stack cleanup.

No 2000-unit scan, no selection dependency, no timer refill, no dynamic observer candidate is used.

PageDown now toggles the hover-death checkbox via the trainer's existing `GetAsyncKeyState` polling. Infinity Watchtower and Demolition Mode are removed from the visible list; F11 demolition hotkey is removed; legacy runtime writes for both are forced OFF. Dormant code remains for later work.

## Build pin
Branch: `instant-death-v3-native-hover`
Head: `dc3d7ec958250af18755e73fa5a9a7021b207e63`
Workflow: `Instant Death v3 — Native Hover Probe`
Run: `34437008955` SUCCESS
Artifact: `10136556551` (`BRZE-Trainer-InstantDeathV3-NativeHover`)
Artifact ZIP digest: `sha256:6b7737430cee685d8e62e3676e496ede72dd026e109a514b1edf345c63ae5925`
Published EXE SHA-256 after download/extract: `c7ea1d6e0360963030b1fec10d5f1931019a4ac9198c07708f379c2a1089c357`

## Runtime test target
- close Wand/WeMod and old trainers; restart BRZE fresh;
- enable `Instant Death — hover enemy` by checkbox or PageDown;
- do NOT select/click enemy;
- hover stationary enemy: expected death without heal-back;
- move cursor to friendly/local unit: must not die;
- hover moving enemy: native hit-test may miss while moving; when movement stops and cursor resolves the unit, expected death;
- disable PageDown/checkbox: native call bytes must be restored and hover becomes normal.
