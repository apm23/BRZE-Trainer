from pathlib import Path

# Keep V20 as the generated base, then patch only the replay queue facade and UI version text.
dispatch=Path('IntegratedFrameDispatcherCore.cs')
s=dispatch.read_text(encoding='utf-8')
start=s.find('    public static string QueueReplaySelected(uint ability1,uint ability2,string label)')
end=s.find('    // ---------------- Copy Unit facade',start)
if start<0 or end<0:
    raise SystemExit('V22 could not locate Replay V2 facade')
new=r'''    static string QueueReplayUnitsLocked(IEnumerable<uint> unitAddresses,uint ability1,uint ability2,string label)
    {
        if(!Attach())return $"{label}: game not attached.";
        var units=unitAddresses.Where(u=>u!=0&&R32((long)u+OFF_DEF)!=0).Distinct().Take(MAX_SELECTED).ToList();
        if(units.Count==0)return $"{label}: no valid target units.";
        if(!ValidateApply())return $"{label}: APPLY helper bytes mismatch.";
        if(!Install())return $"{label}: blocked — {status}";
        long c=cave.ToInt64();
        if(R32(c+Q_TYPE)!=QUEUE_IDLE)return $"{label}: shared native queue busy.";
        var bytes=new byte[units.Count*4];
        for(int i=0;i<units.Count;i++)Buffer.BlockCopy(BitConverter.GetBytes(units[i]),0,bytes,i*4,4);
        if(!WriteBytes(c+ENTRIES,bytes))return $"{label}: queue write failed.";
        bool both=ability2!=uint.MaxValue;
        W32(c+Q_COUNT,(uint)units.Count);W32(c+Q_INDEX,0);W32(c+Q_CALLS,0);W32(c+Q_A1,ability1);W32(c+Q_A2,both?ability2:uint.MaxValue);W32(c+Q_MODE,both?2u:1u);
        W32(c+Q_SUCCESS,0);W32(c+Q_FAIL,0);W32(c+Q_TYPE,QUEUE_REPLAY);
        lastQueue=$"{label}: queued {units.Count} explicit targets";
        return lastQueue;
    }

    public static string QueueReplaySelected(uint ability1,uint ability2,string label)
    {
        lock(Sync)return QueueReplayUnitsLocked(SelectedUnits(),ability1,ability2,label);
    }

    public static string QueueReplayUnits(IEnumerable<uint> unitAddresses,uint ability1,uint ability2,string label)
    {
        lock(Sync)return QueueReplayUnitsLocked(unitAddresses,ability1,ability2,label);
    }

'''
s=s[:start]+new+s[end:]
dispatch.write_text(s,encoding='utf-8')

ui=Path('FinalV18MainForm.cs')
u=ui.read_text(encoding='utf-8')
repls=[
    ('Text="BRZE Trainer — V20 Diagnostics";','Text="BRZE Trainer — V22 Diagnostics";'),
    ('Text="BRZE Trainer — V20 Clean";','Text="BRZE Trainer — V22 Clean";'),
    ('V20  ·  COPY UNIT + HERO EFFECT  ·  INTEGRATED','V22  ·  GROUP-SAFE HERO EFFECT + COPY UNIT  ·  INTEGRATED')
]
for old,newtext in repls:
    if old not in u: raise SystemExit('V22 title marker missing: '+old)
    u=u.replace(old,newtext,1)
ui.write_text(u,encoding='utf-8')
print('V22 generated: explicit replay targets + signature-safe concurrent Hero Effect groups + V20 layout preserved')
