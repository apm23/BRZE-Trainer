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

Reference: `reference/current-brze/selection120-headroom160-integrated-runtime-success-20260909.md`.

---

# Selection500 architecture — RUNTIME PROVEN
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

## Failed first fast-peasant attempt
EXE `295e9a4c...` initially spawned very quickly but stopped completely around total unit 167. This correlated with the two native PeasantManager population-stop checks above.

---

# LOCKED BASELINE — Selection500 ALWAYS + Fixed1s Peasant
Branch: `selection-500-always-fixed1s-peasant`

Build pin:
- head `774d17d90460570b51fe8f3f140c7d4a1c33c879`
- run `34375730054` SUCCESS
- artifact `10113811417`
- EXE SHA-256 `40f37d73122ae0a827d7e8a197e535a534ae7e9cbef629f933f2e408cce2d1b0`

Architecture:
- Selection500 + headroom160 arms automatically; no selection toggle.
- F4 controls only max population; selection remains independent.
- local Fast Peasant calls native scheduler then forces next timestamp to current game time + fixed interval.
- both PeasantManager population-used calls at `0x17FF1B` and `0x17FF71` are wrapped.
- only local player + Fast Peasant sees those two stop checks bypassed; AI/non-local players stay native.

**User runtime result: all requested Selection500/independent-F4/fixed1s behavior worked and peasant production continued beyond the previous ~167 stop.**
Reference: `reference/current-brze/selection500-always-fixed1s-runtime-success-20260910.md`.

Pop9M successor:
- branch `selection-500-always-fixed1s-pop9m`
- run `34377039566` SUCCESS
- artifact `10114328832`
- selection remains 500; F4 population value restored to `9,999,999`.

---

# Merged main trainer v1 — BUILD PROVEN, PARTIAL RUNTIME FAILURE
Branch `main-merge-selection500-hp-stamina-horse-v1`.
Build run `34378691096` SUCCESS, artifact `10114980884`.

Merged features include existing rice/water/yin-yang/pop/training/building/wolf/demolition plus Selection500/headroom160/Fast Peasant and experimental HP/stamina/horse work.

Runtime feedback:
- Selection500/peasant path remained usable.
- fixed 1s Peasant was too aggressive for balance: user reached roughly 800 peasants within minutes and FPS dropped. New requested balance = **fixed 3 seconds**.
- Unlimited Stamina v1 FAILED: selection top-up worked once, but running/skills still depleted stamina. Therefore hooking only `AddStamina` is not the complete stamina-consumption path.
- Unlimited Horses v1 FAILED: after building/selecting a Stable, enabling the cheat left Stable horse stock at 0. Writing the assumed `HorseRespawnTime=0` config is NOT the required Stable-stock mechanism.

Do not treat either v1 stamina or v1 horse implementation as solved.

---

# Stamina static lead
Target exports/disassembly show `UnitSetStamina` at RVA `0x0CC7DF` / VA `0x4CC7DF`. That function eventually calls `0x5CCE79` with a computed fixed-point stamina amount. This is evidence that paths can set stamina without going through the currently hooked `AddStamina` entry at `0x5CCDFB`; it is a candidate central setter to compare against the proven Wand implementation.

Do not patch this candidate blindly before comparison/runtime evidence.

# Horse static leads
Target exports include:
- `GetPlayerNumHorsesCollected` RVA `0x0CC2F0`
- `RegisterHorseBefriended` RVA `0x0CA9EA`
- `RegisterNumHorsesCaptured` RVA `0x0CA81D`

`GetPlayerNumHorsesCollected` reads through global pointer `0x8416A4` and per-player stride `0xA0`, field `+0x2C`. Nearby code also accesses `+0x30` and a six-DWORD region beginning `+0x34`; these are promising horse/stat counters but are not yet mapped to Stable stock. Do not guess-write them.

---

# Wand / WeMod 12.53.0 reverse-engineering lead
User supplied Wand install/package data. Static analysis of `app.asar` established that game-specific trainer artifacts are NOT in `%LOCALAPPDATA%\\Wand\\packages`.

Wand config uses:
- storage root = Electron `userData/App`
- trainer cache = `${storage}/trainers`

Expected Windows location for this install:
**`%APPDATA%\\Wand\\App\\trainers`**

Cache naming observed in Wand source:
- native: `Trainer_<trainerId>_<hash10>.dll`
- Lua/Wand contract: `Artifact_<id>_<artifactHash10>.wlc`
- WLC envelope begins `WLC1`.

The supplied `%LOCALAPPDATA%` package contained generic TrainerLib/CELib/Lua runtime and an overlay log, but not the game-specific Battle Realms horse/stamina trainer logic.

Next exact evidence: obtain `%APPDATA%\\Wand\\App\\trainers` after launching BRZE 1.60 Wand trainer and activating Unlimited Horses / Unlimited Stamina at least once. Analyze statically only; never execute imported trainer binaries.

A read-only `WeModHorseStaminaDiff` observer is also being built on branch `main-merge-v2-peasant3s-wemod-port`; it snapshots BRZE `.text`, selected Stable, selected unit, and local horse-player block before/after a Wand cheat toggle to identify exact code/data diffs.

---

# CURRENT DEVELOPMENT — merged v2
Branch `main-merge-v2-peasant3s-wemod-port`.

Locked deltas:
- Selection500/headroom160 unchanged.
- Fast Peasant interval changed only from 1000 ms to **3000 ms** while preserving native scheduler + local pop-gate bypass.
- Horse and stamina remain under investigation; no new guessed write is allowed before Wand/runtime-diff evidence.

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
- horse `HorseRespawnTime=0` as a claim of Stable-stock unlimited
- stamina `AddStamina`-only hook as a claim of complete hard lock

## Current hinge
Obtain/compare proven Wand BRZE 1.60 Unlimited Horses + Unlimited Stamina implementation, either from `%APPDATA%\\Wand\\App\\trainers` artifacts or the read-only before/after diff observer. Then port only exact horse/stamina semantics into merged v2. Peasant3s is a balance-only delta on the proven Selection500/Fast Peasant base.
