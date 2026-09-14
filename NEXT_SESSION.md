# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Authoritative stable base
- Main trainer rollback/final stable base remains V18.3 as documented in `FINAL_CURRENT.md`.
- Do not mutate locked V18.3 directly; layer new work above it.

## Proven side feature
`UnitCloneLab/STATE.md`
- Native Copy/Paste selected units is RUNTIME-PROVEN, including hero / unique / story units.
- Never raw-clone Unit structs.

## Hero Effect locked facts
`HeroEffectSniffer/STATE.md`
- Grayback runtime ability `0xC0`; Issyl runtime ability `0xA5`.
- Target helper RVA `0x1F0C32`; magic-create helper RVA `0x13FFD1`.

`HeroEffectReplayV2/STATE.md`
- **RUNTIME-PROVEN one-shot replay.** Keep untouched as known-good fallback.
- Uses game-thread frame hook RVA `0x135C43`, max 4 native calls/frame, max 120 selected.
- Proven standalone SHA-256 `2b5671e9a4d13768a2e3adccd12a82b0137419cbd926d2a3cbb6870fe930a702`.

`HeroEffectReplayV3/STATE.md`
- **RUNTIME-REJECTED.** Repeated native replay stacks/compounds effects. NEVER REUSE.

`HeroEffectDurationProbeV3/STATE.md`
- `Unit+0x460` hard-rejected as duration-specific.

`HeroEffectSignatureProbeV6/STATE.md`
- Original Issyl lifetime stable around 10.72–10.73 s.
- Effect parent identity: `parent+0x058 == ability ID` AND `parent+0x17C == pinned Unit*`.
- Parent paths move between casts; never hard-code transient path.
- Parent `+0x194` rejected as duration-specific.

`HeroEffectChildProbeV7/STATE.md`
- Ability config discovered at `parent+0x1F4`.
- Issyl config contains `config+0x000=A5`, `config+0x0E0=15000`.

`HeroEffectAbilityConfigProbeV8/STATE.md`
- Issyl A5: nominal `+0x0E0=15000`, natural wall `10727.1 ms`.
- Grayback C0: nominal `+0x0E0=60000`, natural wall `42222.1 ms`.
- Config ratio exactly 4x; wall ratio ~3.936x.

`HeroEffectDurationWriteProbeV9/STATE.md`
- **RUNTIME-PROVEN — DURATION FIELD CONFIRMED.**
- Issyl controlled write:
  - `15000 -> 10738.0 ms`
  - `30000 -> 21222.5 ms`
  - ratio `1.976390x`
  - restore `30000 -> 15000` succeeded.
- IMPORTANT source correction: V9 held `30000` for the ENTIRE patched effect lifetime and restored only after natural expiry. Do NOT claim duration was copied/latching at creation.

## Merged V2 + duration history
`HeroEffectReplayDurationMerged/STATE.md`

### Attempt #1 — REJECTED
Restore on first lifecycle visibility:
- baseline `10749.7 ms`
- replay `10623.7 ms`
- ratio `0.988283x`
- same A5 config verified
- restore OK

### Attempt #2 / V10.1 — REJECTED
Restore after V2 native helper returned (`native calls > 0`):
- baseline `10717.2 ms`
- replay `10611.9 ms`
- ratio `0.990176x`
- same A5 config verified
- restore OK

Locked conclusion from both failures:
- neither lifecycle appearance nor native-helper return is late enough to restore the global A5 config;
- never reuse either restore timing;
- these failures do not invalidate `config+0x0E0`, because V9 already proved it causally.

## V10.2 — BUILT / CI-PROVEN, RUNTIME TEST NEXT
Design now mirrors V9 exactly:
1. capture original Issyl baseline/config;
2. patch `15000 -> 30000`;
3. queue one known-good V2 Issyl replay;
4. keep 30000 resident for the ENTIRE replay lifetime;
5. read-only watcher tracks pinned Unit transient roots;
6. after natural expiry is stable for 4 polls, restore `30000 -> 15000`;
7. measure lifetime/ratio automatically.

Build pin:
- workflow `Hero Effect Replay Duration Merged`
- run `34797240949` SUCCESS
- job `103832611033` SUCCESS
- head `0062f32fa0d3d903f85ef613eb71dd8034de95a3`
- artifact `10330421265`
- digest `sha256:4dcdf829c606197fb631257248753befa5d6d00757a38e32c3281b33675a7b77`
- standalone SHA-256 `d522c611f48e84b1a9b0259938e8847f87e9c968ec07bbb2cfb2c10f58f51c29`
- small SHA-256 `6a22bc4611edc41daa174a024d3334c6b152b7481838887d4edc1c270892c17d`

## Exact next action — V10.2 runtime test
Use ONLY `BRZE-Hero-Effect-Replay-Duration-Merged-V10.2.exe`.

1. Fresh/reload BRZE.
2. Open V10.2 merged EXE only.
3. Select exactly ONE clean target.
4. Click `1) CAPTURE ISSYL BASELINE`.
5. Cast ORIGINAL Issyl Haste ONCE on that target.
6. Wait until baseline completes (~10.7 s).
7. Select the same target again after Haste fully expires.
8. Click `2) REPLAY ISSYL 2X` ONCE.
9. Do NOT manually cast Issyl the second time.
10. Wait until V10.2 COMPLETE (~21 s expected).
11. COPY REPORT.

Expected:
- baseline ~10.7 s;
- merged replay ~21 s;
- ratio near 2.0x;
- restore to 15000 occurs only after replay expiry;
- no repeated application / no stacking.

## Locked rejects / safety
- no repeated native reapplication;
- no `Unit+0x460` duration writes;
- no parent `+0x194` duration writes;
- no hard-coded transient parent path;
- no lifecycle-first restore timing;
- no native-call-return restore timing;
- no claim that duration is copied at effect creation;
- keep original HeroEffectReplayV2 source/binary untouched as fallback;
- no main V18.3 integration until merged runtime proof passes.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub is authoritative. V9 proved Issyl config+0x0E0 duration causally: 15000->30000 changed 10738.0ms->21222.5ms (1.976390x), but V9 actually held 30000 for the entire effect lifetime and restored only after expiry. Merged lifecycle-first restore failed at 0.988283x; V10.1 native-call-return restore also failed at 0.990176x. V10.2 now mirrors V9 exactly: hold 30000 through the full Replay V2 lifetime, then restore after natural expiry. CI run 34797240949 success; runtime-test V10.2 next.`
