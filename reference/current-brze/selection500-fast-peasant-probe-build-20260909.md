# Selection500 + Fast Peasant probe — build pin

Date: 2026-09-09 (Asia/Tokyo)
Target: authoritative `Battle_Realms_F(5).exe`

Baseline:
- runtime-proven integrated Selection120 + Headroom160
- integrated success reference: `reference/current-brze/selection120-headroom160-integrated-runtime-success-20260909.md`

Branch:
- `selection-500-fast-peasant-probe`

Source/project:
- `Selection500FastPeasant.cs`
- `Selection500FastPeasant.csproj`

Build:
- head `7b1a8832075ace66db8bc2e17953fbb4675d15f9`
- Actions run `34371976218` — SUCCESS
- compile smoke SUCCESS
- x86 single-file publish SUCCESS
- artifact upload SUCCESS
- artifact `BRZE-Selection-500-Fast-Peasant-Probe`
- artifact ID `10112324559`
- ZIP SHA-256 `097cbe040623d636fc88303c6db4a8a12b67b27f4e60747819391219e0a52059`
- EXE SHA-256 `295e9a4c65d9e63092189b6bb96c3c22898ec7ef1d90da457ad570af7203babf`
- EXE size `151,059,670` bytes

## Selection/population 500 design

- local max-pop value is written as exactly `500`
- manual local selection gate at `0x5A7000` is replaced by a narrow code-cave wrapper that rejects `count >= 500`
- active local list is armed to first=500, growth=0 before first allocation
- five proven selection-only list reset call sites are redirected through one wrapper that substitutes first-capacity 500 while selection500 is latched:
  - simulation init `0x5A6C57`
  - simulation reset `0x5A6E3B`
  - sort temp A `0x5A71B2`
  - sort temp B `0x5A71BF`
  - active sort/rebuild `0x5A729F`
- the shared list-reset function `0x4ABFE6` itself is NOT globally patched
- event type-0 stays LIVE
- physical event backing remains native 256
- runtime-proven logical headroom160 is retained
- candidate rectangle list is unchanged (native first=128/growth=128)

Reason for wrappers: 500 cannot be represented by the old one-byte `push 90` / imm8 selection patches safely.

## Fast Peasant design

Static remap reference: `reference/current-brze/peasant-production-timing-remap-20260909.md`

- native PeasantManager scheduler call site `0x57FFA5 -> 0x57FFB2` is redirected through a wrapper
- wrapper always invokes original native scheduling logic first
- only when Fast Peasant is enabled AND the scheduled player equals local player id:
  - capture old per-player next-production timestamp through `[0x867AF0] + player*4`
  - read the new native timestamp after original scheduler returns
  - shorten only the newly scheduled positive delta by factor 20
  - clamp shortened interval to minimum 1000 ms
- AI/non-local players retain native schedule
- no fake unit spawn, no PeasantManager bypass, no shared race-config mutation
- peasant creation enable flag is only observed, not forced

Status: build/compile proven; both Selection500 runtime behavior and Fast Peasant timing are **not runtime-proven yet**.

## Runtime test priority

1. fresh BRZE
2. run this probe only
3. F4 before any selection
4. verify status `ARMED selection500+headroom160`, ACTIVE/SIM `first:500 grow:0`, popLimit 500, event remain 160
5. test crossing the old 120 boundary first (121+), then 150-200 if available; move + attack
6. test one-shot large rectangle drag again
7. enable Fast Peasant and observe whether subsequent native peasant production intervals are materially faster; target design is ~20x, minimum 1s
8. 500 selected is optional/later because gathering 500 units may take time
