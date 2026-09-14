# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks / current
- V18.3 = locked main stable fallback (`FINAL_CURRENT.md`).
- V29 = runtime-proven standalone Hero reset primitive.
- V30 = **runtime-proven integrated gameplay base** and must remain locked.
- V31 overlay = **runtime-rejected** because Alt+W did not show.
- V32 overlay = **runtime-proven Alt+W + visual baseline**; user approved overall overlay look/behavior.
- V33 = latest final overlay/UI candidate; **CI PASS, runtime smoke pending**.

Do not reopen Hero reset research unless a real gameplay regression appears.

## Locked V30 gameplay
Hero reset primitive:
- exact same-effect record signature uses `record+0x58` ability ID and `record+0x17C` target Unit*;
- current BRZE tick source is module RVA `0x440A3C`;
- same effect already active -> write current tick to `record+0x194` -> restart same instance without stacking;
- missing effect -> one-shot native Replay V2 apply;
- APPLY BOTH partitions mixed selections so existing same effects are never replayed over themselves;
- custom duration retains proven full-lifetime config hold/restore.

V30 integrated runtime result from user: **WORK / PASS**.

Never reintroduce:
- repeated native replay/refresh on already-active same effect;
- `record+0x194 = 0` reset;
- Unit+0x460 duration writes;
- CreateRemoteThread;
- hard-coded transient effect path.

## V31 — RUNTIME REJECTED
User test: Alt+W did not make the overlay appear.
Rejected architecture: `RegisterHotKey(IntPtr.Zero, ...)` plus `Application.AddMessageFilter` for thread-level WM_HOTKEY.

## V32 — RUNTIME-PROVEN OVERLAY BASELINE
Alt+W now works over BRZE using:
- dedicated message-only `NativeWindow` HWND owning `RegisterHotKey`;
- edge-triggered `GetAsyncKeyState` fallback every 45 ms;
- shared debounce.

User supplied screenshot and approved overall visual/behavior. Requested final polish only:
- duration value needed to be visibly displayed;
- COPY UNIT offset needed visible/adjustable;
- SINGLE KILL button unnecessary;
- Death Burst toggle unnecessary because press/hold already supplies single/burst;
- replace those with ENEMY ONLY / ALL UNITS target-policy switch;
- add mini Unit Changer without reducing functionality;
- raise opacity from 87% to 91%.

## V33 — FINAL PREMIUM OVERLAY
State: `V33_OVERLAY_STATE.md`.

V33 keeps V30 gameplay byte-locked and changes overlay/UI only.

Overlay changes:
- opacity **91%**;
- Hero duration has a dedicated visible value badge (`N SEC`) plus -5 / +5 / reset 30;
- COPY UNIT has a dedicated visible base +X offset badge plus -4 / +4 / reset 8;
- PASTE uses the selected base offset through the existing `IntegratedFrameDispatcherCore.PasteCopied(copyOffset)` path;
- direct overlay SINGLE KILL action removed;
- stale Death Burst quick toggle removed;
- one `DEATH TARGET // ENEMY ONLY` / `ALL UNITS` switch mirrors the authoritative `killAll` control; game-side tap=single / hold=burst behavior remains unchanged;
- overlay keeps the V32 premium visual language and Alt+W architecture.

### Mini Unit Changer — no feature cut
V33 uses a second page inside the same overlay rather than duplicating the gameplay core.
It bridges to the existing authoritative V18.3 Unit Changer UI controls by reflection:
- `profileSelect`: all 12 native training-building profiles;
- `slotOn[0..8]`: all nine output slots;
- `slotOut[0..8]`: full current unit catalog.

Mini page includes:
- all 12 buildings;
- 9 slot ON/OFF controls;
- full existing catalog;
- ALL / DRAGON / SERPENT / LOTUS / WOLF / HEROES filters;
- 1-9 ON and ALL OFF;
- shared state with the normal trainer so the overlay and full UI cannot fight each other.

## V33 CI proof
- workflow `Final V33 Final Premium Overlay Four-Pack`
- run `34842216589` — SUCCESS
- job `103969580217` — SUCCESS
- head `5d33adfea082e84a6935775c71b299a8cbfcd737`
- artifact `10346841656`
- artifact digest `sha256:dc309e75f1bb98c3a3627c6ce8c9a213d84dfdfc21eebfde13a2dbd312655f0f`
- CLEAN standalone SHA256 `f1c1718d92028949757fcdad34b83f4855286bf251c62a56c62469b1ef6b760f`
- DIAGNOSTICS standalone SHA256 `a996b33c8ff67d463c79f69fc3743f8ee18e34fe71a4a59535d7d134598d1928`
- CLEAN small SHA256 `c32ec4fb0ebcdba72d47fcadbebd458e778ea34434e1d55e9bf0f6e714d68b6d`
- DIAGNOSTICS small SHA256 `c9fa9f636363c5afe7fd43173ae92138baed61d3a7356b7e3c33a2382d57791a`

All V33 architecture checks, CLEAN/DIAGNOSTICS compile smoke, all four publishes, hashing and artifact upload passed.
The unrelated legacy `.github/workflows/fix-large-selection-freeze.yml` failure remains irrelevant.

## EXACT NEXT ACTION — V33 RUNTIME FINAL SMOKE
Use `BRZE-Trainer-FINAL-V33-Diagnostics.exe` first.

1. BRZE foreground -> Alt+W show/hide/show.
2. Confirm opacity/visual feel at 91%.
3. Confirm Hero duration visibly shows exact seconds and changes with -5/+5/reset.
4. Confirm COPY UNIT base +X offset visibly changes and PASTE status reports the expected actual +X placement.
5. Toggle DEATH TARGET between ENEMY ONLY and ALL UNITS; tap/hold kill behavior should otherwise remain unchanged.
6. Open UNIT CHANGER page and verify one building + one slot + one catalog selection; normal trainer must mirror the same state.

If these pass, V33 is the final overlay/UI layer over locked V30 gameplay.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V30 integrated gameplay is RUNTIME-PROVEN PASS and locked. V31 hotkey architecture is rejected. V32 Alt+W overlay is runtime-proven and visually approved. V33 is the final polish: 91% opacity, visible Hero duration, visible adjustable COPY UNIT offset, no redundant Single Kill/Death Burst overlay controls, ENEMY ONLY/ALL UNITS target switch, and a full-function mini Unit Changer sharing the normal trainer state. V33 CI run 34842216589 SUCCESS, artifact 10346841656. Next: one final V33 Diagnostics runtime smoke.`
