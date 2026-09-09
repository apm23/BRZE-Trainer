# Bulk rectangle freeze — corrected event type-0 path

Target: Battle_Realms_F(5).exe, SHA-256 d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5.

Runtime: Simulation-Pipeline-120 allows >90 selection and normal orders when built through small drags, but one large rectangle drag still freezes. EventBuffer-1024 did not change that result.

Correct dispatcher mapping from jump table at 0x5609C2:

- type 0 -> 0x56016B -> resolve unit -> 0x5A736F(player, unit)
- type 1 -> 0x560198 -> 0x5A73A9(player, unit)
- type 2 -> 0x5601C5 -> 0x55437F
- type 3 -> 0x5601E5 -> 0x5543CA

This corrects the earlier mistaken assumption that 0x5543CA was the type-0 consumer.

0x5A736F performs membership lookup in the per-player selection container, appends if absent, sets unit+0x3AC=1, then calls 0x5A7B97(player) at 0x5A739E. Thus a one-shot rectangle selecting 80-100 units creates 80-100 calls to 0x5A7B97 in one consumer burst.

Next surgical diagnostic: from clean Simulation-Pipeline-120, NOP only the call at 0x5A739E (`E8 F4 07 00 00`) while leaving event type 0, simulation append, selected flag, remove path, and UI/sort/sim 120 capacity changes intact. If large drag becomes stable, replace the per-unit refresh with one batch/deferred refresh rather than permanent suppression.