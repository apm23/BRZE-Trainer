# BRZE Hero Effect Duration Write Probe V9 — STATE

Status: **RUNTIME-PROVEN — DURATION FIELD CONFIRMED**
Date: 2026-09-14 JST

## Final runtime proof
V9 completed the guarded Issyl 2X experiment successfully on the same pinned target.

Observed report:
- Pinned Unit*: `0x281ABE6C`
- Baseline parent: `0x28C0329C` via `Unit+0x20C->+0x008`
- A5 config: `0x227F4664`
- `config+0x000 == 0xA5`
- baseline nominal `config+0x0E0 = 15000`
- test nominal `config+0x0E0 = 30000`
- baseline natural wall lifetime: `10738.0 ms`
- 2X-patched natural wall lifetime: `21222.5 ms`
- observed ratio: `1.976390x`
- test parent: `0x28C03890` via `Unit+0x1E4->+0x014`
- same config + 30000 verified: `True`
- automatic restore attempted: `True`
- automatic restore OK: `True`
- final `config+0x0E0` returned to `15000`

## Locked conclusion
`parent+0x1F4 -> config+0x0E0` is the hero-effect nominal duration parameter.

The proof is causal, not merely correlational:
- doubling Issyl `15000 -> 30000` produced ~`1.976x` natural wall lifetime;
- no repeated application/refresh was used;
- the effect remained extended after config was restored, proving the duration value is copied/consumed when the effect instance is created rather than continuously read from the config record.

This validates a safe integration strategy for one-shot replay:
1. identify the correct ability config;
2. temporarily patch only `config+0x0E0` immediately before the native apply call;
3. invoke the proven native one-shot helper on the game thread;
4. immediately restore the original nominal config value after the call;
5. never use repeated native refresh.

## Supporting V8 cross-ability proof
- Issyl A5: `config+0x0E0 = 15000`, natural wall `10727.1 ms`.
- Grayback C0: `config+0x0E0 = 60000`, natural wall `42222.1 ms`.
- config ratio exactly `4.0x`; observed wall ratio ~`3.936x`.

## Safety / rejected approaches remain locked
- NEVER repeated native reapplication/refresh (V3 rejected).
- NEVER use `Unit+0x460` as duration.
- NEVER use parent `+0x194` as duration.
- NEVER hard-code transient parent path.
- V7 cleanup-only child fields remain rejected.
- low-address/vtable `10272` remains rejected.

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
- Standalone SHA-256 `9dae8e6c07c66417c268fc5226e8273109de4187b0915bc24bce8a3121ebdd72`
- Small SHA-256 `b11092f3a7d30b205fe3d59279ba76899d5952a4120d678ca74c1649131ef453`

## Next engineering objective
Build a separate V10 integration lab above known-good `HeroEffectReplayV2`, preserving V2 untouched as fallback. V10 should use temporary per-call duration patch + immediate restore, with strong config identity/value guards, and must prove the integration before touching the main V18.3 trainer.
