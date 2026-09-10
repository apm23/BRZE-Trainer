# Unit Changer — native retraining map function — 2026-09-11

Static reverse of current BRZE 1.60 target.

## Decisive function
Preferred VA `0x4D69F6` maps a training input unit type to the building's configured output unit type.

Calling convention observed:
- `ECX` = Building*
- `[EBP+8]` / stack argument = input unit type ID
- `building+0x258` = BuildingDef*
- return `EAX` = configured output unit type ID, or `0xFFFFFFFF` when no recipe matches.

The function checks exactly six native recipe slots in BuildingDef:

| slot | UnitIn | UnitOut | Rate | Time |
|---|---:|---:|---:|---:|
| 1 | +0x68 | +0x6C | +0x70 | +0x74 |
| 2 | +0x78 | +0x7C | +0x80 | +0x84 |
| 3 | +0x88 | +0x8C | +0x90 | +0x94 |
| 4 | +0x98 | +0x9C | +0xA0 | +0xA4 |
| 5 | +0xA8 | +0xAC | +0xB0 | +0xB4 |
| 6 | +0xB8 | +0xBC | +0xC0 | +0xC4 |

Pseudo-logic:

```
def = building->def_0x258;
if (input == def->UnitIn1) return def->UnitOut1;
if (input == def->UnitIn2) return def->UnitOut2;
if (input == def->UnitIn3) return def->UnitOut3;
if (input == def->UnitIn4) return def->UnitOut4;
if (input == def->UnitIn5) return def->UnitOut5;
if (input == def->UnitIn6) return def->UnitOut6;
return -1;
```

## Parser confirmation
The BR data parser around `0x622A18+` loads keys `UnitIn1`, `UnitOut1`, `UnitTrainingRate1`, `UnitTrainingTime1`, then repeats the same 0x10-byte layout for slots 2..6. This independently confirms the BuildingDef recipe layout above.

## Relevant eligibility callers
Several runtime callers invoke `0x4D69F6` before issuing or validating training-related orders. Two especially useful candidate return addresses are:
- call at `0x531A50` -> return `0x531A55`
- call at `0x555459` -> return `0x55545E`

At `0x531A50`, the code obtains the input type from `[unit+0x74]->type`, calls `0x4D69F6` with the target building in ECX, and explicitly compares the return against `0xFFFFFFFF`. This is a strong static candidate for the user-facing eligibility/red-X path.

## Safer architecture implication
Do NOT broad-bypass every invalid unit/building pair.

If runtime telemetry confirms that the red-X path is driven by `0x4D69F6`, Unit Changer can temporarily populate one unused native recipe slot in the selected BuildingDef while enabled. That lets all existing BRZE code see a legitimate UnitIn -> UnitOut mapping, rather than forcing validation branches to succeed.

This approach is preferable because training start, training progress, normal completion, AI/pathing and downstream bookkeeping continue through native BRZE logic.

## Next runtime proof
Install a behavior-transparent entry telemetry hook on `0x4D69F6` that records:
- total calls
- caller return address
- Building*
- input unit type
- BuildingDef*
- counters for `0x531A55` and `0x55545E`

The hook must execute the displaced stock prologue and return to original code without changing the result or eligibility decision.
