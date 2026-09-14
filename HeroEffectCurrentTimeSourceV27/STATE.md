# BRZE Hero Effect Current-Time Source V27 — STATE

Status: **BUILT NEXT — READ-ONLY CALLER/CLOCK-SOURCE DISCOVERY**
Date: 2026-09-14 JST

## Why V27 exists
V26 runtime rejected the hypothesis that writing `record+0x194 = 0` causes an already-active effect to re-enter the normal timestamp-initialization path.

V26 runtime result:
- live A5 record `0x235ADCA4`
- old `record+0x194 = 375600`
- V26 wrote only `+0x194 = 0`
- game did not repopulate it within ~1 second
- fail-safe restored `375600` successfully

Therefore zero-reset is permanently rejected.

## Still-proven V25 fact
Function RVA `0x13A5EB` consumes a stack argument at `[EBP+8]` that participates as the current game-time/tick input:
- creation/init path copies `[EBP+8]` into `record+0x194` when the record is being initialized;
- later expiry logic subtracts `record+0x194` from current time and compares elapsed time with `record+0x1F4 -> config+0xE0`.

A safe clock reset may still be possible if we can read the **actual current BRZE game-time value** and write that exact value into an existing same-effect record. No guessed wall-clock conversion is allowed.

## V27 purpose
Strict read-only discovery of the source of RVA `0x13A5EB` argument `[EBP+8]`.

V27:
1. scans `.text` for direct `E8` call xrefs to RVA `0x13A5EB`;
2. reconstructs likely caller function boundaries and dumps instructions around each call;
3. samples absolute memory operands used by caller code twice, 350ms apart, highlighting changing clock-like values;
4. scans non-`.text` sections for relocated runtime pointers to the tick function, to reveal possible vtable/indirect dispatch;
5. prints a recursive direct-caller summary up to depth 3;
6. optionally records the currently selected live A5 record for runtime context.

## Safety
- PROCESS_VM_READ | PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no hooks;
- no VirtualAllocEx;
- no CreateRemoteThread;
- no native ability calls;
- no cleanup/destructor calls;
- no mutation of BRZE.

## Decision after runtime
PASS for this discovery stage means the report identifies a plausible real source for the current game-time argument, ideally a readable global or a short caller chain ending in one.

Only after that source is identified should the next guarded write proof set an existing A5 record's `+0x194` directly to the freshly read game-time value.

CI trigger note: this state update intentionally occurred after the V27 workflow file existed so GitHub Actions registers and runs the new read-only workflow.
