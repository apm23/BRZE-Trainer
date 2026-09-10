# BRZE Trainer — MASTER STATE

Last updated: 2026-09-10 (Asia/Tokyo)

## Continuity rules
- GitHub source, pinned builds, and runtime results are authoritative.
- One hypothesis -> minimal patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Preserve failed experiments; do not reintroduce disproven broad patches.
- New thread handoff: `CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`.

## Authoritative target
`Battle_Realms_F(5).exe`
- PE32/x86, size `4,521,984`, preferred image base `0x400000`
- SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- ASLR active: always use `moduleBase + RVA`.
- Reference: `reference/current-brze/target-binary-20260909.md`.

Legacy WOTW + old trainer are reference-only.

---

# Selection architecture — LOCKED RUNTIME-PROVEN

## Local/UI list
- RVA `0x441708`
- free `+0x08`, count `+0x18`, blocks `+0x1C`, first `+0x20`, growth `+0x24`
- stock first=90, growth=0

## Local admission gate
Function `0x5A6FD8`:
- stock gate `0x5A7000`
- reject `0x5A70D5`
- continue `0x5A700D`
- accepted unit sets `unit+0x3A8=1` and emits type-0 selection event.

## Sort/rebuild reset call sites
- `0x5A71B2 -> 0x4ABFE6`
- `0x5A71BF -> 0x4ABFE6`
- `0x5A729F -> 0x4ABFE6`

## Simulation selection
- base pointer RVA `0x441730`
- 10 lists, stride `0x28`
- init reset `0x5A6C57 -> 0x4ABFE6`
- runtime reset `0x5A6E3B -> 0x4ABFE6`

## Event path
- AddUnit type-0 producer `0x552FFA`
- type 0 -> `0x56016B` -> simulation add `0x5A736F(player,unit)`
- SIM add sets `unit+0x3AC=1` and refreshes through `0x5A7B97`.

## Rectangle selection
- routine `0x5D4356`
- candidate list RVA `0x479748`
- native first=128/growth=128
- accepted candidate `0x5D455A -> 0x5A6FD8`
- clear `0x5D4573`
- final sort `0x5D457D -> 0x5A719F`

## Event queue
- used RVA `0x441C94`
- remaining RVA `0x441C98`
- pointer RVA `0x441C9C`
- physical backing 256 bytes
- logical init immediate RVA `0x161770`
- logical post-flush reset RVA `0x161D77`
- flush helper `0x561B2A`

## Decisive freeze telemetry
Observer run `34365054121`:
- ACTIVE=64
- SIM=0
- EVENT used=258
- EVENT remain=`0xFFFFFFFE`

Root cause: rectangle clear contributes 2 bytes; `2 + 63*4 = 254`; next 4-byte event overflows the logical native window before flush.

## Runtime-proven headroom160 fix
Branch `selection-event-headroom-160-probe`, run `34367473165`:
- physical backing remains 256
- logical reset window =160
- type-0 stays LIVE
- large one-shot rectangle no freeze
- MAX ACTIVE=103, MAX SIM=103
- healthy flushes `used=0/remain=160`

## Locked baseline
Branch `selection-120-headroom160-integrated`:
- head `b623167036bed91299f4cc8184bb2a2fd821ca34`
- run `34368982513` SUCCESS
- artifact `10111151679`
- EXE SHA-256 `5a13f24b7bc6b6dafbea9bf837e5037358c265f01594ea1c6bd934f23e85790c`

User runtime regression passed large one-shot drag, >90 selection, move, and attack.
Reference: `reference/current-brze/selection120-headroom160-integrated-runtime-success-20260909.md`.

## Selection failures that remain rejected
- Surgical Growth: #91 rejected.
- Manual120 run `34343788268`: #91 crash.
- FirstBlock120 run `34347542420`: #91 crash / sort reset issue.
- SortPipeline120 run `34348488434`: large drag freeze.
- NoAddNotify120 run `34349623960`: UI 108 but broken move/attack semantics.
- EventBuffer1024 run `34358214184`: failed.
- NoPerAddRefresh run `34359923529`: failed.
- Rectangle Sleep(1) run `34363669046`: failed.

---

# Selection500 / Peasant state

Selection500 candidate uses narrow wrappers only:
- local admission rejects count >=500
- ACTIVE first=500/growth=0
- only five proven selection reset call sites substitute first=500
- shared `0x4ABFE6` must NOT be globally patched
- type-0 remains LIVE
- headroom160 retained
- rectangle candidate list remains native 128/128

Peasant timing mapping:
- creation enable array RVA `0x467AF4`
- MinTime race config `+0x6C`
- MaxTime race config `+0x70`
- scheduler `0x57FFB2`
- auto update `0x57FEE9 -> 0x57FFA5`
- pop-stop calls `0x57FF1B -> 0x582D5E` and `0x57FF71 -> 0x582D5E`

Old fast-peasant runtime stopped around total unit 167. Keep this work separate from the locked Selection120 + Headroom160 fallback until independently proven.

---

# Instant Death — LOCKED RUNTIME-PROVEN

## Rejected paths
- V3/V4 call-site-only hook at RVA `0x135F27`: passive hover produced `qcalls:0`; rejected.
- V5 direct pointer RVA `0x3DD858`: caused global/sweeping enemy damage; permanently rejected as hover target source.

