# Selection Capacity Investigation — BRZE 1.60 vs BRWOTW

Status: ACTIVE PRIMARY CRASH INVESTIGATION

## Runtime evidence from user

With Specimen B and only F1-F4 + F7 enabled (HP/stamina cheats disabled), attempting a mass selection around 100 units still crashed. Before the crash investigation, the user observed a stronger invariant: after selecting many units, some remaining units refuse to enter the selection even via Shift-click/Shift-drag; after selected units die, previously rejected units become selectable. This strongly indicates a finite central selection capacity / available slot mechanism.

## External/manual behavior evidence

The original Battle Realms manual defines box selection, Shift-add/remove, and control groups as first-class native selection paths. Zen Edition retains these systems and later changed control/input behavior, so selection code must be compared structurally rather than assuming the 2001 implementation is byte-identical.

## Investigation rules

1. Do NOT patch a guessed `cmp count,N` until backing storage and every important consumer are proven.
2. Find the authoritative selected-unit container: pointer/base, count/end pointer, element width, capacity/end pointer, allocation/init, clear, add, remove/death cleanup.
3. Trace every insertion path: single click, box selection, Shift-add, double-click/select-same-type, select-all-combat, control-group recall.
4. Trace important consumers: command dispatch, UI portrait/card enumeration, battle-gear enumeration, movement/order construction, group assignment/recall, save/load if applicable.
5. Compare BRWOTW and BRZE structurally. Determine whether ZE widened/replaced the container or retained a legacy fixed capacity.
6. Audit Specimen B hooks at current 0x5A70C6 and 0x5A78CC against the authoritative container. Per-unit selected flags +0x3A8/+0x3AC must NOT be treated as the central list unless proven.
7. Keep max-pop 99,999,999 unchanged during this isolation. Population is not to be modified while selection is the variable under investigation.
8. No external high-frequency pool scanning.

## Required proof before raising selection capacity

A safe capacity patch requires all of:
- authoritative container layout;
- current hard limit and where it is enforced;
- actual allocated/backing capacity;
- initialization/allocation site;
- removal/death compaction semantics;
- UI iteration safety;
- order/command packet iteration safety;
- control-group storage compatibility;
- BRWOTW homolog comparison.

If the hard limit equals physical storage, the correct solution is to enlarge/replace storage (or redirect to a larger allocation), not merely bypass the comparison.

## Runtime acceptance tests for a future diagnostic

Use a diagnostic with no HP/stamina selection hooks. Keep F1-F4/F7 behavior otherwise identical to the user's failing scenario.

Test exact boundaries around discovered N: N-1, N, N+1, then 100, 128, 150, 200 where supported. For each size test box select, Shift-add, control-group assignment/recall, movement command, attack command, deselection/reselection, unit death while selected, and save/reload. A capacity change is not accepted if it merely permits highlighting units but crashes a downstream consumer.

## Current working hypothesis

The observed 'rejected until another selected unit dies' behavior is substantially more diagnostic of a full selection container than of population pressure. The earlier 86/99/100 crash observations therefore cannot be interpreted as exact selected counts until the native selected-count source is found.
