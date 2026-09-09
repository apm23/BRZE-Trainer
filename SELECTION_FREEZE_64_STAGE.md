# Selection Freeze ~64 — Isolation Stage

## Runtime evidence

User reports the clean-room capacity-120 specimen still freezes around 64 selected units. This is a simulation freeze rather than process crash: world/unit animation and gameplay simulation stop, while command input can still produce acknowledgement audio. This differs from the previous >90 control-group crash.

## Important correction: previous CLEANROOM patch was still too broad

Static verification of BRZE 1.60 shows the patched immediate at VA `0x5A6BCB` is loaded through `push 0x5A / pop esi`, then the same `esi` capacity value is reused to initialize multiple selection-manager containers:

- `0x841708` active selection
- `0x841734`
- `0x84175C`
- `0x841784`
- `0x8417AC`

Therefore changing only the immediate byte at VA `0x5A6BCC` from 90 to 120 does **not** surgically change only active selection; it changes several auxiliary containers at manager initialization.

The clean-room specimen additionally changed the temporary-container initializers at `0x5A71B0` and `0x5A71B8`, plus the active-selection rebuild immediate at `0x5A7298`, and wrote growth fields for active + two temporary containers. Thus the ~64 freeze cannot yet be treated as a proven native 64-selection limit.

No direct `selected_count == 64` guard has been proven in the selection-manager code. Constants `0x40` found elsewhere must not be interpreted as unit-count limits without semantic proof.

## Next diagnostic: surgical active-growth only

New branch: `selection-capacity-surgical-growth`.

Design:
- F5/F6/F7 absent from runtime behavior; no hooks installed.
- no selection-event hook.
- no manual selection-cap patch; native manual guard remains 90.
- no constructor immediate patches.
- no auxiliary/temp-container changes.
- only active selection object `0x841708 + 0x24` (growth-block parameter) is rescued from 0 to 90 while F4 is enabled.
- max population remains bundled with F4 for the user's intended final UX.
- probe displays authoritative selected count (`0x841720`), first-block size (`+0x20`) and growth-block size (`+0x24`).

This directly tests the proven #91 allocator failure while leaving all selection topology/layout policy unchanged through selections 1..90. If ~64 freezes again under this surgical probe, the freeze is independent of the broad 120 constructor modifications and requires tracing the simulation consumer reached around that count. If 64 is passed and >90 succeeds, the previous freeze was introduced by over-broad capacity patching rather than a native 64 hard cap.
