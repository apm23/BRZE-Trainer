# BRZE Hero Effect Clock Reset V26 — STATE

Status: **RUNTIME-REJECTED — ZERO-REINIT HYPOTHESIS FALSE**
Date: 2026-09-14 JST

## Proven V25 background
V25 runtime disassembly proved `record+0x194` participates as the effect start/lifecycle timestamp, not duration:
- tick function RVA `0x13A5EB` checks `record+0x194` for zero at `0x13A8B3`;
- on the creation/init path, `[EBP+8]` is copied into `record+0x194` at `0x13A8BC..0x13A8BF`;
- later the game subtracts `record+0x194` from current time at `0x13B1C3..0x13B1C6`;
- `record+0x1F4 -> config+0xE0` supplies nominal duration;
- elapsed vs duration is compared at `0x13B1E2`, with expiry branch at `0x13B1E5`.

This remains valid.

## V26 runtime result — REJECTED
User runtime report on one live stock Issyl A5 instance:
- PID `6528`, moduleBase `0x00870000`
- Unit* `0x22B47F2C`, UnitDef* `0x1688993C`, owner `0`
- A5 record `0x235ADCA4` via `Unit+0x1E4->+0x008`
- signature `ability=0xA5`, `target=0x22B47F2C`
- config `0x1D1FA664`, config ID `A5`, duration `15000`
- old `record+0x194 = 375600`
- V26 wrote exactly `record+0x194 = 0`
- immediate post-write sample remained `0`
- the game did **NOT** repopulate the field within ~1 second
- V26 fail-safe restored old timestamp successfully

### Conclusion
The assumption "mid-lifecycle `+0x194 = 0` makes the normal tick creation path initialize a fresh timestamp" is FALSE.

Do not use this reset primitive again.
Do not interpret the failure as disproving the timestamp role. It only proves the zero-initialization branch is not re-entered/reached for an already-active effect instance in the way V26 assumed.

## Next hypothesis
A safer reset may still be possible without native reapply if we write the **actual current BRZE game-time value** directly into the existing effect's `record+0x194`.

This requires first identifying the source of function RVA `0x13A5EB` argument `[EBP+8]` / current game time from its real caller(s). No guessed timestamps and no synthetic wall-clock conversion.

## V26 CI pin
Workflow: `Hero Effect Clock Reset V26 Guarded`
- run `34828770662` — SUCCESS
- job `103926891243` — SUCCESS
- head `14f9fd9d9e8e5bfe294d518e58be2ad31503a330`
- artifact `10341645845`
- artifact digest `sha256:4e4fb7a7918bf1510626798bfefb1f9ad56bc206891352cf3e8b791d81322e9a`
- standalone SHA256 `0900937462afc93b1aae290c135872b07ff673de1884b208b5648411d18dcaab`
- small SHA256 `f0723b84921604b2558dee30da212db574d42fe6f05e942d1a77fba374b55828`

## Locked safety conclusion
- V26 binary is historical evidence only; do not integrate it.
- Never zero `record+0x194` as the final reset implementation.
- Keep avoiding repeated native replay/stacking.
- Next work must be read-only caller/current-time-source discovery before another write proof.
