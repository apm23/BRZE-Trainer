# BRZE Hero Effect Signature Probe V6 — STATE

Status: **BUILT / CI-PROVEN READ-ONLY — RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V6 exists
Hero Effect Transient Probe V5 completed two clean ORIGINAL ISSYL passes:
- 10763.1 ms / 173 samples
- 10805.1 ms / 174 samples

The automatic lifecycle detector is stable, but the transient root topology swaps between casts. Therefore effect identity cannot be hard-coded by path.

Pass A captured a child record with two simultaneous identity fields:
- `record+0x058 = 0xA5` — exact proven Issyl runtime ability ID
- `record+0x17C = pinned Unit*` — exact armed target Unit pointer

V6 tracks by those contents instead of by transient path.

## Architecture
Strictly read-only:
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION`
- no writes
- no allocation/injection
- no hooks
- no native ability call/replay

Automatic flow:
1. select exactly ONE clean target;
2. ARM;
3. select Issyl and cast ORIGINAL Haste once on that target;
4. V6 auto-detects lifecycle start using the V5-proven root pattern;
5. it searches transient roots and one-level root children for a record satisfying BOTH:
   - `+0x058 == 0xA5`
   - `+0x17C == pinned Unit*`
6. once found, that address is pinned regardless of which root/path exposed it;
7. record size `0x240` is sampled by a 20 ms UI timer;
8. lifecycle expiry is automatic after all roots equal baseline for 4 consecutive samples;
9. COPY REPORT after COMPLETE.

## Report priorities
- signature record address + discovery path;
- signature discovery delay;
- root lifecycle;
- focus offsets including `+0x058`, `+0x17C`, `+0x194`;
- dynamic fields ranked strongly toward high change-rate + zero direction flips;
- secondary static numeric constants from the content-identified A5+target record only.

## Important V5 evidence retained
A repeatable static value `20700 (0x50DC)` appeared at a sibling/config path in both V5 passes. It does not directly equal the ~10.8 s wall time and is NOT proven duration. V6 intentionally refuses to infer duration from path-only sibling data.

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
- Standalone: 151,067,850 bytes — SHA-256 `38bd623c56bada4e155e4fcc5827147bae562218c58ac8f1942e89a5469ea5be`
- Small: 155,372 bytes — SHA-256 `e21668996208aae24cc7cfe885e78937d5426be9b723216b3b75ce35d834456e`

## Runtime objective
Identify a monotonic field inside the actual A5+target effect record whose scale/lifetime correlates with natural ~11 s Issyl expiry.

No write is allowed from V6 alone. Any strong candidate must survive at least two signature-locked passes before an isolated write experiment.

## Exact runtime flow
1. fresh/reload BRZE;
2. select exactly ONE clean normal target unit;
3. click `ARM TARGET + AUTO WATCH`;
4. select Issyl;
5. cast ORIGINAL Haste exactly once on the armed target;
6. do nothing until V6 shows `COMPLETE`;
7. click `COPY REPORT` and return the full report.

## Locked rules
- `HeroEffectReplayV2` remains the runtime-proven one-shot replay.
- repeated native reapplication remains permanently rejected.
- `Unit+0x460` remains rejected.
- transient paths may swap; never hard-code them across casts.
- no duration integration into the main trainer until effect-instance timing is runtime-proven.
