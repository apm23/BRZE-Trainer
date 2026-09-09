# BRZE 1.60 — No-Add-Notify runtime result and simulation selection mapping

Date: 2026-09-09 Asia/Tokyo

Authoritative target binary SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`

## Runtime result — No-Add-Notify 120 probe

Tested artifact:

- branch: `selection-no-add-notify-120-probe`
- built head: `832815431be30ef3ec6ac8a6bc4a4909e1657df9`
- workflow run: `34349623960`
- artifact: `BRZE-Selection-No-Add-Notify-120-Probe`
- EXE SHA-256: `205bee07d1808f729e413a23d34e46e6c124e9906429595df7c9d6595db5ccbf`

The probe preserved the UI/sort 120 changes but NOPed the post-add call at VA `0x5A70D0` to `0x552FFA`.

User runtime observations:

- slow selection successfully reached **108 selected units** without the #91 crash
- rectangle/shift-drag no longer selected a group; even a large drag selected only one unit
- selected units, whether one unit or the full 108-unit selection, could not be commanded with right-click move/attack
- units remained alive/autonomous and could react to enemies, but player orders did not control them

Proof / interpretation:

1. Local/UI selection can exceed 90 when the post-add event is suppressed.
2. `0x552FFA` is not cosmetic. Its selection-add event is required to update authoritative/simulation selection state.
3. NOPing it is diagnostic only and cannot be the final fix.
4. The previous #91 crash occurs after local selection admission and is strongly tied to event-driven simulation selection processing.

## Event producer

Manual AddUnit `0x5A6FD8`:

1. appends unit to active/UI list `0x841708`
2. sets unit selected flag
3. calls `0x552FFA` from `0x5A70D0`

`0x552FFA` emits a 4-byte type-0 selection-add event containing the unit ID into the game command/event buffer when `[0x841194] != 0`.

## Event type-0 consumer

Static follow-up found the type-0 consumer around `0x5543CA`.

It resolves the unit ID stored at payload `+2`, then calls simulation-side selection logic around `0x5A75B2(player, unit)`.

This consumer does not update the UI container `0x841708`. It updates a second family of per-player selection containers.

## Simulation-side per-player selection containers

Global pointer:

- VA `0x841730`
- RVA `0x441730`

Layout:

- 10 containers
- container stride: `0x28`
- local player's container: `[0x841730] + localPlayerId * 0x28`
- local player ID: VA `0x8416D0`, RVA `0x4416D0`

Each container uses the same node-container layout as the UI selection list, including:

- `+0x18` count
- `+0x1C` block count
- `+0x20` first block size
- `+0x24` growth block size

### Initialization hard-cap

During construction of the 10 simulation selection containers, BRZE calls the container constructor with:

- first block = 90
- growth = 0

Hardcoded first-block immediate:

- VA `0x5A6C53`
- RVA `0x1A6C53`
- stock byte `0x5A`

### Reset hard-cap

A reset loop reconstructs the same 10 containers with first=90, growth=0.

Hardcoded reset immediate:

- VA `0x5A6E37`
- RVA `0x1A6E37`
- stock byte `0x5A`

## Main hypothesis supported by runtime + static evidence

The local/UI list can be made >90, but with event delivery LIVE, the type-0 consumer attempts to append unit #91 to the local player's simulation-side selection container that remains first=90/growth=0.

This explains the observed sequence precisely:

- #91 becomes visibly selected locally
- around ~0.3 s later the simulation processes the event
- simulation-side selection list reaches its native 90 boundary
- BRZE crashes

When add-notify is suppressed, this simulation append never occurs, so 108 can remain visible locally—but player commands do not work because authoritative simulation selection was never updated.

## Next diagnostic specimen

Branch: `selection-sim-pipeline-120-probe`

Base: clean sort-pipeline probe with selection-add event LIVE.

Patch set:

- UI active first block: 120 while pristine
- UI growth: 0
- manual admission cap: 120
- sort temp A first-block hardcode: 120
- sort temp B first-block hardcode: 120
- active rebuild first-block hardcode: 120
- simulation init hardcode at RVA `0x1A6C53`: 90 -> 120
- simulation reset hardcode at RVA `0x1A6E37`: 90 -> 120
- local player's already-instantiated simulation container `+0x20`: 90 -> 120 only while pristine
- simulation growth remains 0
- post-add `0x552FFA` event remains **LIVE**

Source logic commit: `cb3a73eab81e289d521ea7a1b47b69dc6da1fd04`
Workflow commit: `297813e9048bffd72196d79e29c8b09bdeecf393`
Built head: `eb8a610b810ba5f17d4eb1b9fe3c914889caedad`
Workflow run: `34352052932` — success
Artifact ID: `10104163056`
Artifact ZIP SHA-256: `0a15006d2970c30c3c662c79fc56dda2518db03ac929a581021c6935c21dd444`
Built EXE SHA-256: `46a6d19bfbc8cf6e942c1872283454245d168f0aeced7a1f8e684a5147aaf7ff`

Required first test:

- fresh BRZE
- enable F4 before selecting anything
- status must show `ARMED sim-pipeline-120`
- `cap:0x78`
- sort bytes `78/78/78`
- sim-code `78/78`
- `add-notify:LIVE`
- ACTIVE first=120/grow=0
- SIM first=120/grow=0
- slowly cross 89 -> 90 -> 91 -> 92 -> 100 -> 108
- test right-click move and attack immediately

Only after slow path is proven stable, use a separate fresh process to test rectangle/shift-drag. If slow >90 and commands work while drag still freezes, treat drag as a separate bug and investigate candidate list `0x879748` / rectangle-only processing.
