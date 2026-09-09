# Runtime result — FirstBlock 120 probe — 2026-09-09

Target: current BRZE `Battle_Realms_F(5).exe` (SHA-256 `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`).

Probe: `BRZE-Selection-FirstBlock-120-Probe`.

User runtime result:

- selection up through 90: game remains alive;
- clicking/selecting unit #91: **immediate game crash**.

Initial interpretation that this disproved allocator-transition involvement was revised after static analysis found selection rebuild/sort routine `0x5A719F`, which reinitializes active list and two sort temp lists with `first=90, growth=0`. Therefore this probe's one-time active `+0x20=120` can be overwritten by native game logic before #91.

Next test must keep only the proven selection sort/rebuild pipeline at first-block 120 while leaving growth zero and avoiding broad constructor patches.
