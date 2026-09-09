# Selection Capacity TKP Audit

## Runtime evidence from current BRZE test

Specimen B with only F1-F4 and F7 enabled still crashes when attempting the large ~100-unit selection. F5/F6 are disabled, so active HP/stamina protection is no longer required to reproduce the crash.

More importantly, before the crash investigation the game itself visibly refuses to add some units to the current selection by box select or Shift+click. When selected units die, previously refused units can then enter selection. This is strong runtime evidence of a finite selection capacity / selected-unit container.

## Public/original behavior corroboration

Original Battle Realms documentation explicitly supports multi-selection via box drag and Shift+left-click, and team/control-group assignment from the selected set. Zen Edition community/developer discussion confirms selection/control behavior remains intentionally rooted in the original design and mentions Ctrl+C for selecting combat units on screen. This does not establish the numeric cap, but supports treating selection and hotkey groups as native subsystems rather than trainer abstractions.

## Primary RE target

Trace BRWOTW and BRZE separately:

1. mouse box selection / Shift add path
2. AddUnitToSelection / remove-selection path
3. selected-count storage
4. selected-pointer storage/container
5. capacity comparison / reject branch
6. UI unit-card consumers of selected-count
7. order dispatch consumers of selected list
8. hotkey/control-group copy-in and recall paths
9. death/removal cleanup path that frees a selection slot

## Critical rule

Do NOT patch only a compare/branch that appears to be the cap. First prove storage capacity and every important consumer. A fixed pointer array with N entries plus a patched `cmp count,N` would otherwise become an out-of-bounds write and could explain or worsen the crash.

## Relation to existing trainer hooks

Current known selection-event hook sites 0x5A70C6 and 0x5A78CC write unit selection state (+0x3A8). They must be mapped against the authoritative selected-list/count path. A unit-level selected flag is not proof that the unit has a valid slot in the central selected-unit container.

Specimen B also contains native-delta HP/stamina hooks, but F5/F6 disabled means their enabled branches should not suppress damage. The large-selection crash therefore must be investigated independently of active invulnerability behavior.

## Population status

Do not convict 99,999,999 max-pop from this symptom. Existing max-pop audit found no direct allocation or selection-container sizing consumer. Keep population unchanged during selection-capacity isolation so only one variable changes.

## WOTW / old trainer comparison

Old trainer PageUp traverses the old game's native selection mechanism. Use that code as a bridge to locate the BRWOTW selected-list/count representation, then find its structural homolog in BRZE. Compare capacities and storage layout rather than raw address deltas.

## Acceptance evidence before any raised-cap patch

- exact native maximum selected count proven
- exact backing storage proven
- death/removal decrements/compacts list correctly
- box select and Shift add share or safely converge on the same capacity logic
- hotkey group recall cannot overflow selection
- UI/order-dispatch consumers tolerate proposed raised count
- 100, 150, and 200 selected units can be exercised without OOB writes
- no population, HP, stamina, or training feature is needed to make selection stable
