# BRZE Hero Effect Replay + Duration — MERGED STATE

Status: **BUILT / CI-PROVEN — RUNTIME MERGED TEST PENDING**
Date: 2026-09-14 JST

## Purpose
Single-window / single-EXE merge of the known-good HeroEffectReplayV2 native replay path and the V10 duration controller.

The original proven cores are linked directly into one assembly rather than rewritten:
- `HeroEffectReplayV2/ReplayCore.cs`
- `HeroEffectReplayDurationV10/ReplayDurationCore.cs`

## User-facing flow
1. Select one clean target.
2. Click `1) CAPTURE ISSYL BASELINE`.
3. Cast ORIGINAL Issyl Haste once on that target and wait for natural expiry.
4. Select the same clean target again.
5. Click `2) REPLAY ISSYL 2X`.
6. The merged UI automatically arms V10 duration control and queues Replay V2 Issyl in the same click.
7. V10 detects replay lifecycle, restores A5 config duration to 15000, and measures natural replay lifetime.
8. Wait for COMPLETE and COPY REPORT.

No second application window is needed.

## Preserved V2 functions
The merged window also keeps direct normal one-shot replay buttons:
- Grayback 0xC0
- Issyl 0xA5
- Both

## Proven basis
V9 runtime proof:
- baseline Issyl natural lifetime: 10738.0 ms at config+0x0E0=15000
- patched Issyl natural lifetime: 21222.5 ms at config+0x0E0=30000
- ratio: 1.976390x
- automatic restore to 15000 succeeded

This confirms config+0x0E0 is the duration parameter.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Workflow: `Hero Effect Replay Duration Merged`
Run: `34796236065` — SUCCESS
Job: `103829774919` — SUCCESS
Head: `c873a613801fb681485ad0f50fdd1bd9a0d08f04`
Artifact: `10330070782`
Artifact digest: `sha256:640421a5825bacfe735d626a24c16a9eaac4395989c4404e0b31aaca82e2ff7d`

Binaries:
- Standalone: 151,076,066 bytes — SHA-256 `bc57c6689b6526230aad79f78e32725752bd4e8e69f80d8ddf7f8ceef84ae9d7`
- Small: 164,100 bytes — SHA-256 `ea97594995ba047b7f8fcf12e0743a2f3e5974be8e3f9b975d0625a26e5ff8cd`

## Runtime goal
Confirm the merged one-click second phase reproduces the expected ~2x Issyl lifetime while current A5 config returns to 15000.

Do not replace the proven V2/V10 source cores until the merged runtime test passes.
