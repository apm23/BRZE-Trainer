# v56 F5/F6 runtime result

Runtime report on supplied BRZE 1.60:
- Selecting 56 units no longer freezes after removing the external selected-unit top-up polling loop.
- F6 is functionally ineffective: incoming damage still reduces HP.
- F5 is functionally ineffective: running/skill use still reduces stamina.

Conclusion:
- Freeze root cause is strongly isolated to the external high-frequency selected-unit RPM/WPM path.
- Current central delta hooks are not sufficient/currently not intercepting the actual runtime HP/stamina consumption paths.
- Do not restore the polling loop.
- Next implementation must follow the legacy PageUp architecture: game-side hook plus native selected-unit list handling, remapped to current BRZE 1.60 fields HP +404 and stamina +408. The old trainer hook site 4A4D54 is only a signature/reference and must be remapped by instruction context, as was done successfully for v49 F7.
