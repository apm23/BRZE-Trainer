# Unit Changer V3 — RUNTIME PASS — 2026-09-11

## Verdict
**RUNTIME PROVEN SUCCESS.**

User reported the V3 Completion-Only build works successfully in Battle Realms: Zen Edition without the V2 crash.

Confirmed behavior:
- normal native-valid training input completes normally;
- V3 replaces the completion output with the configured dropdown Unit Type;
- output replacement is not limited to the original clan;
- user specifically tested cross-clan outputs including **Lotus Master Warlock** and **Wolf Werewolf**, and both spawned successfully as configured;
- game remained stable during these successful tests.

## Proven architecture
V3 completion-only architecture is now the locked base for Unit Changer output work:
- leave global mapper `0x4D69F6` stock;
- patch only the training-completion call at preferred `0x4D5E08` / RVA `0x0D5E08`;
- call native mapper with the original training input first;
- only replace EAX for valid local-player completion;
- let stock code continue into native creation/finalization around `0x4D6A88`.

This proves **1 native-valid input -> 1 arbitrary regular configured output** through the native completion/create path.

## Rejected base
V2 global-mapper override remains rejected because it crashed at runtime. Do not reintroduce its architecture.

## Cross-clan proof
Cross-clan output is runtime-proven at least for:
- Lotus Master Warlock
- Wolf Werewolf

Therefore the output Unit Type does not need to match the input/building clan for the proven 1->1 completion path.

## Next milestone
Advance carefully to **1 input -> 2 outputs** while preserving V3's first output path exactly.

Do not clone raw Unit structs. The second output must reuse a native BRZE creation/finalization path and the mandatory per-unit bookkeeping. Prove 1->2 stable before unlocking slots 3..9.

V3 build pin:
- branch `instant-death-v4-hover-telemetry`
- head `ac324b493a0dc771bb7623f6fcc9bfa6abd1cd8e`
- workflow run `34545809935` SUCCESS
- job `103098037229` SUCCESS
- artifact `10178983173`
- standalone SHA-256 `1e220754a33b54a50afc739f99eb29602a9995fdb535664a3c29319b190fe309`
- small SHA-256 `bcfc4b172021722d6ce1c2a5809c1adaa6d3dcf64f074aa350a69670d0329fb6`
