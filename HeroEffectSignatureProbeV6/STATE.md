# BRZE Hero Effect Signature Probe V6 — STATE

Status: **IMPLEMENTED — CI/RUNTIME PENDING**
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

## Runtime objective
Identify a monotonic field inside the actual A5+target effect record whose scale/lifetime correlates with natural ~11 s Issyl expiry.

No write is allowed from V6 alone. Any strong candidate must survive at least two signature-locked passes before an isolated write experiment.

## Locked rules
- `HeroEffectReplayV2` remains the runtime-proven one-shot replay.
- repeated native reapplication remains permanently rejected.
- `Unit+0x460` remains rejected.
- transient paths may swap; never hard-code them across casts.
- no duration integration into the main trainer until effect-instance timing is runtime-proven.
