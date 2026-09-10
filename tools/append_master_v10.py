from pathlib import Path

p=Path('MASTER_STATE.md')
s=p.read_text(encoding='utf-8')
marker='# FINAL V10 SAFE POLISH'
if marker in s:
    print('V10 already recorded')
    raise SystemExit(0)

s += r'''

---

# FINAL V10 SAFE POLISH — BUILD COMPLETE / RUNTIME UI-SAFETY TEST PENDING

V10 preserves all runtime-proven V8/V9 cheat cores and only changes final UI, pre-game safety gating, and polling/render behavior.

## UI decisions
- Dark Battle Realms V9 theme preserved.
- Window enlarged to 1270x770; switch tiles widened to 190 px.
- Header uses explicit rectangles so `ZEN EDITION · BRZE 1.60 · FINAL PACK` cannot overlap the title.
- Every toggle/action has a visible small second-line hotkey label.
- Toggle mapping: Rice F1; Water F2; Yin/Yang F3; Population F4; Training F7; Peasant F12; Health F6; Stamina F5; Horses F11; Wolves F10; Reveal Insert; Death Burst End.
- Actions: SINGLE KILL PgDn; BUILD NOW Del/F8; ALL ON F9; ALL OFF Shift+F9.
- Old bottom-right helper sentence removed.
- Bottom-right signature: `Create By PokakBg` using a small Segoe Script accent style.

## Safe pre-game gate
- `GameGate` is read-only and polls readiness at low frequency.
- No cheat writer handle/core/hook is started while BRZE is absent, starting, in menu, or battle data is not valid.
- User toggles remain ARMED before a match and apply automatically only once battle/player data is ready.
- SINGLE KILL and BUILD NOW safely no-op before battle.
- Current readiness validation uses RVA player pointer `0x4416A0`, local ID `0x4416D0`, player stride `0x5E8`, and a readable local-player object.

## Window smoothness
- poll timer 100 ms instead of 16 ms
- status repaint throttled to 250 ms and only changes text when content actually changed
- readiness process scan cached 400 ms
- procedural background cached as bitmap and rebuilt only on size change
- polling timer pauses during `WM_ENTERSIZEMOVE` and resumes at `WM_EXITSIZEMOVE`

## Build pin
- source/build head `eddce98d551fd53f8154fde5d8ff36199b2e55a9`
- workflow `Final V10 Safe Polish`
- run `34455956083` SUCCESS
- job `102802264108` SUCCESS
- artifact `10143503857` (`BRZE-Trainer-FINAL-V10-Pack`)
- artifact ZIP SHA-256 `719182489f590a096e55151ca0166bf9ddb45f9b118966f8df640aa55df15579`
- Standalone: 66,022,253 bytes; SHA-256 `8852e15c13eb420f9aa4d760ad7a84a8db4f62efca42f9e68974e9827ab8199f`
- Small: 198,302 bytes; SHA-256 `e6a69b97747323421836d95cdd96ad4a20a0beaf167e595837ac2801853ed9cc`

Reference: `reference/current-brze/final-v10-safe-polish-20260910.md`.

## Acceptance test still required
Do not call V10 UI/safety runtime-locked until user confirms:
1. no clipped header/tile/action text at their DPI;
2. trainer can be opened first, then BRZE launches normally;
3. toggles can be armed before entering a match without crash and activate after battle is ready;
4. moving the trainer window is visibly smoother;
5. locked Burst/Single, Reveal Map, and Wolves behavior still regress cleanly.
'''
p.write_text(s,encoding='utf-8')
print('Appended FINAL V10 SAFE POLISH to MASTER_STATE.md')
