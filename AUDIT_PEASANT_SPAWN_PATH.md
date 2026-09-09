# BRZE 1.60 peasant/population path audit

Static trace from the user-supplied BRZE 1.60 executable, compared against BRWOTW/old trainer behavior.

## Confirmed max-pop storage
- BRZE max-units array: VA 0x867B90 / RVA 0x467B90, indexed by player id.
- Native SetMaxUnits writes `[eax*4+0x867B90] = ecx`.
- Stage-1 trainer cap target: 1000, replacing the unsafe 9,999,999 workaround.

## Important BRZE production gate discovered
At VA 0x57FEE9 a player-production path obtains a player state object and calls 0x582D5E. The returned value is compared directly against the player's max-units entry:

```
57ff19  mov ebx,eax
57ff1b  mov ecx,ebx
57ff1d  call 0x582d5e
57ff20  cmp eax,[edi*4+0x867b90]
57ff27  jb  0x57ff35
```

The same routine later obtains the value again and computes remaining capacity:

```
57ff5d  mov esi,[edi*4+0x867b90]
...
57ff7c  call 0x582d5e
57ff81  sub esi,eax
57ff83  cmp esi,1
```

This is strong evidence that 0x582D5E supplies a current/used-unit quantity to this production-management path. It is NOT yet proof that globally replacing its return value is safe; it can have other callers.

## Safe implementation rule
Do not globally overwrite the real used-unit counter and do not globally hook 0x582D5E to return 1. Preserve real unit accounting for AI, selection, combat, saves, UI, and entity management.

Preferred next patch is call-site scoped: only the peasant-production calculation should see a low pressure value / accelerated timer, and only for the local player. Before patching, trace the caller at 0x57FEE9 through the peasant-creation scheduler and identify the exact delay/cooldown write/read. If an independent peasant birth timer exists, patch that timer rather than spoofing unit count.

## Old trainer comparison
Old F2 only inflated the old max-pop array at 0x718568; it did not prove that changing the true used-unit counter was safe. The new implementation therefore improves on the old workaround instead of copying its extreme 99,999,999 value.

Status: max-pop mapping CONFIRMED; production pressure path STRONG STATIC; exact peasant birth timer still under trace and must not be guessed.
