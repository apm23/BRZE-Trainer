# BRZE Hero Effect Container Probe V4 — STATE

Status: **RUNTIME-PROVEN LIFECYCLE OBSERVER — TIMER NOT YET ISOLATED**
Date: 2026-09-14 JST

## Why V4 exists
Duration Probe V3 decisively rejected direct `Unit+0x460` as duration-specific: during a movement-only no-buff control it changed `55` times with `0` flips, from `413000` to `432800`.

Therefore direct Unit movement/transform fields are no longer the target for duration research.

## V4 runtime result — original Issyl pass
The user intentionally used ORIGINAL ISSYL instead of Grayback because Grayback lasts about one minute. This is valid because V4 is ability-agnostic and observes target object/container lifetime only.

Root lifecycle was exceptionally clean:
- `Unit+0x094`: PRE `0x168F5D70` -> FX `0x168F5D70` -> EXP `0x168F5D70` (stable)
- `Unit+0x098`: PRE `0x2B435EC8` -> FX `0x2B435EC8` -> EXP `0x2B435EC8` (stable)
- `Unit+0x1E4`: PRE `0` -> FX `0x1E01B958` -> EXP `0`
- `Unit+0x1E8`: PRE `0` -> FX `0x1E01B964` -> EXP `0`
- `Unit+0x1F4`: stable across PRE/FX/EXP
- `Unit+0x20C`: PRE `0` -> FX `0x1DF8EA80` -> EXP `0`
- `Unit+0x210`: PRE `0` -> FX `0x1DF8EA80` -> EXP `0`
- `Unit+0x214`: PRE `0x1DF8EA80` -> FX `0` -> EXP `0x1DF8EA80`
- `Unit+0x21C`: stable across PRE/FX/EXP

This proves that `+0x1E4/+0x1E8` and `+0x20C/+0x210` are transient lifecycle roots tightly correlated with visible Issyl effect lifetime. `+0x214` is the inverse lifecycle marker and returns to its exact pre-effect pointer.

Important structural detail:
- `+0x1E4` and `+0x1E8` point 0x0C bytes apart (`0x1E01B958` vs `0x1E01B964`), suggesting two views/fields into one transient structure rather than independent unrelated allocations.
- `+0x20C` and `+0x210` are the exact same pointer (`0x1DF8EA80`).

## Why V4 did not isolate the timer
The report ranking over-rewarded NEW effect-only fields that were static for the entire effect (`FX chg 0`) merely because their containing transient object disappeared at expiry. This flooded the top report with lifecycle/identity/visual data and could hide the truly changing timer/elapsed field.

Examples from the V4 run:
- many `Unit+0x1E4->field+...` entries: NEW, `CTRL unseen`, `FX chg 0`, `EXP UNREADABLE`;
- many `Unit+0x20C->field+...` entries: same pattern;
- the known deeper object under `Unit+0x1E4->+0x008` again appeared only during effect and vanished at expiry, but its top-ranked fields were mostly static.

Therefore V4 proved object lifetime, not the internal duration field.

## Next phase — V5 transient object timer probe
Do NOT broaden recursive guessing again.

V5 should:
- remain strictly read-only;
- ARM before the original skill is cast;
- automatically detect effect start from the proven lifecycle roots;
- pin the actual transient object addresses that appeared at `+0x1E4/+0x1E8/+0x20C/+0x210`;
- sample those exact objects at high frequency;
- automatically detect expiry when lifecycle roots return to baseline, removing the need for millisecond-perfect user clicks;
- rank **changing** fields first (especially monotonic low-flip values), while listing static effect-only constants separately so they cannot bury timers;
- support Issyl and Grayback without hardcoding an ability ID.

## Read-only architecture
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION` only;
- no `WriteProcessMemory`;
- no `VirtualAllocEx` / `VirtualProtectEx`;
- no `CreateRemoteThread`;
- no hooks;
- no native ability replay.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `e2e805a4312d1dd278bf70ce0d8aeda0a9e498d0`
Workflow: `Hero Effect Container Probe V4 Read Only`
Run: `34791583601` — SUCCESS
Job: `103816698699` — SUCCESS
Artifact: `10328387975`
Artifact digest: `sha256:9170fbf96b3b922ab411932dc9d2f6d0f6dafef7bed2e166e9ab13a00af4133a`

Binaries:
- Standalone: 151,071,946 bytes — SHA-256 `2294592d2844c05a00a72dd06f306e22b0308d99cc7cfeb9a06b66368925a422`
- Small: 158,956 bytes — SHA-256 `8b8b98155acce0e8690a9c09c5e56a84bae58d30e2669cb1c3ff0cd0542ea7f4`

## Locked rules
- `HeroEffectReplayV2` one-shot replay remains proven and untouched.
- repeated native re-application is permanently rejected.
- `Unit+0x460` must not be reused for duration writing.
- no duration write until a field inside a proven transient effect object is correlated with automatic natural expiry.
