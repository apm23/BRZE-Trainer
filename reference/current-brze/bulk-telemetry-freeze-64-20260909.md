# Runtime telemetry — one-shot large rectangle freeze

Date: 2026-09-09 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe`
Trainer: proven `BRZE-Selection-Sim-Pipeline-120-Probe.exe`
Observer: `BRZE-Selection-Bulk-Telemetry-Observer.exe` (read-only)

## User runtime observation

A one-shot large rectangle drag reproduced the known simulation freeze. The observer remained responsive and captured:

- `CAND n:0 b:0 first:128 grow:128`
- `ACTIVE n:64 b:1 first:120 grow:0`
- `SIM n:0 b:0 first:120 grow:0`
- `EVENT used:258`
- `EVENT remain:4294967294` (`0xFFFFFFFE`, i.e. unsigned representation of -2)
- event pointer non-null (`0x02D41898` in this run)
- history transition from `A:0 S:0 E:0/256` to `A:64 S:0 E:258/4294967294`

## Interpretation

This is direct runtime evidence that the large-drag freeze occurs before simulation-side selection consumption begins. The UI/producer side reaches 64 selected units, while the event-buffer state crosses its nominal 256-byte boundary and the remaining counter underflows to `0xFFFFFFFE`.

`CAND n:0` means the sampled post-freeze state no longer contains rectangle candidates. Because the candidate phase is very short and observer sampling is ~5 ms, `MAX cand:0` does not prove the candidate list was never populated; it only means the observer did not catch it while non-zero.

The decisive signal is the producer/queue divergence: ACTIVE=64, SIM=0, EVENT remaining underflowed.

This re-opens the event-buffer/flush path as the primary suspect despite the earlier EventBuffer-1024 probe failure. That earlier probe must be audited for whether its 1024-byte backing state and all queue semantics were truly active during the failing drag; its runtime failure can no longer be treated as proof that the stock 256-byte boundary is irrelevant.
