from pathlib import Path
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('F5 Infinite Stamina (selected only) — LEGACY PAGEUP REMAP','F5 No Stamina Loss (selected only) — SAFE DELTA V62')
s=s.replace('F6 Infinite Health (selected only) — LEGACY PAGEUP REMAP','F6 No Damage (selected only) — SAFE DELTA V62')
s=s.replace('const int RVA_ADD_HEALTH=0x1CCD78,RVA_ADD_STAMINA=0x1CCE03,RVA_TRAIN_PROGRESS_READ=0x0D5DDB;','const int RVA_ADD_HEALTH=0x1CCD4D,RVA_ADD_STAMINA=0x1CCDFB,RVA_TRAIN_PROGRESS_READ=0x0D5DDB;')
s=s.replace('static readonly byte[] HpOriginal={0x8B,0x86,0x04,0x04,0x00,0x00};\n    static readonly byte[] StOriginal={0x8B,0xBE,0x08,0x04,0x00,0x00}; static readonly byte[] TrainOriginal={0x8B,0x83,0x90,0x04,0x00,0x00};','static readonly byte[] HpOriginal={0x55,0x8B,0xEC,0x83,0xE4,0xF8,0x56,0x8B,0xF1,0x57};\n    static readonly byte[] StOriginal={0x55,0x8B,0xEC,0x56,0x8B,0xF1,0x57,0x56}; static readonly byte[] TrainOriginal={0x8B,0x83,0x90,0x04,0x00,0x00};')
a=s.index('    static byte[] BuildHook(')
b=s.index('    static byte[] BuildTrainingHook',a)
new=r'''    static byte[] BuildHook(long stub,long flag,long target,byte[] original,int backOffset)
    {
        // Safe event hook: preserve the original function prologue.  If the feature is
        // enabled and this call is a negative delta for a selected local unit, return
        // without applying it. Positive deltas/healing remain native. No sentinel writes.
        var b=new List<byte>();
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)flag);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jDisabled=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x7C,0x24,0x04,0x00}); // cmp dword [esp+4],0
        b.AddRange(new byte[]{0x0F,0x8D});int jNonNeg=b.Count;I32(b,0);
        b.Add(0x50); // preserve eax
        b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));
        b.AddRange(new byte[]{0x39,0x81,0x40,0x02,0x00,0x00}); // cmp [ecx+240],eax
        b.AddRange(new byte[]{0x0F,0x85});int jOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB9,0xA8,0x03,0x00,0x00,0x01});
        b.AddRange(new byte[]{0x0F,0x85});int jSelA=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB9,0xAC,0x03,0x00,0x00,0x01});
        b.AddRange(new byte[]{0x0F,0x85});int jSelB=b.Count;I32(b,0);
        b.Add(0x58);b.AddRange(new byte[]{0xC2,0x04,0x00}); // block negative delta
        int popOriginal=b.Count;b.Add(0x58);
        int originalLabel=b.Count;b.AddRange(original);b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jDisabled,stub+jDisabled+4,stub+originalLabel);
        PatchRel(b,jNonNeg,stub+jNonNeg+4,stub+originalLabel);
        PatchRel(b,jOwner,stub+jOwner+4,stub+popOriginal);
        PatchRel(b,jSelA,stub+jSelA+4,stub+popOriginal);
        PatchRel(b,jSelB,stub+jSelB+4,stub+popOriginal);
        PatchRel(b,jBack,stub+jBack+4,target+backOffset);
        return b.ToArray();
    }
'''
s=s[:a]+new+s[b:]
p.write_text(s,encoding='utf-8')
