# BRZE Hero Effect Lab — STATE

Status: **V1 RUNTIME-REJECTED (NO VISIBLE EFFECT) — SNIFFER PHASE NEXT**
Date: 2026-09-14 JST

## Runtime verdict — V1
User tested the first Grayback + Issyl native-target prototype and reported that applying the captured effects produced **no visible/behavioral effect at all**.

Therefore V1 is NOT runtime-proven and must not be integrated into the main trainer.

What V1 proved only:
- source hero / BattleGear / AbilityDef tables can be read without crashing;
- game-thread queue/hook architecture builds and runs stably enough to execute;
- direct use of the guessed `AbilityDef.CreateMagicAtTarget (+0x288)` candidate is insufficient for Grayback / Issyl.

## Why V1 was rejected
Static reverse engineering confirms `CreateMagicAtTarget` is a real AbilityDef field and native helper `0x5F0C32` is used by BRZE to create target-side magic. However, AbilityDef also contains special-case flags/paths (for example `PackMasterWolfHowl` at `AbilityDef +0x2C8`). Grayback/Issyl may therefore use a special path, a different derived ability, or additional source/context that V1 did not reproduce.

## Next phase — runtime effect sniffer
Do not guess effect IDs from data fields again. Hook the real native target-ability receiver and record live calls while the user activates the original Grayback / Issyl skills in-game.

Initial sniff target:
- native target-side helper preferred VA `0x5F0C32`, RVA `0x1F0C32`;
- entry contract observed from real callsites: `ECX = target Unit*`, stack arg = target ability type;
- log ability id, target Unit*, target UnitType, owner, call count;
- preserve exact original prologue `55 8B EC 56 8B F1` and jump back to `+6`;
- no game-state mutation in sniff mode.

If Grayback/Issyl generate calls here, capture their real runtime ability IDs and replay those IDs to selected units. If either skill produces no calls, expand sniffing to its special native path instead of forcing `CreateMagicAtTarget`.

## V1 build pin (rejected behavior, kept for provenance)
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `6955844d97405f1d35301dd6e25093e562afb2ee`
Workflow: `Hero Effect Lab Native Grayback Issyl`
Run: `34769032391`
Job: `103755098395`
Artifact: `10320254697`
Artifact digest: `sha256:052c6ccaea52a098970c31221e6dff7f087e1b9233a6a6a0bfa400f92c5df4e9`
