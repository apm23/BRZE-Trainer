# Unit Changer V5 — FULL 1->9 RUNTIME PROVEN

Date: 2026-09-11 (Asia/Tokyo)

## Verdict
User runtime report: **SUKSES BESAR — 9 BIJI KELUAR**.

V5 full nine-output architecture is now runtime-proven for the core target:
- one BRZE-native-valid training completion produced 9 configured units;
- all 9 slots were exercised in one completion;
- no crash/freeze was reported in the successful run;
- this validates scaling the V3/V4 architecture from 1->1 and 1->2 to full 1->9.

## Locked architecture
- Keep global UnitIn->UnitOut mapper at RVA `0x0D69F6` stock.
- Keep eligibility/red-X logic stock.
- Primary output uses the V3-proven completion-only override at RVA `0x0D5E08`.
- Extra outputs use the V4-proven native create/finalize + full per-unit bookkeeping path after the first unit reaches the stock register point at RVA `0x0D5EA4`.
- Building training reset remains stock and executes once after all outputs are handled.
- AI-owned training remains stock.
- Never reintroduce V2 global-mapper override.
- Never clone raw Unit structs for extra outputs.

## V5 build pin
- branch `instant-death-v4-hover-telemetry`
- head `35e6c6330665808034da7d846df9b70e856ce1b9`
- workflow `Unit Changer Lab v5 Full Nine Output`
- run `34550087750` SUCCESS
- job `103110964921` SUCCESS
- artifact `10180487723` (`BRZE-Unit-Changer-Lab-v5-FullNine`)
- artifact ZIP SHA-256 `0d367fe942fd38a19527b932d6e8a06c37dfbd24e9e911456da0b9eea99a97dd`
- standalone size `66,007,332` bytes, SHA-256 `ab8e14e9dbfe7297dd28eb6fc053a91b61f1c157979f97c0fddf8c75af63e50b`
- small size `158,378` bytes, SHA-256 `308110aaf7abe6d634838736bdd345d8863d168c3a77450af8bb60942a1c534e`

## Status
V5 is the current **runtime-proven full-output base**. Future work must preserve this path unless the user reports a regression.

Useful follow-up validation before merge/final polish:
- sparse slots (e.g. only 2/5/9 ON => exactly 3 outputs),
- repeated consecutive training completions,
- multiple buildings training at once,
- population-limit accounting,
- Journey map transitions,
- save/load after generated units exist,
- optional hero/unique output support only after explicit testing.
