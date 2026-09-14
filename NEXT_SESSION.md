# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks / current
- V18.3 = locked main stable fallback (`FINAL_CURRENT.md`).
- V29 = runtime-proven standalone Hero reset primitive.
- V30 = **runtime-proven integrated gameplay base** and remains locked.
- V31 overlay = runtime-rejected (Alt+W failed).
- V32 = runtime-proven Alt+W / visual baseline.
- V33 = runtime-proven premium overlay baseline; user approved appearance/behavior.
- V34 = **latest FINAL CANDIDATE**: CI PASS + static runtime collision audit PASS; one user runtime smoke remains before FINAL 100%.

Do not reopen Hero reset research unless a real gameplay regression appears.

## Locked V30 gameplay
Hero reset primitive:
- exact same-effect signature: `record+0x58` ability + `record+0x17C` target Unit*;
- current BRZE tick = module RVA `0x440A3C`;
- active same effect -> `record+0x194 = current tick` -> restart same instance without stacking;
- missing effect -> one-shot native Replay V2 apply;
- APPLY BOTH partitions mixed selections;
- custom duration keeps proven full-lifetime config hold/restore.

V30 integrated runtime result from user: **WORK / PASS**.
Never reintroduce repeated same-effect native replay, `record+0x194=0`, Unit+0x460 duration writes, CreateRemoteThread, or hard-coded transient effect paths.

## V33 premium overlay baseline
User approved the V33 look.
- Alt+W global hotkey works over BRZE.
- separate no-activate premium overlay;
- opacity 91%;
- explicit Hero duration;
- explicit COPY UNIT offset;
- death target switch ENEMY ONLY / ALL UNITS;
- no redundant overlay Single Kill or Death Burst toggle;
- full mini Unit Changer page sharing the normal trainer state.

## V34 final candidate
Detailed state: `V34_CANDIDATE_STATE.md`.

Requested final changes:
- COPY UNIT offset buttons now step by **0.1**;
- minimum **0.1**, maximum 64.0, reset 8.0;
- visible readout stays `+X N.N`;
- overlay stays 91%.

### HOME / REFRESH TRAINER = hard trainer↔BRZE rebind
This is NOT a BRZE process restart. It cleanly tears down every trainer-owned binding, then force re-probes BRZE and reattaches still-enabled cheats on the next normal timer tick.
Refresh includes:
- `HeroEffectDirectCoreV30.Shutdown()`;
- `IntegratedFrameDispatcherCore.StopAll()`;
- UnitChanger / PausePeasant / Wolf / Reveal / InstantDeath / Stamina / Horse / HookCore / Selection / Native teardown;
- stale hotkey-edge clear;
- Windows BRZE Process refresh;
- forced `GameGate.Probe(true)` (fresh PID/base/player state).
COPY UNIT buffer is intentionally cleared by the dispatcher reset.

### Full collision scan result
First V34 scan found real duplicate COMPILED legacy writers even though they were dormant in the current UI:
- `0x1A70C6` old Native vs HookCore;
- `0x1CCD4D` old Native vs HookCore;
- `0x467AF4` old Native vs PausePeasantCore.

V34 removed/retired those obsolete Native mutation paths rather than allowlisting them.
Second scan PASS:
- 15 compiled source files scanned;
- 14 unique fixed mutation sites resolved;
- no fixed-address mutation site has multiple compiled owners;
- render hook `0x135C43` owner = IntegratedFrameDispatcherCore only;
- Instant Death + Hero replay share that dispatcher;
- HookCore stamina input remains false; StaminaCore is the active stamina owner;
- hard refresh teardown + forced BRZE re-probe verified.

## V34 CI proof
Workflow `Final V34 Candidate Four-Pack`
- run `34848703691` — SUCCESS
- job `103990876302` — SUCCESS
- head `53d3c2f36d1ca2cbaa19c2a4ca21a2a3780192ae`
- artifact `10349337544`
- artifact digest `sha256:f90388fa3f9fd25d5df5a1575be2038a85920d29fcac91feba86cc87f750be7c`

Hashes:
- CLEAN standalone `38790b4572c6810e9acacc98930706a590b7d7673237018a9be93fb402573105`
- DIAGNOSTICS standalone `4c8fa36e797ddabcd353625c4a90edaf53f251975a8a7cec7ac5589690831448`
- CLEAN small `fd8fbf76635a40b8e9a93b673d4ebedd0e91c0a4e739b2e6ada79437ce864729`
- DIAGNOSTICS small `2c088a63b4459055a3d402955e15520e836dd51fca1e63e310ed1c5db84cd433`

All V34 architecture verification, collision audit, Clean/Diagnostics compile, four publishes, hashing and artifact upload passed. Ignore unrelated legacy `fix-large-selection-freeze.yml` failure.

## EXACT NEXT ACTION — ONE FINAL USER SMOKE
Use `BRZE-Trainer-FINAL-V34-Diagnostics.exe`.

1. BRZE foreground -> Alt+W show/hide.
2. COPY offset: verify 8.0 changes in 0.1 steps; test cramped placement around 0.1–1.0.
3. Quick regression: Hero effect/reset, death target policy, Unit Changer.
4. Enable normal cheats and press HOME / REFRESH TRAINER while BRZE is running. After the next tick, enabled cheats should reattach automatically.
5. If the old condition “BRZE no longer responds to trainer cheats” occurs, HOME should recover the trainer-to-BRZE binding without restarting BRZE.

If user reports this PASS: **promote V34 to FINAL 100% immediately**. No new probe/version unless a concrete regression exists.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V30 gameplay is runtime-proven and locked. V33 premium Alt+W overlay is visually/runtime approved. V34 is the final candidate: COPY offset 0.1 steps, 91% overlay, full hard trainer↔BRZE refresh/rebind, and static collision scan clean after retiring three dormant duplicate legacy writers. V34 CI run 34848703691 SUCCESS, artifact 10349337544. Next: one final V34 Diagnostics runtime smoke; if PASS, promote FINAL 100%.`