## V6 targeting architecture — LOCKED
- per-frame game-thread hook RVA `0x135C43`
- InterfaceMouse object RVA `0x443640`
- cursor Unit* helper RVA `0x135EFB`, called with native common args `(1,0)`
- require target def `+0x74`
- owner `+0x240`
- relation filter RVA `0x1848E8`
- local ID RVA `0x4416D0`
- enemy-only sentinel writes HP `+0x404`, stamina `+0x408`, value `0xFF000000`
- no selection dependency
- no unit-pool scan
- no `0x3DD858`

V6 runtime proved only the enemy directly under the mouse dies, with no click/select and no global sweep.
Reference: `reference/current-brze/instant-death-v5-runtime-rejected-v6-hover-map-20260910.md`.

## V7 control split — RUNTIME-PROVEN
The V6 targeting architecture is preserved, with remote mode gate:
- `MODE_IDLE=0`
- `MODE_BURST=1`
- `MODE_SINGLE=2`

Runtime result from user on 2026-09-10:
- **BURST / ERASER = PASS**
- **SINGLE one-shot = PASS**

SINGLE self-clears after one active render frame. Pressing SINGLE disables BURST in UI.

V7 build pin:
- head `0b51ac108f03a8a0588876b6e12512b9a00e9f0d`
- run `34445622029` SUCCESS
- artifact `10139554308`
- EXE SHA-256 `3114f51f5ad7741bca346a825f0533972f7391993d77948d02c0afc4ac8f9dab`

**LOCK:** preserve V6 targeting + V7 BURST/SINGLE control architecture.

---

# Reveal Map — LOCKED RUNTIME-PROVEN

Current BRZE FOW setter:
- preferred VA `0x50DBD7`
- RVA `0x10DBD7`
- `EnableFogOfWar` pushes `1`
- `DisableFogOfWar` pushes `0`
- native setter returns `ret 4`

`RevealMapCore.cs` invokes native setter only when checkbox state changes. Shutdown restores normal FOW if Reveal Map was active.

Runtime result:
- Reveal Map ON reveals full map.
- Reveal Map OFF immediately restores normal Fog of War.

**LOCK:** do not reopen this implementation without a regression.

---

# Unlimited Wolves / F10 — V8 LOCKED RUNTIME-PROVEN

## V7 rejected
V7 hooked the per-unit `UnitGiveWolfToUnit` cap compare at RVA `0x0C5704`, using unit current wolves `+0x63C` vs UnitDef max `+0x1B8`.

User runtime result: **did not work for the intended Unlimited Wolves behavior**.

**REJECT:** do not reintroduce the V7 per-unit owned-wolf cap hook as the F10 solution.

## Exact legacy F10 semantic
Old trainer performs a direct one-byte write:

`[object + 0x250] = 250`

Current BRZE static mapping proved that `+0x250` is Wolves Den stock:
- building type field `+0x78`
- Wolves Den type `0x44`
- building owner `+0x84`
- selected-building globals RVA `0x4417D4` and `0x4417D8`
- wolf stock `building+0x250`
- native stock cap around 12
- production increments/clamps this field; releasing a wolf decrements it.

## V8 implementation — LOCKED
`WolfCore.cs`:
- no `UnitGiveWolfToUnit` hook
- accepts only selected building owned by local player
- requires type exactly `0x44` (Wolves Den)
- latches the Den while checkbox is ON
- maintains **one byte value 250** at `den+0x250`
- selection can safely change after latching
- multiple local Dens can be latched by selecting each once
- OFF stops writes and clears latch; does not forcibly reduce existing stock.

## V8 build pin
- branch `instant-death-v4-hover-telemetry`
- source/build head `d6ab40fbe93eed598a94e12b1c18129a00a9d7d7`
- workflow `Death V8 Wolves Den Stock`
- run `34449027111` SUCCESS
- job `102780200805` SUCCESS
- artifact `10140856463` (`BRZE-Trainer-V8-WolvesDenStock`)
- artifact ZIP SHA-256 `26a460008142794a8c915bfe1adc6a31b094e39e8f19b9c596b92d07e6ab1163`
- EXE SHA-256 `909d599d5270caeae83c16f5f14ad2b65509d222dff0d9870fa11fa3016311f3`

## Runtime proof — 2026-09-10
User tested V8 and reported **"work total"**.

**LOCK:** V8 local-owner + type `0x44` + latched Wolves Den + one-byte `+0x250 = 250` is now the authoritative Unlimited Wolves/F10 implementation.

Reference: `reference/current-brze/v7-wolves-rejected-v8-wolf-den-stock-20260910.md`.

---

# Do not reintroduce without new evidence
- global/shared list constructor patching
- blanket every-90 selection patching
- candidate capacity patch
- permanent type-0 suppression
- EventBuffer1024 implementation
- permanent `0x5A7B97` suppression
- in-function Sleep throttle
- heavy selected-unit polling scans
- Instant Death direct-pointer polling at RVA `0x3DD858`
- Instant Death V3/V4 call-site-only hook at `0x135F27`
- V7 Unlimited Wolves per-unit `UnitGiveWolfToUnit` cap hook
- blind `+0x250` writes to arbitrary selected objects; V8 must retain owner + Wolves Den type validation and latch.

---

# Current locked milestone
As of 2026-09-10:
- Selection120 + Headroom160: runtime-proven.
- Instant Death targeting V6: runtime-proven.
- Instant Death BURST + SINGLE controls: runtime-proven.
- Reveal Map ON/OFF: runtime-proven.
- Unlimited Wolves V8 Wolves Den stock: runtime-proven.

Current trainer milestone is stable on these features. New work must preserve all locks above unless the user reports a specific regression.
