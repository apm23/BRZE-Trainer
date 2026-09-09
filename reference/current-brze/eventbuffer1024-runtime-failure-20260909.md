# EventBuffer-1024 runtime result

User runtime result on BRZE target `Battle_Realms_F(5).exe`:

- one large rectangle/shift-drag still immediately freezes gameplay
- this occurs despite the 1024-byte backing event buffer probe

Therefore the native 256-byte event-buffer rollover is not sufficient to explain the remaining bulk-drag freeze. Do not carry EventBuffer-1024 into the next probe by default. Return to the clean Simulation-Pipeline-120 base and investigate the burst consumer path for event type 0.