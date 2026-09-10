# Wand / WeMod trainer 49882 static analysis — 2026-09-10

User supplied two snapshots from `%APPDATA%\Wand\App\trainers`:
- `Wand_BRZE_trainers1.zip`: user context = Unlimited Horses + Unlimited Stamina enabled.
- `Wand_BRZE_trainers.zip`: user context = Unlimited Horses + Unlimited Stamina + Unlimited Wolves enabled.

## Disk-package result
The two ZIPs are byte-for-byte identical.
- ZIP SHA-256 for both: `ba83736f32eaa7e1f0659f4c421f17d4d94ea63407d0851e9ef3c569632934a8`.
- Each contains exactly one file: `Trainer_49882_268ab6557e.dll`.
- DLL size: `1,843,512` bytes.
- DLL SHA-256: `268ab6557e0670d0c6e25e3c5db7fe01111547e166164fea6161817a187b41f3`.

Conclusion: enabling individual cheats does not mutate the trainer package on disk. Horse/Stamina/Wolves are already packaged in the same trainer DLL and toggle state is runtime state.

## Static unpacking
The outer trainer DLL is a PE32/x86 DLL and its entry stub matches FSG 1.33-style packing. The outer FSG stage was decompressed statically without executing imported trainer code.
- decompressed `.wemod` stage size: `0x28A000`
- compressed stream consumed: `0x1B1B93`
- decompression terminated cleanly at the expected stage size.

The decompressed stage still contains WinLicense/Themida-style protection markers, including `GWinLicenseInstance`, `WLSoftwareVersion`, `WinLicenseVersion`, `Software\WinLicense`, and `WLProjectName`. Literal `horse`, `stamina`, and `wolves` cheat strings were not exposed in this stage.

Therefore the exact cheat patch logic is not yet statically proven from the protected DLL. Do not claim the WeMod horse/stamina implementation has been extracted.

## Runtime evidence path
Use the read-only `BRZE-WeMod-Horse-Stamina-Diff-Observer` to capture OFF -> ON -> OFF for one cheat at a time. It snapshots BRZE executable/writable module sections plus the selected Stable/unit and local horse-player block. Reversible diffs can expose exact code patches or object fields even when the Wand trainer payload remains protected.
