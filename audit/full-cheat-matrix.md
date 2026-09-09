# Full cheat audit matrix

Rule: Old trainer -> exact BRWOTW behavior/structure -> BRZE 1.60 homolog -> runtime proof. No feature is final from address similarity alone.

| Old cheat | Old/BRWOTW evidence | BRZE mapping | Current verdict |
|---|---|---|---|
| F1 Maximum Resources | old player resource fields; BRWOTW native resource APIs | player +D8/+DC and max +E0/+E4 | runtime-proven rice/water; keep |
| F2 Maximum Population | old global 0x718568 = 99,999,999; BRWOTW exports SetMaxUnits | per-player RVA 0x467B90 | structure strong; current 9,999,999 policy reopened |
| F3 Maximum Yin/Yang | old +2E4/+2E8; BRWOTW player semantics | BRZE +2E8/+2EC | runtime-proven; keep |
| F4 Instant Unit Training | exact old hook at 0x41BECB writes progress sentinel | BRZE homolog RVA 0x0D5DDB | v49 runtime-proven; protect baseline |
| F5 Infinite Watchtowers | old four faction globals 0x6F9448/0x6F95E8/0x6F96C8/0x6F9798 zeroed | current counter not yet established | NOT IMPLEMENTED |
| Delete Instant Build/Repair/Research/BattleGear | old selected object writes +8E, +492, +4BE | BRZE selected-building logic currently completes build/train | partial runtime proof only; research/repair/BG need mapping |
| F10 Maximum Wolves | old selected object byte +250 = FA | BRZE candidate +250 = 250 | static candidate; runtime test required |
| F8 Demolition | old hook writes object +8C=0; BRWOTW has EnableDemolish | BRZE native demolition flag RVA 0x3D7A1C | strong native candidate; runtime test required |
| F9 Pause Peasant Production | old four faction globals toggle 0/1; BRWOTW Enable/DisablePeasantCreation | BRZE per-player RVA 0x467AF4 | strong native candidate; runtime test required |
| PageDown Instant Death | old writes FF000000 to old +400/+404 | current HP +404, stamina +408 | current HP=0 is semantic substitute, not exact port; audit required |
| PageUp Infinite Health/Stamina | old game-side selected-list sentinel writes | current A/B mass-selection crash/freeze | FAILED architecture; redesign after confounders removed |
| Horses | BRWOTW/current native horse APIs/config exist | current respawn-time +48 candidate | not equivalent-proven to old trainer behavior |

## Cross-cutting decisions
- Remove population from HP/stamina isolation specimens.
- Never reintroduce external 2000-unit high-frequency polling.
- Do not use unit +0x6A4 invincibility in production.
- Do not carry A/B selection-event refill into the next clean specimen.
- v49 training remains the reference example for exact cross-version remapping.
