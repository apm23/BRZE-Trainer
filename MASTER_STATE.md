# BRZE Trainer — MASTER STATE

Last updated: 2026-09-09 (Asia/Tokyo)

## Continuity rules

- **GitHub source / pinned build / runtime results are authoritative.**
- This file is the cross-chat forensic ledger.
- Distinguish observation, static proof, runtime proof, inference, and hypothesis.
- One hypothesis -> smallest patch -> runtime test -> record result.
- Compile success is not runtime proof.
- Do not reintroduce disproven/broad patches without new evidence.
- Use disposable saves and restart BRZE between invasive probes.

## Authoritative target

- `Battle_Realms_F(5).exe`
- PE32/x86, size `4,521,984`
- image base `0x400000`
- SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- fingerprint: `reference/current-brze/target-binary-20260909.md`

Legacy WOTW + old trainer are reference-only.

---

## Proven selection architecture

### Local/UI selection

- list VA `0x841708`, RVA `0x441708`
- layout: head +0x00, tail +0x04, free +0x08, count +0x18, blocks +0x1C, first +0x20, growth +0x24
- stock first=90, growth=0
- manual cap immediate VA `0x5A7006`, RVA `0x1A7006`

### UI sort/rebuild `0x5A719F`

Hardcoded first=90:

- temp A `0x5A71B1` / RVA `0x1A71B1`
- temp B `0x5A71B9` / RVA `0x1A71B9`
- active rebuild `0x5A7299` / RVA `0x1A7299`

### Simulation selection

- global pointer VA `0x841730`, RVA `0x441730`
- 10 per-player lists, stride `0x28`
- same list layout
- init first=90 immediate `0x5A6C53`, RVA `0x1A6C53`
- reset first=90 immediate `0x5A6E37`, RVA `0x1A6E37`

### Local AddUnit / event producer

`0x5A6FD8`:

1. append UI list around `0x5A70B6`
2. set `unit+0x3A8=1`
3. call selection event producer `0x552FFA` at `0x5A70D0`

`0x552FFA` emits a 4-byte event type **0** containing unit ID.

### Corrected event dispatcher mapping

Jump table VA `0x5609C2`:

- type 0 -> `0x56016B` -> resolve unit -> `0x5A736F(player, unit)`
- type 1 -> `0x560198` -> `0x5A73A9(player, unit)`
- type 2 -> `0x5601C5` -> `0x55437F`
- type 3 -> `0x5601E5` -> `0x5543CA`

Earlier notes that treated `0x5543CA -> 0x5A75B2` as type-0 were wrong.

### Simulation add `0x5A736F`

For each type-0 event:

1. calculate `[0x841730] + player*0x28`
2. membership check via `0x4AC0BA`
3. append if absent via `0x4ACB59`
4. set `unit+0x3AC=1`
5. call `0x5A7B97(player)` at VA `0x5A739E`

### Event buffer

- used bytes VA `0x841C94`, RVA `0x441C94`
- remaining bytes VA `0x841C98`, RVA `0x441C98`
- buffer pointer VA `0x841C9C`, RVA `0x441C9C`
- native size 256 bytes

---

## Rectangle selection path — static proof

Function `0x5D4356`:

- candidate list `0x879748`, RVA `0x479748`
- candidate append around `0x5D44B5`
- candidate list default constructor gives first=128, growth=128
- accepted candidates call AddUnit at `0x5D455A`
- candidate clear `0x5D4573`
- UI sort/rebuild `0x5A719F` at `0x5D457D`

Rectangle-only accepted-unit call site:

- VA `0x5D455A`, RVA `0x1D455A`
- original bytes `E8 79 2A FD FF` -> call `0x5A6FD8`
- immediately preceded by `push esi` at `0x5D4559`

Candidate capacity is not the ~80 freeze cause. Small drags that accumulate >100 also run the same final sort, so final UI sort itself is not yet the leading cause.

---

# Runtime experiment history

## Surgical Growth

- growth 0->90, manual cap stayed 90
- stable at 90, #91 rejected

## Manual 120 — FAILED

- branch `selection-capacity-manual-120`
- run `34343788268`
- EXE `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`
- #91 crashes

## First-Block 120 — FAILED / incomplete

- run `34347542420`
- #91 crashes; later found UI sort can restore first=90

## Sort-Pipeline 120 — FAILED in two modes

- run `34348488434`
- EXE `13f2d21bc28adeb3f9588ae0d84add9d64b2ec05c471ef965c34c975d3dfe660`
- large drag ~80 freezes
- slow #91 visibly selects then crashes

## No-Add-Notify 120 — diagnostic >90 success, semantics broken

- run `34349623960`
- EXE `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`
- reached 108 UI-selected
- group drag degraded to one unit
- right-click move/attack broken
- proves event type-0 path is authoritative; permanent suppression invalid

## Simulation Pipeline 120 — ~70% SUCCESS

- branch `selection-sim-pipeline-120-probe`
- built head `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`
- run `34352052932` SUCCESS
- EXE `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`

Patch: UI/sort/simulation first=120, growth=0, event type-0 LIVE.

Runtime:

- multiple small rectangle drags can build a large working selection
- >90 + normal move/attack materially work
- one large rectangle drag still immediately freezes

Conclusion: 90-boundary problem materially solved; remaining problem is one-shot rectangle/burst-specific.

## EventBuffer-1024 + Sim120 — FAILED TO FIX BULK DRAG

