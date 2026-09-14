# BRZE Trainer — NEXT SESSION HANDOFF

Date: 2026-09-15 JST
Branch: `instant-death-v4-hover-telemetry`
GitHub is authoritative.

## Authoritative current / fallbacks
- V18.3 = locked historical stable fallback (`FINAL_CURRENT.md`).
- V29 = runtime-proven standalone Hero reset primitive.
- V30 = **runtime-proven integrated gameplay base** and remains locked.
- V31 overlay = runtime-rejected (Alt+W failed).
- V32 = runtime-proven Alt+W baseline.
- V33 = runtime-proven premium overlay baseline; user approved appearance/behavior.
- V34 = runtime-proven final-candidate base: user reported all gameplay functions safe; hard trainer↔BRZE refresh and collision cleanup retained.
- V35 = **latest FINAL RC**: CI PASS, gameplay cores locked; runtime smoke pending only for the last UI/session polish.

Do not reopen Hero reset research unless a concrete gameplay regression appears.

## Locked V30 gameplay
Hero reset primitive:
- exact same-effect signature: `record+0x58` ability + `record+0x17C` target Unit*;
- current BRZE tick = module RVA `0x440A3C`;
- active same effect -> `record+0x194 = current tick` -> restart same instance without stacking;
- missing effect -> one-shot native Replay V2 apply;
- APPLY BOTH partitions mixed selections;
- custom duration keeps proven full-lifetime config hold/restore.

V30 integrated runtime result from user: **WORK / PASS**.
Never reintroduce repeated same-effect native replay, `record+0x194=0`, Unit+0x460 duration writes, CreateRemoteThread, or hard-coded transient effect paths.

## V34 locked safety/base retained by V35
- Premium overlay opacity 91%.
- Alt+W global hotkey.
- full mini Unit Changer page sharing normal trainer state.
- HOME / REFRESH TRAINER hard rebind: tears down trainer-owned hooks/handles/caves/Hero holds, force re-probes BRZE, then enabled cheats rebuild on the next timer tick.
- COPY buffer intentionally clears on hard refresh.
- prior collision cleanup retired duplicate legacy writers at `0x1A70C6`, `0x1CCD4D`, `0x467AF4`.

## V35 FINAL RC — latest requested polish
### COPY UNIT
Both normal trainer and in-game overlay:
- minimum `0.1`;
- maximum `64.0`;
- increment `0.1`;
- **default = 0.1**;
- overlay reset button also returns to `0.1`.

### Movable in-game overlay
- 91% opacity retained.
- Drag using the header/title area.
- nav/hide/action buttons remain clickable normally.
- position is preserved across Alt+W hide/show for that trainer session.
- drag completion returns focus to BRZE.

### Persistent Unit Changer memory
UI state is saved to:
`%LOCALAPPDATA%\BRZE-Trainer\unit-changer-v1.json`

Saved:
- selected building/profile;
- 9 output units for every building profile;
- slot 1–9 ON/OFF state for every building profile.

Persistence is updated immediately after relevant UI changes and again on normal trainer close. It stores **UI configuration only** — never PID/module base, Unit pointers, hooks, caves, effect records, or any transient BRZE runtime address. Corrupt/old settings safely fall back without blocking trainer startup.

## V35 collision / architecture audit
PASS:
- 15 compiled sources scanned;
- 14 fixed module mutation sites resolved;
- no fixed-address mutation site has multiple compiled owners;
- render hook `0x135C43` owner = `IntegratedFrameDispatcherCore` only;
- Hero + Instant Death native work remains serialized through shared dispatcher;
- Stamina ownership remains dedicated/non-duplicated;
- hard refresh teardown + force BRZE re-probe preserved;
- COPY offset verified 0.1..64.0 / step 0.1 / default-reset 0.1 in both UIs;
- Unit Changer LOCALAPPDATA persistence markers verified;
- movable 91% overlay markers verified.

## V35 CI proof
Workflow `Final V35 RC Four-Pack`
- run `34863185931` — SUCCESS
- job `104040341580` — SUCCESS
- head `8dbad8e8221c37ce1553acdad63f14df38dd37ec`
- artifact `10356247507`
- artifact digest `sha256:b57567b72910c58b272e64a493984f76654ba2cb7c5f0dbc8d9d53497a14b6e5`

Hashes:
- CLEAN standalone `1973fb1b947df36a1170a4cebeecce3c7b50e1fcab7db59e223a3cd8948be387`
- DIAGNOSTICS standalone `9103373809b69d93c7de65fcbcb719c9e73e4a3005a68e564a16970fdbf8a83c`
- CLEAN small `9fc2155286b589848cc228bafbeac4877de0b64b650a1b9dd51a42c970cdba98`
- DIAGNOSTICS small `2e60a0db5bfc6abc9d5e87e5e08982d6547d170405d4338989e24fa9786bceee`

All V35 audit/architecture checks, Clean+Diagnostics compile, four publishes, hashes and artifact upload passed. Ignore unrelated legacy `fix-large-selection-freeze.yml` failure.

## EXACT NEXT ACTION — FINAL USER SMOKE
Use `BRZE-Trainer-FINAL-V35-Diagnostics.exe`.

1. Confirm normal trainer COPY offset starts at `0.1` and can step 0.1.
2. Alt+W: confirm overlay COPY offset also starts at `0.1`; drag overlay from header, hide/show it and confirm moved position remains.
3. Unit Changer: change building/profile + several outputs + ON/OFF slots, close trainer with X, reopen V35 and confirm the configuration is restored.
4. Quick regression only: one Hero Effect action, one COPY/PASTE, and HOME hard refresh while BRZE runs.

If user reports PASS: **promote V35 to FINAL 100% immediately**. No new experimental version unless a concrete regression exists.

## New-chat bootstrap
`CONTINUE BRZE TRAINER — READ NEXT_SESSION.md FIRST — GitHub authoritative. V30 gameplay is runtime-proven/locked; V34 gameplay/collision/hard-refresh base is runtime safe. V35 FINAL RC adds default COPY offset 0.1 in normal+overlay, draggable 91% overlay, and persistent Unit Changer UI memory in %LOCALAPPDATA%\\BRZE-Trainer\\unit-changer-v1.json. V35 CI run 34863185931 SUCCESS, artifact 10356247507. Next: one final V35 Diagnostics smoke; if PASS, promote FINAL 100%.`
