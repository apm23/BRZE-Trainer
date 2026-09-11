# Unit Changer V4 — 1 -> 2 Runtime PASS

Date: 2026-09-11 (Asia/Tokyo)

## Verdict
**RUNTIME PROVEN — SUCCESS**

User runtime report after testing `BRZE-Unit-Changer-Lab-v4-TwoOutputProof`:
- one native-valid training completion successfully emitted **two units**;
- both configured outputs appeared;
- no crash was reported;
- user described the result as a large success.

## Architecture now locked
V4 proves that the runtime-proven V3 completion-only Slot 1 path can coexist with one additional unit created after the first unit's native registration, using BRZE's native creation/finalization and repeated per-unit bookkeeping.

Preserve these rules in all later versions:
- do not return to V2 global mapper replacement;
- keep BRZE eligibility / red-X behavior stock;
- keep Slot 1 on the V3 completion-only architecture;
- additional units must use native `0x4D6A88` creation and the proven per-unit bookkeeping sequence;
- do not clone raw Unit structures;
- building training reset remains once per original training completion, after extra outputs are handled;
- player-local isolation remains required.

## Proven scale
- V3: 1 input -> 1 configured output — runtime proven, including cross-clan regular units.
- V4: 1 input -> 2 configured outputs — runtime proven.

## Next milestone
Open Slot 3 using the same extra-unit boundary and prove 1 -> 3 before scaling to slots 4..9. If 1 -> 3 remains stable, the architecture can be generalized into a bounded loop over enabled output slots rather than adding one bespoke hook per slot.
