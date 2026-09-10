# BRZE Trainer — MASTER STATE

Last updated: 2026-09-10 (Asia/Tokyo)

## Continuity rules
- GitHub source, pinned builds, and runtime results are authoritative.
- One hypothesis -> minimal patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Preserve failed experiments; do not reintroduce disproven broad patches.
- Use fresh BRZE/restart between invasive selection probes unless a procedure explicitly says otherwise.
- New thread handoff: `CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`.

## Authoritative target
`Battle_Realms_F(5).exe`
- PE32/x86, size `4,521,984`, preferred image base `0x400000`
- SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- ASLR active: use moduleBase+RVA.
- reference: `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only.

---

# Selection architecture — proven

## Local/UI list
- RVA `0x441708`
- free `+0x08`, count `+0x18`, blocks `+0x1C`, first `+0x20`, growth `+0x24`
- stock first=90, growth=0

## Local admission gate
Function `0x5A6FD8`:
- stock gate at `0x5A7000`, reject `0x5A70D5`, continue `0x5A700D`
- then append UI list, set `unit+0x3A8=1`, emit type-0 selection event

## Sort/rebuild selection-only reset calls
- sort A `0x5A71B2 -> 0x4ABFE6`
- sort B `0x5A71BF -> 0x4ABFE6`
- active rebuild `0x5A729F -> 0x4ABFE6`

## Simulation selection
- base pointer RVA `0x441730`
- 10 lists, stride `0x28`
- init reset `0x5A6C57 -> 0x4ABFE6`
- runtime reset `0x5A6E3B -> 0x4ABFE6`

## Event path
- AddUnit type-0 event producer `0x552FFA`
- type 0 -> `0x56016B` -> simulation add `0x5A736F(player,unit)`
- SIM add checks membership, appends, sets `unit+0x3AC=1`, refreshes via `0x5A7B97`

## Rectangle selection
- `0x5D4356`
- candidate list RVA `0x479748`, native first=128/growth=128
- accepted candidate `0x5D455A -> 0x5A6FD8`
- clear `0x5D4573`, final sort `0x5D457D -> 0x5A719F`

## Event queue
- used RVA `0x441C94`
- remaining RVA `0x441C98`
- pointer RVA `0x441C9C`
- native physical backing 256 bytes
- logical init immediate RVA `0x161770`
- logical post-flush reset RVA `0x161D77`
- flush helper `0x561B2A`

## Selection failures that must stay rejected
- Surgical Growth: stable to 90; #91 rejected.
- Manual120 run `34343788268`: #91 crash.
- FirstBlock120 run `34347542420`: #91 crash; sort reset issue.
- SortPipeline120 run `34348488434`: large drag freeze.
- NoAddNotify120 run `34349623960`: UI reached 108 but move/attack semantics broke; event path required.
- EventBuffer1024 run `34358214184`: failed; do not reuse.
- NoPerAddRefresh run `34359923529`: failed.
- Rectangle Sleep(1) run `34363669046`: failed.

## Decisive bulk-drag telemetry
Observer run `34365054121`:
- ACTIVE=64
- SIM=0
- EVENT used=258
- EVENT remain=`0xFFFFFFFE` (-2)

Boundary: rectangle clear contributes 2 bytes; `2 + 63*4 = 254`; next 4-byte add reaches native flush too late -> `258/-2`.
Reference: `reference/current-brze/bulk-telemetry-freeze-64-20260909.md`.

## Runtime-proven headroom160 fix
Branch `selection-event-headroom-160-probe`, run `34367473165`.
- physical backing stays 256
- logical reset window = 160
- type-0 event remains LIVE
- large one-shot rectangle no freeze
- MAX ACTIVE=103, MAX SIM=103
- healthy flushes `used=0/remain=160`

Reference: `reference/current-brze/event-headroom160-runtime-success-20260909.md`.

## LOCKED selection baseline — Integrated Selection120 + Headroom160
Branch `selection-120-headroom160-integrated`.
- head `b623167036bed91299f4cc8184bb2a2fd821ca34`
- run `34368982513` SUCCESS
- artifact `10111151679`
- EXE SHA-256 `5a13f24b7bc6b6dafbea9bf837e5037358c265f01594ea1c6bd934f23e85790c`

User runtime regression passed large one-shot drag, >90 selection, move, and attack.
Reference: `reference/current-brze/selection120-headroom160-integrated-runtime-success-20260909.md`.

---

# Selection500 / Peasant state

Selection500 candidate uses narrow wrappers only:
- local admission rejects count >=500
- ACTIVE first=500/growth=0
- only five proven selection reset call sites substitute first=500
- shared `0x4ABFE6` is NOT globally patched
- type-0 stays LIVE
- headroom160 retained
- rectangle candidate list remains native 128/128

Peasant timing static mapping:
- creation enable array RVA `0x467AF4`
- MinTime race config `+0x6C`
- MaxTime race config `+0x70`
- scheduler `0x57FFB2`
- auto update `0x57FEE9 -> 0x57FFA5`
- pop-stop calls `0x57FF1B -> 0x582D5E` and `0x57FF71 -> 0x582D5E`

Old fast-peasant runtime stopped around total unit 167. Current Selection500/peasant implementations remain separate from the locked selection fallback; do not regress the proven Selection120 + Headroom160 architecture while iterating them.

---

# Instant Death — LOCKED RUNTIME-PROVEN BASELINE

## Legacy behavior
Old PageDown trainer uses game-internal target state and writes sentinel `0xFF000000` to target HP/stamina. It does not use Win32 cursor APIs.
Reference: `reference/current-brze/instant-death-oldtrainer-exact-pagedown-static-20260910.md`.

## Rejected V3/V4
V3/V4 intercepted the existing call at RVA `0x135F27` inside InterfaceMouse helper RVA `0x135EFB`. Runtime V4 telemetry showed `qcalls:0` on passive hover. This action/context-specific call-site interception is rejected.

## Rejected V5
V5 directly polled Unit* global RVA `0x3DD858`.
User runtime result was decisive:
- enabling the cheat alone affected enemies globally;
- no cursor aim was needed;
- enemies became near-dead and died when movement stopped.

Conclusion: RVA `0x3DD858` is simulation/spatial scratch state that sweeps through units. Sentinel death value is effective, but this RVA is permanently rejected as hover target source.

## V6 — SUCCESS, LOCK THIS ARCHITECTURE
Branch: `instant-death-v4-hover-telemetry`.
Build pin:
- source/build head `755a1f8f7888d496a7d43626c201df9dc56c5b80`
- workflow `Instant Death v6 — Hover Only + Reveal Map`
- run `34442885171` SUCCESS
- job `102761415852` SUCCESS
- artifact `10138596135` (`BRZE-Trainer-InstantDeathV6-HoverOnly-RevealMap`)
- artifact ZIP SHA-256 `79b76d18e90d4bda2c5f1bf2b4fa46ea87e4dac2f5a292ea5278f34f9572a477`
- published EXE SHA-256 `b7b2f0712213601761a2167be8587e01515987c773ed08149851ed551af07349`

Architecture:
- per-frame game-thread hook RVA `0x135C43`, reached from preferred VA `0x544086`
- InterfaceMouse object RVA `0x443640`
- invoke cursor Unit* helper RVA `0x135EFB` with native common args `(1,0)` once per frame
- require target definition `+0x74`
- owner `+0x240`
- same/allied filter RVA `0x1848E8`, local ID RVA `0x4416D0`
- enemy-only sentinel writes HP `+0x404`, stamina `+0x408`, value `0xFF000000`
- no selection dependency
- no unit-pool scan
- no `0x3DD858`

### Runtime proof — 2026-09-10
User reports **SUCCESS BESAR**:
- only the enemy unit directly under the mouse cursor dies;
- no select/click is required;
- other enemy units no longer die globally;
- V5 global sweep behavior is gone.

Screenshot while enabled showed trainer status `DEATH V6: ON ... NO SELECT / NO SWEEP`; after the cursor was no longer over a target, `hover:0x00000000`, confirming the target does not remain sticky. The current `kills` telemetry is a per-frame execution/write count, not a count of unique dead units.

**LOCK:** V6 active per-frame InterfaceMouse query is now the runtime-proven Instant Death baseline. Preserve it exactly unless a later regression is demonstrated.

Reference: `reference/current-brze/instant-death-v5-runtime-rejected-v6-hover-map-20260910.md`.

---

# Reveal Map — native implementation present

Current BRZE FOW setter:
- preferred VA `0x50DBD7`, RVA `0x10DBD7`
- `EnableFogOfWar` pushes `1`
- `DisableFogOfWar` pushes `0`
- native setter returns `ret 4`

`RevealMapCore.cs` calls the native setter only when checkbox state changes: `0` reveals/disables fog, `1` restores normal fog. Trainer shutdown restores normal FOW when Reveal Map was active.

V6 screenshot while enabled reports `MAP: REVEALED — native Fog-of-War disabled`, proving the trainer-side native call completed. Visual in-game reveal and OFF/restoration still require explicit user confirmation before this feature is marked fully runtime-locked.

Reference: `reference/current-brze/instant-death-v5-runtime-rejected-v6-hover-map-20260910.md`.

---

## Do not reintroduce without new evidence
- global/shared list constructor patching
- blanket every-90 patching
- candidate capacity patch
- permanent type-0 suppression
- old EventBuffer1024 implementation
- permanent `0x5A7B97` suppression
- in-function Sleep throttle
- heavy selected-unit polling scans
- Instant Death RVA `0x3DD858` direct-pointer polling
- Instant Death V3/V4 call-site-only hook at `0x135F27`

## Current hinge
**Instant Death V6 is solved and runtime-locked.** Next unresolved item in this V6 package is only visual confirmation that Reveal Map ON visibly removes FOW and OFF restores it. For future Instant Death work, start from V6 and do not reopen V3/V4/V5 target-source experiments unless a regression appears.
