# BRZE Unit Clone Lab — STATE

Status: **RUNTIME-PROVEN / HERO-CAPABLE NATIVE COPY-PASTE LOCKED**
Date: 2026-09-14 JST

## Runtime verdict
The user directly runtime-tested the first hero-capable build and reported immediate success.

Locked conclusions:
- selected-unit COPY works at runtime;
- PASTE BESIDE works at runtime;
- the native position-spawn path is accepted by BRZE;
- hero/unique/story unit types are allowed by this clone architecture and were not blocked by trainer-side filtering;
- group duplication through native spawn is now the authoritative base for future clone integration.

Exact hero identities from the successful mixed test were not separately enumerated in chat, so do not invent a per-hero whitelist. Treat the architecture as hero-capable, and investigate only if a specific hero later shows a unique restriction.

## Goal
Copy the currently selected units and paste native-created duplicates beside the original formation.

Hero/unique/story unit types are intentionally allowed. There is no regular-unit-only gate.

## Architecture
- Read current selection from selection-list RVA `0x441708`.
- List node layout confirmed from BRZE container code: `next +0x0`, value/Unit* `+0x8`, list count `+0x18`.
- Capture only compact descriptors: Unit Type (via UnitDef `+0x74`), owner `+0x240`, X `+0x28`, Y `+0x2C`.
- Never raw-clone a Unit struct.
- Native position-spawn wrapper: preferred VA `0x4C4A1C`, RVA `0x0C4A1C`.
  - observed arguments: unit type, owner, output unit-id pointer, X, Y.
  - wrapper prepares native spawn descriptor / terrain Z internally then calls native allocator `0x5D2DB5`.
- Game-thread execution uses render-frame hook RVA `0x135C43`, exact stock bytes `55 8B EC 83 EC 5C`.
- No CreateRemoteThread.
- Full x87/MMX/XMM state guarded with FXSAVE/FXRSTOR plus pushfd/pushad.
- Paste queue processes max 4 native spawns per render frame.
- Group formation is preserved and shifted on +X. Repeated paste increases the shift to avoid exact overlap.
- Max copied selection: 120 units, matching the proven Selection120 production ceiling.
- Hook uninstall restores stock bytes only if current bytes still exactly match this lab's own patch.

## Coexistence rule
This lab currently uses the same frame-hook site as Instant Death. Standalone lab tests should still close the main trainer before arming Paste. When integrating into the main trainer, merge clone dispatch into the existing frame-hook architecture instead of installing a competing hook.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Head that triggered build: `9b4e6409e2c84bcfda964d24e9d1f0f35d4b7096`
Workflow: `Unit Clone Lab Native Copy Paste`
Run: `34767166086` — SUCCESS
Job: `103750083856` — SUCCESS
Artifact: `10320592461`
Artifact digest: `sha256:54281cedbc89b6ef5870c84b566d3a03333fa305e316fb9f9f6d4b116ff91db5`

Binaries:
- Standalone SHA-256 `059a47fd7d6b98daea82ec202d573fe6730d836bf76972170bee0bfbbf5f5b43`
- Small SHA-256 `34c85b1d5e18335b7a254b08b696ff1a6e3362c5a299288f7ad6304b4396f51f`

## Next phase
Use this runtime-proven clone architecture as-is. Do not redo the normal-unit-vs-hero proof split.

Next research target requested by the user:
1. identify Grayback's real native buff/effect application path;
2. identify Issyl's real native speed-effect application path;
3. apply those effects to all currently selected units;
4. prefer native effect/status application over permanent raw stat edits;
5. once proven, integrate clone + selected-unit hero effects into the main trainer while preserving the existing V18.3/V19 rollback bases.
