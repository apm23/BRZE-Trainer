from pathlib import Path

# ---- SelectionCore: fixed 3 seconds instead of fixed 1 second ----
p = Path('SelectionCore.cs')
s = p.read_text(encoding='utf-8')
old = 'const uint SELECTION_LIMIT = 500, LOGICAL_WINDOW = 160, PEASANT_FIXED_MS = 1000;'
new = 'const uint SELECTION_LIMIT = 500, LOGICAL_WINDOW = 160, PEASANT_FIXED_MS = 3000;'
if old not in s:
    raise SystemExit('SelectionCore: PEASANT_FIXED_MS=1000 marker missing')
s = s.replace(old, new, 1)
s = s.replace('peasant1s:{fastPeasant}/{pf}', 'peasant3s:{fastPeasant}/{pf}')
p.write_text(s, encoding='utf-8')

# ---- HookCore: BRZE has a separate direct SubStamina function at RVA 0x1CDF46 ----
p = Path('HookCore.cs')
s = p.read_text(encoding='utf-8')

def rep(old: str, new: str, name: str):
    global s
    if old not in s:
        raise SystemExit(f'HookCore marker missing: {name}')
    s = s.replace(old, new, 1)

rep('    const int RVA_ADD_STAMINA = 0x1CCDFB;\n',
    '    const int RVA_ADD_STAMINA = 0x1CCDFB;\n    const int RVA_SUB_STAMINA = 0x1CDF46;\n',
    'RVA_SUB_STAMINA')

rep('    static readonly byte[] StOriginal = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1, 0x57, 0x56 };\n',
    '    static readonly byte[] StOriginal = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1, 0x57, 0x56 };\n    static readonly byte[] SubStOriginal = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1, 0x57 };\n',
    'SubStOriginal')

marker = '    static byte[] BuildTrainingHook(long stub, long flag, long target)\n'
if marker not in s:
    raise SystemExit('HookCore marker missing: BuildTrainingHook')
sub_method = r'''    static byte[] BuildSubStaminaHook(long stub, long flag, long target)
    {
        // Battle Realms 1.60 has a direct SubStamina(amount) path which bypasses AddStamina.
        // When F5 is enabled, reject the subtraction before the native prologue for a
        // local unit that is selected in either the UI list (+3A8) or SIM list (+3AC).
        var b = new List<byte>();
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)flag); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jDisabled = b.Count; I32(b, 0);
        b.Add(0x50); // preserve eax
        b.Add(0xA1); U32(b, (uint)(moduleBase + RVA_LOCAL_ID));
        b.AddRange(new byte[] { 0x39, 0x81, 0x40, 0x02, 0x00, 0x00 }); // owner
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jOwner = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0xB9, 0xA8, 0x03, 0x00, 0x00, 0x01 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jSelA = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0xB9, 0xAC, 0x03, 0x00, 0x00, 0x01 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jNotSelected = b.Count; I32(b, 0);
        int block = b.Count;
        b.Add(0x58); b.AddRange(new byte[] { 0xC2, 0x04, 0x00 }); // skip SubStamina(amount)
        int popOriginal = b.Count;
        b.Add(0x58);
        int originalLabel = b.Count;
        b.AddRange(SubStOriginal);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);
        Rel(b, jDisabled, stub + jDisabled + 4, stub + originalLabel);
        Rel(b, jOwner, stub + jOwner + 4, stub + popOriginal);
        Rel(b, jSelA, stub + jSelA + 4, stub + block);
        Rel(b, jNotSelected, stub + jNotSelected + 4, stub + popOriginal);
        Rel(b, jBack, stub + jBack + 4, target + SubStOriginal.Length);
        return b.ToArray();
    }

'''
s = s.replace(marker, sub_method + marker, 1)

rep('        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)2048, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);',
    '        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)3072, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);',
    'cave size')
rep('        long hpStub = c + 32, stStub = c + 224, trainStub = c + 416, sel1Stub = c + 640, sel2Stub = c + 896;\n        long hpTarget = moduleBase + RVA_ADD_HEALTH, stTarget = moduleBase + RVA_ADD_STAMINA;\n        long trainTarget = moduleBase + RVA_TRAIN_PROGRESS_READ, s1 = moduleBase + RVA_SELECT_ONE, s2 = moduleBase + RVA_SELECT_BOX;\n',
    '        long hpStub = c + 32, stStub = c + 224, trainStub = c + 416, sel1Stub = c + 640, sel2Stub = c + 896, subStStub = c + 1408;\n        long hpTarget = moduleBase + RVA_ADD_HEALTH, stTarget = moduleBase + RVA_ADD_STAMINA, subStTarget = moduleBase + RVA_SUB_STAMINA;\n        long trainTarget = moduleBase + RVA_TRAIN_PROGRESS_READ, s1 = moduleBase + RVA_SELECT_ONE, s2 = moduleBase + RVA_SELECT_BOX;\n',
    'stub/target layout')
