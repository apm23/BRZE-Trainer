# V18 Quick Tabs — Build Pin

Status: BUILD/STATIC PROVEN; runtime UI test pending.
Date: 2026-09-12 Asia/Tokyo

## Build
- Workflow: `Final V18 Quick Tabs Clean Diagnostics`
- Run: `34689579316` SUCCESS
- Head: `1a196b9f12037c3ecbba6926c54f4361d73700d9`
- Artifact: `10296367574` (`BRZE-Trainer-FINAL-V18-Quick-Tabs`)
- Artifact ZIP digest: `sha256:05fa591d79d994454b20aafeba13278a344b6648becb7f155076f0278cf0c2b7`

Outputs:
- Clean standalone: 66,044,142 bytes; SHA-256 `4657a45f406f5d9e0298ba210f6029177c0df74c6556a7e3adf801e78765c1f1`
- Diagnostics standalone: 66,044,390 bytes; SHA-256 `248478ff4b17975c10106fc5f430388597da8943b6bcf4fc21b418f1b3d2c7bb`
- Clean small: 255,646 bytes; SHA-256 `fb25fc4ce377d2443d5b65e78e8490bf5006fc026235bf37fe567235533c3ffe`
- Diagnostics small: 256,158 bytes; SHA-256 `f9c4e9b827629361b9c536e45a3c56932c65afc8792c7657afc425957727b79b`

## V18 UI/catalog policy
- No unit or hero is red/risk-blocked; WotW units/heroes are ordinary selectable outputs.
- Compact quick filters: `ALL`, `DRAGON`, `SERPENT`, `LOTUS`, `WOLF`, `HEROES`.
- Special/unique non-heroes are folded into their clan instead of a separate category.
  - Serpent includes Spirit Warrior variants and Necromancer.
  - Lotus includes Brothers and Golem.
  - WotW non-hero units are folded into Dragon/Serpent/Lotus/Wolf.
- All classic + WotW heroes/story variants remain in `HEROES`.
- Quick-tab filtering never changes configured outputs; if a slot currently points outside the active filter, its current unit is temporarily kept in that combo so configuration is not lost.
- `ALL 1–9 ON` enables all nine outputs for the currently edited building profile in one click.
- Reserve placeholder IDs 145..154 remain hidden.

## Locked game-side core / Refresh Trainer
- V14 runtime-proven `UnitChangerCore.cs` SHA-256 is checked before/after V18 generation and must remain identical.
- `Refresh Trainer` was reviewed and intentionally left functionally unchanged because it already performs a full runtime rebind:
  - resets Unit Changer;
  - stops Pause Peasant, Wolf, Instant Death, Stamina, Horse, Hook, and Selection cores;
  - drops Reveal hook without heavy FOW restore;
  - calls `Native.Detach()`;
  - clears ready state and forces `GameGate.Probe(true)`;
  - preserved toggles are rebound on the next timer tick;
  - no recursive `TickTrainer()` call is allowed inside refresh.
- Unit Changer `Reset()` clears process handle, module base, cave, installed state, and retry state; the next attach reads `process.MainModule.BaseAddress` again.

Build gates passed: generator, V18 invariant guard, UnitChangerCore hash lock, Refresh full-rebind guard, Clean compile, Diagnostics compile, all four publish steps, artifact upload.

Do not call V18 runtime-proven until the user tests the UI/filter behavior in BRZE.
