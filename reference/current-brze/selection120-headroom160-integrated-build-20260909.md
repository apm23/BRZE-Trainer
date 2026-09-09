# Integrated Selection120 + Headroom160 build pin

Date: 2026-09-09 (Asia/Tokyo)

Branch: `selection-120-headroom160-integrated`
Source: `Integrated120Headroom160.cs`
Project: `Integrated120Headroom160.csproj`
Workflow: `.github/workflows/selection-120-headroom160-integrated.yml`
Built head: `b623167036bed91299f4cc8184bb2a2fd821ca34`
Actions run: `34368982513` — SUCCESS

CI:
- compile smoke: success
- x86 self-contained single-file publish: success
- artifact upload: success

Artifact:
- name `BRZE-Selection-120-Headroom160-Integrated`
- artifact ID `10111151679`
- ZIP SHA-256 `c47e2bdf4aa36d4996b8336efb1839cdb1ac14b875ab3688018e0704b9730456`
- EXE SHA-256 `5a13f24b7bc6b6dafbea9bf837e5037358c265f01594ea1c6bd934f23e85790c`
- EXE size `151,055,598` bytes

Integrated behavior:
- F4 max population for local player
- local/UI selection first=120, growth=0
- sort/rebuild temp A/B/active first=120
- local-player simulation selection first=120, growth=0
- manual admission cap 90 -> 120
- selection type-0 event remains LIVE
- physical event backing remains native 256 bytes
- logical event remaining/reset window becomes 160
- no EventBuffer-1024 allocation
- no event suppression
- no per-add refresh suppression
- no Sleep/throttle wrapper

This single EXE is the direct integration of the two-process combination that was runtime-proven successful: Sim-Pipeline-120 + Event Headroom160.

Required regression:
1. fresh BRZE
2. integrated F4 before selecting anything
3. one-shot large drag 80-110
4. confirm no freeze
5. confirm ACTIVE/SIM converge
6. right-click move
7. right-click attack
8. verify >90 remains stable
