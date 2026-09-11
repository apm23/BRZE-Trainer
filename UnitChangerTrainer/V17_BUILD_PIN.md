# V17 WotW-Only Warning — Build Pin

Status: BUILD/STATIC PROVEN; runtime UI test pending.
Date: 2026-09-11 Asia/Tokyo

- Workflow: `Final V17 WotW Only Warning Clean Diagnostics`
- Run: `34566489119`
- Head: `2d4a644e510140c357175cd43afc39c74fac9adc`
- Artifact: `10186231013` (`BRZE-Trainer-FINAL-V17-WotW-Only-Warning`)
- Artifact ZIP digest: `sha256:174a137d4efeb70f6092401a779fa9f909eac4faa8c26afab4b06feb93c23c8c`

Outputs:
- Clean standalone: 66,043,302 bytes; SHA-256 `621c37764e1e1f0c497a1b5430cebb124c8abb9ab54eb14847c9d5bcda81dd39`
- Diagnostics standalone: 66,043,537 bytes; SHA-256 `3b16e8a136c8676f348509de2f2ee0213c2bb5e1d619847cdd72740f78afa875`
- Clean small: 254,110 bytes; SHA-256 `687b31975aa178e3dce44196a737c8c86ddbec86f3eae7a5f183e6cd16255d46`
- Diagnostics small: 254,622 bytes; SHA-256 `3a7d8a0e7e0df01000898fd87047cd41e8dd9f616d7bbee661c213a1e7c78a5c`

V17 policy:
- ONLY WotW-release-exclusive units/heroes are red/risk-marked.
- Classic heroes such as Arah, Grayback, Kenji, Otomo, Shinja, Zymeth are normal entries.
- Old special/unique units such as Spirit Warrior, Lotus Brothers/Golem, Serpent Necromancer are normal entries.
- WotW units 116..127 remain red.
- WotW story/hero variants 136..144 remain red.
- No modal Yes/No confirmation.
- No automatic slot OFF when selecting WotW content.
- A small red warning line appears below the Unit Changer slots when WotW-only output(s) are selected.
- Reserve placeholder IDs 145..154 remain hidden.

Build gates passed:
- V14 runtime-proven `UnitChangerCore.cs` SHA-256 before/after V16+V17 UI layers is identical.
- WotW-only policy guard PASS.
- Clean compile PASS.
- Diagnostics compile PASS.
- All four publish steps PASS.

Do not call V17 runtime-proven until user visually/runtime tests it in BRZE.
