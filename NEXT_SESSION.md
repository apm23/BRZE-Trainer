# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Authoritative stable base
- Main trainer rollback/final stable base remains V18.3 as documented in `FINAL_CURRENT.md`.
- Do not mutate the locked V18.3 baseline directly; layer new work above it.

## Proven side feature
`UnitCloneLab/STATE.md`
- Native Copy/Paste selected units is RUNTIME-PROVEN.
- Hero / unique / story units work.
- Never raw-clone Unit structs.

## Hero Effect locked facts
`HeroEffectSniffer/STATE.md`
- Grayback runtime ability `0xC0`; Issyl runtime ability `0xA5`.
- Target helper RVA `0x1F0C32`; magic-create helper RVA `0x13FFD1`.

`HeroEffectReplayV2/STATE.md`
- RUNTIME-PROVEN one-shot replay. Keep untouched as known-good fallback.

`HeroEffectReplayV3/STATE.md`
- RUNTIME-REJECTED. Repeated native replay stacks/compounds effects. NEVER REUSE.

`HeroEffectDurationProbeV3/STATE.md`
- `Unit+0x460` hard-rejected as duration-specific.

`HeroEffectContainerProbeV4/STATE.md`
- Issyl proved transient lifecycle roots `+0x1E4/+0x1E8/+0x20C/+0x210`; `+0x214` returns to baseline at expiry.

`HeroEffectTransientProbeV5/STATE.md`
- Two automatic Issyl passes proved lifecycle timing and showed transient paths swap between casts.

`HeroEffectSignatureProbeV6/STATE.md`
- **RUNTIME-PROVEN SIGNATURE TRACKER — PARENT TIMER NOT ISOLATED.**
- Three clean ORIGINAL ISSYL passes with intentionally different human timing:
  - 10725.9 ms / 343 polls
  - 10713.8 ms / 342 polls
  - 10733.2 ms / 343 polls
  - spread only 19.4 ms, proving human click speed is irrelevant to measured lifecycle.
- Every pass found the actual effect parent using BOTH content fields:
  - `parent+0x058 == 0xA5`
  - `parent+0x17C == pinned Unit*`
- Parent addresses/paths differed:
  - A `0x28BFFD08` via `Unit+0x20C->+0x008`, 34.1 ms discovery
  - B `0x28C00100` via `Unit+0x1E4->+0x014`, 30.7 ms discovery
  - C `0x28C004F8` via `Unit+0x20C->+0x008`, 31.6 ms discovery
- Therefore effect identity MUST remain content-signature-based, never path-based.
- Parent `+0x194` HARD-REJECTED as duration: terminal values rose `623600 -> 653700 -> 678400` while lifetime stayed ~10.72 s; likely absolute/global counter/cleanup timestamp.
- Parent high-change `+0x028/+0x02C/+0x030` have many flips and are spatial/visual/activity noise, not clean timer.

`HeroEffectChildProbeV7/STATE.md`
- **NEW READ-ONLY CHILD TIMER PROBE — CI BUILD IN PROGRESS at handoff update.**
- After A5+target parent signature lock, V7 scans the parent `0x240` bytes for readable pointer fields.
- Deduplicates alias offsets pointing to the same child.
- Pins up to 24 child objects, scans `0x200` bytes each, 20 ms UI timer.
- Automatic lifecycle start/end; no human timing.
- Dynamic child fields ranked by high change-rate + low/zero direction flips.
- Static duration-like child constants separated.
- No writes/injection/hooks/native replay.
- Workflow: `Hero Effect Child Probe V7 Read Only`.
- Initial run ID `34793489888`, job `103822036340` (build status must be rechecked).

## Exact next action
1. Check V7 CI run `34793489888`.
2. If compile fails, fix V7 before runtime testing.
3. If green, download/extract V7 artifact and runtime-test ORIGINAL ISSYL:
   - fresh/reload BRZE;
   - select exactly ONE clean normal target;
   - ARM;
   - select Issyl;
   - cast original Haste once on armed target;
   - do nothing until COMPLETE;
   - COPY REPORT.
4. Analyze child pointer map and child dynamic fields.
5. Prefer child fields with many changes, `flip 0`, and scale matching ~10.72 s.
6. If one strong child timing field appears, confirm with at least a second clean V7 pass before any write.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient path;
- no duration integration into main trainer until a real effect-instance timing field is proven.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot replay is proven; repeated refresh and Unit+0x460 are rejected. V6 completed THREE Issyl passes at 10725.9/10713.8/10733.2 ms despite different human timing, proving auto lifecycle stability and A5+target content signature. Parent +0x194 is rejected as duration. Continue from HeroEffectChildProbeV7: recheck CI run 34793489888, then test child objects of the signature-locked A5 parent.`
