# Instant Death V5 runtime rejection -> V6 hover-only + Reveal Map — 2026-09-10

## V5 runtime result — REJECTED AS HOVER SOURCE
User runtime test of V5 (`RVA 0x3DD858`) was decisive:
- merely enabling Instant Death was enough to affect enemy units globally;
- no cursor hover was required;
- enemy units became near-dead and then died when they stopped moving.

Conclusion: `RVA 0x3DD858` is NOT a hover-only target. It behaves as the simulation/spatial scratch Unit* predicted by the earlier static xrefs and sweeps through non-local units over time. The sentinel death write itself is proven effective, but this pointer source is permanently rejected for hover behavior.

Do not reintroduce `RVA 0x3DD858` as an Instant Death target unless new evidence overturns this runtime result.

## Why V3/V4 also failed
V3/V4 patched only the existing call at preferred VA `0x535F27` (`InterfaceMouse helper 0x535EFB -> unit query 0x5D4888`). V4 telemetry remained `qcalls:0` during passive hover, proving that particular existing call site is action/context driven rather than the passive hover path used by the requested cheat.

The underlying helper itself remains useful: it reads the live InterfaceMouse cursor coordinates at object `0x843640 +4/+8`, converts them to world coordinates, and calls the native Unit* query. Many native command paths invoke the helper with `(1,0)`.

## V6 architecture
V6 no longer waits for an existing action-specific call site. It hooks the entry of preferred VA `0x535C43` (RVA `0x135C43`), which is called every render frame from preferred VA `0x544086`.

The injected game-thread code performs exactly one live cursor query per frame:
1. preserve flags/registers;
2. set `ECX = moduleBase + RVA 0x443640` (InterfaceMouse object);
3. call `moduleBase + RVA 0x135EFB` with args `(1,0)`;
4. require non-null Unit* and non-null definition at `+0x74`;
5. require target owner `0..9`;
6. call native relation helper RVA `0x1848E8` using local player id RVA `0x4416D0` and target owner;
7. if same/allied, do nothing;
8. enemy-only: write legacy death sentinel `0xFF000000` to current BRZE HP `+0x404` and stamina `+0x408`;
9. restore flags/registers and execute the displaced original prologue.

This design has no selection dependency and no 2000-unit scan. It actively evaluates only the Unit* returned by BRZE's own live cursor query.

## Reveal Map static proof
Current BRZE exports:
- `BattleScriptInterface::EnableFogOfWar()` preferred VA `0x4CB7AD` -> `push 1; call 0x50DBD7`
- `BattleScriptInterface::DisableFogOfWar()` preferred VA `0x4CB7B5` -> `push 0; call 0x50DBD7`

Underlying native setter preferred VA `0x50DBD7` / RVA `0x10DBD7` accepts one argument and returns with `ret 4`. V6 uses a one-shot `CreateRemoteThread` call on state changes: argument `0` to reveal/disable fog, argument `1` to restore normal fog. The reveal code is not a memory freeze and does not scan units.

## Build pin — STATIC/COMPILE PROVEN
Branch: `instant-death-v4-hover-telemetry`
Head: `755a1f8f7888d496a7d43626c201df9dc56c5b80`
Workflow: `Instant Death v6 — Hover Only + Reveal Map`
Run: `34442885171` — SUCCESS
Job: `102761415852` — SUCCESS
Artifact: `10138596135` (`BRZE-Trainer-InstantDeathV6-HoverOnly-RevealMap`)
Artifact ZIP SHA-256: `79b76d18e90d4bda2c5f1bf2b4fa46ea87e4dac2f5a292ea5278f34f9572a477`
Published EXE SHA-256: `b7b2f0712213601761a2167be8587e01515987c773ed08149851ed551af07349`

CI passed V6 architecture guard, Reveal Map native-path guard, merged UI wiring guard, compile smoke, x86 single-file publish, rename, and artifact upload.

## Runtime status / required proof
Runtime proof still pending. Required test:
- with V6 ON and cursor parked on empty ground, no enemy anywhere should die;
- hover exactly one enemy without selecting/clicking: only that hovered enemy should die;
- hover friendly/local unit: it must not die;
- move cursor to a second enemy: only the second hovered enemy should then die;
- Reveal Map ON must remove fog; OFF must restore normal fog.