- branch `selection-event-buffer-1024-probe`
- built head `1f3dc34285ee415a3d5cb8132b2a9f19ec27e210`
- run `34358214184` SUCCESS
- EXE `c797463a44a7c283747a36f0af2827c409536a83f386bd9a4b6cd9a00d45803b`
- real event backing buffer widened 256 -> 1024
- runtime: large drag still immediately freezes

Proof: 256-byte event-buffer rollover is not sufficient to explain freeze.

## No-Per-Add-Refresh + Sim120 — FAILED TO FIX BULK DRAG

- branch `selection-no-per-add-refresh-probe`
- built head `13b857c92c7fcf8af166d3dee8a4cd842c540a45`
- run `34359923529` SUCCESS
- artifact `BRZE-Selection-No-Per-Add-Refresh-Probe`, ID `10107440081`
- EXE `853ec688772da02fbd244a848803588dbfed4bda114eb340247f356b0e3a2e98`
- only per-add `call 0x5A7B97` suppressed
- runtime: large drag still immediately freezes

Proof: per-add refresh storm is not sufficient to explain freeze. Do not carry this NOP forward.

## Rectangle AddUnit Throttle 1 ms — FAILED TO FIX BULK DRAG

- branch `selection-rectangle-throttle-1ms-probe`
- first artifact had a trainer-side `IndexOutOfRangeException` because wrapper machine-code was 24 bytes but array was allocated as 23 bytes
- fixed source persisted at branch commit `a1b73549ed4bb82b173f45bbe900c341835cfb61`
- fixed build run `34363669046` SUCCESS
- fixed EXE SHA-256 `ee5dd9a4efb6cbc894430b5e887b828cab630fa33a17996a850316332d1aedba`
- runtime with fixed build: **one large rectangle drag still freezes**

Important interpretation correction:

- `Sleep(1)` executes inside the same rectangle-selection invocation/game thread
- therefore this result only proves that adding wall-clock delay inside the same invocation does not help
- it does **not** prove that distributing AddUnit/event processing across multiple game ticks would fail
- reference: `reference/current-brze/rectangle-throttle1ms-runtime-failure-20260909.md`

---

# CURRENT DIAGNOSTIC — READ-ONLY BULK TELEMETRY OBSERVER

Goal: stop blind patching and capture exact state at the freeze boundary.

Observer branch:

- `selection-bulk-telemetry-observer`
- source `TelemetryObserver.cs`
- project `TelemetryObserver.csproj`
- built head `93730bbdfa7b9fceb1c3a0e65178862dd6212784`

Actions:

- run `34365054121` — **SUCCESS**
- compile smoke success
- x86 publish success
- artifact upload success

Artifact:

- `BRZE-Selection-Bulk-Telemetry-Observer`
- artifact ID `10109550140`
- ZIP SHA-256 `2caa34831e2af9ecddb712db9599c65c2227afdaf73111444c34afbacad0aab6`
- EXE SHA-256 `4f73f3e23b77f954fe424c8d48dd8a2b8e4a39b85b959e849b79028303cd761c`

Observer semantics:

- **read-only**: opens BRZE with VM_READ + QUERY only; performs no writes and no code patches
- samples every ~5 ms on a background thread
- records only when state changes
- tracks current + maxima for:
  - rectangle candidate list `0x879748`: count/blocks/first/growth
  - active UI list `0x841708`: count/blocks/first/growth
  - local-player simulation list from `[0x841730] + player*0x28`
  - event-buffer used/remaining/pointer at `0x841C94/98/9C`
- keeps recent change history in the UI
- also appends a log at `%TEMP%\BRZE-Selection-Telemetry.log`

Required runtime procedure:

1. fully restart BRZE
2. run the proven `BRZE-Selection-Sim-Pipeline-120-Probe.exe` (EXE hash `46a6...`) and enable F4 before any selection
3. run `BRZE-Selection-Bulk-Telemetry-Observer.exe` alongside it; observer requires no F-key and writes nothing
4. immediately reproduce the known one-shot large rectangle drag freeze
5. after freeze, do **not** close BRZE yet
6. capture the observer window, especially current CAND/ACTIVE/SIM/EVENT values and recent history; alternatively provide `%TEMP%\BRZE-Selection-Telemetry.log`

Decision tree from telemetry:

- CAND remains non-zero at freeze -> rectangle producer/iteration itself is stuck before candidate clear
- CAND becomes 0 while ACTIVE/SIM diverge -> problem lies in event/authoritative simulation synchronization after rectangle AddUnit production
- CAND=0 and ACTIVE/SIM both reach the intended count -> problem is downstream/end-of-batch after authoritative selection state is already synchronized
- EVENT used/remaining stuck at a boundary helps identify whether dispatcher/flush state is involved even though larger backing buffer did not fix the issue

---

## Do not reintroduce without evidence

- global/shared list constructor patching
- blanket every-90 patching
- candidate-list capacity patch
- permanent event type-0 suppression
- EventBuffer-1024 by default
- permanent `0x5A7B97` suppression
- in-function `Sleep(1)` as if it were a cross-tick throttle
- heavy per-frame selected-unit scans
- multiple unrelated changes per probe

## Handoff

`CONTINUE BRZE TRAINER — MASTER_STATE AUTHORITATIVE`

Read this file first.

**Current unresolved hinge:** telemetry snapshot/log from a known large-drag freeze while using clean Sim120 + the read-only bulk telemetry observer.