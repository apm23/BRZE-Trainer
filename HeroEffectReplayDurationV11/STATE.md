# BRZE Hero Effect Replay + Duration V11 — STATE

Status: **BUILT / CI-PROVEN — CONFIGURABLE DURATION RUNTIME TEST PENDING**
Date: 2026-09-14 JST

## Proven base
V11 layers above the locked V10.2 runtime proof. V10.2 remains untouched as fallback.

V10.2 runtime proof:
- baseline ORIGINAL Issyl: `10755.0 ms`
- Replay V2 2X: `21115.9 ms`
- ratio: `1.963355x`
- config restored `30000 -> 15000` only after natural expiry
- no repeated application / no refresh loop

Locked rule: any modified Issyl duration must stay resident for the ENTIRE active replay lifetime, then restore only after natural expiry.

## V11 purpose
Single-window configurable Issyl replay duration using the known-good Replay V2 native replay path.

After one baseline capture, the user can replay repeatedly without recapturing baseline:
- `1X` = nominal config `15000`
- `2X` = `30000`
- `3X` = `45000`
- `CUSTOM` = `15000 × multiplier`
- custom UI range: `0.25x .. 20.00x`

The current V11 runtime proof is intentionally single-target only. Multi-target remains future work after single-target runtime validation.

## Architecture
- `HeroEffectReplayV2/ReplayCore.cs` is linked unchanged and remains the only native replay path.
- V11 config core discovers Issyl A5 config through the already-proven content signature / parent path.
- Duration field remains `parent+0x1F4 -> config+0x0E0`.
- Original Issyl nominal duration remains `15000`.
- V11 writes only the discovered A5 config duration after identity/value guards.
- A separate read-only watcher tracks the pinned target's transient roots.
- For non-1X replay, desired duration remains resident for the full replay lifetime.
- After natural expiry is stable for 4 polls, V11 restores to `15000`.
- No repeated native application / refresh.

## Safety / guardrails
- exactly one clean target for V11 initial runtime proof;
- baseline capture required once per fresh trainer/game session;
- replay is blocked unless captured config identity remains A5 and current duration is exactly 15000;
- configurable nominal duration hard guard: `1000..600000`;
- UI custom range currently `0.25x..20.00x` (`3750..300000`);
- while an extended replay is active, starting another configurable replay is blocked;
- normal replay buttons are blocked by UI while an extended hold is active;
- manual RESTORE 15000 available;
- RESET and normal close attempt restore;
- do not manually cast Issyl during an extended hold because A5 config is global during that effect lifetime.

## CI build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Replay Duration V11 Configurable`
Run: `34797972869`
Job: `103834678005`
Head: `ba5f8d63dd578bbc4e2f07980c1c2ae9f256cd6f`
Artifact: `10330576956`
Artifact digest: `sha256:cb7dbf423a52000ab38434f0128c5de3c136dbd86394c1c13402a287d3033444`

CI:
- architecture invariants PASS
- compile smoke PASS
- standalone publish PASS
- small publish PASS
- artifact upload PASS

Binaries:
- Standalone: 151,088,336 bytes — SHA-256 `5d5525754185d34c90313fe3215022121e5fab46c0c66e15d0cccf156800029b`
- Small: 174,322 bytes — SHA-256 `8aba3e1246f890160ac41495c6cb5185f65c7307d26872fef0ae1c55426eab02`

## Exact next runtime test
Use standalone V11.

1. Fresh/reload BRZE.
2. Open V11 only.
3. Select exactly one clean target.
4. Click `1) CAPTURE ISSYL BASELINE`.
5. Cast ORIGINAL Issyl Haste once and wait until V11 reports baseline complete / READY.
6. Select the same clean target again.
7. Click `REPLAY 3X` once.
8. Do not manually cast Issyl again.
9. Wait for natural expiry and automatic restore.
10. COPY REPORT.

Primary proof target:
- baseline around current ~10.7 s;
- 3X replay around ~31–32 s under same game-time scale;
- observed ratio near 3.0x;
- current config returns to 15000 after expiry;
- no stacking/compounded behavior.

If 3X passes, test one custom value such as `4.00x` to verify arbitrary multiplier behavior.
