# V34 FINAL CANDIDATE STATE

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
Status: **BUILT / CI-PROVEN / STATIC COLLISION AUDIT PASS / RUNTIME FINAL SMOKE PENDING**

## Runtime base
- V30 integrated gameplay remains the runtime-proven base.
- Hero reset stays: active same effect -> `record+0x194 = current BRZE tick` from RVA `0x440A3C`; missing effect -> one native Replay V2 apply.
- V33 premium overlay visual/Alt+W baseline was user-approved.

## V34 requested polish
- COPY UNIT base offset now adjusts by **0.1** per click.
- minimum offset **0.1**, maximum 64.0, reset 8.0.
- readout remains explicit: `+X N.N`.
- premium overlay stays at **91% opacity**.

## Hard REFRESH TRAINER / BRZE rebind
HOME / REFRESH TRAINER is now a full trainer-to-BRZE runtime rebind, not a BRZE process restart.
It:
1. stops the trainer timer;
2. calls `HeroEffectDirectCoreV30.Shutdown()` to restore duration holds and clear Hero tracking;
3. calls `IntegratedFrameDispatcherCore.StopAll()` to restore the shared frame hook, free its cave/handle and clear native queue/COPY buffer;
4. resets/stops UnitChanger, PausePeasant, Wolf, Reveal, InstantDeath facade, Stamina, Horse, HookCore, Selection and Native;
5. clears stale hotkey edge state;
6. refreshes the Windows BRZE Process object and force-runs `GameGate.Probe(true)` to re-read PID/module/player state;
7. restarts the UI timer; the next normal tick rebuilds only still-enabled features.

This is intended to recover from the user's observed case where BRZE is still running but trainer features stop affecting it.

## Conflict audit — important finding and repair
The first V34 audit run correctly found three duplicate compiled fixed-address writers left over from dormant legacy `Native` code:
- `0x1A70C6`: legacy Native + `HookCore`;
- `0x1CCD4D`: legacy Native + `HookCore`;
- `0x467AF4`: legacy Native + `PausePeasantCore`.

These were not active through the current MainForm, but keeping duplicate compiled writers was unnecessary risk. V34 therefore retired the obsolete legacy Native hook installer / ApplyLegacyRuntime fixed writes and removed its old hook restoration path. Active resource/build methods in Native remain.

Second audit run PASS:
- 15 compiled source files scanned;
- 14 unique fixed module mutation sites resolved;
- **no fixed-address mutation site is owned by multiple compiled cores**;
- frame hook `0x135C43` owner = **IntegratedFrameDispatcherCore only**;
- Instant Death + Hero native replay use the same shared dispatcher;
- HookCore stamina input remains disabled while `StaminaCore` owns active stamina behavior;
- hard refresh teardown/re-probe markers verified.

Resolved fixed owners include:
- Horse `0x0D5482` -> HorseCore;
- training read `0x0D5DDB` -> HookCore;
- UnitChanger completion `0x0D5E08` -> UnitChangerCore;
- FOW reinit `0x10D868` -> RevealMapCore;
- shared render hook `0x135C43` -> IntegratedFrameDispatcherCore;
- peasant auto schedule `0x17FFA5` + cap gate `0x1A7000` -> SelectionCore;
- select/HP/stamina hooks `0x1A70C6`, `0x1A78CC`, `0x1CCD4D`, `0x1CCDFB`, `0x1CCE79` -> HookCore;
- stamina read `0x1D3B42` -> StaminaCore;
- peasant creation `0x467AF4` -> PausePeasantCore.

## CI proof
Workflow: `Final V34 Candidate Four-Pack`
- run `34848703691` — SUCCESS
- job `103990876302` — SUCCESS
- head `53d3c2f36d1ca2cbaa19c2a4ca21a2a3780192ae`
- artifact `10349337544`
- artifact digest `sha256:f90388fa3f9fd25d5df5a1575be2038a85920d29fcac91feba86cc87f750be7c`

Binaries:
- CLEAN standalone SHA256 `38790b4572c6810e9acacc98930706a590b7d7673237018a9be93fb402573105`
- DIAGNOSTICS standalone SHA256 `4c8fa36e797ddabcd353625c4a90edaf53f251975a8a7cec7ac5589690831448`
- CLEAN small SHA256 `fd8fbf76635a40b8e9a93b673d4ebedd0e91c0a4e739b2e6ada79437ce864729`
- DIAGNOSTICS small SHA256 `2c088a63b4459055a3d402955e15520e836dd51fca1e63e310ed1c5db84cd433`

Architecture verifier, conflict audit, CLEAN/DIAGNOSTICS build, all four publishes, hashes and artifact upload all passed.
Legacy unrelated `fix-large-selection-freeze.yml` failure remains irrelevant.

## Exact runtime final smoke
Use V34 Diagnostics standalone.
1. Alt+W overlay still show/hide while BRZE remains live.
2. COPY offset: verify 8.0 -> 7.9/8.1 and test a cramped placement such as 0.1–1.0.
3. Confirm Hero apply/reset, death target policy and mini Unit Changer still work as in V33/V30.
4. Turn on a few normal cheats; press HOME / REFRESH TRAINER while BRZE is running. After the next tick, still-enabled cheats should reattach automatically. COPY buffer is intentionally cleared.
5. If the trainer ever reaches the prior 'BRZE won't take cheats' state, press HOME once and verify it recovers without restarting BRZE.

If the user reports this smoke PASS, promote V34 artifacts/source to **FINAL 100%**. Do not add new probes or redesigns unless a concrete regression is reported.
