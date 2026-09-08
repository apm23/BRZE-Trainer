from pathlib import Path
import re
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('RVA_ADD_HEALTH=0x1CCD4D,RVA_ADD_STAMINA=0x1CCDFB', 'RVA_ADD_HEALTH=0x1CCD78,RVA_ADD_STAMINA=0x1CCE03')
s=re.sub(r'static readonly byte\[\] HpOriginal=\{.*?\};\s*static readonly byte\[\] StOriginal=\{.*?\};', 'static readonly byte[] HpOriginal={0x8B,0x86,0x04,0x04,0x00,0x00};\n    static readonly byte[] StOriginal={0x8B,0xBE,0x08,0x04,0x00,0x00};', s, count=1, flags=re.S)
start=s.index('    static byte[] BuildHook(')
end=s.index('    static byte[] BuildTrainingHook(', start)
new=r'''    static byte[] BuildHook(long stub,long flag,long target,byte[] original,int backOffset)
    {
        // Legacy PageUp architecture remapped to BRZE 1.60.
        // Hook the game's own HP/stamina read/update path where ESI is the unit.
        // No external unit scan: on a selected local unit event, preload the field
        // with a deliberately huge fixed-point value; the game's own update then
        // clamps it back to the unit's real maximum. This mirrors the old trainer.
        bool hp = original.Length==6 && original[1]==0x86;
        uint field = hp ? 0x404u : 0x408u;
        uint preload = hp ? 0x01FFFFFFu : 0x7D00FFFFu;
        var b=new List<byte>();
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)flag);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jDisabled=b.Count;I32(b,0);
        b.Add(0x50); // preserve eax
        b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));
        b.AddRange(new byte[]{0x39,0x86,0x40,0x02,0x00,0x00}); // cmp [esi+240],eax
        b.AddRange(new byte[]{0x0F,0x85});int jOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xBE,0xA8,0x03,0x00,0x00,0x01});
        b.AddRange(new byte[]{0x0F,0x85});int jSelA=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xBE,0xAC,0x03,0x00,0x00,0x01});
        b.AddRange(new byte[]{0x0F,0x85});int jSelB=b.Count;I32(b,0);
        b.Add(0x58);
        b.AddRange(new byte[]{0xC7,0x86});U32(b,field);U32(b,preload); // mov [esi+field],preload
        int originalLabel=b.Count;b.AddRange(original);
        b.Add(0xE9);int jBack=b.Count;I32(b,0);
        int popOriginal=b.Count;b.Add(0x58);b.AddRange(original);b.Add(0xE9);int jBack2=b.Count;I32(b,0);
        PatchRel(b,jDisabled,stub+jDisabled+4,stub+originalLabel);
        PatchRel(b,jOwner,stub+jOwner+4,stub+popOriginal);
        PatchRel(b,jSelA,stub+jSelA+4,stub+popOriginal);
        PatchRel(b,jSelB,stub+jSelB+4,stub+popOriginal);
        PatchRel(b,jBack,stub+jBack+4,target+backOffset);
        PatchRel(b,jBack2,stub+jBack2+4,target+backOffset);
        return b.ToArray();
    }
'''
s=s[:start]+new+s[end:]
s=s.replace('F5 No Stamina Consumption (selected only) — EVENT HOOK','F5 Infinite Stamina (selected only) — LEGACY PAGEUP REMAP')
s=s.replace('F6 No Damage (selected only) — EVENT HOOK','F6 Infinite Health (selected only) — LEGACY PAGEUP REMAP')
p.write_text(s,encoding='utf-8')
print('patched legacy PageUp remap')
