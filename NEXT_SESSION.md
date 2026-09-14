# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative fallbacks / current
- V18.3 = locked main stable fallback (`FINAL_CURRENT.md`).
- V20 = integrated fallback.
- V22 = group-safe Hero integrated fallback.
- V29 = runtime-proven standalone reset primitive.
- V30 = **runtime-proven integrated gameplay base**.
- V31 = **latest UI/packaging candidate**: V30 gameplay unchanged + Alt+W no-activate translucent overlay; CI PASS, runtime overlay smoke next.

Do not mutate V18.3 directly. Do not reopen Hero reset research unless a real regression appears.

## Locked Hero Effect facts
- Issyl ID `0xA5`; Grayback ID `0xC0`.
- effect record signature: `record+0x58 == ability ID` and `record+0x17C == target Unit*`.
- transient root path varies; never hard-code one path.
- duration: `record+0x1F4 -> config+0xE0`.
- Issyl nominal baseline `15000`; Grayback nominal baseline `60000`.
- modified duration must remain resident for the full tracked natural lifetime, then restore.
- never restore on lifecycle appearance or native-helper return.

Rejected forever:
- repeated native replay/refresh on an already-active same effect;
- V26 `record+0x194 = 0` zero-reset;
- treating `record+0x194` as duration;
- Unit+0x460 duration writes;
- CreateRemoteThread;
- assuming duration latches at creation.

## V29 — CURRENT-TICK RESET RUNTIME-PROVEN PASS
State: `HeroEffectCurrentTickResetV29/STATE.md`.

User runtime proof:
- active A5 record `0x235B0E40`;
- old start timestamp `507200`;
- current BRZE tick `519700` from module RVA `0x440A3C`;
- V29 wrote only `record+0x194 = 519700`;
- exact readback passed;
- same A5+target signature remained valid;
- no native ability helper / no second effect instance;
- selected/reset unit visually kept Issyl longer than the unreset comparison unit.

Locked primitive:
**existing same-effect record -> write current BRZE tick to `record+0x194` -> restart same instance from now without stacking.**

## V30 — RUNTIME-PROVEN INTEGRATED GAMEPLAY BASE
State: `V30_RESET_HERO_STATE.md`.

User reported integrated V30 **WORK / PASS**.

Semantics:
- same requested effect already active -> reset existing instance timestamp;
- effect pending -> do not duplicate;
- effect missing -> one-shot Replay V2 native apply only for that missing effect;
- unrelated buffs untouched;
- multi-select supported;
- APPLY BOTH partitions mixed selections so an active same ability is never replayed over itself;
- custom duration retains proven full-lifetime config hold/restore rules.

V30 CI:
- run `34835329285` SUCCESS
- job `103947740941` SUCCESS
- artifact `10343956283`
- digest `sha256:632412dbbc6756b0d06023b5a4f1bf056db81493ea2e78ef12acc926dda6ea90`

V30 is now locked gameplay. V31 may change UI/overlay/packaging only unless a real gameplay regression is demonstrated.

## V31 — ALT+W OVERLAY FOUR-PACK
State: `V31_OVERLAY_STATE.md`.

New UI behavior:
- global hotkey **Alt+W**;
- first Alt+W converts/shows the existing full trainer as a gaming overlay;
- TopMost, borderless, opacity `0.93`;
- centered over `Battle_Realms_F` when available;
- `WS_EX_NOACTIVATE` + `SW_SHOWNOACTIVATE`;
- `WM_MOUSEACTIVATE -> MA_NOACTIVATE` so mouse actions do not foreground the trainer;
- foreground explicitly returned to BRZE after show/hide;
- subsequent Alt+W toggles overlay visibility;
- keyboard focus intentionally remains with BRZE; use mouse for overlay controls / duration spinner while overlay mode is active.

V31 workflow byte-locks V30 gameplay sources before applying the overlay finalizer:
- UnitChangerCore
- HookCore
- InstantDeath wrapper
- IntegratedFrameDispatcherCore
- HeroEffectDirectCoreV30
- IntegratedFeaturePanelsV30

V31 CI:
- workflow `Final V31 Alt-W Overlay Four-Pack`
- run `34837413187` — SUCCESS
- job `103954293809` — SUCCESS
- head `8cf6fe7a138eedda3c60ac159c2a9811b143ed54`
- artifact `10344892769`
- digest `sha256:a4b3a8abf7043d23a5474b5b26b592e20e3ba22b25892b3e1d05f29f5e2d2372`
- CLEAN standalone SHA256 `dcdeb446843a149c75c2580a9c23c8b822a19775ba4d4c82d27e543cb8822b3f`
- DIAGNOSTICS standalone SHA256 `278af5063393b41dabef8b01fdca177bf1f32069e170171d33fb4cde5951fbe8`
- CLEAN small SHA256 `443a250836a2c49ac531cc8cf8c32e03297039fd9d720c9dcea4a7d175e1b460`
- DIAGNOSTICS small SHA256 `ee0dc1f5cf807f01bd9ed74c4414ccdf7fd9b1f9f6a60a45882fe1fa5579978d`

## EXACT NEXT ACTION — V31 OVERLAY RUNTIME SMOKE
Use `BRZE-Trainer-FINAL-V31-Diagnostics.exe` first.

1. Start trainer + BRZE and make BRZE foreground while visibly running.
2. Press Alt+W: overlay should appear above BRZE, borderless/slightly transparent, while BRZE keeps moving instead of pausing.
3. Click one trainer toggle/button: action should register without BRZE losing foreground/running state.
4. Alt+W hides; Alt+W again shows.
5. Quick Hero Effect APPLY/RESET smoke to ensure the UI-only V31 layer did not regress proven V30 gameplay.

If this passes, promote V31 as final overlay distribution candidate.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V30 integrated Hero reset is RUNTIME-PROVEN PASS and locked gameplay. V31 adds Alt+W TopMost 0.93-opacity no-activate overlay only; gameplay sources are CI byte-locked. V31 CI run 34837413187 SUCCESS, artifact 10344892769. Next: runtime overlay smoke: BRZE foreground -> Alt+W show -> game must keep running, mouse trainer action must register without stealing focus, Alt+W hide/show, then quick Hero reset regression smoke.`
