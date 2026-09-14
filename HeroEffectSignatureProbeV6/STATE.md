# BRZE Hero Effect Signature Probe V6 — STATE

Status: **RUNTIME-PROVEN SIGNATURE TRACKER — PARENT TIMER NOT ISOLATED**
Date: 2026-09-14 JST

## Runtime result
Three clean ORIGINAL ISSYL passes completed on the same armed target with intentionally different human input timing.

Observed lifetimes:
- Pass A: 10725.9 ms / 343 polls
- Pass B: 10713.8 ms / 342 polls
- Pass C: 10733.2 ms / 343 polls
- total spread: only 19.4 ms

Therefore the automatic lifecycle timing is stable and effectively independent of user reaction speed.

## Signature lock — proven
Every pass found an effect record satisfying BOTH identity fields:
- `record+0x058 = 0xA5` — proven Issyl runtime ability ID
- `record+0x17C = pinned Unit*` — exact armed target

Passes:
- A: record `0x28BFFD08`, discovered via `Unit+0x20C->+0x008`, delay 34.1 ms
- B: record `0x28C00100`, discovered via `Unit+0x1E4->+0x014`, delay 30.7 ms
- C: record `0x28C004F8`, discovered via `Unit+0x20C->+0x008`, delay 31.6 ms

This proves path-independent content signature is required. Never hard-code the transient discovery path.

## Parent-record conclusion
No convincing continuously changing, zero-flip duration field was isolated in the A5+target parent record.

The high-change fields around `+0x028/+0x02C/+0x030` have many direction flips and are ordinary spatial/visual/activity state, not a clean timer.

`+0x194` is HARD-REJECTED as a duration field:
- A ended at 623600
- B ended at 653700
- C ended at 678400
while the natural Issyl lifetime remained ~10.72 s.
The monotonic increase across sequential casts is consistent with an absolute/global counter or cleanup timestamp, not remaining duration.

Static constants (`1`, `100`, `102`, `128`, `65535`, etc.) are stable structure/config values and are not duration proof.

## Next direction
Follow pointer fields owned by the signature-identified A5+target parent record and observe their child objects.
Promising parent pointer clusters seen in every pass include aliases around:
- `+0x060/+0x064`
- `+0x100/+0x104`
- `+0x108`
- `+0x10C`
- `+0x110`
- `+0x190`

Do not assume any one child is the timer. Build a bounded read-only child-object observer keyed from the signature parent, deduplicate aliases, and rank child fields for monotonic high-change/low-flip behavior across natural expiry.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Head: `9145fe9fabed681df1662f177998f5948e322335`
Workflow: `Hero Effect Signature Probe V6 Read Only`
Run: `34793023696` — SUCCESS
Job: `103820717005` — SUCCESS
Artifact: `10328444898`
Artifact digest: `sha256:312392823c7aa0ea58cc82dfed9bdb222f32d20cd66693f6e04c2073ec206ea7`

Binaries:
- Standalone SHA-256 `38bd623c56bada4e155e4fcc5827147bae562218c58ac8f1942e89a5469ea5be`
- Small SHA-256 `e21668996208aae24cc7cfe885e78937d5426be9b723216b3b75ce35d834456e`

## Locked rules
- `HeroEffectReplayV2` remains runtime-proven one-shot replay.
- repeated native reapplication remains permanently rejected.
- `Unit+0x460` remains rejected.
- `+0x194` in the A5 parent record is rejected as duration-specific.
- transient paths may swap; identify effects by content signature, not path.
- no duration write/integration until an effect-instance timing field is proven.
