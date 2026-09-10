Runtime feedback for merged main trainer v1:

- Horse feature failed: after building a Stable and activating the horse cheat, the Stable horse stock remained 0. The old `HorseRespawnTime=0` config write is not the required Stable-stock behavior and must not be treated as solved.
- Stamina hard-lock failed: selecting a unit refilled stamina once, but running or using stamina-consuming skills reduced stamina again. The current AddStamina negative-delta hook does not intercept all real stamina-consumption paths.
- User supplied a proven Wand/WeMod trainer for BRZE 1.60 and requested exact horse + stamina logic be recovered from that trainer artifact rather than guessed.

Next step: obtain `%APPDATA%\\Wand\\App\\trainers` artifact(s), identify Unlimited Horses and Unlimited Stamina implementation, then port only those semantics into the custom trainer.
