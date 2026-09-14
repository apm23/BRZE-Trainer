# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks
- V18.3 = locked main stable fallback (`FINAL_CURRENT.md`).
- V20 = integrated fallback.
- V22 = group-safe Hero integrated fallback.
- V29 = runtime-proven standalone reset primitive.
- V30 = latest integrated candidate, built/CI-proven; runtime integration smoke next.

Do not mutate V18.3 directly.

## Locked Hero Effect facts
- Issyl ID `0xA5`; Grayback ID `0xC0`.
- effect record signature: `record+0x58 == ability ID` and `record+0x17C == target Unit*`.
- transient root path varies; never hard-code one path.
- duration: `record+0x1F4 -> config+0xE0`.
- Issyl nominal baseline `15000`; Grayback nominal baseline `60000`.
- modified duration must remain resident for the full tracked natural lifetime, then restore.
- never restore on lifecycle appearance or native-helper return.

Rejected forever:
- repeated native replay/refresh on an already-active same effect;
- V26 `record+0x194 = 0` zero-reset;
- treating `record+0x194` as duration;
- Unit+0x460 duration writes;
- CreateRemoteThread;
- assuming duration latches at creation.

## BRZE minimize behavior
User runtime fact: when BRZE is minimized, simulation/game time pauses. A game-clock sample can legitimately remain unchanged while minimized. Do not reject the proven tick source for delta=0 under minimize.

## V28 — current simulation tick source RUNTIME-PROVEN
Exact register flow:
- `0x1C3181`: `ESI <- [0x00CB0A3C]`
- `0x1C3243`: `PUSH ESI`
- next caller passes it as `[EBP+8]`
- `0x140106`: `EBX <- [EBP+8]`
- `0x140120`: `PUSH EBX`
- `0x140125`: CALL effect tick RVA `0x13A5EB`.

At moduleBase `0x00870000`, absolute `0x00CB0A3C` = module RVA **`0x440A3C`**.

## V29 — CURRENT-TICK RESET RUNTIME-PROVEN PASS
State: `HeroEffectCurrentTickResetV29/STATE.md`.

User runtime report:
- active A5 record `0x235B0E40`;
- old start timestamp `507200`;
- current BRZE tick `519700` from module RVA `0x440A3C`;
- V29 wrote only `record+0x194 = 519700`;
- exact readback passed;
- same A5+target signature remained valid;
- no native ability helper / no second effect instance.

Visual proof: selected/reset unit kept Issyl active longer than the comparison unit that was not reset.

Locked reset primitive:
**existing same-effect record -> write current BRZE tick to `record+0x194` -> restart the same instance from now without stacking.**

## V30 — INTEGRATED HERO RESET CANDIDATE
State: `V30_RESET_HERO_STATE.md`.

Source:
- `HeroEffectDirectCoreV30.cs`
- `IntegratedFeaturePanelsV30.cs`
- `MergedTrainerV30.csproj`
- `tools/finalize_v30_reset_hero.py`
- `.github/workflows/final-v30-reset-hero.yml`

Final semantics:
- selected unit already has requested same effect -> signature-lock and reset existing `+0x194` to current tick;
- effect pending -> do not duplicate;
- effect absent -> one-shot Replay V2 native apply only for that missing effect;
- unrelated buffs untouched;
- multi-select supported;
- `APPLY BOTH` partitions mixed selections into missing-both / missing-Grayback-only / missing-Issyl-only batches so no active same effect is replayed on top of itself;
- mixed native batches serialize through the single shared dispatcher;
- custom duration keeps proven full-lifetime config hold/restore rules.

V30 CI:
- workflow `Final V30 Runtime-Proven Hero Reset`
- run `34835329285` — SUCCESS
- job `103947740941` — SUCCESS
- head `13d0aff27a7a278e9cc8f1937a97285351a4dcff`
- artifact `10343956283`
- digest `sha256:632412dbbc6756b0d06023b5a4f1bf056db81493ea2e78ef12acc926dda6ea90`
- CLEAN standalone SHA256 `603a141f1806f5fadeba01378e51ac97d3ead10b534ee5e1fc65871497ff565c`
- DIAGNOSTICS standalone SHA256 `f2e4f9f2e6e28dc2e0ab55338a90e36655ae1c010b0ca80f6f7d2a4d18094517`
- CLEAN small SHA256 `2f7bbafb490702f82c36f03204f6e048f8c9eb0701e23018a9fddd0fa0009ef4`
- DIAGNOSTICS small SHA256 `2c046f4034ca5686b6fcb63d578e205073cab973b41c0a762d35e4cb408d1237`

CI locked:
- V20 integrated base generation;
- V22 explicit-target dispatcher facade;
- V30 finalizer cannot mutate shared dispatcher;
- UnitChanger / HookCore / InstantDeath wrapper hashes unchanged;
- exactly one frame-hook owner at RVA `0x135C43`;
- CLEAN + DIAGNOSTICS compile and all four publishes PASS.

## EXACT NEXT ACTION — ONE COMPACT V30 RUNTIME SMOKE
Use `BRZE-Trainer-FINAL-V30-Diagnostics.exe` first.

1. Have one unit with active Issyl and one unit without Issyl; select both and APPLY ISSYL at one duration.
   - active one must restart;
   - missing one must get one fresh apply.
2. Before expiry, select both and APPLY the same Issyl/duration again.
   - both existing instances must restart;
   - no stacking/compound behavior.
3. Quick Grayback or APPLY BOTH mixed-group smoke.
4. Only re-test Copy Unit / Instant Death if something suspicious appears; their shared base/dispatcher invariants are CI-locked.

If this passes, promote V30 as integrated Hero Effect final candidate. Do NOT resume reset research/probes unless a real regression appears.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V29 current-tick reset is RUNTIME-PROVEN PASS: existing A5 record start 507200 -> current BRZE tick 519700 at module RVA 0x440A3C, exact readback and same signature, visual lifetime restart confirmed. V30 integrates same-effect reset + missing-only native apply + mixed APPLY BOTH partitioning. V30 CI run 34835329285 SUCCESS, artifact 10343956283. Next: one compact runtime smoke using V30 Diagnostics; if pass, promote final candidate. No more reset probes.`
