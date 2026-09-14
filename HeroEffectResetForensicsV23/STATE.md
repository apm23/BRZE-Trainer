# BRZE Hero Effect Reset Forensics V23 — STATE

Status: **RUNTIME-PROVEN READ-ONLY DISCOVERY — V24 DEEP FUNCTION ANALYSIS NEXT**
Date: 2026-09-14 JST

## Goal
Find a real per-instance cleanup/expire path for active hero effects so final trainer semantics can become:

`APPLY again = remove/reset old A5/C0 instance on selected unit -> one-shot apply new instance -> new duration starts from zero`

without stacking/repeated refresh.

## Why this probe exists
User requested replacement/reset semantics instead of V22 skip/hold behavior.

Rejected implementation:
- blindly call Replay V2 again on a unit that already has the same active effect;
- V3 runtime already proved repeated native application can compound/stack into extreme speed, invulnerability-like damage behavior, one-hit buildings, and other corruption.

Therefore V23 does NOT modify or remove anything. It performs strict read-only forensics first.

## V23 method
1. user selects exactly one unit with ACTIVE Issyl A5;
2. content-signature lock using proven V6 identity:
   - `record+0x058 == 0xA5`
   - `record+0x17C == selected Unit*`;
3. capture parent record/vtable/teardown/config;
4. parse live BRZE PE `.text`;
5. decode with Iced x86;
6. inspect vtable slots and rank `.text` references to `+0x194` with nearby known effect fields.

## Safety
- PROCESS_VM_READ + PROCESS_QUERY_INFORMATION only;
- no WriteProcessMemory;
- no VirtualAllocEx;
- no hooks;
- no native ability call;
- no object destructor invocation;
- no vtable modification;
- no CreateRemoteThread.

## Locked prior evidence
- V6 content-signature identity is robust; transient root path is not.
- `parent+0x194` is NOT duration; observed values tracked teardown/global timing, so it is used only as a static-analysis lead here.
- V7 found no safe continuously-changing per-instance timer.
- V11 duration config path remains proven and unchanged.
- V22 remains fallback integrated trainer while reset semantics are researched.

## CI build pin
Authoritative build workflow: `Hero Effect Reset Forensics V23B Read Only`
- run `34825682469` — SUCCESS
- job `103917157659` — SUCCESS
- head `5a3c22b563eb879be2212afa20a05c49b9af625a`
- artifact `10340067680`
- artifact digest `sha256:60153b70296f7f254eb9785812f2518c9a85f60f9ba8648d9f81cfb9cf704f6b`
- standalone SHA256 `db5005e355f6be87b6330a033ed100ddf4b906079b2f5a593874178e8d6273d7`
- small SHA256 `cef5f68a820b95512b22939d2ed18e19ef584cc83823fa6461a1fd2bff73630c`

## Runtime result — 2026-09-14
V23B successfully locked an active Issyl instance:
- module base `0x00870000`
- selected Unit* `0x22B47F2C`
- UnitDef* `0x1688993C`
- owner `0`
- A5 parent `0x235ABEE0`
- discovered via `Unit+0x1E4->+0x008`
- parent vtable/type `0x00C138EC`
- parent+0x194 `247400`
- parent+0x1F4 config `0x1D1FA664`
- config ID `0xA5`
- nominal duration `15000`

Observed relevant vtable methods:
- VT[4]  -> RVA `0x14053A`
- VT[5]  -> RVA `0x1405A2`
- VT[20] -> RVA `0x142450`
- VT[23] -> RVA `0x142C28`
- VT[24] -> RVA `0x142C6E`

Top `.text` cleanup lead:
- RVA `0x13DA9C`
- write to displacement `+0x194`
- local decoded window also contains references to all known effect fields `+0x058`, `+0x17C`, `+0x194`, `+0x1F4`
- score `44`, highest result in V23

Secondary leads include `0x13A8B3`, `0x13A8BF`, `0x16B76A`, `0x2151DE`, `0x24A7B8`, but they have weaker local field co-occurrence than `0x13DA9C`.

## Interpretation
`0x13DA9C` is now the strongest structural cleanup/teardown lead, but V23 does NOT establish a callable function boundary, calling convention, arguments, or whether the containing routine is create/tick/expire/cleanup.

Therefore DO NOT call `0x13DA9C` and DO NOT write `parent+0x194`.

## Exact next action
Build/run V24 strict read-only deep function analysis:
- resolve probable function boundary containing `0x13DA9C`;
- decode full local function with operands/branch/call targets;
- inspect the five effect-record vtable methods above;
- compare secondary candidates only as needed;
- no game writes/hooks/native calls.

Only after V24 identifies a structurally justified native cleanup boundary should a guarded runtime proof be considered.
