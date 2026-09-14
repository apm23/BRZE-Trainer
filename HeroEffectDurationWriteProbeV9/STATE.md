# BRZE Hero Effect Duration Write Probe V9 — STATE

Status: **SOURCE READY — BUILD PENDING**
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
7. V9 writes ONLY `config+0x0E0: 15000 -> 30000` and verifies the readback.
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

If the 2x result is not clean, do NOT integrate this field into the main trainer.
