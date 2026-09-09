# Current BRZE target binary — 2026-09-09

This note fingerprints the exact BRZE executable supplied by the user for the trainer project. Treat this binary as the current target specimen for all future BRZE address/remap work unless a newer target is explicitly supplied and recorded.

## Supplied target

- Supplied filename: `Battle_Realms_F(5).exe`
- Role: **current BRZE trainer target / authoritative binary specimen**
- File size: `4,521,984` bytes
- SHA-256: `d62de491b8d4d5002b6efc5b9ad492050472bc1e10ab223df1080392733ea5e5`
- MD5: `97b64386f00a07bd3ddd5de5264e4972`
- Format: PE32 / x86 (`Machine = 0x014C`, optional-header magic `0x010B`)
- Sections: `6`
- Preferred image base: `0x400000`
- PE timestamp: `1787669671` / `2026-08-25T14:54:31Z`

## Continuity rule

Do not assume that offsets proven against the legacy WOTW specimen or another BRZE executable apply to this file merely because names or visible version strings match. Before a patch is promoted to the trainer, verify it against this binary by static remap/pattern correspondence and preserve the proof level (static proof vs runtime proof).

The old trainer and matching WOTW executable remain historical references only. This file is the BRZE binary against which the current trainer should be validated.

## Current selection experiment relation

The existing `selection-capacity-manual-120` experiment was built using current BRZE mappings already recorded in the repository. Future forensic work should cross-check those RVAs against this exact SHA-256 target before expanding or finalizing the trainer.
