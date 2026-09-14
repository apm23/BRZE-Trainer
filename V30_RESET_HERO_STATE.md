# BRZE Trainer V30 — Runtime-Proven Hero Reset Integration

Status: **RUNTIME-PROVEN PASS / LOCKED GAMEPLAY BASE FOR V31**
Date: 2026-09-14 JST
Branch: `instant-death-v4-hover-telemetry`

## Runtime proof inherited from V29
V29 proved the reset primitive on active stock Issyl A5:
- old `record+0x194 = 507200`;
- BRZE current simulation tick from module RVA `0x440A3C = 519700`;
- exact write/readback `record+0x194 = 519700` passed;
- same A5+target signature remained valid;
- no native replay / no second effect instance;
- user visual proof: the selected/reset unit kept Issyl active longer than the comparison unit that was not reset.

Locked conclusion: writing the current BRZE simulation tick into an existing same-effect record's `+0x194` restarts that same instance's lifetime without stacking.

BRZE pauses its simulation/game clock while minimized. Static tick values while minimized are expected.

## V30 integrated runtime result
User runtime result after V30 integration: **WORK / PASS**.

Promoted behavior:
- selected unit already has requested same effect -> reset existing instance timestamp to current BRZE tick;
- selected unit missing requested effect -> one fresh native apply only;
- repeated APPLY on active same effect restarts the same instance rather than stacking;
- mixed selected groups work with the integrated reset/missing-only semantics.

No more reset research/probes unless an actual regression is observed.

## V30 semantics
For every selected unit × requested ability:
- existing same-effect signature -> reset its existing `+0x194` to current BRZE tick;
- same effect still pending -> do not duplicate it;
- missing effect -> one-shot Replay V2 native apply only for that missing effect;
- unrelated buffs untouched.

`APPLY BOTH` safely partitions mixed selections into missing-both / missing-Grayback-only / missing-Issyl-only batches, so an already-active same ability is never native-replayed on top of itself.

Mixed sub-batches serialize through the existing single shared frame dispatcher; no second hook owner is introduced.

## Duration rules retained
- Issyl nominal baseline 15000; Grayback nominal baseline 60000.
- `record+0x1F4 -> config+0xE0` remains the proven duration path.
- modified duration stays resident for the tracked natural lifetime and restores only after expiry.
- a held duration can be retuned only when no other tracked group of that ability exists outside the selected set and no pending replay batch uses it.

## Locked safety
- no repeated native refresh/replay on an already-active same effect;
- no V26 zero-reset;
- no Unit+0x460 duration writes;
- no CreateRemoteThread;
- max 120 selected targets;
- one shared dispatcher/hook owner at RVA `0x135C43`;
- Copy Unit / Instant Death / Unit Changer locked against V30 finalizer changes.

## V30 CI pin
Workflow: `Final V30 Runtime-Proven Hero Reset`
- run `34835329285` — SUCCESS
- job `103947740941` — SUCCESS
- head `13d0aff27a7a278e9cc8f1937a97285351a4dcff`
- artifact `10343956283`
- artifact digest `sha256:632412dbbc6756b0d06023b5a4f1bf056db81493ea2e78ef12acc926dda6ea90`

Outputs:
- CLEAN standalone SHA256 `603a141f1806f5fadeba01378e51ac97d3ead10b534ee5e1fc65871497ff565c`
- DIAGNOSTICS standalone SHA256 `f2e4f9f2e6e28dc2e0ab55338a90e36655ae1c010b0ca80f6f7d2a4d18094517`
- CLEAN small SHA256 `2f7bbafb490702f82c36f03204f6e048f8c9eb0701e23018a9fddd0fa0009ef4`
- DIAGNOSTICS small SHA256 `2c046f4034ca5686b6fcb63d578e205073cab973b41c0a762d35e4cb408d1237`

CI PASS:
- locked V20 integrated base generation;
- explicit-target dispatcher facade generation;
- dispatcher hash lock after facade;
- V30 architecture verifier;
- CLEAN + DIAGNOSTICS compile smoke;
- all four publishes;
- hashes + artifact upload.

V30 is now the locked runtime-proven gameplay base. V31 may change UI/overlay/packaging only unless a real gameplay regression is demonstrated.
