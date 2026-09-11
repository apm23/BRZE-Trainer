# BRZE Trainer FINAL V15 Compact UI — Build Candidate

Date: 2026-09-11 (Asia/Tokyo)

Status: build/static PASS; runtime visual check pending user.

Runtime foundation:
- V14 building-profile core is locked from user runtime PASS: Dragon Dojo 9 outputs + Dragon Target Range 9 outputs simultaneously, independent per-building outputs, no reported cross-talk.
- V15 changes UI only; profiled Unit Changer runtime core remains generated from the same V14 architecture.

V15 UI changes:
- Client size reduced from 1720x820 to 1640x720.
- Top row uses equal 258px height for Main Cheats (left) and Unit Changer (right).
- SYSTEM STATUS now spans full width below both top panels and remains vertically scrollable.
- Removed 12 large building buttons; one compact Building dropdown selects which of the 12 persistent profiles is being edited.
- All 12 profiles still retain independent 1..9 output settings and may stay active simultaneously.
- Removed `Use` text from slot checkboxes; checkbox is icon-only.
- Unit slots are compact 3x3 single-row strips: S# + checkbox + unit dropdown.
- Unit dropdown display removes hexadecimal suffix to maximize readable unit-name width. Raw IDs remain available in Diagnostics.
- Clean and Diagnostics variants retained.

Build pin:
- branch: instant-death-v4-hover-telemetry
- workflow: Final V15 Compact UI Clean Diagnostics
- run: 34563835023
- job: 103151783037
- build head: 6bbe891826b545ba1be6a2e1ff006260dd2b6b6a
- artifact: 10185305510
- artifact ZIP SHA-256: ac35cbd0f6f38323088d2f73011da28952baa8da4b1f475e39e1aa4b4d8d30cc

Outputs:
- BRZE-Trainer-FINAL-V15-Clean.exe
  - 66,040,956 bytes
  - SHA-256 610249a52b51074ae0d38228bad3c9c39d6395ebe353d1f758bd2f84f34f2f15
- BRZE-Trainer-FINAL-V15-Diagnostics.exe
  - 66,041,162 bytes
  - SHA-256 26fe5620b1363ccbb8871bf3a629e39d66df6cf000436f3abb1e878519aa4708
- BRZE-Trainer-FINAL-V15-Clean-Small.exe
  - 247,454 bytes
  - SHA-256 60bdf8c89cdecd4ddc9d122a97f8e36ce9c7823925b269bfce49d2a8392c816e
- BRZE-Trainer-FINAL-V15-Diagnostics-Small.exe
  - 247,966 bytes
  - SHA-256 4eca6fdf72939a44832413c9c0f0adfaea6c254d61eab488a0c5db8dca1fd1c2

CI:
- V15 UI architecture guard PASS.
- V14 UnitChangerCore architecture guard PASS.
- Clean compile PASS.
- Diagnostics compile PASS.
- Standalone + Small publish PASS.

Runtime visual validation requested:
- confirm top-left cheat panel and top-right Unit Changer panel have comparable/equal visual height;
- confirm no slot label/checkbox/dropdown clipping;
- confirm long unit names are substantially more readable;
- confirm building dropdown switches editable profile without losing settings;
- confirm SYSTEM STATUS is fully usable in Clean and Diagnostics.
