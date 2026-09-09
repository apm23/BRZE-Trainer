from pathlib import Path

p=Path('Program.cs')
s=p.read_text(encoding='utf-8')

s=s.replace('F5 No Stamina Loss (selected only) — SAFE DELTA V62','F5 No Stamina Loss (selected only) — SPECIMEN B NATIVE DELTA')
s=s.replace('F6 No Damage (selected only) — SAFE DELTA V62','F6 No Damage (selected only) — SPECIMEN B NATIVE DELTA')

s=s.replace('const int RVA_ADD_HEALTH=0x1CCD4D,RVA_ADD_STAMINA=0x1CCDFB,RVA_TRAIN_PROGRESS_READ=0x0D5DDB;',
'''const int RVA_ADD_HEALTH=0x1CCD78,RVA_ADD_STAMINA=0x1CCE03,RVA_TRAIN_PROGRESS_READ=0x0D5DDB;''')

s=s.replace('static readonly byte[] HpOriginal={0x55,0x8B,0xEC,0x83,0xE4,0xF8,0x56,0x8B,0xF1,0x57};',
'''static readonly byte[] HpOriginal={0x8B,0x86,0x04,0x04,0x00,0x00,0x03,0x45,0x08};''')
s=s.replace('static readonly byte[] StOriginal={0x55,0x8B,0xEC,0x56,0x8B,0xF1,0x57,0x56};',
'''static readonly byte[] StOriginal={0x8B,0xBE,0x08,0x04,0x00,0x00,0x8B,0x46,0x74,0x03,0x7D,0x08};''')

start=s.index('    static byte[] BuildHook(')
end=s.index('    static byte[] BuildTrainingHook', start)
new_hook=r'''    static byte[] BuildHook(long stub,long flag,long target,byte[] original,int backOffset)
    {
        // Specimen B: hook the real delta application site, not the function entry.
        // Negative selected-local deltas are suppressed, but the game continues through
        // its native post-delta path (events/clamps/death checks/etc). No ret 4 shortcut.
        bool hp = target == moduleBase + RVA_ADD_HEALTH;
        var b=new List<byte>();
        if(hp)
            b.AddRange(new byte[]{0x8B,0x86,0x04,0x04,0x00,0x00}); // mov eax,[esi+404]
        else
            b.AddRange(new byte[]{0x8B,0xBE,0x08,0x04,0x00,0x00,0x8B,0x46,0x74}); // mov edi,[esi+408]; mov eax,[esi+74]

        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)flag);b.Add(0); // cmp [flag],0
        b.AddRange(new byte[]{0x0F,0x84});int jAddDisabled=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x7D,0x08,0x00}); // cmp [ebp+8],0
        b.AddRange(new byte[]{0x0F,0x8D});int jAddNonNeg=b.Count;I32(b,0);
        b.Add(0x52); // push edx
        b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // mov edx,[localId]
        b.AddRange(new byte[]{0x39,0x96,0x40,0x02,0x00,0x00}); // cmp [esi+240],edx
        b.AddRange(new byte[]{0x0F,0x85});int jRestoreAddOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xBE,0xA8,0x03,0x00,0x00,0x01}); // selected A
        b.AddRange(new byte[]{0x0F,0x85});int jRestoreAddA=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xBE,0xAC,0x03,0x00,0x00,0x01}); // selected B
        b.AddRange(new byte[]{0x0F,0x85});int jRestoreAddB=b.Count;I32(b,0);
        b.Add(0x5A); // pop edx; selected local negative delta -> skip add only
        b.Add(0xE9);int jDoneBlocked=b.Count;I32(b,0);

        int restoreAdd=b.Count;b.Add(0x5A); // pop edx
        int doAdd=b.Count;
        if(hp) b.AddRange(new byte[]{0x03,0x45,0x08});      // add eax,[ebp+8]
        else   b.AddRange(new byte[]{0x03,0x7D,0x08});      // add edi,[ebp+8]
        int done=b.Count;b.Add(0xE9);int jBack=b.Count;I32(b,0);

        PatchRel(b,jAddDisabled,stub+jAddDisabled+4,stub+doAdd);
        PatchRel(b,jAddNonNeg,stub+jAddNonNeg+4,stub+doAdd);
        PatchRel(b,jRestoreAddOwner,stub+jRestoreAddOwner+4,stub+restoreAdd);
        PatchRel(b,jRestoreAddA,stub+jRestoreAddA+4,stub+restoreAdd);
        PatchRel(b,jRestoreAddB,stub+jRestoreAddB+4,stub+restoreAdd);
        PatchRel(b,jDoneBlocked,stub+jDoneBlocked+4,stub+done);
        PatchRel(b,jBack,stub+jBack+4,target+backOffset);
        return b.ToArray();
    }

'''
s=s[:start]+new_hook+s[end:]

# Make status comments match the implementation and leave v49 F7 / selection refill untouched.
s=s.replace('Our central delta hooks are already event-driven, so external top-up polling is removed.',
'''Specimen B suppresses only the native negative add instruction and preserves the rest of the game function.\n    // External selected-unit polling remains removed.''')

p.write_text(s,encoding='utf-8')
print('patched specimen B native delta')
