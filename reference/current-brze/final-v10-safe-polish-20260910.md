# BRZE Trainer FINAL V10 Safe Polish — 2026-09-10

## User-requested polish
Preserve the V9 dark theme and tile style, but fix all clipping and add a second-line hotkey hint to every control.

Final tile mapping:
- Rice — F1
- Water — F2
- Yin / Yang — F3
- Population — F4
- Training — F7
- Peasant 3s — F12
- Health — F6
- Stamina — F5
- Horses — F11
- Wolves — F10
- Reveal — Insert
- Death Burst — End

Action tiles:
- SINGLE KILL — PgDn
- BUILD NOW — Del / F8
- ALL ON — F9
- ALL OFF — Shift + F9

The old bottom-right hotkey helper text is removed and replaced with the signature `Create By PokakBg` in a small Segoe Script accent style.

Header clipping fix:
- larger fixed window 1270x770
- header row increased to 88 px
- title and `ZEN EDITION · BRZE 1.60 · FINAL PACK` use explicit non-auto-sized label rectangles
- switch tiles widened to 190 px with explicit text rectangles, preventing the V9 clipping seen on Yin/Yang and Training.

## Safe pre-game / early-launch gate
V10 adds `GameGate`, a low-frequency read-only readiness probe.
- no BRZE: toggles can be armed; no game writer handle, hook, or cheat core is started.
- BRZE starting/menu/loading: only a read-only process handle is opened for the readiness probe; all cheat writes/hooks remain suspended.
- battle/player data ready: armed toggles automatically become active and the proven cores run.
- SINGLE KILL / BUILD NOW are ignored safely before battle instead of touching invalid game state.
- returning from battle to non-ready state suspends/detaches runtime cores.

Readiness uses current BRZE `RVA_PLAYER_PTR=0x4416A0`, `RVA_LOCAL_ID=0x4416D0`, player stride `0x5E8`, plus a readable local-player structure validation.

## Drag/window smoothness
V10 addresses the reported window-drag stutter without changing proven cheat logic:
- trainer poll interval reduced from 16 ms to 100 ms;
- status text only changes if content changed and is refreshed at most every 250 ms;
- game readiness process enumeration is cached for 400 ms;
- procedural dark backdrop is pre-rendered into a cached bitmap and only rebuilt on size change;
- the polling timer is suspended during `WM_ENTERSIZEMOVE` and resumes after `WM_EXITSIZEMOVE`.

Game-side hooks remain installed while the form is being moved, so stopping the UI polling timer during drag does not remove proven hook behavior.

## Proven cores preserved
CI explicitly rejects:
- V5 death sweep global RVA 0x3DD858
- V7 per-unit wolf-cap hook

It verifies the locked V6/V7 death targeting/control architecture, V8 Wolves Den stock path, and Reveal Map native setter before generating V10.

## Build pin
- branch: `instant-death-v4-hover-telemetry`
- source/build head: `eddce98d551fd53f8154fde5d8ff36199b2e55a9`
- workflow: `Final V10 Safe Polish`
- run: `34455956083` SUCCESS
- job: `102802264108` SUCCESS
- artifact: `10143503857` (`BRZE-Trainer-FINAL-V10-Pack`)
- artifact ZIP SHA-256: `719182489f590a096e55151ca0166bf9ddb45f9b118966f8df640aa55df15579`

Standalone:
- `BRZE-Trainer-FINAL-V10-Standalone.exe`
- 66,022,253 bytes (~62.96 MiB)
- SHA-256 `8852e15c13eb420f9aa4d760ad7a84a8db4f62efca42f9e68974e9827ab8199f`

Small:
- `BRZE-Trainer-FINAL-V10-Small.exe`
- 198,302 bytes (~0.189 MiB)
- SHA-256 `e6a69b97747323421836d95cdd96ad4a20a0beaf167e595837ac2801853ed9cc`
- framework-dependent; requires compatible .NET 8 Windows Desktop Runtime x86.

## Runtime state
All core cheat behaviors were already runtime proven in V8/V9 lineage. V10 changes UI, launch safety gating, and polling/render behavior. Final user runtime confirmation is still required for:
- no text clipping at user DPI
- trainer-open-first then BRZE startup
- toggles armed before entering a match
- smooth window dragging.
