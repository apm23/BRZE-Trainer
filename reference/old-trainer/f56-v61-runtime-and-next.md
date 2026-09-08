# F5/F6 v61 runtime result and next design

Runtime report:
1. HP/stamina refill is delayed.
2. Later in a match, selecting already injured/depleted units may no longer heal/refill them.
3. Large selection no longer freezes, but can crash the game.

Diagnosis from current source + legacy behavior:
- v61 hooks only execute when BRZE reaches the chosen HP/stamina read/update sites. Selection itself does not execute those sites, explaining delayed/no refill after selecting an already injured unit.
- v61 writes huge sentinel values into live unit fields (+404/+408). This is unnecessarily risky in current BRZE and differs from the desired final semantics (lock/refill to actual max).
- External polling must remain removed; v56 proved it causes simulation freeze.

Next architecture (v49 methodology, using both old trainer and BRZE 1.60):
- Do not use +6A4 invincibility in production.
- Do not restore full-pool or selected-list RPM/WPM polling.
- Split concerns:
  A) selection event hook: when a local unit becomes selected, immediately set HP and/or stamina to that unit definition's real max (16.16 fixed point), so injured/depleted units refill immediately on selection;
  B) HP/stamina delta hooks: while selected and enabled, suppress only negative HP/stamina deltas (or restore exact max safely) so values cannot decrease after selection.
- All logic must execute game-side in injected code and preserve registers/flags required by the original routines.
- Keep F7 v49 hook unchanged.
- Add exact original-byte guards and restore originals on detach.

Candidate selection event context already statically known in BRZE 1.60:
- selection manager/container RVA 0x441708
- selecting path calls 0x4ACB59 and then sets unit +0x3A8 = 1
- selected state +0x3A8/+0x3AC and owner +0x240 are runtime proven.

Before shipping next build, remap/use the exact selection event instruction context rather than a heuristic pool scan.
