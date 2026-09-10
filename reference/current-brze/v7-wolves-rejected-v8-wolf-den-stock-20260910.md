# V7 Wolves rejected -> V8 Wolves Den stock candidate — 2026-09-10

## Runtime results locked
- Instant Death BURST / ERASER: PASS.
- Instant Death SINGLE one-shot: PASS.
- Reveal Map ON/OFF: PASS and already locked.
- V7 Unlimited Wolves per-unit `UnitGiveWolfToUnit` cap bypass: FAIL in runtime. Do not use that path as the F10/Maximum Wolves solution.

## Exact old F10 evidence
The unpacked legacy trainer resolves its current object and performs a direct one-byte write:

`[object + 0x250] = 250`

This is not a code hook and not a per-unit companion-cap modification.

## Current BRZE static proof
Current BRZE code identifies Wolves Den as building type `0x44` and uses building `+0x250` as its wolf stock counter:
- Wolves Den type check: `[building+0x78] == 0x44`.
- Native full-stock comparison: `[building+0x250]` against 12.
- Native wolf production increments `+0x250` and clamps to 12.
- Releasing/using a wolf decrements `+0x250`.
- Current selected-building globals are RVA `0x4417D4` and `0x4417D8`.
- Building owner is `+0x84`.

## V8 candidate
`WolfCore.cs` no longer patches `UnitGiveWolfToUnit`.
It accepts only a selected building that:
- belongs to the local player;
- has building type exactly `0x44` (Wolves Den).

The Den is latched while the checkbox is ON, then the trainer maintains the exact legacy one-byte value `250` at `den+0x250`. Changing selection after latching cannot redirect the write to another building. Multiple local Dens can be added by selecting each once while ON.

OFF stops all writes and clears the latch; it does not forcibly reduce existing stock.

This V8 path is compile/static-evidence candidate only until user runtime confirms Wolves Den can repeatedly supply wolves beyond the normal stock limit.
