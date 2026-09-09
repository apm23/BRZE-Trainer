# Main merged trainer v1 — Selection500 + hard HP/stamina locks + horses

Date: 2026-09-10 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe`

Branch: `main-merge-selection500-hp-stamina-horse-v1`
Built head: `c84b0bf19c6c9c97a04307fa1d78198606d85e6e`
Actions run: `34378691096` — SUCCESS
Artifact: `BRZE-Trainer-Merged-Selection500-HardLocks-Horses-v1`
Artifact ID: `10114980884`
ZIP SHA-256: `fc9f806ab66192b167bccf2d2cd7b5b0ef2e9f8465922d0c6750e89622f0f27f`
EXE SHA-256: `4258f21d0355a4ab323585dac056588b646da697b2fcb9842bfc4c96aa5a1f4c`
EXE size: `151,080,060` bytes

## Integrated features

Main trainer features retained from `Program.cs`:
- F1 Infinite Rice
- F2 Infinite Water
- F3 Infinite Yin + Yang
- F4 Unlimited Population 9,999,999
- F7 legacy instant training path
- instant building/repair/research actions
- demolition / wolf / legacy controls
- horse native respawn configuration path

Added runtime-proven selection/peasant core:
- Selection500 ALWAYS ON independent of F4
- active/SIM first=500, growth=0 through selection-only wrappers
- logical event headroom160, physical backing native 256
- event type-0 LIVE
- fixed 1.0 second local peasant schedule
- local PeasantManager population-stop bypass while Fast Peasant is enabled

F4 correction:
- saves local stock max-pop and restores it when F4 is OFF
- selection500 remains active regardless of F4

## HP / stamina hard-lock candidate

`HookCore.cs` owns HP/stamina/training hooks so the old main hook implementation stays dormant.

Hard-lock behavior:
- central AddHealth/AddStamina negative deltas are rejected before native mutation for selected local units
- selected predicate accepts either UI-selected `unit+0x3A8` OR SIM-selected `unit+0x3AC` instead of requiring both
- positive healing/stamina gains remain native
- when F5/F6 transitions OFF->ON, currently selected units are topped to their real max once
- newly selected units are topped to real max through selection hooks while the respective lock is enabled
- no per-frame HP/stamina refill walker; target visual behavior is a flat bar with no periodic down/up refill pulse

Status: compile/build proven only; runtime hard-lock behavior still requires user test.

## Horse behavior carried into merged build

The main trainer's horse path writes the current race config `HorseRespawnTime` field (`race config +0x48`) to zero while Unlimited Horses is enabled, then restores the original value when disabled.

This intentionally uses BRZE's native horse/stable respawn system rather than spawning horses manually. Target runtime behavior requested by user: an empty stable/native stock returns up to its normal six horses, and taking a horse is immediately replenished instead of creating an unbounded spawn stream.

Status: static mapping/build proven; exact stable-six runtime behavior still requires user confirmation in this merged build.

## Required runtime test
1. fully restart BRZE and use only this merged EXE;
2. verify Selection500 arms automatically before selection;
3. F4 ON -> 9,999,999, F4 OFF -> stock population while bulk selection remains >90 capable;
4. Fast Peasant fixed1s still produces beyond the old ~167 stop;
5. F6: select a damaged local unit, enable F6; it should jump once to max then never visibly dip under attack;
6. F5: enable on selected units and use stamina-heavy actions; bar should remain flat with no drain/refill pulse;
7. change selection repeatedly and test 100+ selected units to verify hard locks do not stop mid-game;
8. Unlimited Horses: test an empty stable/native horse stock and consume horses; expected native stock remains/replenishes to six without endless extra horses.
