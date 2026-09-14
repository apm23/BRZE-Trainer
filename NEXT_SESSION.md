# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Authoritative stable base
- Main trainer rollback/final stable base remains V18.3 as documented in `FINAL_CURRENT.md`.
- Do not mutate the locked V18.3 baseline directly; layer new work above it.

## Proven side feature
`UnitCloneLab/STATE.md`
- Native Copy/Paste of currently selected units is RUNTIME-PROVEN.
- Hero / unique / story units are allowed and successfully duplicated.
- Uses native coordinate spawn path; never raw-clones Unit structs.

## Hero Effect research status
`HeroEffectSniffer/STATE.md`
- RUNTIME-PROVEN.
- Grayback runtime ability `0xC0`; Issyl runtime ability `0xA5`.
- Target helper RVA `0x1F0C32`; magic-create helper RVA `0x13FFD1`.

`HeroEffectReplayV2/STATE.md`
- **RUNTIME-PROVEN** one-shot effect replay.
- Keep untouched as known-good fallback.

`HeroEffectReplayV3/STATE.md`
- **RUNTIME-REJECTED — NEVER REUSE REFRESH/STACK APPROACH.**
- Repeated native application stacks/compounds effect instances and corrupts combat behavior.

`HeroEffectDurationProbeV3/STATE.md`
- `Unit+0x460` is hard-rejected as duration-specific because movement-only no-buff control changed it 55 times (`413000 -> 432800`).

`HeroEffectContainerProbeV4/STATE.md`
- ORIGINAL ISSYL proved transient lifecycle roots.
- `+0x1E4/+0x1E8/+0x20C/+0x210` appear during effect and return to zero at expiry.
- `+0x214` returns exactly to its baseline pointer.

`HeroEffectTransientProbeV5/STATE.md`
- **RUNTIME-PROVEN OBSERVER — TWO ISSYL PASSES COMPLETE.**
- Pass A wall time `10763.1 ms`, 173 samples.
- Pass B wall time `10805.1 ms`, 174 samples.
- Automatic start/end detection is stable (~42 ms difference).
- Critical: transient topology swaps across casts. In one pass `+0x1E4=0x1E01BF30/+0x1E8=0x1E01BF3C`; in the other those addresses swap. NEVER hard-code a transient path.
- Strong Pass A identity record:
  - `record+0x058 = 0xA5` (Issyl runtime ability ID)
  - `record+0x17C = pinned Unit*` (exact armed target)
- repeatable sibling/config constant `0x50DC = 20700` appeared in both passes, but it does not directly match the ~10.8 s wall time and is NOT proven duration.

`HeroEffectSignatureProbeV6/STATE.md`
- **BUILT / CI-PROVEN READ-ONLY — RUNTIME PENDING.**
- V6 searches transient roots/children by CONTENT rather than path.
- Signature lock requires BOTH:
  - `+0x058 == 0xA5`
  - `+0x17C == pinned Unit*`
- Once found, the exact record address is pinned and sampled with a 20 ms UI timer until automatic natural expiry.
- No writes, injection, hooks, or native replay.
- Run `34793023696` SUCCESS; job `103820717005` SUCCESS.
- Artifact `10328444898`, digest `sha256:312392823c7aa0ea58cc82dfed9bdb222f32d20cd66693f6e04c2073ec206ea7`.
- Standalone SHA-256 `38bd623c56bada4e155e4fcc5827147bae562218c58ac8f1942e89a5469ea5be`.
- Small SHA-256 `e21668996208aae24cc7cfe885e78937d5426be9b723216b3b75ce35d834456e`.

## Exact next action
Runtime-test `HeroEffectSignatureProbeV6` using ORIGINAL ISSYL:
1. fresh/reload BRZE;
2. select exactly ONE clean normal target unit;
3. click `ARM TARGET + AUTO WATCH`;
4. select Issyl;
5. cast ORIGINAL Haste exactly once on the armed target;
6. do nothing until V6 shows `COMPLETE`;
7. click `COPY REPORT` and return the full report.

Analysis priority for V6 report:
- confirm `Signature record` is FOUND;
- confirm signature check shows `+0x058 = 0xA5` and `+0x17C = pinned Unit*`;
- inspect dynamic fields from this content-identified record only;
- prioritize high change-rate + `flip 0` fields whose total/rate correlates with ~11 s natural Issyl lifetime;
- inspect `+0x194` specifically but do not assume it is duration;
- if one strong candidate emerges, repeat V6 once more before any write.

Do not integrate duration into the main trainer yet. Never reintroduce repeated native re-application. Never hard-code V5 transient paths.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot hero replay is runtime-proven (Grayback=0xC0, Issyl=0xA5); repeated refresh and Unit+0x460 are rejected. V5 completed two Issyl passes (~10.8 s each), proved transient paths swap between casts, and captured an effect record with +0x058=A5 plus +0x17C=target Unit*. V6 read-only signature tracker is built successfully; runtime-test V6 next.`