rep('        var hn = new byte[HpOriginal.Length]; var sn = new byte[StOriginal.Length]; var tn = new byte[TrainOriginal.Length];\n        var q1 = new byte[SelectOriginal.Length]; var q2 = new byte[SelectOriginal.Length];\n',
    '        var hn = new byte[HpOriginal.Length]; var sn = new byte[StOriginal.Length]; var sun = new byte[SubStOriginal.Length]; var tn = new byte[TrainOriginal.Length];\n        var q1 = new byte[SelectOriginal.Length]; var q2 = new byte[SelectOriginal.Length];\n',
    'verify buffers')
rep('        if (!ReadExact(stTarget, sn) || !System.Linq.Enumerable.SequenceEqual(sn, StOriginal)) { error = "stamina hook byte mismatch"; return false; }\n        if (!ReadExact(trainTarget, tn)',
    '        if (!ReadExact(stTarget, sn) || !System.Linq.Enumerable.SequenceEqual(sn, StOriginal)) { error = "stamina add hook byte mismatch"; return false; }\n        if (!ReadExact(subStTarget, sun) || !System.Linq.Enumerable.SequenceEqual(sun, SubStOriginal)) { error = "stamina sub hook byte mismatch"; return false; }\n        if (!ReadExact(trainTarget, tn)',
    'SubStamina stock verify')
rep('        byte[] ss = BuildHardLockHook(stStub, stFlag, stTarget, StOriginal);\n        byte[] ts = BuildTrainingHook(trainStub, trainFlag, trainTarget);',
    '        byte[] ss = BuildHardLockHook(stStub, stFlag, stTarget, StOriginal);\n        byte[] sus = BuildSubStaminaHook(subStStub, stFlag, subStTarget);\n        byte[] ts = BuildTrainingHook(trainStub, trainFlag, trainTarget);',
    'build SubStamina stub')
rep('        if (!WriteRaw(hpStub, hs) || !WriteRaw(stStub, ss) || !WriteRaw(trainStub, ts) || !WriteRaw(sel1Stub, a) || !WriteRaw(sel2Stub, b))',
    '        if (!WriteRaw(hpStub, hs) || !WriteRaw(stStub, ss) || !WriteRaw(subStStub, sus) || !WriteRaw(trainStub, ts) || !WriteRaw(sel1Stub, a) || !WriteRaw(sel2Stub, b))',
    'write SubStamina stub')
rep('            !WriteCode(stTarget, JmpPatch(stTarget, stStub, StOriginal.Length)) ||\n            !WriteCode(trainTarget,',
    '            !WriteCode(stTarget, JmpPatch(stTarget, stStub, StOriginal.Length)) ||\n            !WriteCode(subStTarget, JmpPatch(subStTarget, subStStub, SubStOriginal.Length)) ||\n            !WriteCode(trainTarget,',
    'install SubStamina detour')
rep('        return $"LOCKS hooks:{hooksInstalled} | HP-hard:{hp} ST-hard:{stamina} F7:{training} | lastTopUp:{lastTopUp}"',
    '        return $"LOCKS hooks:{hooksInstalled} | HP-hard:{hp} ST-hard(Add+Sub):{stamina} F7:{training} | lastTopUp:{lastTopUp}"',
    'status')
rep('                WriteCode(moduleBase + RVA_ADD_STAMINA, StOriginal);\n                WriteCode(moduleBase + RVA_TRAIN_PROGRESS_READ, TrainOriginal);',
    '                WriteCode(moduleBase + RVA_ADD_STAMINA, StOriginal);\n                WriteCode(moduleBase + RVA_SUB_STAMINA, SubStOriginal);\n                WriteCode(moduleBase + RVA_TRAIN_PROGRESS_READ, TrainOriginal);',
    'restore SubStamina')

p.write_text(s, encoding='utf-8')
print('V2 source patch OK: peasant=3000ms, stamina hooks AddStamina + SubStamina')
