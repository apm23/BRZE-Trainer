# Selection First-Block 120 Probe — 2026-09-09

Purpose: isolate the BRZE selection crash at the native 90/91 boundary after `Selection Manual 120` runtime-failed.

## Runtime evidence that motivated this probe

The pinned `BRZE-Selection-Manual-120` build (EXE SHA-256 `848ecb59e588b92bd92d06940d57fc1e34a94ed71d0aa7d37f9bbf6046e6ce2d`) crashes as selection reaches the native 90-unit boundary after opening the manual guard above 90. The prior Surgical Growth Probe was stable at 90 while the guard remained 90.

## Static allocator proof on authoritative current BRZE binary

Target SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`.

- manual selection guard immediate: VA `0x5A7006`, stock `0x5A` (90)
- active selection object: VA `0x841708`
- append routine: `0x4ACB59`
- free-node refill/allocation routine: `0x4AC4C1`
- container `+0x1C`: allocated block count
- container `+0x20`: first block size
- container `+0x24`: growth block size
- stock active-list construction: first=90, growth=0

`0x4AC4C1` uses `+0x20` when block count is zero and `+0x24` when block count is nonzero.

## Probe design

Branch: `selection-firstblock-120-probe`

Probe logic source commit: `89c8a2044524bd20f8fbe5cbea2eabe02af11180`

Final build trigger commit: `6d8f4f1c939ae368770b78c75959fedff62481d5`

Behavior:

- growth remains 0
- first block changes 90 -> 120 only while allocator is pristine
- manual guard changes 90 -> 120 only after first-block-120 state is armed
- if allocator was already used, cap remains closed and UI instructs user to restart
- no constructor code patch
- no temporary/auxiliary container patch
- no HP/stamina/F7/selection-event hooks

## Build

Workflow: `.github/workflows/selection-firstblock-120-probe.yml`

Actions run: `34347542420` — SUCCESS

Artifact: `BRZE-Selection-FirstBlock-120-Probe`

Artifact ID: `10102381793`

Artifact ZIP digest: `sha256:bc454e4dacc9f04162682166a2f9e4fcb368a514643c4d6a70523f711f6172f8`

Extracted EXE SHA-256: `aa412ad2ad132d03220dc9ae9e04507a31712b09a0719eb72a4ace20fde1f441`

## Runtime decision

- If #91 works with `first=120`, `growth=0`, `blocks=1`, the previous failure strongly implicates the second-block growth/transition path.
- If it still crashes at 90/91 with `first=120`, `growth=0`, `blocks=1`, move to downstream consumers that assume selected count <=90.

Runtime result: PENDING.
