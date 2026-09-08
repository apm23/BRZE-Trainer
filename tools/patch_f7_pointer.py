from pathlib import Path
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')
a=s.index('    static void ApplyInstantUnitTraining(uint lid)')
b=s.index('    public static string Apply(',a)
new='''    static void ApplyInstantUnitTraining(uint lid)
    {
        if((DateTime.UtcNow-lastBuildingScan).TotalMilliseconds<50)return;
        lastBuildingScan=DateTime.UtcNow; trainingBuildings=0;
        uint mgr=R32(moduleBase+RVA_BUILDING_POOL); if(mgr==0)return;
        var seen=new HashSet<uint>();
        ScanBuildingPointerTable(mgr,lid,seen,512);
        for(int off=0;off<=0x80;off+=4)
        {
            uint table=R32((long)mgr+off);
            if(table>=0x10000)ScanBuildingPointerTable(table,lid,seen,512);
        }
    }
    static void ScanBuildingPointerTable(uint table,uint lid,HashSet<uint> seen,int max)
    {
        for(int i=0;i<max;i++)
        {
            uint obj=R32((long)table+i*4);
            if(obj<0x10000||!seen.Add(obj))continue;
            if(R32((long)obj+OFF_BUILD_OWNER)!=lid)continue;
            uint type=R32((long)obj+OFF_TRAIN_TYPE);
            if(type==0xFFFFFFFF)continue;
            uint prog=R32((long)obj+OFF_TRAIN_PROGRESS);
            if(prog>TRAIN_COMPLETE_FIXED)continue;
            trainingBuildings++;
            Boost(obj,lid);
        }
    }
'''
s=s[:a]+new+s[b:]
s=s.replace('if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,9_999_999);trainingBuildings=0;','if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,9_999_999);')
s=s.replace('selected:{selectedLocked} | F5:', 'selected:{selectedLocked} trainFound:{trainingBuildings} | F5:')
p.write_text(s,encoding='utf-8')
