from pathlib import Path

p = Path('HookCore.cs')
s = p.read_text(encoding='utf-8')


def rep(old: str, new: str):
    global s
    if old not in s:
        raise SystemExit('MISSING EXPECTED FRAGMENT:\n' + old)
    s = s.replace(old, new, 1)

rep(
'''    const int RVA_ADD_STAMINA = 0x1CCDFB;\n    const int RVA_TRAIN_PROGRESS_READ = 0x0D5DDB;''',
'''    const int RVA_ADD_STAMINA = 0x1CCDFB;\n    // Absolute stamina setter at VA 0x5CCE79. Running/skill paths can reach this\n    // without passing through AddStamina, which is why the v1 delta-only hook leaked.\n    const int RVA_SET_STAMINA = 0x1CCE79;\n    const int RVA_STAMINA_MAX_HELPER = 0x1D19B1;\n    const int RVA_TRAIN_PROGRESS_READ = 0x0D5DDB;''')

rep(
'''    static readonly byte[] StOriginal = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1, 0x57, 0x56 };\n    static readonly byte[] TrainOriginal''',
'''    static readonly byte[] StOriginal = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1, 0x57, 0x56 };\n    static readonly byte[] SetStOriginal = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1, 0x57, 0x8B, 0x7D, 0x08 };\n    static readonly byte[] TrainOriginal''')

marker = '''    static byte[] BuildTrainingHook(long stub, long flag, long target)\n'''
if marker not in s:
    raise SystemExit('MISSING BuildTrainingHook marker')

method = r'''    static byte[] BuildSetStaminaHook(long stub, long flag, long target)
    {
        // BRZE has an absolute SetStamina path at 0x5CCE79 in addition to AddStamina.
        // For a selected local unit while F5 is ON, replace SetStamina(value) with
        // SetStamina(native effective max). We still execute the original setter so its
        // native clamp/side effects remain intact. No external per-tick unit scan.
        var b = new List<byte>();
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)flag); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jDisabled = b.Count; I32(b, 0);
        b.Add(0x50); // preserve EAX; original stack arg moves from +4 to +8
        b.Add(0xA1); U32(b, (uint)(moduleBase + RVA_LOCAL_ID));
        b.AddRange(new byte[] { 0x39, 0x81, 0x40, 0x02, 0x00, 0x00 }); // owner == localId
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jOwner = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0xB9, 0xA8, 0x03, 0x00, 0x00, 0x01 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jSelected = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0xB9, 0xAC, 0x03, 0x00, 0x00, 0x01 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jNotSelected = b.Count; I32(b, 0);
        int selected = b.Count;
        b.AddRange(new byte[] { 0x8B, 0x41, 0x74 }); // eax=[ecx+def]
        b.AddRange(new byte[] { 0x85, 0xC0 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jNoDef = b.Count; I32(b, 0);
        b.Add(0x51); // preserve ECX across native max helper
        b.Add(0x51); // helper arg2 = unit
        b.AddRange(new byte[] { 0xFF, 0xB0, 0x80, 0x00, 0x00, 0x00 }); // helper arg1 = def->maxStamina
        b.Add(0xE8); int helperCall = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xC1, 0xE0, 0x10 }); // native fixed16
        // helper is ret 8, so ESP is entryESP-8 here; original arg is [esp+0x0C].
        b.AddRange(new byte[] { 0x89, 0x44, 0x24, 0x0C });
        b.Add(0x59); // restore ECX
        int restore = b.Count;
        b.Add(0x58); // restore EAX
        int originalLabel = b.Count;
        b.AddRange(SetStOriginal);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);

        Rel(b, jDisabled, stub + jDisabled + 4, stub + originalLabel);
        Rel(b, jOwner, stub + jOwner + 4, stub + restore);
        Rel(b, jSelected, stub + jSelected + 4, stub + selected);
        Rel(b, jNotSelected, stub + jNotSelected + 4, stub + restore);
        Rel(b, jNoDef, stub + jNoDef + 4, stub + restore);
        Rel(b, helperCall, stub + helperCall + 4, moduleBase + RVA_STAMINA_MAX_HELPER);
        Rel(b, jBack, stub + jBack + 4, target + SetStOriginal.Length);
        return b.ToArray();
    }

'''
s = s.replace(marker, method + marker, 1)

rep(
'''        long hpStub = c + 32, stStub = c + 224, trainStub = c + 416, sel1Stub = c + 640, sel2Stub = c + 896;\n        long hpTarget = moduleBase + RVA_ADD_HEALTH, stTarget = moduleBase + RVA_ADD_STAMINA;\n        long trainTarget''',
'''        long hpStub = c + 32, stStub = c + 224, trainStub = c + 416, sel1Stub = c + 640, sel2Stub = c + 896, setStStub = c + 1152;\n        long hpTarget = moduleBase + RVA_ADD_HEALTH, stTarget = moduleBase + RVA_ADD_STAMINA, setStTarget = moduleBase + RVA_SET_STAMINA;\n        long trainTarget''')

