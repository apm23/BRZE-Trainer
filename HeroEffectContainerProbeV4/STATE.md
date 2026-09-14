# BRZE Hero Effect Container Probe V4 — STATE

Status: **BUILD/STATIC PROVEN — RUNTIME PENDING**
Date: 2026-09-14 JST

## Why V4 exists
Duration Probe V3 decisively rejected direct `Unit+0x460` as Grayback-duration-specific: during a movement-only no-buff control it changed `55` times with `0` flips, from `413000` to `432800`.

Therefore direct Unit movement/transform fields are no longer the target for duration research.

## V4 goal
Find an actual effect-instance/container field by subtracting ordinary movement-driven object activity from one original Grayback effect pass.

Target root pointers:
- `Unit+0x094`
- `Unit+0x098`
- `Unit+0x1E4`
- `Unit+0x1E8`
- `Unit+0x1F4`
- `Unit+0x20C`
- `Unit+0x210`
- `Unit+0x214`
- `Unit+0x21C`

V2/V3 evidence makes these regions more promising than direct transform fields. `Unit+0x1E4`-derived objects may be visual/particle state, so V4 does not assume any one root is the gameplay timer.

## Read-only architecture
- `PROCESS_VM_READ | PROCESS_QUERY_INFORMATION` only;
- no `WriteProcessMemory`;
- no `VirtualAllocEx` / `VirtualProtectEx`;
- no `CreateRemoteThread`;
- no hooks;
- no native ability replay;
- original Grayback is cast manually by the user.

Graph:
- targeted roots only;
- node scan `0x180` bytes;
- recursive depth <= 4;
- max 260 readable nodes per sample;
- sample interval 250 ms.

## Runtime flow
1. fresh/reloaded BRZE;
2. select exactly ONE clean normal unit;
3. press `1 START 15s MOVE CONTROL` and keep that unit walking continuously with NO hero buff;
4. after auto-stop, stop the unit;
5. press `2 CAPTURE PRE-EFFECT`;
6. cast ORIGINAL Grayback exactly once on that unit;
7. immediately press `3 START EFFECT WATCH`;
8. do not recast;
9. the instant visible Grayback ends naturally, press `4 MARK EXPIRED`;
10. press `COPY REPORT` and return it.

## Ranking logic
Movement-only changes are strongly penalized. Candidates are boosted when they:
- are absent/unseen during movement control;
- appear only during Grayback;
- change smoothly with few direction flips during Grayback;
- return exactly to pre-effect state at natural expiry;
- become zero or unreadable/disappear at natural expiry.

V4 also retains static fields inside newly-created effect objects, because an expiry timestamp/duration constant may remain unchanged during the effect and disappear only when the object is freed.

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

## Fallback if V4 still cannot isolate timer
Use the already runtime-proven effect creation path from `HeroEffectSniffer`:
- magic-create helper RVA `0x13FFD1`;
- target helper RVA `0x1F0C32`;
- Grayback runtime ability `0xC0`;
- Issyl runtime ability `0xA5`.

Next fallback should capture the actual created effect-object pointer or downstream container insertion, then observe that object before any write.

## Locked rules
- `HeroEffectReplayV2` one-shot replay remains proven and untouched.
- repeated native re-application is permanently rejected.
- `Unit+0x460` must not be reused for duration writing.
- no duration write until an effect-instance/container field survives movement control and correlates with natural expiry.
