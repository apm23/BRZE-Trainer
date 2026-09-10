# Final V12 — Clean + Diagnostics status variants — 2026-09-10

## Scope
V12 is a presentation-only split built on the same V11 Journey-safe runtime core. It does not modify Peasant 3s logic, Instant Death targeting/modes, Wolves V8, Horse core, Health/Stamina, or Reveal transition safety.

## Shared human-readable SYSTEM STATUS
Both executables show status in this order:
1. `GAME` — current BRZE/battle readiness and safety state.
2. `ACTIVE CHEATS` — concise list of enabled/armed cheats.
3. `CHEAT STATUS` — one cheat per aligned line with ON/OFF/ARMED and a short human-readable behavior/status description.

The old mixed raw dump and scattered `MAP/WOLVES/HORSES` layout are removed.

## Clean executable
`BRZE-Trainer-FINAL-V12-Clean.exe`
- No RAW DIAGNOSTICS section is compiled into the visible status output.
- Window stays at 1270x770.
- Intended normal-play build.

## Diagnostics executable
`BRZE-Trainer-FINAL-V12-Diagnostics.exe`
- Adds `RAW DIAGNOSTICS` under CHEAT STATUS.
- Window is 1270x920 to provide extra vertical room.
- Raw subsystem messages remain exact and are labeled by source:
  - WOLVES
  - DEATH
  - HORSES
  - PEASANT/SEL
  - STAMINA
  - HOOKS
  - NATIVE
- Intended for investigating a scene-specific or runtime-specific failure without losing the readable status summary above it.

## Preserved V11 behavior
- Journey-safe Reveal transition guard preserved.
- ALL ON preserves Horses, Wolves, and Death Burst as manual state; it enables the normal/core group and Reveal.
- ALL OFF disables every cheat without exception.
- Peasant 3s wiring remains `SelectionCore.Tick(peasant.Checked)` and is intentionally unchanged pending scene-specific investigation.

## Build pin
- branch `instant-death-v4-hover-telemetry`
- build head `7b341a05562383a9f5b7b9b8995c25d5ccdcdf75`
- workflow `Final V12 Status Variants`
- run `34464050701` SUCCESS
- job `102828338136` SUCCESS
- artifact `10146791568`
- artifact SHA-256 `4867025fa74dd127368d4300f7aef9030fe1de63029434f58aafb4c3bb0af161`

### Clean standalone
- size `66,023,777` bytes
- SHA-256 `090e98115f120fa5ea5d12bb3ccda6427308ea045d218ddf03d0bcdc365b14f1`

### Diagnostics standalone
- size `66,024,168` bytes
- SHA-256 `96ff4a3d42a7940bebfe901278eed20382f461da44b1a897b4ce72cec6e0f6db`

## Runtime state
Compilation and static architecture checks pass. V12 status presentation itself is pending user visual/runtime smoke. V11 runtime issues/fixes remain separate and authoritative until user confirms V12 presentation.
