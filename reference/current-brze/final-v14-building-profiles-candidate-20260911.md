# BRZE Trainer FINAL V14 — Building Profiles Candidate

Date: 2026-09-11 (Asia/Tokyo)

## Status
**Architecture/static/build proven. Runtime multi-profile smoke pending user test.**

V14 advances the V13 integrated trainer with per-building output profiles while deliberately staying inside normal vanilla training buildings. Arbitrary Peasant Hut/tree training and red-X eligibility bypass remain deferred.

## UI
- Main window: `1720x820`.
- Left: main cheats + SYSTEM STATUS.
- Right: Unit Changer + Building Selector + 9-slot editor.
- SYSTEM STATUS is a read-only multiline text box with vertical scrolling, preventing the V12/V13 bottom clipping problem by construction.
- Both CLEAN and DIAGNOSTICS variants are built.
- DIAGNOSTICS appends raw subsystem/core status including Unit Changer game/context/monitor/native recipes.

## Building profiles
Twelve independent profiles, three base training buildings per clan:
- Dragon: Dojo, Target Range, Alchemist Hut
- Serpent: Tavern, Sharpshooter's Guild, Alchemist Hut
- Lotus: Forge, Blade Garden, Training Yard
- Wolf: Combat Pit, Ballistics Grounds, Quarry

Each profile stores its own nine independent output slots. Multiple profiles can remain active simultaneously with different output lists. Per-profile mode is automatic: 0 slots OFF, 1 slot SINGLE, 2–9 slots MULTI.

## Profile identification architecture
The trainer does not guess hard-coded building type IDs. It calls BRZE's stock native UnitIn -> UnitOut mapper (`RVA 0x0D69F6`) using a vanilla first-tier training fingerprint for each profile. Unmatched buildings remain fully native.

Fingerprints:
- inputs `{5,5,5,24,24,24,40,40,40,51,51,51}`
- outputs `{8,0,1,29,21,23,30,38,41,46,48,49}`

The mapper itself remains stock and the V5/V13 completion + native extra-unit creation architecture is retained and generalized to per-profile configuration.

## Preserved behavior
- Peasant 3s remains independent through `SelectionCore.Tick(peasant.Checked)`.
- Pause Peasant remains separate through `PausePeasantCore` / local creation flag RVA `0x467AF4`.
- V11 ALL ON manual exclusions remain; building profiles are also left unchanged by ALL ON.
- ALL OFF disables all building-profile slots too.
- Refresh Trainer preserves profile selections and dropdown choices while cleanly rebinding runtime cores.
- Reveal Journey transition behavior from V11 remains preserved outside explicit refresh.

## Build pin
- branch: `instant-death-v4-hover-telemetry`
- head: `0ff3c609baccc4c8c332ad5058b6bb467c434699`
- workflow: `Final V14 Building Profiles Clean Diagnostics`
- run: `34561393800`
- job: `103144673297`
- artifact: `10184476597`
- artifact name: `BRZE-Trainer-FINAL-V14-BuildingProfiles-Clean-Diagnostics`
- artifact ZIP SHA-256: `44c491bf3eb654ec47d06a731c1553f7c42ff70485677fcaf9a2baada41a3303`

Outputs:
- CLEAN standalone: `BRZE-Trainer-FINAL-V14-Clean.exe`, 66,041,305 bytes, SHA-256 `d24b352d85ac7152a0bb546e5b5ff4c8b13035405fb91d134ae7eafb5028c54b`
- DIAGNOSTICS standalone: `BRZE-Trainer-FINAL-V14-Diagnostics.exe`, 66,041,545 bytes, SHA-256 `4762cd121beb03c100973be639cb57668aade0ecf30dab968081fe6e7c20eed3`
- CLEAN small: `BRZE-Trainer-FINAL-V14-Clean-Small.exe`, 248,478 bytes, SHA-256 `46b2941e19e18d1c03fe34fe42118edcf2cb8c31116a5df4ad0166a276a5eea2`
- DIAGNOSTICS small: `BRZE-Trainer-FINAL-V14-Diagnostics-Small.exe`, 248,990 bytes, SHA-256 `18fa2c3cca41ab0176d60d790ea87053bd09295a8c39aa347702eb9324ea5979`

Small builds require .NET 8 Windows Desktop Runtime x86.

CI: generation PASS, architecture guard PASS, CLEAN compile PASS, DIAGNOSTICS compile PASS, all four publishes PASS, 0 errors. Four warnings are pre-existing unused fields in legacy `Native` code.

## Runtime smoke required before locking V14
1. Verify 1720x820 layout fits and SYSTEM STATUS scrolls instead of clipping.
2. Open both CLEAN and DIAGNOSTICS.
3. Configure at least two different building profiles with different outputs (e.g. Dragon Dojo vs Target Range), keep both active, and train in each building.
4. Confirm no cross-profile bleed.
5. Confirm an unconfigured normal training building remains vanilla.
6. Stress more profiles / nine outputs after 1–2 profile proof.
7. If a failure occurs, capture the DIAGNOSTICS Unit Changer monitor/profile-hit lines.
