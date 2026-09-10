# V11 Journey Reveal transition-safe candidate — 2026-09-10

## User runtime report from V10
- Final V10 UI/readability: user reported everything clear.
- New Journey regression: in Kenji Journey, leaving Reveal Map ON across story/map scene transitions crashes BRZE. Manually disabling Reveal before transition avoids the crash.
- Peasant 3s did not work in the scene after Swan's Pool, but user is still investigating whether this is scene-specific. **Peasant code is intentionally untouched in V11.**
- Requested master preset change: ALL ON controls normal/core cheats only; Horses, Wolves, and Death Burst remain manual-only. ALL OFF disables every cheat without exception.

## Static transition finding
Current BRZE Fog-of-War reinitialization entry is preferred VA `0x50D868`, RVA `0x10D868`.
Its first five stock bytes are `53 8B DC 51 51`.
This function immediately calls FOW teardown at preferred VA `0x50DAD0` (call site `0x50D887`) before allocating buffers for the new map.
The native FOW state global is preferred VA `0x841D04`, RVA `0x441D04`.

## V11 Reveal architecture
`RevealMapCore.cs` retains the proven native setter at RVA `0x10DBD7`, but adds a tiny game-thread transition guard at RVA `0x10D868` while Reveal has been used:
- before FOW teardown, force native FOW state to `1` (normal/enabled),
- increment a remote generation counter,
- execute the displaced stock five-byte prologue,
- jump back to BRZE.

The trainer no longer stops/detaches RevealMapCore when the general `GameGate` temporarily reports not-ready during Journey/menu transitions. This keeps the guard alive across the map swap.

When the generation counter changes, RevealMapCore treats Reveal as temporarily normalized, waits 1200 ms for the new FOW map to settle, then re-applies Reveal automatically if the user toggle is still ON.

On stable in-battle trainer shutdown it restores native fog and removes the guard. During an unsafe loading/transition shutdown it avoids invoking the heavy FOW setter.

## ALL ON / ALL OFF V11 — corrected semantics
ALL ON (button or F9):
- turns ON Rice, Water, Yin/Yang, Population, Training, Peasant 3s, Health, Stamina, Reveal;
- **does not change Horses, Wolves, or Death Burst at all.** They remain exactly in their current manual state.

ALL OFF (button or Shift+F9): disables every toggle without exception, including Horses, Wolves, Reveal, and Death Burst.

## Corrected final build pin
- branch `instant-death-v4-hover-telemetry`
- build head `81dab371ed16403f65aabbf34f636e3f69937b90`
- workflow `Final V11 Journey Safe`
- run `34461390521` SUCCESS
- job `102819806353` SUCCESS
- artifact `10145725257` (`BRZE-Trainer-FINAL-V11-JourneySafe-Pack`)
- artifact ZIP digest `dc36628c79a70ac3790e36550d339d7473f575e5b89162337bdf41cbdbd7259a`
- Standalone 66,023,256 bytes, SHA-256 `acc703543a03c075c8c4272c1e6ec4911005678b4096eea770061339e64615d3`
- Small 201,374 bytes, SHA-256 `384c28e68668b4ea899827c91544370367dd85845d430643d551014f328075b7`

## Runtime status
V11 is **compile/static-proven only**. Critical runtime test: leave Reveal ON through the same Kenji Journey narration/map transition that crashed V10. Success requires no crash and Reveal automatically returning on the next map without manual toggling.
