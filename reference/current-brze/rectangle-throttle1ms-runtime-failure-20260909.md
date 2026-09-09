# Runtime result — rectangle throttle 1 ms probe

Target: authoritative `Battle_Realms_F(5).exe`.

Probe: `BRZE-Selection-Rectangle-Throttle-1ms-Probe-FIXED`.

Runtime result reported by user on 2026-09-09:

- one large rectangle drag still freezes the game
- the fixed trainer itself no longer throws the earlier `IndexOutOfRangeException`

Interpretation:

- simply inserting `Sleep(1)` after each rectangle AddUnit call does **not** fix the bulk-drag freeze
- important correction: `Sleep(1)` still executes inside the same rectangle-selection invocation/game tick, so this result does **not** prove that distributing AddUnit processing across multiple ticks would fail
- do not treat 1 ms in-function throttling as a real cross-tick scheduling experiment

Next step: use a read-only telemetry observer alongside the clean Sim120 probe to capture candidate/active/simulation/event-buffer state at the freeze boundary before changing more game code.
