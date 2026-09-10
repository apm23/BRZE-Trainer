# Wand / WeMod trainer cache path discovery — 2026-09-10

Static analysis of user-supplied Wand 12.53.0 package (`packages.zip`) found the game-specific trainer storage implementation.

Relevant Wand source behavior:
- `cheats/storage/index` receives `cacheDirectory = ${app.info.paths.storage}/trainers`.
- Main Electron process sets `paths.storage = join(app.getPath("userData"), "App")`.
- Therefore on Windows the game-specific trainer artifacts are under the Electron user-data directory, expected as `%APPDATA%\\Wand\\App\\trainers` for this Wand install, not `%LOCALAPPDATA%\\Wand\\packages`.
- Native trainer cache naming: `Trainer_<trainerId>_<hash10>.dll`.
- Lua/Wand contract cache naming: `Artifact_<id>_<artifactHash10>.wlc`.
- WLC artifacts are validated by `WLC1` envelope signature before caching.

The uploaded Local Wand `packages.zip` contained the application installer (`Wand-12.53.0-full.nupkg`) and `Battle_Realms_F.log`, but no game-specific cached trainer artifact. The Battle Realms log was overlay-only and did not expose horse/stamina cheat addresses.

Next evidence required for exact WeMod horse/stamina port: copy the contents of `%APPDATA%\\Wand\\App\\trainers` after launching the BRZE 1.60 trainer and activating Unlimited Horses / Unlimited Stamina at least once.
