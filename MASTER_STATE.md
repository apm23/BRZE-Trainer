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

---

# Selection runtime history

Failed/partial:
- Surgical Growth: stable to 90; #91 rejected.
- Manual120 run `34343788268`: #91 crash.
- FirstBlock120 run `34347542420`: #91 crash; later found sort reset issue.
- SortPipeline120 run `34348488434`: large drag freeze; slow #91 briefly selected then crash.
- NoAddNotify120 run `34349623960`: UI reached 108 but move/attack and multi-drag semantics broke; event path required.
- EventBuffer1024 run `34358214184`: failed; old 1024 implementation must not be reused.
- NoPerAddRefresh run `34359923529`: failed.
- Rectangle Sleep(1) run `34363669046`: failed.

## Decisive bulk-drag failure telemetry
Observer run `34365054121`:
- ACTIVE=64
- SIM=0
- EVENT used=258
- EVENT remain=`0xFFFFFFFE` (-2)

Boundary:
- rectangle clear contributes 2 bytes
- `2 + 63*4 = 254`
- next 4-byte add reaches native flush too late -> `258/-2`

References:
- `reference/current-brze/bulk-telemetry-freeze-64-20260909.md`
- `reference/current-brze/bulk-telemetry-freeze-64-log-20260909.txt`

## Runtime-proven bulk fix — headroom160
Branch `selection-event-headroom-160-probe`, run `34367473165`.
- physical backing stays 256
- logical reset window = 160
- type-0 event remains LIVE

Runtime proof:
- one-shot large rectangle no freeze
- MAX ACTIVE=103
- MAX SIM=103
- repeated healthy flushes `used=0/remain=160`
- SIM catches ACTIVE

Reference: `reference/current-brze/event-headroom160-runtime-success-20260909.md`.

---

# LOCKED BASELINE — Integrated Selection120 + Headroom160
Branch `selection-120-headroom160-integrated`.
- head `b623167036bed91299f4cc8184bb2a2fd821ca34`
- run `34368982513` SUCCESS
- artifact `10111151679`
- EXE SHA-256 `5a13f24b7bc6b6dafbea9bf837e5037358c265f01594ea1c6bd934f23e85790c`

User runtime regression:
- single integrated EXE passed
- large one-shot drag passed
- >90 stable
- move + attack authoritative

**This is the solved/runtime-proven selection baseline.**
Reference: `reference/current-brze/selection120-headroom160-integrated-runtime-success-20260909.md`.

---

# Selection500 architecture candidate
Because 500 cannot fit the old imm8 90->120 patch, Selection500 uses narrow wrappers:
- local admission wrapper rejects count >=500
- ACTIVE first=500/growth=0 before allocation
- only five proven selection reset call sites route through a wrapper that substitutes first=500
- shared `0x4ABFE6` is NOT globally patched
- event type-0 stays LIVE
- headroom160 retained
- rectangle candidate list remains native 128/128

Initial Selection500 + Fast Peasant build:
- branch `selection-500-fast-peasant-probe`
- run `34371976218` SUCCESS
- artifact `10112324559`
- EXE `295e9a4c65d9e63092189b6bb96c3c22898ec7ef1d90da457ad570af7203babf`

---

# Peasant production — proven static paths

Timing:
- creation enable array RVA `0x467AF4` is enable/disable, not timing
- `MinTimeToCreatePeasant` -> race config `+0x6C`
- `MaxTimeToCreatePeasant` -> race config `+0x70`
- scheduler `0x57FFB2` converts those values to milliseconds
- automatic update `0x57FEE9` calls scheduler at `0x57FFA5`

Population-stop gates in automatic update:
- `0x57FF1B -> 0x582D5E`, compare at `0x57FF20` against per-player max-pop array `0x867B90`
- `0x57FF71 -> 0x582D5E`, compare at `0x57FF76` against same max-pop
- when population-used is at/above max, native PeasantManager stops/avoids further automatic production scheduling/spawn

References:
- `reference/current-brze/peasant-production-timing-remap-20260909.md`
- `reference/current-brze/selection500-fast-peasant-popgate-static-20260910.md`

---

# Runtime result — old Fast Peasant stopped around total unit 167
Tested EXE `295e9a4c...`:
- peasants initially came out extremely fast / burst-like
- at total unit count about **167**, automatic peasant production stopped completely

This is now correlated with the two native PeasantManager population-stop checks above.
Reference: `reference/current-brze/selection500-fast-peasant-runtime-167-stop-20260910.md`.

User requests after this test:
1. Selection500 must stay active independently from Max Population; turning Max Pop off must not revert selection or reintroduce crash.
2. Fast Peasant should not use a multiplier; use a fixed 1–2 second schedule.
3. Fix production stopping at the population threshold.

---

# CURRENT RUNTIME CANDIDATE — Selection500 ALWAYS + Fixed1s Peasant
Branch: `selection-500-always-fixed1s-peasant`

Files:
- `Selection500AlwaysFixedPeasant.cs`
- `Selection500AlwaysFixedPeasant.csproj`
- `.github/workflows/selection-500-always-fixed1s-peasant.yml`

