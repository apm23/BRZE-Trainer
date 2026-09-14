# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks
- Locked main stable fallback: V18.3 (`FINAL_CURRENT.md`).
- V19 = latest pre-integration main-trainer fallback.
- V20 = integrated fallback.
- V21 Direct Hero = runtime-rejected due generic transient-root gating/hold bugs.
- V22 Group-Safe Hero = built/CI-proven integrated fallback while true reset/replace semantics are finalized.
- Unit Clone Lab standalone remains runtime-proven.
- Replay V2 / V11 duration proof remain runtime-proven references.

Do not mutate V18.3 directly.

## Locked duration facts
- Issyl A5 config base nominal `15000`; natural wall ~`10.7s`.
- Grayback C0 config base nominal `60000`; natural wall ~`42.2s`.
- `record+0x1F4 -> config+0x0E0` is the proven duration path.
- Extended config must remain resident through natural effect lifetime, then restore.
- V11 final proof: baseline `10742.1ms`; Issyl nominal `45000` -> `31612.4ms`, ratio `2.942838x`; restore `45000 -> 15000` OK.

Rejected forever:
- repeated native replay refresh / stacking;
- Unit+0x460 duration writes;
- record+0x194 as duration;
- lifecycle-first restore;
- native-helper-return restore;
- assuming duration fully latches at creation.

## User-requested final semantics
Hero Effect APPLY should feel like replacement/reset:
- if selected unit already has same Issyl/Grayback buff, restart that same buff from zero;
- if selected unit does not have it, apply once normally;
- unrelated buffs remain untouched;
- never blindly native-reapply on top of an active same effect.

## V25 Expiry Tail — RUNTIME-PROVEN READ-ONLY
Runtime report established the exact duration clock flow for A5:
- active A5 record `0x235AD8AC` on Unit* `0x22B47F2C`;
- `record+0x194 = 355400`;
- `record+0x1F4 -> config 0x1D1FA664`;
- config ID `A5`, config+0xE0 `15000`.

Important code flow:
- `0x13A8B3`: if record+0x194 is zero;
- `0x13A8BC..0x13A8BF`: write current tick argument `[EBP+8]` into record+0x194;
- `0x13B1C3..0x13B1C6`: currentTime - record+0x194;
- `0x13B1CC`: load config+0xE0 duration;
- `0x13B1E2`: elapsed/duration compare;
- `0x13B1E5`: branch to `0x13B74D` on expiry condition.

Conclusion: record+0x194 is a START/LIFECYCLE TIMESTAMP. This creates a safer reset primitive: zero timestamp and let BRZE itself start a new clock on the existing effect instance. Do not call cleanup/destructor and do not native-reapply the same active effect.

## V26 Clock Reset — BUILT / CI-PROVEN GUARDED WRITE PROOF
State: `HeroEffectClockResetV26/STATE.md`

Purpose:
- prove that an already-active Issyl A5 can restart from zero WITHOUT creating another effect instance.

Mechanism:
1. exactly one selected unit;
2. signature-lock active A5 with record+0x58=A5 and record+0x17C=selected Unit*;
3. require config ID A5 and stock config+0xE0=15000;
4. capture nonzero old record+0x194;
5. write ONLY record+0x194=0;
6. no native replay/call;
7. poll same record until BRZE itself writes a new nonzero timestamp;
8. if it stays zero ~1s with signature intact, attempt to restore old timestamp.

### V26 CI pin
Workflow: `Hero Effect Clock Reset V26 Guarded`
- run `34828770662` — SUCCESS
- job `103926891243` — SUCCESS
- head `14f9fd9d9e8e5bfe294d518e58be2ad31503a330`
- artifact `10341645845`
- digest `sha256:4e4fb7a7918bf1510626798bfefb1f9ad56bc206891352cf3e8b791d81322e9a`
- standalone SHA256 `0900937462afc93b1aae290c135872b07ff673de1884b208b5648411d18dcaab`
- small SHA256 `f0723b84921604b2558dee30da212db574d42fe6f05e942d1a77fba374b55828`

## EXACT NEXT ACTION — ONE V26 RUNTIME PROOF
1. Give stock/original Issyl to exactly ONE unit.
2. While Issyl is visibly active, select only that unit.
3. Open `BRZE-Hero-Effect-Clock-Reset-V26-Guarded.exe`.
4. Click `RESET ACTIVE ISSYL CLOCK` exactly once.
5. Copy/send report.
6. Observe whether the visible Issyl effect lasts about a fresh full stock lifetime from the reset moment.

Do not run repeated tests until the first report is reviewed.

PASS requires same A5 record + same target signature, old nonzero timestamp -> new different nonzero timestamp, no crash/freeze/corruption, and visually restarted lifetime.

If PASS: integrate this clock-reset primitive into the final Hero Effect panel. Active same-effect targets reset their existing clocks; targets without the effect get the proven one-shot native apply. Then extend the identical guarded mechanism to C0 Grayback and multi-selection.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. User wants Hero Effect APPLY to reset an already-active same buff instead of stacking or blocking. V25 runtime proved record+0x194 is the start timestamp: zero -> tick initializes [EBP+8], later currentTime-start compared to config+0xE0. V26 guarded proof is BUILT/CI-PROVEN: one selected stock A5, write ONLY record+0x194=0, no native replay, verify same record gets a new timestamp. Run 34828770662 SUCCESS, artifact 10341645845. Next: ONE V26 runtime proof and send report.`
