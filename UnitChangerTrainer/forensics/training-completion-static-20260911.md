# Unit Changer — static training completion lead — 2026-09-11

Target: current BRZE 1.60 executable used by the trainer project.

This note is a static lead only. Function roles below are not runtime-proven yet.

## Main training update/completion region
A building training update routine begins near preferred VA `0x4D5CD2`.

Relevant state:
- building training type: `[building+0x488]`
- building training progress: `[building+0x490]`
- fixed completion threshold: `0x00640000`

At `0x4D5DDB` the game reads progress, adds the current training delta, writes progress back, and compares it against `0x640000`.

Completion branch starts at approximately `0x4D5DF1`.

## Candidate native output pipeline
On completion, the routine performs this sequence:

1. `0x4D5DF1`: calls `0x4D285D` with the building in `ECX`.
2. Pushes `1`, building owner `[building+0x84]`, and training type `[building+0x488]`.
3. Calls `0x4D69F6`.
4. Pushes that return value and calls `0x4D6A88` with the building in `ECX`.
5. Return value from `0x4D6A88` is stored as a Unit* candidate (`ESI`).
6. If non-null, the building calls `0x4D231C` with that Unit*.
7. The routine then performs owner/local notification, unit bookkeeping, population/count changes and copies additional building training state into the new unit.
8. Building training state is reset (`+0x490=0`, `+0x488=-1`, and related fields reset).

## Why this matters for multi-output
This strongly suggests BRZE already has a native creation/finalization chain capable of returning a fully initialized Unit* from the building/training context.

The Unit Changer should investigate reusing this native chain for extra configured outputs rather than copying a raw unit structure.

A safe experiment order is:
- prove the meaning/arguments/return of `0x4D69F6`;
- prove `0x4D6A88` returns the newly created trained Unit*;
- determine which bookkeeping after `0x4D6A88` is mandatory per additional unit versus once per training completion;
- first attempt only **one additional output** (1 input -> 2 outputs);
- only after runtime stability expand toward slots 1..9.

## Important risk
Simply calling the creation routine N times may still be unsafe if the surrounding code performs mandatory per-unit registration, population accounting, placement, pathing, selection/event notification, or unique-unit restrictions exactly once. The complete per-unit boundary must be identified before multi-output is enabled.

## Blocker A remains separate
This note does not solve the retraining eligibility red-X path. That must be located independently and bypassed narrowly only while Unit Changer mode is active.
