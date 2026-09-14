# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Authoritative fallbacks
- Locked main stable fallback: V18.3 (`FINAL_CURRENT.md`).
- V19 remains the latest pre-integration main-trainer fallback.
- Unit Clone Lab standalone remains runtime-proven.
- Replay V2, V10.2, and V11 standalone remain runtime-proven fallbacks.

Do not mutate the locked V18.3 baseline directly.

## V11 configurable duration — PROVEN FINAL
Final authoritative runtime proof:
- original Issyl baseline `10742.1 ms`;
- `ISSYL 3X`, nominal `45000`;
- replay `31612.4 ms`;
- ratio `2.942838x`;
- restore `45000 -> 15000` OK;
- hold inactive after expiry;
- custom duration also runtime-proven longer;
- no repeated native refresh/reapplication.

Duration research is CLOSED. Do not reopen multiplier testing unless a real regression appears.

Locked duration path:
- `parent+0x1F4 -> config+0x0E0`;
- Issyl A5 nominal `15000`;
- modified value must stay resident for the whole effect lifetime;
- restore only after natural expiry.

Rejected forever:
- repeated native replay refresh (old V3);
- Unit+0x460 duration writes;
- parent+0x194 duration writes;
- lifecycle-first restore;
- native-helper-return restore;
- claim that duration latches at creation.

## V20 integrated trainer — BUILT / CI-PROVEN
State: `V20_INTEGRATED_STATE.md`

V20 directly integrates:
- existing V19 trainer mechanics/UI;
- V11 Issyl configurable duration/replay;
- runtime-proven Unit Clone Lab as **COPY UNIT**.

### Layout locked for V20
- top-left: old game cheat toggles;
- top-right: Unit Changer;
- middle-left: `HERO EFFECT // ISSYL DURATION`;
- middle-right: `COPY UNIT // UNIT CLONE LAB`;
- lower row: `SYSTEM STATUS` full-width, aligned with the full left/right width of the upper layout, heading centered;
- bottom action bar retained;
- client area `1640x900` to avoid intentionally cramped/cropped text.

User explicitly rejected further image mockups. Do not generate UI images; modify/build source directly.

### Single shared frame dispatcher
V20 has exactly ONE compiled owner of render-frame hook RVA `0x135C43`: `IntegratedFrameDispatcherCore.cs`.

The shared dispatcher serves:
1. V19 Instant Death cursor action;
2. Hero Replay target helper calls;
3. Copy Unit native position-spawn calls.

Locked native RVAs:
- frame hook `0x135C43`;
- replay target helper `0x1F0C32`;
- native position spawn `0x0C4A1C`;
- cursor unit query `0x135EFB`.

Rules:
- no competing hook at `0x135C43`;
- no CreateRemoteThread;
- FXSAVE/FXRSTOR + pushfd/pushad preserved;
- max 120 replay selected units;
- max 120 copied units;
- shared native queue serializes Replay and Copy Unit;
- no raw Unit struct clone.

### V20 CI pin
Workflow: `Final V20 Integrated Copy Unit Hero Effect`
Run: `34819704044` — SUCCESS
Job: `103898203112` — SUCCESS
Head: `51d18424c8bbb02ac962dce2a33a0a77da374627`
Artifact: `10338255531`
Artifact digest: `sha256:7088f5ef16d945e4cb4705dfaa6ac4ba09e2a73262a531965887aa463c896e6f`

CI passed:
- full V18.3 -> V19 generator chain;
- V20 layout generation;
- locked non-target core hash invariants;
- full-width/no-clipping layout invariants;
- COPY UNIT naming invariant;
- exactly one compiled frame-hook owner;
- V11 duration invariants;
- CLEAN compile;
- DIAGNOSTICS compile;
- all four publish outputs;
- hash + artifact upload.

Build hashes:
- CLEAN standalone `7ac953345ccd2a6774da798b17f484abead475e77b9d291cdfd77c405a93b686`
- DIAGNOSTICS standalone `50327732e173d5f5adcfeaa8b03e4ab7f71a0df439caf36c14de521213c974ec`
- CLEAN small `5f6a36ee5734da3903ed5f127af2d832d592af979f5606d5ac13ef34039b825a`
- DIAGNOSTICS small `f70b8939f29e7e814e5ed4cec247b3f44ebafe1196b97d119c085b2b7af0963d`

## Exact next action — ONE V20 integrated smoke test
Do NOT start another research loop.

Use V20 Diagnostics first:
1. launch BRZE and V20 Diagnostics;
2. confirm attach/status and one ordinary pre-V20 cheat still works;
3. Copy Unit: select unit(s) -> `COPY SELECTED UNITS` -> `PASTE BESIDE` once;
4. Hero Effect: capture one original Issyl baseline -> run one 2X/3X/custom replay -> wait for automatic restore to `15000`;
5. use Instant Death once after those actions to prove the shared dispatcher still serves the V19 path;
6. visually report only real clipping/overlap if any at the user's actual Windows scaling.

If that one integrated smoke passes, promote V20 to the main trainer candidate/final. No repeated duration multiplier testing.

## New-chat bootstrap sentence
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V11 duration is PROVEN FINAL. V20 integrated trainer is BUILT/CI-PROVEN: V19 + V11 Hero Effect + COPY UNIT, precise 3-row layout, SYSTEM STATUS full width, and exactly one shared owner of hook RVA 0x135C43 for Instant Death + Replay + Copy Unit. CI run 34819704044 SUCCESS, artifact 10338255531. Next: one V20 Diagnostics integrated smoke test only, then promote if pass.`
