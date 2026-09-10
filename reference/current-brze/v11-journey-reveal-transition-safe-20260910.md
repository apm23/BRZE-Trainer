# V11 Journey Reveal transition-safe candidate — 2026-09-10

## User runtime report from V10
- Final V10 UI/readability: user reported everything clear.
- New Journey regression: in Kenji Journey, leaving Reveal Map ON across story/map scene transitions crashes BRZE. Manually disabling Reveal before transition avoids the crash.
- Peasant 3s did not work in the scene after Swan's Pool, but user is still investigating whether this is scene-specific. **Peasant code is intentionally untouched in V11.**
- Requested master preset change: ALL ON must enable everything except Horses, Wolves, and Death Burst. ALL OFF must disable every cheat without exception.

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

## ALL ON / ALL OFF V11
ALL ON (button or F9):
- ON: Rice, Water, Yin/Yang, Population, Training, Peasant 3s, Health, Stamina, Reveal.
- forced/manual OFF: Horses, Wolves, Death Burst.

ALL OFF (button or Shift+F9): disables every toggle without exception.

## Build pin
- branch `instant-death-v4-hover-telemetry`
- build head `f253834bc2880bb760fbe13f8e864f2d79853e8a`
- workflow `Final V11 Journey Safe`
- run `34460846552` SUCCESS
- job `102818055069` SUCCESS
- artifact `10145507923` (`BRZE-Trainer-FINAL-V11-JourneySafe-Pack`)
- artifact ZIP digest `efb7b26c0090538401e27cf4073b5c7413dd5d7d0dc9c6b0af14367bb1995c75`
- Standalone 66,023,257 bytes, SHA-256 `b5dd85ebdcce60d3f6b6cf371c0c97fab386c3aeb3ce117f2221d5c93d65de3f`
- Small 201,374 bytes, SHA-256 `f1f6067792b9adb2e853cda5eeb6a44b6b848ea6c727e9272e683fea80752de3`

## Runtime status
V11 is **compile/static-proven only**. Critical runtime test: leave Reveal ON through the same Kenji Journey narration/map transition that crashed V10. Success requires no crash and Reveal automatically returning on the next map without manual toggling.
