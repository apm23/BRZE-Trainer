# V16 Categorized Unit Catalog — Build Pin

Status: BUILD/STATIC PROVEN; runtime catalog/UI test pending.
Date: 2026-09-11 Asia/Tokyo

- Workflow: `Final V16 Categorized Unit Catalog Clean Diagnostics`
- Run: `34565194791`
- Head: `34244c6deb3f251c98dd3f2b25b7ee917d02f421`
- Artifact: `10185779463` (`BRZE-Trainer-FINAL-V16-Categorized-Clean-Diagnostics`)
- Artifact ZIP digest: `sha256:69950f94c8aaf51053cad2e7d1682f50a1c43fd995f575faeb8cfd58034e67aa`

Outputs:
- Clean standalone: 66,043,476 bytes; SHA-256 `0745cf2295dc2b36b3e29900bab3642afdfff3ba09a9acd8053f1627d71fd4f0`
- Diagnostics standalone: 66,043,707 bytes; SHA-256 `f792cd5e0f156a8d5a6797370d476052faa942485307e8c620b7ba9480aac322`
- Clean small: 254,622 bytes; SHA-256 `37f2c11fb2255723a61f717d84d023dee8bea713bc4e560aee209488140086a2`
- Diagnostics small: 255,134 bytes; SHA-256 `30a3ff852b9dc9ec89f9b557b0003f25d6911e22d9ba515c1517310074530a1a`

Build gates passed:
- V14 runtime-proven `UnitChangerCore.cs` SHA-256 before/after V16 catalog layer is identical.
- Categorized regular/special/classic hero/WotW unit/WotW hero catalog markers present.
- Reserve IDs 145..154 absent.
- Red owner-drawn risk styling present.
- Risky selection while an active slot is ON disables that slot.
- Explicit Yes/No confirmation required when re-enabling a risky output.
- Clean compile PASS.
- Diagnostics compile PASS.
- All four publish steps PASS.

Do not call V16 runtime-proven until user tests it in BRZE.
