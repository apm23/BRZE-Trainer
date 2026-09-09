# BRZE 1.60 selection sort/rebuild pipeline — 2026-09-09

Target binary: `Battle_Realms_F(5).exe`, SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`.

## New runtime observation

The `BRZE-Selection-FirstBlock-120-Probe` still crashes when attempting selection #91.

This does **not** yet prove that a downstream consumer rejects count >90, because static analysis found that the game's own selection rebuild/sort routine resets the active list back to first-block 90.

## Selection rebuild/sort routine

Function: absolute VA `0x5A719F` (RVA `0x1A719F`).

The routine performs the following relevant operations:

- `0x5A71B0: push 0x5A` then initializes list `0x841784` with `first=90, growth=0`.
  - immediate byte: `0x5A71B1` / RVA `0x1A71B1`.
- `0x5A71B8: push 0x5A` then initializes list `0x8417AC` with `first=90, growth=0`.
  - immediate byte: `0x5A71B9` / RVA `0x1A71B9`.
- It walks active selection list `0x841708`, partitions/sorts selected units through the two temporary lists above, and later copies them back.
- `0x5A7298: push 0x5A` then reinitializes active list `0x841708` with `first=90, growth=0` before repopulating it.
  - immediate byte: `0x5A7299` / RVA `0x1A7299`.

Therefore, merely changing active `+0x20` from 90 to 120 before first allocation is not persistent: this routine can restore the active selection container to 90/0 during normal selection processing.

## Next narrow probe

Patch only the three proven selection-sort/rebuild first-block immediates above from `0x5A (90)` to `0x78 (120)`, plus:

- the already-proven manual admission guard immediate at RVA `0x1A7006`: `90 -> 120`, and
- active list `+0x20` from `90 -> 120` only before its first allocation.

Keep growth at 0 everywhere. Do **not** patch the global constructor at `0x5A6BCB` because that value is reused to initialize several unrelated/auxiliary selection containers and broad changes previously destabilized the game.

This probe isolates whether the crash is caused by the sort/rebuild pipeline silently restoring 90-node containers.
