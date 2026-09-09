# Selection500 ALWAYS + Fixed1s Fast Peasant — build pin

Date: 2026-09-10 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe`

Branch: `selection-500-always-fixed1s-peasant`
Head: `774d17d90460570b51fe8f3f140c7d4a1c33c879`
Actions run: `34375730054` — SUCCESS
Artifact: `BRZE-Selection-500-Always-Fixed1s-Peasant`
Artifact ID: `10113811417`
ZIP SHA-256: `7a29f99e5da33a638634d35b49a31a86397ff2f1e26d37775ffd036872ed4890`
EXE SHA-256: `40f37d73122ae0a827d7e8a197e535a534ae7e9cbef629f933f2e408cce2d1b0`
EXE size: `151,059,694` bytes

## Exact behavior

### Selection
- Selection500 + headroom160 arms automatically while trainer is attached; it is no longer controlled by F4.
- ACTIVE and local SIM lists are armed first=500/growth=0 before use.
- proven selection-specific reset call wrappers and cap500 wrapper remain.
- event type-0 stays live; physical event backing stays native 256; logical window stays 160.

### F4 Max Population
- F4 now controls only the local max-pop value 500.
- trainer attempts to save the original local max-pop value and restore it when F4 is OFF.
- selection500 remains active regardless of F4 state.

### Fast Peasant
- old 20x multiplier logic is removed.
- automatic local schedule wrapper first invokes native `0x57FFB2`, then sets the local next-production timestamp to caller current-game-time + exactly 1000 ms.
- two native PeasantManager population-stop count calls at RVAs `0x17FF1B` and `0x17FF71` are wrapped.
- normally their native population-used result is preserved.
- only when Fast Peasant is ON and the current update player is the local player, the wrapper returns 0 to those two PeasantManager stop checks so automatic peasant production is not halted by max-pop.
- no global population-counter patch; AI/non-local players remain native.

Build/compile is proven. Runtime behavior is not yet proven.

## Required test
1. fully restart BRZE and use only this EXE;
2. open trainer before selecting any unit; Selection500 should arm even with F4 unchecked;
3. with F4 OFF, verify large multi-drag still works and ACTIVE/SIM can exceed old limits;
4. toggle F4 ON/OFF and verify only max-pop changes, not selection capability;
5. enable Fast Peasant and verify peasants appear at roughly one per second rather than a burst;
6. specifically continue beyond the previous stop around total unit 167;
7. if available, continue toward 200+ / 300+ units and verify selection/move/attack remain stable.
