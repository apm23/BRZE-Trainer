from pathlib import Path
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')

# F7 must use the old trainer's dedicated training-progress hook, not the F8/Delete selected-building writes.
s=s.replace('readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training — AUTO F8 COMPLETION", AutoSize = true };',
            'readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training — LEGACY F4 EXACT REMAP", AutoSize = true };')
s=s.replace('Native.SetHooks(f5.Checked, f6.Checked, false);', 'Native.SetHooks(f5.Checked, f6.Checked, f7.Checked);')

# Current BRZE equivalent of the old F4 read site: old target 0x41BECB was a dedicated +0x490 progress read.
# In BRZE 1.60 the matching path is 0x4D5DDB: mov eax,[ebx+0x490], then add progress and compare with 0x640000.
s=s.replace('RVA_TRAIN_PROGRESS_READ=0x0D65DC', 'RVA_TRAIN_PROGRESS_READ=0x0D5DDB')
s=s.replace('static readonly byte[] TrainOriginal={0x03,0x86,0x90,0x04,0x00,0x00};',
            'static readonly byte[] TrainOriginal={0x8B,0x83,0x90,0x04,0x00,0x00};')

start=s.index('    static byte[] BuildTrainingHook(long stub,long flag,long target)')
end=s.index('    static byte[] JmpPatch', start)
new='''    static byte[] BuildTrainingHook(long stub,long flag,long target)\n    {\n        var b=new List<byte>();\n        // Old trainer F4 used a dedicated training progress hook. BRZE 1.60 equivalent at 0x4D5DDB:\n        //   mov eax,[ebx+0x490] ; add eax,esi ; mov [ebx+0x490],eax ; cmp eax,0x640000\n        // If enabled for our building, force +0x490 to 0x64B540 immediately before that read.\n        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)flag);b.Add(0);\n        b.AddRange(new byte[]{0x0F,0x84});int jDisabled=b.Count;I32(b,0);\n        b.Add(0x52); // push edx\n        b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // mov edx,[localId]\n        b.AddRange(new byte[]{0x39,0x93,0x84,0x00,0x00,0x00}); // cmp [ebx+84],edx\n        b.Add(0x5A); // pop edx\n        b.AddRange(new byte[]{0x0F,0x85});int jNotOwner=b.Count;I32(b,0);\n        b.AddRange(new byte[]{0xC7,0x83,0x90,0x04,0x00,0x00,0x40,0xB5,0x64,0x00}); // [ebx+490]=0x64B540\n        int originalLabel=b.Count;b.AddRange(TrainOriginal);\n        b.Add(0xE9);int jBack=b.Count;I32(b,0);\n        PatchRel(b,jDisabled,stub+jDisabled+4,stub+originalLabel);\n        PatchRel(b,jNotOwner,stub+jNotOwner+4,stub+originalLabel);\n        PatchRel(b,jBack,stub+jBack+4,target+TrainOriginal.Length);\n        return b.ToArray();\n    }\n\n'''
s=s[:start]+new+s[end:]

# Remove the false-positive pointer-table auto scanner completely. F7 is now hook-only.
start=s.index('    static void ApplyInstantUnitTraining(uint lid)')
end=s.index('    public static string Apply(', start)
s=s[:start]+s[end:]
s=s.replace('if(instantTrain)ApplyInstantUnitTraining(lid);', '')
s=s.replace(' trainFound:{trainingBuildings}', ' F7LegacyHook:{instantTrain}')

p.write_text(s,encoding='utf-8')
