# BRZE Trainer V22 — Group-Safe Hero Effect State

Date: 2026-09-14 JST
Status: **BUILT / CI-PROVEN — RUNTIME FIX PENDING USER SMOKE**
Branch: `instant-death-v4-hover-telemetry`

## Why V22 exists
User runtime screenshots exposed two V21 defects:
1. multi-select was rejected if any selected unit had any active transient root, even when the transient belonged to an unrelated effect;
2. Hero Effect stayed HELD after the requested Issyl/Grayback effect had visually ended because V21 tracked generic lifecycle roots rather than the actual A5/C0 effect record. That prevented immediately applying effects to another selected group.

## V22 correction
V22 replaces generic transient-root gating/tracking with the runtime-proven V6 content signature:
- effect record `+0x058 = ability ID`;
- effect record `+0x17C = target Unit*`;
- transient roots searched: Unit `+0x1E4`, `+0x1E8`, `+0x20C`, `+0x210`, including one-level pointer children.

Consequences:
- unrelated buffs/debuffs do not block Hero Effect application;
- a selected unit that already has the requested effect is skipped, not allowed to fail the entire selection;
- explicit filtered target lists are queued through the existing single shared frame dispatcher;
- no generic `RootsClean` requirement remains.

## Concurrent-group policy
Ability duration config is global and V11 proved it must remain resident through natural expiry.
Therefore V22 uses one independent hold per ability:
- Issyl A5 hold;
- Grayback C0 hold.

While a hold is active:
- another group may receive the SAME ability immediately if it requests the SAME duration;
- the other ability may be used independently at its own duration;
- changing the duration of an already-held ability is blocked until all tracked instances of that ability naturally expire.

This preserves the V11 natural-expiry duration rule without making one group unnecessarily block all later groups.

## Dispatcher change
V22 patches only the Replay facade of the V20 shared dispatcher in the build workspace:
- existing `QueueReplaySelected(...)` remains;
- new `QueueReplayUnits(IEnumerable<uint> ...)` accepts the filtered explicit unit list;
- render-frame hook RVA `0x135C43` still has one owner;
- Replay V2 native target helper remains `0x1F0C32`;
- Copy Unit spawn wrapper remains `0x0C4A1C`;
- Instant Death still shares the same dispatcher;
- no CreateRemoteThread;
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
- V20 generated base;
- V22 explicit-target dispatcher patch;
- signature tracking invariants;
- generic transient-root blocking forbidden;
- concurrent same-duration group policy present;
- independent Issyl / Grayback holds present;
- CLEAN compile;
- DIAGNOSTICS compile;
- four publish outputs;
- hash and artifact upload.

## Runtime smoke required
1. select many units that may have unrelated effects -> APPLY ISSYL 30s must not reject the whole group;
2. while that Issyl group is still active, select another clean group -> APPLY ISSYL 30s again must queue/apply immediately;
3. while Issyl 30s remains held, APPLY GRAYBACK to another group must be allowed independently;
4. attempt Issyl with a different duration while prior Issyl instances are still active -> must block with a duration-conflict message;
5. after the last A5/C0 signature disappears, that ability's hold must restore automatically and become ready for a new duration.
