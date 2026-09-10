# V7 Wolves rejected -> V8 Wolves Den stock — RUNTIME LOCKED — 2026-09-10

## Runtime results locked
- Instant Death BURST / ERASER: PASS.
- Instant Death SINGLE one-shot: PASS.
- Reveal Map ON/OFF: PASS and locked.
- V7 Unlimited Wolves per-unit `UnitGiveWolfToUnit` cap bypass: FAIL in runtime. Permanently rejected unless new evidence appears.
- V8 Wolves Den stock implementation: **PASS TOTAL / runtime-proven**.

## Exact old F10 evidence
The unpacked legacy trainer resolves its current object and performs a direct one-byte write:

`[object + 0x250] = 250`

This is not a code hook and not a per-unit companion-cap modification.

## Current BRZE mapping
Current BRZE code identifies Wolves Den as building type `0x44` and uses building `+0x250` as its wolf stock counter:
- Wolves Den type check: `[building+0x78] == 0x44`.
- Native full-stock comparison: `[building+0x250]` against 12.
- Native wolf production increments `+0x250` and clamps to 12.
- Releasing/using a wolf decrements `+0x250`.
- Current selected-building globals: RVA `0x4417D4` and `0x4417D8`.
- Building owner: `+0x84`.

## V8 implementation — LOCKED
`WolfCore.cs` does not patch `UnitGiveWolfToUnit`.
It accepts only a selected building that:
- belongs to the local player;
- has building type exactly `0x44` (Wolves Den).

The Den is latched while the checkbox is ON, then the trainer maintains the exact legacy one-byte value `250` at `den+0x250`. Changing selection after latching cannot redirect the write to another building. Multiple local Dens can be added by selecting each once while ON.

OFF stops all writes and clears the latch; it does not forcibly reduce existing stock.

## Build pin
- branch: `instant-death-v4-hover-telemetry`
- source/build head: `d6ab40fbe93eed598a94e12b1c18129a00a9d7d7`
- workflow: `Death V8 Wolves Den Stock`
- run: `34449027111` SUCCESS
- job: `102780200805` SUCCESS
- artifact: `10140856463` (`BRZE-Trainer-V8-WolvesDenStock`)
- artifact ZIP SHA-256: `26a460008142794a8c915bfe1adc6a31b094e39e8f19b9c596b92d07e6ab1163`
- EXE SHA-256: `909d599d5270caeae83c16f5f14ad2b65509d222dff0d9870fa11fa3016311f3`

## Runtime acceptance result
User tested V8 and reported **"work total"**. Therefore the exact Wolves Den stock path is now the authoritative Unlimited Wolves/F10 implementation.

**LOCK:** preserve V8 local-owner + type `0x44` + latched Wolves Den + one-byte `+0x250 = 250` architecture. Do not revert to the rejected V7 per-unit owned-wolf cap hook.
