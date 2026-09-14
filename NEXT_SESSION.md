# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks
- Locked main stable fallback: V18.3 (`FINAL_CURRENT.md`).
- V19 remains the latest pre-integration main-trainer fallback.
- V20 integrated remains a built fallback, but its Hero Effect UI is superseded by V21.
- Unit Clone Lab standalone remains runtime-proven.
- Replay V2 / V11 duration research remain runtime-proven references.

Do not mutate the locked V18.3 baseline directly.

## Duration research — CLOSED / LOCKED
Authoritative V11 runtime proof:
- original Issyl baseline `10742.1 ms`;
- Issyl 3X nominal `45000` -> `31612.4 ms`, ratio `2.942838x`;
- custom duration runtime-proven longer;
- restore `45000 -> 15000` OK after natural expiry;
- no repeated native refresh/reapplication.

Locked duration path:
- `parent+0x1F4 -> config+0x0E0`;
- Issyl A5 base nominal `15000`;
- Grayback C0 base nominal `60000`;
- modified config must stay resident through the effect lifetime;
- restore after natural expiry.

Rejected forever:
- repeated native replay refresh;
- Unit+0x460 duration writes;
- parent+0x194 duration writes;
- lifecycle-first / immediate restore;
- native-helper-return restore;
- claim that duration latches completely at creation.

## V20 finding from user runtime visual check
V20 compiled/integrated successfully, but user screenshot showed:
1. Hero Effect controls clipped on the right (`ISSYL CUSTOM` visibly cut);
2. Hero Effect still looked like a research/test tool because it required `CAPTURE ISSYL BASELINE` before configurable replay.

User wants final trainer semantics: select unit -> choose duration -> directly apply Issyl/Grayback effect. No image mockups; edit/build source directly.

## V21 Direct Hero Effect — BUILT / CI-PROVEN, RUNTIME PENDING
State: `V21_DIRECT_HERO_STATE.md`

V21 keeps V20 geometry/shared dispatcher/Copy Unit/Instant Death but replaces the Hero Effect panel and compiled duration UI completely.

Final Hero Effect UI:
- `HERO EFFECT // DIRECT APPLY`
- `APPLY ISSYL`
- `APPLY GRAYBACK`
- `APPLY BOTH`
- `DURATION (SEC)` numeric input, `1–420` seconds
- no `CAPTURE ISSYL BASELINE`
- no original hero cast requirement
- no `ISSYL CUSTOM` research row
- no manual `RESTORE 15000` step
- bounded TableLayout, not the old overflowing one-line FlowLayout.

Direct runtime architecture:
1. capture selected clean unit(s), max 120;
2. resolve guarded ability config:
   - A5 known candidate `0x227F4664`, require ID A5 + base duration 15000;
   - C0 known candidate `0x227F9470`, require ID C0 + base duration 60000;
   - fallback guarded readable-memory scan for exact ID/base-duration signature if known address does not validate;
3. convert requested seconds from runtime-proven natural baselines:
   - Issyl 15000 ~= 10.7271 s;
   - Grayback 60000 ~= 42.2221 s;
4. patch config once;
5. queue one-shot native Replay through the existing shared dispatcher;
6. automatically track selected targets' lifecycle roots;
7. KEEP duration config held while effects are active;
8. restore original config only after natural expiry.

Shared dispatcher stays locked from V20:
- exactly one compiled owner of frame hook RVA `0x135C43`;
- replay helper `0x1F0C32`;
- copy spawn wrapper `0x0C4A1C`;
- cursor query `0x135EFB`;
- no CreateRemoteThread;
- FXSAVE/FXRSTOR + pushfd/pushad;
- serialized Hero Replay / Copy Unit native queue;
- no raw Unit struct clone.

## V21 CI pin
Workflow: `Final V21 Direct Hero Effect`
Run: `34821655613` — SUCCESS
Job: `103904381017` — SUCCESS
Head: `15c8213ba19d3af567584139e99545cdc89f1019`
Artifact: `10338672153`
Artifact digest: `sha256:8490e294fcf3ac9333f11c77518b36de0f99c6f7ffb2fe98cf68de9cce877d42`

Build hashes:
- CLEAN standalone `a933409004138e606358b27ccc697ee702447f9faca64f91d5c7cdef6d56b0b9`
- DIAGNOSTICS standalone `f074cec5c0aa75efb56d9df4f2644dc7dbde17fe5cc3994bfc8cbe622a52198a`
- CLEAN small `21893d232bc70ef1d031cc1fa42ed3ce3a092291f82bc6a36eb4785f24669604`
- DIAGNOSTICS small `d97468c525a6864895636f35619981483aefdce378c16f0b868b0df796a11ffc`

CI passed:
- V20 base generation;
- V21 direct Hero finalizer;
- full-width V20 layout retained;
- V20 shared dispatcher unchanged;
- no research Hero Effect core/panel compiled into V21;
- natural-expiry hold invariants;
- CLEAN + DIAGNOSTICS compile;
- all four publish outputs;
- hash + artifact upload.

## Exact next action — V21 runtime smoke
Do NOT reopen duration research.

Use V21 Diagnostics:
1. launch BRZE and V21 Diagnostics;
2. select one clean unit;
3. set `30.0` seconds;
4. click `APPLY ISSYL` — effect must appear directly, without casting original Issyl first;
5. let it naturally expire; status must report config restored;
6. repeat with `APPLY GRAYBACK` on a clean unit;
7. optionally test `APPLY BOTH` once;
8. verify Hero Effect panel has no clipped text/control;
9. one Copy Unit + one Instant Death smoke afterward to confirm shared dispatcher remains intact.

If V21 passes, promote V21 as integrated main-trainer candidate.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V11 duration research is CLOSED: duration config must remain held until natural expiry. V20 screenshot exposed clipped/research-style Hero Effect UI. V21 is BUILT/CI-PROVEN and replaces it with direct selected-unit APPLY ISSYL / APPLY GRAYBACK / APPLY BOTH + DURATION SEC, no baseline capture. V21 CI run 34821655613 SUCCESS, artifact 10338672153. Next: one V21 Diagnostics runtime smoke.`
