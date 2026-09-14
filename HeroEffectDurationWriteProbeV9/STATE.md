# BRZE Hero Effect Duration Write Probe V9 — STATE

Status: **BUILT / CI-PROVEN GUARDED WRITE — RUNTIME 2X TEST PENDING**
Date: 2026-09-14 JST

## Why V9 exists
V8 produced strong cross-ability proof that `parent+0x1F4` is an ability-specific config record and `config+0x0E0` is the nominal duration parameter:

- Issyl A5: config ID `0xA5`, `config+0x0E0 = 15000`, observed natural wall lifetime `10727.1 ms`.
- Grayback C0: config ID `0xC0`, `config+0x0E0 = 60000`, observed natural wall lifetime `42222.1 ms`.
- Config ratio is exactly `4.0x`; observed wall-lifetime ratio is `~3.936x`.
- Both config records stayed static through their entire effect lifecycles with zero read failures.

V9 performs the first isolated write proof. It does NOT touch the main trainer.

## Controlled experiment
Issyl only:
1. ARM one clean target.
2. Cast ORIGINAL Issyl Haste once and measure baseline natural lifetime.
3. Capture the A5 config through the proven A5+target signature and `parent+0x1F4`.
4. Require strict guards before any write:
   - `config+0x000 == 0xA5`
   - `config+0x0E0 == 15000`
5. After baseline expiry, select one clean test target.
6. Click `PATCH 30000 + ARM TEST`.
7. V9 writes ONLY `config+0x0E0: 15000 -> 30000` and verifies readback.
8. Cast ORIGINAL Issyl Haste once on the test target.
9. Measure natural test lifetime.
10. At natural expiry, automatically restore `config+0x0E0: 30000 -> 15000` and verify.
11. Report baseline/test ratio.

Expected proof condition: test natural lifetime is approximately `2x` baseline under the same game time scale.

## Safety guards
- no hooks
- no injection
- no VirtualAllocEx
- no VirtualProtectEx
- no CreateRemoteThread
- no native hero-effect replay
- no repeated application/refresh
- only one guarded data field is writable: discovered Issyl `config+0x0E0`
- manual `RESTORE NOW`
- automatic restore after test expiry
- restore on RESET
- restore on normal tool close
- restore refuses to clobber any unexpected value
- patch refuses unless config identity is exactly A5 and original duration is exactly 15000

CI explicitly verifies the guarded architecture before compilation.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Duration Write Probe V9 Guarded`
Run: `34794886552` — SUCCESS
Job: `103825945080` — SUCCESS
Head: `47058aa56524fbd6581d9003903ac252b9b4d854`
Artifact: `10328933404`
Artifact digest: `sha256:9e755bb48a0383ccdf484479648fd720f5205c83a597b029de7c8088c4fc499a`

Binaries:
- Standalone: 151,067,880 bytes — SHA-256 `9dae8e6c07c66417c268fc5226e8273109de4187b0915bc24bce8a3121ebdd72`
- Small: 154,378 bytes — SHA-256 `b11092f3a7d30b205fe3d59279ba76899d5952a4120d678ca74c1649131ef453`

## Runtime test flow
1. Fresh/reload BRZE.
2. Select exactly ONE clean normal target.
3. Click `1) ARM BASELINE`.
4. Select Issyl and cast ORIGINAL Haste once on that target.
5. Wait until V9 says baseline is complete / ready for patch.
6. Select exactly ONE clean target for the test cast.
7. Click `2) PATCH 30000 + ARM TEST`.
8. Select Issyl and cast ORIGINAL Haste once on that target.
9. Do nothing until natural expiry and V9 says TEST COMPLETE.
10. Confirm UI/report says config auto-restored to 15000.
11. Click `COPY REPORT` and return it.

If anything unexpected happens after the patch, click `RESTORE NOW`. Closing the tool normally or RESET also attempts restore.

If the 2x result is not clean, do NOT integrate this field into the main trainer.
