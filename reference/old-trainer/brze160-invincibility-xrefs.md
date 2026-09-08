# BRZE 1.60 — `unit+0x6A4` invincibility cross-reference autopsy

Exact sample: `Battle_Realms_F.exe` (BRZE 1.60), image base `0x00400000`.

## Direct references to `unit+0x6A4`

Only six meaningful direct references were found in `.text`:

- `0x4AC933` — read/test inside internal predicate `0x4AC8D6`
- `0x4CCB55` — write by exported `UnitSetInvincible`
- `0x5CCD57` — read in central signed health-delta routine `0x5CCD4D`
- `0x5CFD03` — initialization/reset write
- `0x5F1A0A` — state serialization access
- `0x5F302E` — state deserialization access

`0x60820E` contains the same numeric displacement in a stack-relative LEA and is not a unit-field reference.

## Central damage behavior

At `0x5CCD4D`, the game checks `unit+0x6A4` before applying the signed health delta. When the flag is set and the incoming delta is negative, the routine exits without applying damage. Positive health changes are not blocked.

This proves `+0x6A4` really is an invincibility/damage-immunity state, not a guessed field.

## The important discovery: `0x4AC8D6` is broader than HP

The internal predicate beginning at `0x4AC8D6` does not merely return `unit->invincible`.

It first walks/checks other unit state/effect structures (`+0x1CC`, `+0x1D0`, nested `+0x1F4/+0x25C`, and list rooted at `+0x20C`). If any of those conditions match, it returns TRUE. Only when they do not match does it fall back to:

```asm
cmp dword ptr [unit+0x6A4], 0
setne al
```

So `0x4AC8D6` is best treated as a generalized **special protected / invalid-target / excluded-from-normal-processing predicate**, with invincibility being one way to satisfy it.

## Callers of `0x4AC8D6`

14 static callers were found:

- `0x4AB655`
- `0x4AB706`
- `0x4AD2AE`
- `0x53C942`
- `0x53CA1B`
- `0x570BA2`
- `0x570C16`
- `0x5C931C`
- `0x5CB864`
- `0x5CBD86`
- `0x5CC952`
- `0x5D761B`
- `0x5DAAC8`
- `0x5DFB98`

The observed pattern is consistent across these sites: when `0x4AC8D6` returns nonzero, the caller generally aborts/skips a normal gameplay path for that unit.

### Strongly classified callers

`0x4AB624` / call at `0x4AB655` and the neighboring routine / call at `0x4AB706` iterate units, reject units for which `0x4AC8D6` is true, then inspect owner and health ratio. This is target/candidate filtering rather than HP application.

`0x4AD2AE` occurs in a large unit eligibility filter. A TRUE result immediately rejects the unit before type/state and health-based logic.

`0x570BA2` and `0x570C16` occur in range/effect logic. A TRUE result skips the normal interaction path even after distance checks have passed.

`0x5CB864` occurs near a major combat/gameplay routine. A TRUE result jumps directly to the routine exit path (`0x5CCAD4`) before the rest of the combat logic.

`0x5CBD86` suppresses a later timed/status-damage setup path. The nearby code stores state in `+0x768/+0x76C/+0x770`; when `0x4AC8D6` is TRUE, that setup is skipped.

`0x5CC952` is another gameplay/state path that exits early when the predicate returns TRUE.

`0x5D761B`, `0x5DAAC8`, and `0x5DFB98` are AI/target/interact candidate paths where a TRUE result prevents the normal follow-on decision or action.

## Interpretation

This explains the runtime side effects seen when the trainer directly set `unit+0x6A4`:

1. The central HP routine sees the flag and blocks damage — desired effect.
2. The generalized predicate `0x4AC8D6` also sees the same flag as TRUE.
3. At least 14 separate gameplay/AI/target/effect call sites then treat that unit as a special excluded/protected candidate and skip normal behavior.

Therefore the old trainer behavior that looked like invisibility, reduced/odd LOS, targeting anomalies, or skill oddities is technically plausible without requiring `+0x6A4` itself to literally be an invisibility flag. The broader effects come from all the systems that consume `0x4AC8D6`.

## Consequence for BRZE Trainer F6

Do **not** set `unit+0x6A4`.

The clean architecture is to intercept only the negative-health-delta result at `0x5CCD4D` for selected local-player units. This reproduces the desired damage-immunity outcome without making `0x4AC8D6` return TRUE and therefore avoids contaminating targeting/AI/effect eligibility.

This cross-reference autopsy materially strengthens the current F6 hook design.

## Next reverse-engineering target

The next useful target is the non-`+0x6A4` half of `0x4AC8D6`: identify the state/effect objects checked through `+0x1CC`, `+0x1D0`, `+0x20C`, nested `+0x1F4`, and `+0x25C`. This should reveal which legitimate game skills/states share this protected/excluded predicate and may identify the disappearing/stealth skill suspected by runtime behavior.
