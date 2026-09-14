# BRZE Hero Effect Transient Probe V5 — STATE

Status: **RUNTIME-PROVEN OBSERVER — TWO ISSYL PASSES COMPLETE**
Date: 2026-09-14 JST

## Why V5 exists
V4 runtime with ORIGINAL ISSYL proved a clean effect lifecycle:
- `Unit+0x1E4`: 0 -> transient pointer -> 0
- `Unit+0x1E8`: 0 -> transient pointer -> 0
- `Unit+0x20C`: 0 -> transient pointer -> 0
- `Unit+0x210`: 0 -> transient pointer -> 0
- `Unit+0x214`: baseline pointer -> changed pointer during effect -> exact baseline pointer at expiry

V4 ranked too many NEW static fields above genuinely changing fields. V5 narrowed observation to exact transient objects and automated user timing.

## Runtime proof — ORIGINAL ISSYL, two independent casts
Same pinned target `Unit*=0x28195A4C`, runtime ability already proven as `0xA5`.

Pass A:
- wall time `10763.1 ms`
- `173` effect samples
- `31` pinned nodes
- roots: `+0x1E4/+0x1E8/+0x20C/+0x210` all returned to zero at expiry
- `+0x214` returned exactly to baseline pointer

Pass B:
- wall time `10805.1 ms`
- `174` effect samples
- `31` pinned nodes
- same lifecycle behavior

The two observed wall times differ by only ~42 ms, so V5 automatic start/end detection is stable enough for narrow timer research.

## Critical topology result
The transient object layout is NOT path-stable across casts.

Pass A:
- `Unit+0x1E4 = 0x1E01BF30`
- `Unit+0x1E8 = 0x1E01BF3C`

Pass B:
- `Unit+0x1E4 = 0x1E01BF3C`
- `Unit+0x1E8 = 0x1E01BF30`

Therefore no future duration logic may hard-code a path such as `R1E4->+0x014` as the effect instance. Object identity must be found by content/signature or by creation interception.

## Strong signature evidence from Pass A
One transient child record was captured at `R1E4->+0x014` and contained:
- `+0x058 = 0x000000A5` — exact proven Issyl runtime ability ID
- `+0x17C = 0x28195A4C` — exact armed target Unit*

This is the strongest object-identity evidence so far. It strongly suggests an actual Issyl effect record was observed.

The same logical record was not reported through the identical path in Pass B, consistent with the root/path topology swapping between casts.

## Secondary repeatable constant
Both Pass A and Pass B reported:
- `R1E4->+0x008+0x194 = 0x000050DC = 20700`

It is repeatable but does NOT numerically match the ~10.8 s observed wall time directly. Treat it only as a candidate configuration/lifetime-related constant, not as a proven duration field.

## Dynamic fields
Pass A captured several highly changing float-looking fields in the A5-bearing record (`+0x028/+0x02C/+0x030`), but they had many direction flips and look more like transform/visual state than a clean countdown.

No write is justified from V5.

## V5 architecture
Strictly read-only:
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION`
- no writes
- no allocation/injection
- no hooks
- no native ability replay

Workflow:
1. select exactly ONE clean target unit;
2. click ARM;
3. V5 pins Unit* + UnitDef + owner and captures baseline lifecycle roots;
4. selection may change after ARM;
5. cast ORIGINAL ISSYL once;
6. V5 auto-detects effect start;
7. it pins transient roots + bounded one-level children;
8. samples every 50 ms;
9. auto-marks natural expiry after lifecycle roots return to baseline for 3 consecutive samples;
10. COPY REPORT.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `7618406f9488e108f944fc820ed78dd6c99fca13`
Workflow: `Hero Effect Transient Probe V5 Read Only`
Run: `34792424502` — SUCCESS
Job: `103819020397` — SUCCESS
Artifact: `10327958166`
Artifact digest: `sha256:1092ef001f920934a2ee0a1c6b5c861fbd4d72b03d4bf9d696965af424a95f6a`

Binaries:
- Standalone: 151,067,850 bytes — SHA-256 `528e112990cccb86ec52953e8f93ddd8bd00b136915e9bc3d650ff693a9908be`
- Small: 155,884 bytes — SHA-256 `daeae8b8ddf06ae967f54fa1a43428f04ee849bcab2c907c0e01968c93c9e691`

## Exact next action
Build a narrow read-only signature tracker that:
- auto-detects Issyl lifecycle like V5;
- searches transient root children for a record with `field+0x058 == 0xA5` AND `field+0x17C == pinned Unit*`;
- tracks that record by address regardless of which root/path owns it;
- samples it at higher frequency;
- reports monotonic candidates and lifetime/expiry behavior;
- never writes anything.

## Locked rules
- `HeroEffectReplayV2` one-shot replay remains runtime-proven and untouched.
- repeated native reapplication remains permanently rejected.
- `Unit+0x460` remains rejected for duration.
- do not hard-code V5 transient paths across casts.
- do not merge duration control into the main trainer until a real effect-instance field is runtime-proven.
