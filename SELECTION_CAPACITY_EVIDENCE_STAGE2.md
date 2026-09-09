# Selection Capacity Investigation — Stage 2

## New evidence boundary

Runtime evidence remains the strongest clue: when a large selection is saturated, additional units refuse box/Shift selection; after selected units die, previously refused units become selectable. This strongly indicates a finite authoritative selection container or a downstream finite representation.

Public original documentation confirms that box selection, Shift add/remove, and Ctrl+1..9 groups are native selection operations. Zen Edition 1.58 explicitly overhauled controls/input behavior, and 1.58.1 changed ctrl/double-click category selection behavior. Therefore WOTW is a structural bridge, not an address/layout guarantee.

## Current known trainer sites that must NOT be mistaken for the central container

- ZE 0x5A70C6: writes unit selection state +0x3A8
- ZE 0x5A78CC: writes unit selection state +0x3A8
- Probe evidence: +0x3A8/+0x3AC track selected state

These are per-unit state. They do not prove the capacity, count, or storage of the authoritative selected-unit list.

## Old-trainer bridge

The old trainer PageUp implementation is valuable because it traverses the old game's native selected units rather than scanning the whole unit pool. The next RE pass must recover from the unpacked old trainer/BRWOTW path:

1. address of the old selected-list root
2. selected count / end pointer representation
3. element width (pointer/handle/index)
4. iteration termination condition
5. remove/death compaction behavior
6. whether control groups copy the same representation or use another container

Then find the ZE homolog by instruction shape, surrounding callers, and object semantics.

## Capacity proof requirements

A candidate N is not accepted merely because code contains `cmp ..., N`.

For a real hard cap we need at least two of:
- add-selection path rejects when count reaches N
- backing allocation/static array has N entries
- UI/order consumer iterates exactly N or clamps at N
- removal frees/compacts one of N slots
- control-group assignment/recall uses matching N

## Crash hypothesis split

H1 — Native cap only: game safely rejects units beyond N. Trainer crash is elsewhere.

H2 — Trainer hook crosses representations: per-unit +0x3A8 state is changed/replayed for a unit that did not receive a valid central-selection slot, creating inconsistent selected state and a later crash.

H3 — ZE consumer cap: authoritative selection can exceed one downstream fixed representation (UI/order/group), causing native crash around the threshold.

H4 — Population is independent: current max-pop audit has no demonstrated selection allocation dependency. Keep 99,999,999 unchanged during this investigation.

## Safe patch rule

Do not raise a discovered selection cap until backing storage plus all important consumers are proven. If the desired raised cap requires replacing a fixed-size native array, use a dedicated specimen and redirect all proven consumers rather than patching only a compare branch.

## Next implementation specimen once central representation is proven

Build a diagnostic with:
- F1-F4 and proven v49 F7 allowed
- no F5/F6
- no 0x5A70C6/0x5A78CC selection-event hooks
- no external 2000-unit polling
- read-only display/log of authoritative selected count
- optional exact cap/reject-site instrumentation only

This specimen separates native selection behavior from trainer-created per-unit selection-state inconsistency.