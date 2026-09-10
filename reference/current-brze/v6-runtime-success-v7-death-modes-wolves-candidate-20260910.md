# BRZE V6 runtime success -> V7 death modes + wolves candidate

Date: 2026-09-10 (Asia/Tokyo)
Target: `Battle_Realms_F(5).exe`

## Runtime-confirmed locks

### Instant Death V6
User runtime test confirms the V6 InterfaceMouse architecture works exactly as intended:
- only the enemy unit physically under the cursor is affected;
- no selection/click is required;
- unrelated enemy units stay alive;
- allied/local units are filtered;
- rejected V5 global/scratch pointer behavior is gone.

Keep the V6 target architecture locked:
- frame hook RVA `0x135C43`
- InterfaceMouse RVA `0x443640`
- cursor Unit* helper RVA `0x135EFB`
- alliance helper RVA `0x1848E8`
- owner `+0x240`, HP `+0x404`, stamina `+0x408`
- sentinel `0xFF000000`
- never restore direct `0x3DD858` polling.

### Reveal Map
User runtime test confirms both directions:
- ON reveals the entire map / disables Fog of War;
- OFF immediately restores normal Fog of War.

Reveal Map is therefore runtime-solved and locked on native setter RVA `0x10DBD7`.

## Requested V7 Instant Death UX
Preserve the V6 native cursor query but expose two trigger modes:
1. `BURST / ERASER`: persistent mode. Every enemy crossed by the cursor is killed while enabled.
2. `SINGLE`: one-shot pulse. Only when the user presses the SINGLE button or PageDown is one live cursor query armed. It consumes itself after one render-frame query and does not remain enabled.

Implementation candidate at head `0b51ac108f03a8a0588876b6e12512b9a00e9f0d` uses remote gate values `IDLE=0`, `BURST=1`, `SINGLE=2`; SINGLE self-clears after one active frame. Pressing SINGLE also disables BURST in the UI.

## Unlimited Wolves candidate
Old F10 selected-building `+0x250` remap is not authoritative for unlimited owned wolves and must not be used as the new solution.

Current static path in `UnitGiveWolfToUnit`:
- RVA `0x0C5704`: native `cmp eax,[ecx+0x1B8]`
- `eax` is current owned-wolf count read from unit `+0x63C`
- `[ecx+0x1B8]` is `UnitDef.NumOwnedWolves`
- following native `JGE` rejects when the cap is reached
- master unit owner is `unit+0x240`

`WolfCore.cs` candidate hooks only that 6-byte compare. For local-player units it manufactures flags so the following native JGE does not reject; for non-local/AI units it executes the exact original compare so AI keeps the native cap.

This is compile/CI-proven only, not runtime-proven yet.

## V7 build pin
Workflow: `Death V7 Modes + Unlimited Wolves`
Run: `34445622029` — SUCCESS
Job: `102769659900` — SUCCESS
Head: `0b51ac108f03a8a0588876b6e12512b9a00e9f0d`
Artifact: `10139554308` — `BRZE-Trainer-V7-DeathModes-UnlimitedWolves`
Artifact ZIP digest: `sha256:c2231cea7e0543c6c73e61d9017045070317c800f17cf726c7f435efe54e0899`
Published EXE SHA-256 after download/extract: `3114f51f5ad7741bca346a825f0533972f7391993d77948d02c0afc4ac8f9dab`

## Next runtime hinge
Test Unlimited Wolves before final pack. Confirm:
- local unit can receive more wolves than its normal `UnitDef.NumOwnedWolves` cap;
- existing wolf behavior/commands remain normal;
- AI/enemy units still obey native cap;
- OFF restores normal local cap without corrupting existing wolves.

Do not mark Unlimited Wolves locked until user runtime confirms those points.
