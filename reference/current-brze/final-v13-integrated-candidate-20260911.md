# BRZE Trainer FINAL V13 Integrated — Build Candidate

Date: 2026-09-11 (Asia/Tokyo)

## Status
**Architecture/static/build proven. Runtime integrated smoke pending user test.**

This build preserves the previously locked main trainer cores and integrates the temporary-final runtime-proven Unit Changer V5 game-side engine.

## User-requested changes included
1. **Unit Changer merged into main trainer**
   - game-side engine is generated directly from `UnitChangerTrainerV5/Program.cs` rather than rewritten;
   - 9 output slots retained;
   - no manual Unit Changer master checkbox;
   - 0 checked slots = OFF;
   - 1 checked slot = SINGLE OUTPUT automatically;
   - 2..9 checked slots = MULTI OUTPUT automatically;
   - dropdown configuration remains per-slot.

2. **Clean UI clipping fix / redesign**
   - integrated clean window is `1380x1000`;
   - two compact main-cheat rows;
   - dedicated 3x3 Unit Changer panel;
   - SYSTEM STATUS receives a large independent flexible region;
   - action bar no longer overlays the Cheat Status region.

3. **Pause Peasant Production restored as a separate cheat**
   - separate from `Peasant 3s`;
   - uses current BRZE local-player creation enable array RVA `0x467AF4`;
   - local player only;
   - saves/restores the original flag;
   - hotkey: `Pause/Break`;
   - `Peasant 3s` remains F12 and continues through `SelectionCore.Tick(peasant.Checked)`.

4. **Refresh Trainer action**
   - button + `Home` hotkey;
   - drops/re-resolves runtime cores without restarting the trainer;
   - preserves UI toggle states and Unit Changer slot/dropdown configuration;
   - Unit Changer and Pause cores are cleanly restored/stopped before rebind;
   - Reveal is dropped with `Stop(false)` during explicit refresh and is reapplied next ready tick if still armed;
   - recursive Home-key refresh is explicitly guarded by waiting for the next normal timer tick before rebind.

5. **ALL ON / ALL OFF semantics**
   - ALL ON preserves V11 behavior and does not change Horses, Wolves, Death Burst; it also leaves Pause Peasant and Unit Changer manual state unchanged.
   - ALL OFF disables every cheat, including Pause Peasant and all Unit Changer slots.

## Deferred future phase
`UnitChangerTrainer/FUTURE_BUILDING_SELECTOR.md` records the later building-selector/per-building recipe concept. Arbitrary Peasant Hut/tree-style training remains deferred until a narrow red-X eligibility solution is runtime-proven.

## Build pin
- branch: `instant-death-v4-hover-telemetry`
- build head: `f0f002c6477efa97b95987e5c7a90793f3f76892`
- workflow: `Final V13 Integrated Unit Changer`
- run: `34553222193`
- job: `103120262180`
- artifact: `10181587380` (`BRZE-Trainer-FINAL-V13-Integrated`)
- artifact ZIP SHA-256: `e845e46103f98df2705c3531c6fd8141355b149f3b52af0c0a395c2456e2a9c3`

Outputs:
- `BRZE-Trainer-FINAL-V13-Integrated.exe`
  - size: `66,038,093` bytes
  - SHA-256: `28c895b39932e92b4f47bc6be9b0243e59625070714a4c0abaa9d27816572e62`
- `BRZE-Trainer-FINAL-V13-Integrated-Small.exe`
  - size: `241,310` bytes
  - SHA-256: `a879a4ed4923fc4376b634f5e7e59a8939f228b08577ecac29a0655d22eb524e`
  - requires .NET 8 Windows Desktop Runtime x86.

CI result:
- source generation: PASS
- architecture guards: PASS
- compile: PASS
- publish standalone: PASS
- publish small: PASS
- 0 errors; 4 pre-existing unused-field warnings in legacy `Native` code.

## Runtime smoke requested before locking V13
- verify full UI is visible with no Cheat Status clipping;
- verify 1 checked Unit Changer slot produces one configured output;
- verify 2+ checked slots automatically behave as multi-output; stress 9 if desired;
- verify multiple training buildings still work;
- verify `Pause Peasant` stops local automatic peasant creation and OFF resumes it;
- verify `Peasant 3s` remains independently functional;
- with several toggles and Unit Changer slots armed, press `Home` / REFRESH TRAINER and confirm states remain selected and runtime functionality resumes without trainer restart.
