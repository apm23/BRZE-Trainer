# Runtime result — Simulation Pipeline 120 Probe

Date: 2026-09-09 (Asia/Tokyo)

User reports the `BRZE-Selection-Sim-Pipeline-120-Probe` is approximately 70% successful.

Observed:

- Building a large selection by multiple small rectangle/shift-drag operations works.
- The previous immediate #91 crash is no longer the primary failure mode in this specimen.
- A single large rectangle/shift-drag still freezes gameplay.

Interpretation:

- UI + sort/rebuild + local-player simulation selection capacity changes are materially effective.
- The remaining reproducible failure is isolated to the bulk rectangle-selection path / batched event emission.
- Static follow-up proves rectangle candidate list `0x879748` uses the default list constructor `0x4ABFBF`, which sets `first=128, growth=128`; therefore it is not expected to exhaust around ~80 candidates.
- Selection-add event `0x552FFA` emits 4 bytes per admitted unit into the global event buffer. The buffer is allocated as 0x100 (256) bytes, so a large drag crosses a forced flush after roughly 64 4-byte events. Small drags can avoid an in-loop flush by allowing processing between batches.

Next experiment: preserve LIVE events and simulation pipeline 120, but enlarge the event buffer to 0x400 bytes using a real target-process allocation and patch all known native 0x100 reset/allocation constants consistently.
