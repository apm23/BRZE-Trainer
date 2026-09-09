# Runtime success — Sim120 + Event Headroom 160

Date: 2026-09-09 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe`

Test configuration:
- `BRZE-Selection-Sim-Pipeline-120-Probe.exe`
- `BRZE-Event-Headroom-160-Probe.exe`
- read-only `BRZE-Selection-Bulk-Telemetry-Observer.exe`

User runtime result: **large rectangle drag no longer freezes**.

Decisive telemetry from the successful run:
- observer MAX ACTIVE reached `103`
- observer MAX SIM reached `103`
- ACTIVE and SIM repeatedly converged to the same counts after native event flushes
- event queue repeatedly returned to `used:0 / remain:160`
- no `remain=0xFFFFFFFE` underflow occurred
- headroom companion reported `ARMED headroom160`
- native physical backing remained 256 bytes
- reset code showed logical 160 (`init:A0`, `flush:A0`)
- network object reported `state:3`, `mode:2`, `maxPayload:256`

Representative telemetry sequence from the screenshot/history:
- `A:75 S:0 E:144/16`
- native flush -> `A:75 S:0 E:0/160`
- SIM consumes toward `39`, then `75`
- later large selection reaches `A:103`, native flushes, then `S:39 -> 79 -> 103`
- queue returns to `E:0/160`

Runtime conclusion:

The one-shot bulk-selection freeze is caused by the event producer reaching the native 256-byte logical window too late. In the failing stock-window run, `2-byte clear + 63*4-byte add events = 254`, leaving only 2 bytes; the next 4-byte add requests a flush that does not successfully reset before the producer writes, producing `used=258 / remain=-2` and freezing simulation.

Keeping the physical 256-byte backing but lowering the logical remaining/reset window to 160 causes BRZE to invoke its own native flush earlier. This preserves event semantics, lets simulation selection catch up, and allows large rectangle selections above 90 without freeze.

Status: **RUNTIME-PROVEN FIX for the bulk-drag freeze in this test configuration.**

Next step: integrate Sim-Pipeline-120 + logical event headroom 160 into one trainer specimen and regression-test large drag, >90 move, and attack.