Build pin:
- head `774d17d90460570b51fe8f3f140c7d4a1c33c879`
- run `34375730054` SUCCESS
- artifact `BRZE-Selection-500-Always-Fixed1s-Peasant`
- artifact ID `10113811417`
- ZIP SHA-256 `7a29f99e5da33a638634d35b49a31a86397ff2f1e26d37775ffd036872ed4890`
- EXE SHA-256 `40f37d73122ae0a827d7e8a197e535a534ae7e9cbef629f933f2e408cce2d1b0`
- EXE size `151,059,694`

Exact changes:
- Selection500 + headroom160 arms automatically; no selection toggle.
- F4 controls ONLY local Max Population 500.
- original local max-pop is saved when available and restored when F4 is OFF.
- old Fast Peasant 20x/min1s logic removed.
- local Fast Peasant calls native scheduler first, then forces next timestamp to **current game time + 1000 ms**.
- two PeasantManager population-used calls at RVAs `0x17FF1B` and `0x17FF71` are wrapped.
- native result is preserved normally; only local player + Fast Peasant returns 0 to those two PeasantManager stop checks.
- no global population-counter patch; AI/non-local players remain native.

Reference: `reference/current-brze/selection500-always-fixed1s-peasant-build-20260910.md`.

Status: compile/build proven; runtime not yet proven.

## Required next runtime test
1. fresh restart BRZE; use only this EXE
2. open trainer before selecting anything
3. leave F4 OFF and verify Selection500 status is already armed; test a large drag and move/attack
4. toggle F4 ON/OFF and verify selection remains 500 both ways
5. enable Fast Peasant; verify roughly one peasant per second rather than burst
6. specifically pass total unit 167 without production stopping
7. continue toward 200+/300+ if practical; selection/move/attack should remain stable

---

# Instant Death — current state

## Exact legacy behavior
Old PageDown trainer reads game-internal target state, not Win32 cursor APIs. Primary old path dereferences `[global+0x08]` and writes sentinel `0xFF000000` to the target's HP/stamina fields. Reference: `reference/current-brze/instant-death-oldtrainer-exact-pagedown-static-20260910.md`.

## V3/V4 — rejected call-site strategy
Current BRZE InterfaceMouse helper RVA `0x135EFB` reads live cursor coordinates and returns a native Unit* through query RVA `0x1D4888`. V3/V4 intercepted only its existing call at RVA `0x135F27`. Runtime V4 telemetry showed `qcalls:0`, so passive hover does not traverse that specific action/context call site. Do not reuse that call-site-only architecture.

## V5 — runtime rejected as hover source
V5 read Unit* directly from RVA `0x3DD858`. User runtime result:
- enabling the cheat alone affected enemies globally;
- no cursor aim was needed;
- enemies became near-dead and died when movement stopped.

Therefore RVA `0x3DD858` is confirmed simulation/spatial scratch state that sweeps through units. The sentinel write is effective, but this RVA is **permanently rejected as hover-only target source**.

## V6 — current hover-only candidate
Branch: `instant-death-v4-hover-telemetry`.
V6 actively invokes BRZE's own InterfaceMouse Unit* query once per render frame on the game thread instead of waiting for an action-specific call site:
- per-frame hook RVA `0x135C43`, reached each frame from preferred VA `0x544086`
- InterfaceMouse object RVA `0x443640`; cursor coordinates are object `+4/+8`
- cursor Unit* helper RVA `0x135EFB`, called with native common args `(1,0)`
- target definition guard `+0x74`
- target owner `+0x240`
- native same/allied filter RVA `0x1848E8` using local ID RVA `0x4416D0`
- enemy-only sentinel writes: HP `+0x404`, stamina `+0x408`, value `0xFF000000`
- no selection dependency
- no unit-pool scan
- rejected RVA `0x3DD858` removed entirely

Reference: `reference/current-brze/instant-death-v5-runtime-rejected-v6-hover-map-20260910.md`.
Status at ledger update: source committed; V6 workflow run `34442659795` started, runtime proof pending.

---

# Reveal Map — current state

Current BRZE native fog-of-war setter is preferred VA `0x50DBD7`, RVA `0x10DBD7`:
- native `EnableFogOfWar` wrapper pushes `1`
- native `DisableFogOfWar` wrapper pushes `0`
- routine returns `ret 4`

`RevealMapCore.cs` invokes that native setter only on checkbox state changes: `0` for reveal/disable fog, `1` for normal/restore. Trainer shutdown restores normal FOW if Reveal Map was active.

Reference: `reference/current-brze/instant-death-v5-runtime-rejected-v6-hover-map-20260910.md`.
Status: source committed; build/runtime proof pending.

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
Runtime-test the pinned V6 build after CI succeeds: Instant Death must kill **only the enemy Unit* currently under the cursor with no selection**, and Reveal Map must reveal/restore FOW cleanly. Selection120 + Headroom160 remains the known-good selection fallback baseline.
