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
- Every pass found actual effect parent by BOTH content fields:
  - `parent+0x058 == 0xA5`
  - `parent+0x17C == pinned Unit*`
- Parent addresses/paths differed:
  - A `0x28BFFD08` via `Unit+0x20C->+0x008`, 34.1 ms discovery
  - B `0x28C00100` via `Unit+0x1E4->+0x014`, 30.7 ms discovery
  - C `0x28C004F8` via `Unit+0x20C->+0x008`, 31.6 ms discovery
- Effect identity MUST remain content-signature-based, never path-based.
- Parent `+0x194` HARD-REJECTED as duration: terminal values `623600 -> 653700 -> 678400` while lifetime stayed ~10.72 s; likely absolute/global counter/cleanup timestamp.
- Parent `+0x028/+0x02C/+0x030` are high-flip spatial/visual/activity noise, not clean timer.

`HeroEffectChildProbeV7/STATE.md`
- **BUILT / CI-PROVEN READ-ONLY — RUNTIME TEST NEXT.**
- After A5+target parent signature lock, scans parent `0x240` for readable pointer fields.
- Deduplicates alias offsets pointing to same child.
- Pins up to 24 child objects; samples `0x200` bytes each at 20 ms.
- Automatic lifecycle start/end; no human timing.
- Dynamic child fields ranked high-change + low/zero direction flips; static duration-like child constants separated.
- No writes/injection/hooks/native replay.
- Workflow `Hero Effect Child Probe V7 Read Only`.
- Run `34793489888` SUCCESS; job `103822036340` SUCCESS.
- Head `18d22cbaa5f1b6da1f0d017f665e978c3486b95d`.
- Artifact `10329090887`, digest `sha256:83096a67245a1cc1d2163c071f8163cdcda2f0c4fe4706e1fe054021b7d92294`.
- Standalone SHA-256 `ee30cc76f54f91aa93d4f7e26f01d7ed6db3070ca1a53d2c785d8d452c5843d2`.
- Small SHA-256 `46a252b206a8dc78e04ffbf1144aa0a76ff1970f208f66834637e5a6ca582492`.

## Exact next action
Runtime-test V7 with ORIGINAL ISSYL:
1. fresh/reload BRZE;
2. select exactly ONE clean normal target;
3. click `ARM TARGET + AUTO WATCH`;
4. select Issyl;
5. cast ORIGINAL Haste once on armed target;
6. do nothing until V7 shows `COMPLETE`;
7. click `COPY REPORT` and return full report.

Analysis priority for V7:
- verify A5+target parent found;
- inspect `CHILD POINTER MAP` first, noting alias clusters;
- prioritize child dynamic fields with many changes and `flip 0` (or extremely low flips);
- compare integer/float total change and rate against ~10.72 s lifetime;
- inspect static child constants secondarily;
- if one strong child timer appears, repeat V7 on a second clean Issyl cast before any write.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient path;
- no duration integration into main trainer until real effect-instance timing field is proven.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V2 one-shot replay is proven; repeated refresh and Unit+0x460 are rejected. V6 completed THREE Issyl passes at 10725.9/10713.8/10733.2 ms despite different human timing, proved A5+target content signature, and rejected parent +0x194 as duration. V7 read-only child-object timer probe built SUCCESS (run 34793489888, artifact 10329090887). Runtime-test V7 next.`
