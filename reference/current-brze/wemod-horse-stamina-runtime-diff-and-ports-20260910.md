# BRZE 1.60 — Wand/WeMod Horse + Stamina runtime diff evidence and ports

Date: 2026-09-10 (Asia/Tokyo)
Target: Battle_Realms_F(5).exe, x86, SHA-256 d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5

## Unlimited Horses — runtime diff evidence
Read-only OFF -> ON -> OFF observer with the same Stable selected showed one reversible .text detour at RVA `0x0D5482`. The native six-slot Stable loop begins at slot0 `building+0x5AC` with stride `0x1C`.

Selected Stable DWORDs changed while Wand Unlimited Horses was ON:
- `+0x5AC`: 0 -> 1
- `+0x5C8`: 0 -> 1
- `+0x5E4`: 0 -> 1
- `+0x600`: 0 -> 1
- `+0x61C`: 0 -> 1
- `+0x638`: 0 -> 1

The local player horse block showed no DWORD changes. This disproves the earlier `HorseRespawnTime=0` approach as the Stable-stock mechanism.

Port branch: `horse-v2-wemod-slotfill-probe`
- source commit: `1d37970e3b1a5326e0d8381146059e0111610988`
- build run: `34428802655` SUCCESS
- artifact: `10133691448`
- status: compile/build proven only; user runtime test pending.

## Unlimited Stamina — runtime diff evidence
Read-only OFF -> ON -> OFF observer with the same unit selected showed one reversible .text detour:
- RVA `0x1D3B42..0x1D3B49`
- original bytes: `66 0F 6E 9E 08 04 00 00`
- instruction: `movd xmm3,[esi+0x408]`
- Wand ON replaces the 8-byte instruction with a JMP detour + NOP padding.

The selected unit's stamina field changed:
- `unit+0x408`: `0x00640000` (6,553,600 raw, 100.0 fixed16) -> `0x05F5E100` (100,000,000 raw)
- after Wand was disabled, this unit field remained at 100,000,000; the code detour itself reverted.

User runtime feedback before this diff: the AddStamina + SetStamina v2 candidate still did not work for running/skills. Therefore the v2 Add/Set mechanism is not the proven Wand mechanism and must not be treated as solved.

## Stamina v3 — Wand read-site port
Branch: `stamina-v3-wemod-readsite-probe`
- source commit: `dbf23c24e1e68c32137e45de1afe96c2a9ceb9de`
- build run: `34429796723` SUCCESS
- artifact: `10134050671`
- artifact ZIP digest: `sha256:d8e68f1bbe43871bb52acbb16ec06547132baa92d5ceb94f101c49b67593a67f`

Implementation:
- F5 is removed from the old HookCore stamina path by passing `false` for HookCore stamina.
- new `StaminaCore` owns F5.
- exact target RVA `0x1D3B42`, verifies exact 8 original bytes before patching.
- local-player + selected (UI `+0x3A8` OR SIM `+0x3AC`) guard.
- writes exact observer-proven raw stamina `100,000,000` to `unit+0x408` immediately before executing the native `movd xmm3,[esi+0x408]`.
- preserves EFLAGS/register state around the guard/write.
- no external per-frame selected-unit scan.

Status: compile/build proven only; runtime test pending.

## Locked unrelated baselines
- Selection500/headroom160 unchanged.
- Peasant fixed interval = 3000 ms.
- Horse v2 remains independent from Stamina v3.
