# Unit Changer V5 — TEMPORARY FINAL / RUNTIME-PROVEN BASE

Date: 2026-09-11 (Asia/Tokyo)

## Runtime verdict
The user has now runtime-proven the V5 architecture beyond the initial 1->9 stress test:

- 1 native-valid training completion -> 9 configured outputs: PASS.
- Mixed/cross-clan regular outputs: PASS.
- Multiple training buildings operating with Unit Changer: PASS.
- Sparse/non-contiguous slot configuration: PASS.
- No crash/freeze reported in these tests.

V5 is therefore the **temporary FINAL base** for Unit Changer and must remain recoverable unchanged.

Build pin:
- source/build head: `35e6c6330665808034da7d846df9b70e856ce1b9`
- workflow run: `34550087750`
- job: `103110964921`
- artifact: `10180487723` (`BRZE-Unit-Changer-Lab-v5-FullNine`)
- artifact ZIP SHA-256: `0d367fe942fd38a19527b932d6e8a06c37dfbd24e9e911456da0b9eea99a97dd`
- standalone SHA-256: `ab8e14e9dbfe7297dd28eb6fc053a91b61f1c157979f97c0fddf8c75af63e50b`
- small SHA-256: `308110aaf7abe6d634838736bdd345d8863d168c3a77450af8bb60942a1c534e`

## Architecture lock
- Global UnitIn->UnitOut mapper remains stock.
- Only real completion flow is redirected.
- First active slot is primary.
- Every other active slot uses the V4-proven native extra-unit create/finalize + full per-unit bookkeeping path.
- Invalid/red-X input remains stock.
- AI-owned training remains stock.
- Do not clone raw Unit structs.
- Do not reintroduce rejected V2 global mapper override.

## Integration rule
When merged into the main trainer, reuse/extract this V5 core directly. UI simplification is allowed, but the game-side hook and per-unit bookkeeping logic must not be redesigned without a new runtime reason.
