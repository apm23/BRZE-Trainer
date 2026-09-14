# BRZE Hero Effect Ability Config Probe V8 — STATE

Status: **RUNTIME-PROVEN ABILITY CONFIG — `config+0x0E0` STRONGLY IDENTIFIED AS NOMINAL DURATION**
Date: 2026-09-14 JST

## Runtime results
Two clean ORIGINAL hero-effect passes were completed with V8.

### Issyl Haste (`0xA5`)
- observed natural wall lifetime: `10727.1 ms`
- signature parent: `0x28C01EC4`
- path: `Unit+0x20C->+0x008`
- discovery delay: `23.3 ms`
- `parent+0x1F4 = 0x227F4664`
- `config+0x000 = 0xA5` exactly
- `config+0x0E0 = 15000`
- config samples: 343
- read failures: 0
- config record stayed static for the entire effect lifecycle
- wall/config ratio: `0.715138`

### Grayback (`0xC0`)
- observed natural wall lifetime: `42222.1 ms`
- signature parent: `0x28C020C0`
- path: `Unit+0x1E4->+0x014`
- discovery delay: `27.7 ms`
- `parent+0x1F4 = 0x227F9470`
- `config+0x000 = 0xC0` exactly
- `config+0x0E0 = 60000`
- config samples: 1348
- read failures: 0
- config record stayed static for the entire effect lifecycle
- wall/config ratio: `0.703701`

## Interpretation
This is strong cross-ability evidence that `parent+0x1F4` points to an ability-specific configuration record and `config+0x0E0` is the nominal effect duration parameter.

The config duration ratio is exactly `60000 / 15000 = 4.0x` while the observed natural wall-lifetime ratio is `42222.1 / 10727.1 ~= 3.936x`. The small difference is consistent with lifecycle detection/tick timing and/or internal game time scaling. It is far too structured to treat `+0x0E0` as an accidental numeric coincidence.

Important: the config record is static throughout each active effect. Therefore it is a **configuration parameter**, not the per-instance countdown itself.

## Next proof step
Do **one isolated controlled write experiment on Issyl only**:
- discover the live A5 config record safely through an original baseline Issyl cast;
- verify `config+0x000 == 0xA5` and `config+0x0E0 == 15000`;
- only after baseline expiry, temporarily write `30000` to exactly `config+0x0E0`;
- cast original Issyl once more on a clean target and measure natural lifetime;
- expected result if duration interpretation is correct: approximately double the baseline wall lifetime (~21.4 s under the same game time scale);
- automatically restore `15000` immediately after the test effect naturally expires;
- expose manual RESTORE and restore-on-reset/close safeguards.

Do not integrate duration writes into the main trainer until this 2x test passes.

## Locked rejects
- repeated native reapplication / refresh
- `Unit+0x460`
- parent `+0x194` as duration-specific
- path-based effect identity
- V7 cleanup-only dynamic child fields
- low-address/vtable static `10272` as duration merely by numeric coincidence