rep(
'''        var hn = new byte[HpOriginal.Length]; var sn = new byte[StOriginal.Length]; var tn = new byte[TrainOriginal.Length];\n        var q1 = new byte[SelectOriginal.Length]; var q2 = new byte[SelectOriginal.Length];\n        if (!ReadExact(hpTarget, hn) || !System.Linq.Enumerable.SequenceEqual(hn, HpOriginal)) { error = "HP hook byte mismatch"; return false; }\n        if (!ReadExact(stTarget, sn) || !System.Linq.Enumerable.SequenceEqual(sn, StOriginal)) { error = "stamina hook byte mismatch"; return false; }\n        if (!ReadExact(trainTarget, tn)''',
'''        var hn = new byte[HpOriginal.Length]; var sn = new byte[StOriginal.Length]; var ssn = new byte[SetStOriginal.Length]; var tn = new byte[TrainOriginal.Length];\n        var q1 = new byte[SelectOriginal.Length]; var q2 = new byte[SelectOriginal.Length];\n        if (!ReadExact(hpTarget, hn) || !System.Linq.Enumerable.SequenceEqual(hn, HpOriginal)) { error = "HP hook byte mismatch"; return false; }\n        if (!ReadExact(stTarget, sn) || !System.Linq.Enumerable.SequenceEqual(sn, StOriginal)) { error = "stamina AddStamina hook byte mismatch"; return false; }\n        if (!ReadExact(setStTarget, ssn) || !System.Linq.Enumerable.SequenceEqual(ssn, SetStOriginal)) { error = "stamina SetStamina hook byte mismatch"; return false; }\n        if (!ReadExact(trainTarget, tn)''')

rep(
'''        byte[] hs = BuildHardLockHook(hpStub, hpFlag, hpTarget, HpOriginal);\n        byte[] ss = BuildHardLockHook(stStub, stFlag, stTarget, StOriginal);\n        byte[] ts = BuildTrainingHook(trainStub, trainFlag, trainTarget);''',
'''        byte[] hs = BuildHardLockHook(hpStub, hpFlag, hpTarget, HpOriginal);\n        byte[] ss = BuildHardLockHook(stStub, stFlag, stTarget, StOriginal);\n        byte[] sss = BuildSetStaminaHook(setStStub, stFlag, setStTarget);\n        byte[] ts = BuildTrainingHook(trainStub, trainFlag, trainTarget);''')

rep(
'''        if (!WriteRaw(hpStub, hs) || !WriteRaw(stStub, ss) || !WriteRaw(trainStub, ts) || !WriteRaw(sel1Stub, a) || !WriteRaw(sel2Stub, b))''',
'''        if (!WriteRaw(hpStub, hs) || !WriteRaw(stStub, ss) || !WriteRaw(setStStub, sss) || !WriteRaw(trainStub, ts) || !WriteRaw(sel1Stub, a) || !WriteRaw(sel2Stub, b))''')

rep(
'''        if (!WriteCode(hpTarget, JmpPatch(hpTarget, hpStub, HpOriginal.Length)) ||\n            !WriteCode(stTarget, JmpPatch(stTarget, stStub, StOriginal.Length)) ||\n            !WriteCode(trainTarget''',
'''        if (!WriteCode(hpTarget, JmpPatch(hpTarget, hpStub, HpOriginal.Length)) ||\n            !WriteCode(stTarget, JmpPatch(stTarget, stStub, StOriginal.Length)) ||\n            !WriteCode(setStTarget, JmpPatch(setStTarget, setStStub, SetStOriginal.Length)) ||\n            !WriteCode(trainTarget''')

rep(
'''                WriteCode(moduleBase + RVA_ADD_STAMINA, StOriginal);\n                WriteCode(moduleBase + RVA_TRAIN_PROGRESS_READ, TrainOriginal);''',
'''                WriteCode(moduleBase + RVA_ADD_STAMINA, StOriginal);\n                WriteCode(moduleBase + RVA_SET_STAMINA, SetStOriginal);\n                WriteCode(moduleBase + RVA_TRAIN_PROGRESS_READ, TrainOriginal);''')

rep(
'''        return $"LOCKS hooks:{hooksInstalled} | HP-hard:{hp} ST-hard:{stamina} F7:{training} | lastTopUp:{lastTopUp}"''',
'''        return $"LOCKS hooks:{hooksInstalled} | HP-hard:{hp} ST-v2(Add+Set):{stamina} F7:{training} | lastTopUp:{lastTopUp}"''')

p.write_text(s, encoding='utf-8')
print('HookCore.cs patched: stamina v2 AddStamina + SetStamina')
