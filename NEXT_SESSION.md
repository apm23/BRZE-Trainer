# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks
- Locked main stable fallback: V18.3 (`FINAL_CURRENT.md`).
- V19 remains the latest pre-integration main-trainer fallback.
- V20 integrated remains a built fallback.
- V21 Direct Hero is built/CI-proven but runtime-rejected for two integration defects described below.
- Unit Clone Lab standalone remains runtime-proven.
- Replay V2 / V11 duration research remain runtime-proven references.

Do not mutate the locked V18.3 baseline directly.

## Duration research — CLOSED / LOCKED
Authoritative V11 proof:
- Issyl baseline `10742.1 ms`;
- Issyl 3X nominal `45000` -> `31612.4 ms`, ratio `2.942838x`;
- custom duration runtime-proven longer;
- restore `45000 -> 15000` after natural expiry;
- no repeated native refresh/reapplication.

Locked duration rule:
- Issyl A5 base nominal `15000`;
- Grayback C0 base nominal `60000`;
- modified ability config must stay resident through the active effect lifetime;
- restore only after the tracked effect naturally expires.

Rejected forever:
- repeated native replay refresh;
- Unit+0x460 duration writes;
- parent+0x194 duration writes;
- lifecycle-first / immediate restore;
- native-helper-return restore;
- claim that duration fully latches at creation.

## V21 runtime rejection
User runtime screenshots proved two V21 defects:
1. multi-select could be rejected by `selected unit ... has an active transient effect` because V21 required generic transient roots to be clean;
2. Hero Effect could remain `HELD until natural expiry` after the requested visible effect had ended, and that blocked applying the same ability to another selected group.

Root cause: V21 tracked generic lifecycle/transient roots, not the actual requested A5/C0 effect record.

Do NOT return to V21 generic root gating/tracking.

## V22 Group-Safe Hero Effect — BUILT / CI-PROVEN, RUNTIME PENDING
State: `V22_GROUP_SAFE_HERO_STATE.md`

V22 preserves V20 layout + Copy Unit + Instant Death + one shared frame dispatcher, but replaces the Hero Effect runtime tracker.

### Exact effect identity
V22 uses the runtime-proven V6 content signature:
- record `+0x058 = ability ID`;
- record `+0x17C = target Unit*`;
- search through Unit transient roots `+0x1E4`, `+0x1E8`, `+0x20C`, `+0x210` and one-level pointer children.

Therefore:
- unrelated buffs/debuffs no longer block a selected unit;
- if a selected unit already has the requested Issyl/Grayback effect, that unit is skipped instead of failing the whole group;
- generic `RootsClean` gating is forbidden.

### Concurrent selected groups
V22 maintains independent global holds for Issyl and Grayback.

While Issyl 30s is still active on group A:
- group B may immediately receive Issyl 30s too;
- Grayback may be applied independently to another group at its own duration;
- Issyl with a DIFFERENT duration is blocked until all currently tracked Issyl instances naturally expire, because the A5 config is global.

The same rule applies symmetrically to Grayback.

### Explicit filtered replay queue
The V22 build finalizer adds `QueueReplayUnits(...)` to the existing V20 shared dispatcher so Hero Effect can queue only safe filtered targets.

Locked dispatcher rules remain:
- exactly one owner of frame hook RVA `0x135C43`;
- replay target helper `0x1F0C32`;
- Copy Unit spawn wrapper `0x0C4A1C`;
- cursor query `0x135EFB`;
- no CreateRemoteThread;
- FXSAVE/FXRSTOR + pushfd/pushad;
- max 120 targets;
- no raw Unit clone.

## V22 CI pin
Workflow: `Final V22 Group Safe Hero Effect`
Run: `34823248974` — SUCCESS
Job: `103909409512` — SUCCESS
Head: `a8e55e21d5b20e468fd08db14b00d3d6cba5dc73`
Artifact: `10339202775`
Artifact digest: `sha256:17e4c238f421d60aff4525c0953e75285b040be873b4b16f903d67adacf6c5fa`

Build hashes:
- CLEAN standalone `845f860a250eafd23eebb009c306da08ddd14246b62731b37cc32ca665eac310`
- DIAGNOSTICS standalone `a7cf73690102e84ad363ab24a6134da939df384dff3a15de5f1b3b003ce97a67`
- CLEAN small `7d58f0080c79b050af3ccfe1f859ff5a94c271a6b10f16420b7afa4ef23f0eb5`
- DIAGNOSTICS small `11fc5f494976d77fc196302a50a853b4a0d7c74576b732142a1efcad8ade8341`

CI passed:
- V20 base generation;
- explicit filtered target queue patch;
- signature A5/C0 tracking invariants;
- generic transient-root blocking forbidden;
- independent Issyl/Grayback hold policy;
- same-duration concurrent group policy;
- CLEAN + DIAGNOSTICS compile;
- four publish outputs;
- hash + artifact upload.

## Exact next action — ONE V22 runtime smoke
Use V22 Diagnostics.

1. Select MANY units, including units that may have unrelated buffs/effects -> set 30s -> APPLY ISSYL. It must not reject the entire group because of unrelated transient effects.
2. While group A still has Issyl 30s, select group B -> APPLY ISSYL 30s again. It must apply immediately without waiting for group A to expire.
3. While Issyl remains active, select another group -> APPLY GRAYBACK. Grayback must be independent.
4. Optional guard check: while Issyl 30s is still active, change to e.g. 40s and APPLY ISSYL. It should BLOCK only because the same global A5 config is currently held at 30s.
5. After the last tracked A5/C0 record disappears, that ability must automatically restore and become ready for a different duration.
6. One Copy Unit + one Instant Death smoke afterward.

If V22 passes, promote V22 as the integrated main trainer candidate. Do not reopen duration research.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V21 runtime-rejected because generic transient roots blocked multi-select and kept Hero Effect HELD after the requested effect ended. V22 is BUILT/CI-PROVEN: V6 signature tracking (record+0x058 ability ID + record+0x17C target Unit*), skip-not-block multi-select, explicit filtered replay targets, independent Issyl/Grayback holds, and same-duration concurrent groups. CI run 34823248974 SUCCESS, artifact 10339202775. Next: one V22 Diagnostics runtime smoke.`
