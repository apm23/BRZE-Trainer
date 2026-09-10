# Stamina v2 AddStamina + SetStamina probe — 2026-09-10

Target: `Battle_Realms_F(5).exe`, SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`.

## Motivation
User runtime feedback on merged v1 proved that the AddStamina-only hard lock is incomplete: enabling stamina can top up selected units, but running/skill consumption still decreases stamina until the unit is selected again.

## Static target proof
- AddStamina entry: VA `0x5CCDFB`, RVA `0x1CCDFB`.
- Absolute SetStamina entry: VA `0x5CCE79`, RVA `0x1CCE79`.
- Stock first 10 bytes at SetStamina: `55 8B EC 56 8B F1 57 8B 7D 08`.
- SetStamina writes `unit+0x408`, then clamps through native helper VA `0x5D19B1`, RVA `0x1D19B1`.
- `BattleScriptInterface::UnitSetStamina` eventually reaches SetStamina. Multiple native callers reference SetStamina, so stamina can be changed without traversing AddStamina.

## Probe design
Branch: `stamina-v2-setter-probe`.
Source commit: `86205d5191481c1b58ee0275abb2cf451ab06859`.

F5 now combines two game-side hooks for selected local units:
1. AddStamina: reject negative deltas, preserving the existing v1 semantics.
2. SetStamina: when F5 is ON and unit owner is local and either UI-selected `+0x3A8` or SIM-selected `+0x3AC`, replace the absolute SetStamina argument with the native effective maximum returned via `0x5D19B1`, then execute the original SetStamina function.

No external high-frequency selected-unit scanning was added. Selection500/headroom160 and Peasant fixed 3 seconds are unchanged. Horse logic is intentionally untouched in this probe.

## Build pin
Workflow: `Stamina v2 Add+Set probe`.
Run: `34426188276` — SUCCESS.
Artifact ID: `10132757471`.
Artifact ZIP SHA-256: `c32bd1c580b4dd3aad9ff9f8217c219a3a235f770c226dc73daf9f2173692aaf`.
EXE SHA-256: `3989e14d58525dd583e5f4e670b1ebd6b059d87a63243db81cc5b9dbc655c8e5`.
EXE size: `151080060` bytes.

## Status
Compile/build proven only. Runtime behavior is NOT proven yet. Required test: select one local unit, enable F5, then continuously run/use stamina-costing skills without reselecting. The stamina bar should remain flat. If it still drains, capture the WeMod STAMINA tri-diff before patching additional direct-write sites.
