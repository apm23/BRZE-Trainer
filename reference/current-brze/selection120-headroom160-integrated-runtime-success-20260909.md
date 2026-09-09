# Integrated Selection120 + Headroom160 — runtime success

Date: 2026-09-09 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe`

Build:
- branch `selection-120-headroom160-integrated`
- head `b623167036bed91299f4cc8184bb2a2fd821ca34`
- Actions run `34368982513` SUCCESS
- artifact ID `10111151679`
- EXE SHA-256 `5a13f24b7bc6b6dafbea9bf837e5037358c265f01594ea1c6bd934f23e85790c`

User runtime regression result:
- single integrated EXE used alone after a fresh BRZE restart
- F4 armed before selection
- large one-shot rectangle selection passed without freeze
- >90 selection remained functional
- move and attack commands remained functional/authoritative
- user explicitly reported the integrated build "lolos" after retesting

Conclusion:

**Selection120 + logical event headroom160 is runtime-proven as the stable baseline.**

This build is the source baseline for the next population/selection expansion work. Do not reintroduce the failed EventBuffer1024, event suppression, refresh suppression, or in-function Sleep probes.
