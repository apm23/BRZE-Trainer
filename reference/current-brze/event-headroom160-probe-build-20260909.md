# Event Headroom 160 Probe — build pin

Purpose: surgical companion to proven Sim-Pipeline-120. Keep the physical event backing buffer at native 256 bytes while lowering only the logical remaining-byte window to 160 so `0x561B2A` is requested earlier.

Branch: `selection-event-headroom-160-probe`
Built head: `84026dd163f85f1121e4d5607a24b54abdeb5611`
Actions run: `34367473165` — SUCCESS
Artifact: `BRZE-Event-Headroom-160-Probe`
Artifact ID: `10110502418`
ZIP SHA-256: `9188cc07694c7e77ca9832c36ccb27049c54af5100a4afc629d2252d9e23d6a7`
EXE SHA-256: `c25e9ac2962c5659d851e7db3bb722d06a36cd6d03d4023ad259643c0792002a`

Exact new variable:
- physical event buffer remains 256 bytes
- live logical remaining is armed to 160 only while queue used=0
- initialization/reset immediate at `0x56176A` is changed from logical 256 to 160
- successful flush reset immediate at `0x561D71` is changed from logical 256 to 160
- no event suppression, no buffer replacement, no selection capacity changes, no throttle

The companion also reports connection state/mode/maxPayload from the send object for runtime correlation.

Required runtime test:
1. fresh BRZE
2. proven Sim-Pipeline-120 F4 before selecting anything
3. start Event Headroom 160 companion and arm it while EVENT used=0
4. optionally run read-only telemetry observer
5. one large rectangle drag
6. report freeze yes/no, ACTIVE/SIM/EVENT, and companion NET maxPayload

Interpretation:
- stable with native flushes -> packet headroom was the missing condition; integrate logical headroom into final selection fix
- still freezes with EVENT underflow -> flush failure is not just payload size/headroom; inspect send helper state
- no underflow but freeze elsewhere -> selection producer survived and investigation moves downstream
