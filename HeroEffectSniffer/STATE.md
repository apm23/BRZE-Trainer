# BRZE Hero Effect Runtime Sniffer — STATE

Status: **RUNTIME-PROVEN CAPTURE — IDs RESOLVED**
Date: 2026-09-14 JST

## Why this exists
Hero Effect Lab V1 executed without crashes but produced no visible Grayback or Issyl effect. V1 is runtime-rejected. This sniffer replaced static guessing with live observation of BRZE's real effect path.

## Observation hooks
- Target-side ability helper: preferred VA `0x5F0C32`, RVA `0x1F0C32`.
  - exact displaced prologue: `55 8B EC 56 8B F1`.
  - records call count, stack ability argument, target Unit* from original ECX, target UnitType and owner.
- Magic creation helper: preferred VA `0x53FFD1`, RVA `0x13FFD1`.
  - exact displaced prologue: `55 8B EC 83 EC 44`.
  - records call count and live arguments for runtime correlation.
- Observation-only: no HP/stamina/stat writes and no CreateRemoteThread.
- Hook teardown is ownership-checked before restoring original bytes.

## Runtime result — 2026-09-14
User executed the original skills in the requested order: Grayback first, then Issyl after clearing the log.

Captured Grayback trace:
- TARGET calls: 8
- MAGIC calls: 8
- runtime ability ID: `0xC0`
- observed target UnitType in trace: `0x59`, owner 0

Captured Issyl trace:
- TARGET calls: 8
- MAGIC calls: 8
- runtime ability ID: `0xA5`
- observed target UnitType in trace: `0x59`, owner 0

Therefore the current runtime mapping is locked for this executable/build:
- **Grayback buff = ability `0xC0`**
- **Issyl Haste = ability `0xA5`**

Both abilities traverse the observed MAGIC CREATE + TARGET HELPER chain. The old V1 BattleGear-derived IDs must not be reused.

## Next phase
`HeroEffectReplayV2` replays these exact runtime IDs directly through native target helper RVA `0x1F0C32` onto all currently selected units (up to 120). Runtime proof must test Grayback, Issyl, and BOTH separately.

## Build pin
Repository: `apm23/BRZE-Trainer`
Branch: `instant-death-v4-hover-telemetry`
Trigger head: `9a98c4b5b6feff827e4466fdb9cd4d064f7d83e0`
Workflow: `Hero Effect Runtime Sniffer`
Run: `34770094659` — SUCCESS
Job: `103757989698` — SUCCESS
Artifact: `10321517635`
Artifact digest: `sha256:6311eeb7968dcc59177a89ab865ff22ccf6b5cd6ec6f29e9358a63316c386ef5`

Binaries:
- Standalone: 66,002,193 bytes — SHA-256 `0baca6aa8af50c7cb578c119ca6744ba8f70ddc099c8f6c69603d57895adb4e2`
- Small: 147,626 bytes — SHA-256 `cc1519e1f1fd41dd956a76851b4fb4c6b9e01664649d956ebe4d7a1fe06431ce`
