using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal enum DirectHeroMode { Issyl, Grayback, Both }

internal sealed class HeroAbilityHold
{
    public required uint Id;
    public required uint Baseline;
    public required uint Preferred;
    public required double BaselineWallSec;
    public required string Name;
    public uint Config;
    public uint Desired;
    public decimal Seconds;
    public bool Held;
    public bool Patched;
}

internal sealed class HeroEffectTrack
{
    public uint Unit,Def,Owner,Ability,Record;
    public int State; // 0 waiting signature, 1 active, 2 complete, 3 skipped
    public int MissingStable;
    public long CreatedStamp;
}

internal sealed class HeroReplayBatch
{
    public required List<(uint Unit,uint Def,uint Owner)> Targets;
    public required uint Ability1;
    public required uint Ability2;
    public required uint[] Abilities;
    public required string Label;
}

internal readonly record struct HeroResetCandidate(uint Unit,uint Def,uint Owner,uint Ability,uint Record,uint OldStamp);

internal static class HeroEffectDirectCoreV30
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll",SetLastError=true)] static extern UIntPtr VirtualQueryEx(IntPtr h,IntPtr addr,out MEMORY_BASIC_INFORMATION mbi,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public UIntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    const uint ACCESS=0x10|0x20|0x8|0x400;
    const uint MEM_COMMIT=0x1000,PAGE_GUARD=0x100,PAGE_NOACCESS=0x01;
    const int RVA_SELECTION_LIST=0x441708;
    const int RVA_CURRENT_TICK=0x440A3C; // V28 exact register-flow proof, V29 runtime reset proof
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    const int CONFIG_DURATION_OFF=0x0E0;
    const int ABILITY_OFF=0x058,TARGET_OFF=0x17C,STAMP_OFF=0x194,CONFIG_OFF=0x1F4,ROOT_SCAN=0x600;
    static readonly int[] TRANSIENT_ROOTS={0x1E4,0x1E8,0x20C,0x210};
    const uint ISSYL_ID=0xA5,GRAYBACK_ID=0xC0;
    const uint ISSYL_BASE=15000,GRAYBACK_BASE=60000;
    const uint ISSYL_KNOWN=0x227F4664,GRAYBACK_KNOWN=0x227F9470;
    const double ISSYL_BASE_WALL_SEC=10.7271,GRAYBACK_BASE_WALL_SEC=42.2221;
    const uint MIN_NOMINAL=1000,MAX_NOMINAL=600000;
    const int MAX_SELECTED=120,EXPIRY_MISSING_SAMPLES=4;

    static readonly object Sync=new();
    static readonly List<HeroEffectTrack> Tracks=new();
    static readonly Queue<HeroReplayBatch> PendingBatches=new();
    static readonly HeroAbilityHold Issyl=new(){Id=ISSYL_ID,Baseline=ISSYL_BASE,Preferred=ISSYL_KNOWN,BaselineWallSec=ISSYL_BASE_WALL_SEC,Name="ISSYL"};
    static readonly HeroAbilityHold Gray=new(){Id=GRAYBACK_ID,Baseline=GRAYBACK_BASE,Preferred=GRAYBACK_KNOWN,BaselineWallSec=GRAYBACK_BASE_WALL_SEC,Name="GRAYBACK"};

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static int processId;
    static DateTime lastTry;
    static string status="READY — select unit(s), choose duration, then APPLY. Reapplying an active same buff restarts its clock without stacking.";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        DetachHandle();
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<120)return false;
        lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0){status="WAITING — Battle_Realms_F.exe is not running.";return false;}
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}
        catch{process=null;status="ERROR — cannot resolve module base.";return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        if(h==IntPtr.Zero){status="ERROR — OpenProcess failed.";process=null;moduleBase=0;return false;}
        if(processId!=0&&processId!=process.Id)ResetRuntimeStateNoWrite();
        processId=process.Id;
        return true;
    }

    static void DetachHandle()
    {
        try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}
        h=IntPtr.Zero;process=null;moduleBase=0;
    }

    static void ResetRuntimeStateNoWrite()
    {
        Tracks.Clear();PendingBatches.Clear();
        foreach(var a in new[]{Issyl,Gray}){a.Config=0;a.Desired=0;a.Seconds=0;a.Held=false;a.Patched=false;}
    }

    static bool ReadExact(long addr,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(addr),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long addr){var b=new byte[4];return ReadExact(addr,b)?U32(b,0):0;}
    static bool W32(long addr,uint v)
    {
        if(h==IntPtr.Zero)return false;var b=BitConverter.GetBytes(v);
        return WriteProcessMemory(h,A(addr),b,4,out var n)&&n.ToInt64()==4&&R32(addr)==v;
    }
    static bool Readable(uint protect){if((protect&PAGE_GUARD)!=0)return false;uint p=protect&0xFF;return p!=0&&p!=PAGE_NOACCESS;}

    static bool ValidateConfig(uint addr,HeroAbilityHold a,uint expected)
        =>addr>=0x10000&&R32(addr)==a.Id&&R32((long)addr+CONFIG_DURATION_OFF)==expected;

    static uint ResolveConfig(HeroAbilityHold a)
    {
        if(a.Config!=0&&ValidateConfig(a.Config,a,a.Baseline))return a.Config;
        if(ValidateConfig(a.Preferred,a,a.Baseline))return a.Config=a.Preferred;
        uint p=ScanForConfig(a,0x18000000u,0x30000000u);
        if(p==0)p=ScanForConfig(a,0x00010000u,0x7FFF0000u);
        return a.Config=p;
    }

    static uint ScanForConfig(HeroAbilityHold a,uint low,uint high)
    {
        if(h==IntPtr.Zero)return 0;
        var hits=new List<uint>();ulong cursor=low,limit=high;int mbiSize=Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();
        const int CHUNK=1024*1024,OVERLAP=0xE4;
        while(cursor<limit)
        {
            if(VirtualQueryEx(h,A((long)cursor),out var mbi,(UIntPtr)mbiSize)==UIntPtr.Zero)break;
            ulong baseAddr=unchecked((uint)mbi.BaseAddress.ToInt64()),region=mbi.RegionSize.ToUInt64();
            if(region==0){cursor+=0x1000;continue;}
            ulong end=Math.Min(baseAddr+region,limit);
            if(mbi.State==MEM_COMMIT&&Readable(mbi.Protect)&&end>low&&baseAddr<limit)
            {
                for(ulong pos=Math.Max(baseAddr,low);pos<end;pos+=(ulong)CHUNK)
                {
                    int want=(int)Math.Min((ulong)(CHUNK+OVERLAP),end-pos);if(want<CONFIG_DURATION_OFF+4)continue;
                    var buf=new byte[want];
                    if(!ReadProcessMemory(h,A((long)pos),buf,want,out var got)||got.ToInt64()<CONFIG_DURATION_OFF+4)continue;
                    int n=(int)got.ToInt64();
                    for(int o=0;o<=n-(CONFIG_DURATION_OFF+4);o+=4)
                        if(U32(buf,o)==a.Id&&U32(buf,o+CONFIG_DURATION_OFF)==a.Baseline)hits.Add((uint)(pos+(ulong)o));
                }
            }
            ulong next=baseAddr+region;cursor=next>cursor?next:cursor+0x1000;
        }
        if(hits.Count==0)return 0;
        return hits.OrderBy(x=>Math.Abs((long)x-a.Preferred)).FirstOrDefault(x=>ValidateConfig(x,a,a.Baseline));
    }

    static uint DesiredForSeconds(HeroAbilityHold a,decimal seconds)
    {
        double v=(double)seconds*a.Baseline/a.BaselineWallSec;
        return Math.Clamp((uint)Math.Round(v,MidpointRounding.AwayFromZero),MIN_NOMINAL,MAX_NOMINAL);
    }

    static bool PendingUses(uint ability)=>PendingBatches.Any(b=>b.Abilities.Contains(ability));

    static bool AcquireHold(HeroAbilityHold a,decimal seconds,HashSet<uint> selected,out bool newlyHeld,out string error)
    {
        newlyHeld=false;error="";uint desired=DesiredForSeconds(a,seconds);
        if(a.Held)
        {
            if(a.Config==0||R32(a.Config)!=a.Id){error=$"{a.Name} hold identity was lost";return false;}
            uint cur=R32((long)a.Config+CONFIG_DURATION_OFF);
            if(cur!=a.Desired){error=$"{a.Name} held config changed unexpectedly ({cur}, expected {a.Desired})";return false;}
            if(desired!=a.Desired)
            {
                bool otherTracked=Tracks.Any(t=>t.Ability==a.Id&&t.State<2&&!selected.Contains(t.Unit));
                if(otherTracked||PendingUses(a.Id))
                {
                    error=$"{a.Name} has another active group at ~{a.Seconds:0.#}s. Select that group too or wait before changing its duration.";
                    return false;
                }
                if(!W32((long)a.Config+CONFIG_DURATION_OFF,desired)){error=$"{a.Name} duration retune failed";return false;}
                a.Desired=desired;a.Seconds=seconds;a.Patched=desired!=a.Baseline;
            }
            return true;
        }

        uint cfg=ResolveConfig(a);
        if(cfg==0){error=$"could not safely resolve {a.Name} config (ID 0x{a.Id:X2}, baseline {a.Baseline})";return false;}
        if(!ValidateConfig(cfg,a,a.Baseline)){error=$"{a.Name} baseline guard failed";return false;}
        if(desired!=a.Baseline&&!W32((long)cfg+CONFIG_DURATION_OFF,desired)){error=$"{a.Name} duration write/verify failed";return false;}
        a.Config=cfg;a.Desired=desired;a.Seconds=seconds;a.Held=true;a.Patched=desired!=a.Baseline;newlyHeld=true;return true;
    }

    static bool RestoreHold(HeroAbilityHold a)
    {
        if(!a.Held)return true;
        bool ok=true;
        if(a.Config==0||R32(a.Config)!=a.Id)ok=false;
        else
        {
            uint cur=R32((long)a.Config+CONFIG_DURATION_OFF);
            if(cur==a.Baseline)ok=true;
            else if(cur==a.Desired)ok=W32((long)a.Config+CONFIG_DURATION_OFF,a.Baseline);
            else ok=false;
        }
        if(ok){a.Held=false;a.Patched=false;a.Desired=0;a.Seconds=0;}
        return ok;
    }

    static List<(uint Unit,uint Def,uint Owner)> SelectedUnits()
    {
        var result=new List<(uint,uint,uint)>();if(!Attach())return result;
        long list=moduleBase+RVA_SELECTION_LIST;uint count=R32(list+0x18),node=R32(list);
        var seenNodes=new HashSet<uint>();var seenUnits=new HashSet<uint>();int limit=(int)Math.Min(count,MAX_SELECTED);
        for(int i=0;i<limit&&node!=0;i++)
        {
            if(!seenNodes.Add(node))break;uint next=R32(node),unit=R32((long)node+0x08);
            if(unit!=0&&seenUnits.Add(unit))
            {
                uint def=R32((long)unit+OFF_DEF),owner=R32((long)unit+OFF_OWNER);
                if(def!=0)result.Add((unit,def,owner));
            }
            node=next;
        }
        return result;
    }

    static bool SignatureMatches(uint addr,uint ability,uint unit)
        =>Ptr(addr)&&R32((long)addr+ABILITY_OFF)==ability&&R32((long)addr+TARGET_OFF)==unit;

    static bool TryFindRecord(uint unit,uint ability,out uint addr)
    {
        addr=0;var seen=new HashSet<uint>();
        foreach(int off in TRANSIENT_ROOTS)
        {
            uint root=R32((long)unit+off);if(!Ptr(root)||!seen.Add(root))continue;
            if(SignatureMatches(root,ability,unit)){addr=root;return true;}
            var b=new byte[ROOT_SCAN];if(!ReadExact(root,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=U32(b,o);if(!Ptr(p)||!seen.Add(p))continue;
                if(SignatureMatches(p,ability,unit)){addr=p;return true;}
            }
        }
        return false;
    }

    static HeroEffectTrack? LiveTrack(uint unit,uint ability)=>Tracks.LastOrDefault(t=>t.Unit==unit&&t.Ability==ability&&t.State<2);
    static bool UnitValid(HeroEffectTrack t)=>R32((long)t.Unit+OFF_DEF)==t.Def&&R32((long)t.Unit+OFF_OWNER)==t.Owner;
    static double AgeSec(HeroEffectTrack t)=>(Stopwatch.GetTimestamp()-t.CreatedStamp)/(double)Stopwatch.Frequency;

    static IReadOnlyList<HeroAbilityHold> AbilitiesFor(DirectHeroMode mode)
        =>mode==DirectHeroMode.Issyl?new[]{Issyl}:mode==DirectHeroMode.Grayback?new[]{Gray}:new[]{Gray,Issyl};
    static string ModeName(DirectHeroMode mode)=>mode==DirectHeroMode.Issyl?"ISSYL":mode==DirectHeroMode.Grayback?"GRAYBACK":"BOTH";

    static bool PreflightReset(uint unit,uint def,uint owner,uint ability,uint currentTick,out HeroResetCandidate candidate,out string error)
    {
        candidate=default;error="";
        if(!TryFindRecord(unit,ability,out uint rec))return false;
        uint cfg=R32((long)rec+CONFIG_OFF),cfgId=Ptr(cfg)?R32(cfg):0,old=R32((long)rec+STAMP_OFF);
        if(cfgId!=ability){error=$"effect config identity mismatch for 0x{unit:X8}/0x{ability:X2}";return true;}
        if(old==0||currentTick<old){error=$"invalid effect clock for 0x{unit:X8}/0x{ability:X2} (start {old}, now {currentTick})";return true;}
        candidate=new(unit,def,owner,ability,rec,old);return true;
    }

    static void EnsureActiveTrack(HeroResetCandidate c,long now)
    {
        var t=LiveTrack(c.Unit,c.Ability);
        if(t==null)
        {
            Tracks.Add(new HeroEffectTrack{Unit=c.Unit,Def=c.Def,Owner=c.Owner,Ability=c.Ability,Record=c.Record,State=1,MissingStable=0,CreatedStamp=now});
            return;
        }
        t.Def=c.Def;t.Owner=c.Owner;t.Record=c.Record;t.State=1;t.MissingStable=0;t.CreatedStamp=now;
    }

    static bool ApplyResetBatch(List<HeroResetCandidate> resets,uint currentTick,out string error)
    {
        error="";var done=new List<HeroResetCandidate>();
        foreach(var c in resets)
        {
            if(!SignatureMatches(c.Record,c.Ability,c.Unit)||R32((long)c.Record+STAMP_OFF)!=c.OldStamp||!W32((long)c.Record+STAMP_OFF,currentTick)||!SignatureMatches(c.Record,c.Ability,c.Unit))
            {
                foreach(var d in done)
                    if(SignatureMatches(d.Record,d.Ability,d.Unit)&&R32((long)d.Record+STAMP_OFF)==currentTick)W32((long)d.Record+STAMP_OFF,d.OldStamp);
                error=$"clock reset verification failed for unit 0x{c.Unit:X8} ability 0x{c.Ability:X2}; earlier resets rolled back where still safe";
                return false;
            }
            done.Add(c);
        }
        long now=Stopwatch.GetTimestamp();foreach(var c in resets)EnsureActiveTrack(c,now);
        return true;
    }

    static List<HeroReplayBatch> BuildReplayBatches(List<(uint Unit,uint Def,uint Owner)> selected,Dictionary<uint,HashSet<uint>> missing,DirectHeroMode mode,decimal seconds)
    {
        var both=new List<(uint,uint,uint)>();var issylOnly=new List<(uint,uint,uint)>();var grayOnly=new List<(uint,uint,uint)>();
        foreach(var u in selected)
        {
            missing.TryGetValue(u.Unit,out var set);set??=new HashSet<uint>();
            bool i=set.Contains(ISSYL_ID),g=set.Contains(GRAYBACK_ID);
            if(i&&g)both.Add(u);else if(i)issylOnly.Add(u);else if(g)grayOnly.Add(u);
        }
        var batches=new List<HeroReplayBatch>();string root=$"{ModeName(mode)} ~{seconds:0.#}s";
        if(both.Count>0)batches.Add(new HeroReplayBatch{Targets=both,Ability1=GRAYBACK_ID,Ability2=ISSYL_ID,Abilities=new[]{GRAYBACK_ID,ISSYL_ID},Label=root+" missing BOTH"});
        if(grayOnly.Count>0)batches.Add(new HeroReplayBatch{Targets=grayOnly,Ability1=GRAYBACK_ID,Ability2=uint.MaxValue,Abilities=new[]{GRAYBACK_ID},Label=root+" missing GRAYBACK"});
        if(issylOnly.Count>0)batches.Add(new HeroReplayBatch{Targets=issylOnly,Ability1=ISSYL_ID,Ability2=uint.MaxValue,Abilities=new[]{ISSYL_ID},Label=root+" missing ISSYL"});
        return batches;
    }

    static bool StartBatch(HeroReplayBatch b,out string message)
    {
        message=IntegratedFrameDispatcherCore.QueueReplayUnits(b.Targets.Select(x=>x.Unit),b.Ability1,b.Ability2,b.Label);
        if(!message.Contains("queued",StringComparison.OrdinalIgnoreCase))return false;
        long now=Stopwatch.GetTimestamp();
        foreach(var u in b.Targets)
            foreach(uint ability in b.Abilities)
            {
                var t=LiveTrack(u.Unit,ability);
                if(t==null)Tracks.Add(new HeroEffectTrack{Unit=u.Unit,Def=u.Def,Owner=u.Owner,Ability=ability,State=0,CreatedStamp=now});
                else{t.Def=u.Def;t.Owner=u.Owner;t.Record=0;t.State=0;t.MissingStable=0;t.CreatedStamp=now;}
            }
        return true;
    }

    static void RollbackResets(List<HeroResetCandidate> resets,uint currentTick)
    {
        foreach(var c in resets)
            if(SignatureMatches(c.Record,c.Ability,c.Unit)&&R32((long)c.Record+STAMP_OFF)==currentTick)W32((long)c.Record+STAMP_OFF,c.OldStamp);
    }

    public static string Start(DirectHeroMode mode,decimal seconds)
    {
        lock(Sync)
        {
            if(seconds<1m||seconds>420m)return status="BLOCKED — duration must be 1–420 seconds.";
            if(IntegratedFrameDispatcherCore.NativeQueueBusy())return status="BLOCKED — shared native queue is busy; retry in a moment.";
            if(!Attach())return status;

            UpdateTracks();ReleaseIdleHolds();
            var selected=SelectedUnits();if(selected.Count==0)return status="BLOCKED — no valid units selected.";
            var selectedSet=selected.Select(x=>x.Unit).ToHashSet();var abilities=AbilitiesFor(mode);
            uint currentTick=R32(moduleBase+RVA_CURRENT_TICK);if(currentTick==0)return status="BLOCKED — BRZE current simulation tick is unavailable.";

            var resets=new List<HeroResetCandidate>();var missing=new Dictionary<uint,HashSet<uint>>();int pendingExisting=0;
            foreach(var u in selected)
            {
                foreach(var a in abilities)
                {
                    if(PreflightReset(u.Unit,u.Def,u.Owner,a.Id,currentTick,out var c,out string preErr))
                    {
                        if(preErr.Length>0)return status="BLOCKED — "+preErr;
                        resets.Add(c);continue;
                    }
                    if(LiveTrack(u.Unit,a.Id)!=null){pendingExisting++;continue;}
                    if(!missing.TryGetValue(u.Unit,out var set))missing[u.Unit]=set=new HashSet<uint>();set.Add(a.Id);
                }
            }

            bool newIssyl=false,newGray=false;
            foreach(var a in abilities)
            {
                if(!AcquireHold(a,seconds,selectedSet,out bool newly,out string error))
                {
                    if(newIssyl&&!Tracks.Any(t=>t.Ability==ISSYL_ID&&t.State<2)&&!PendingUses(ISSYL_ID))RestoreHold(Issyl);
                    if(newGray&&!Tracks.Any(t=>t.Ability==GRAYBACK_ID&&t.State<2)&&!PendingUses(GRAYBACK_ID))RestoreHold(Gray);
                    return status="BLOCKED — "+error;
                }
                if(a.Id==ISSYL_ID)newIssyl|=newly;else newGray|=newly;
            }

            // Revalidate after any duration-config mutation, then restart existing instances at the exact V28/V29-proven BRZE clock.
            foreach(var c in resets)
                if(!SignatureMatches(c.Record,c.Ability,c.Unit)||R32((long)c.Record+STAMP_OFF)!=c.OldStamp)
                {
                    if(newIssyl&&!Tracks.Any(t=>t.Ability==ISSYL_ID&&t.State<2))RestoreHold(Issyl);
                    if(newGray&&!Tracks.Any(t=>t.Ability==GRAYBACK_ID&&t.State<2))RestoreHold(Gray);
                    return status="BLOCKED — effect changed during reset preflight; retry once.";
                }
            if(!ApplyResetBatch(resets,currentTick,out string resetErr))
            {
                if(newIssyl&&!Tracks.Any(t=>t.Ability==ISSYL_ID&&t.State<2))RestoreHold(Issyl);
                if(newGray&&!Tracks.Any(t=>t.Ability==GRAYBACK_ID&&t.State<2))RestoreHold(Gray);
                return status="RESET FAILED — "+resetErr;
            }

            var batches=BuildReplayBatches(selected,missing,mode,seconds);
            PendingBatches.Clear();
            if(batches.Count>0)
            {
                if(!StartBatch(batches[0],out string q))
                {
                    RollbackResets(resets,currentTick);
                    if(newIssyl&&!Tracks.Any(t=>t.Ability==ISSYL_ID&&t.State<2))RestoreHold(Issyl);
                    if(newGray&&!Tracks.Any(t=>t.Ability==GRAYBACK_ID&&t.State<2))RestoreHold(Gray);
                    return status="APPLY FAILED — "+q;
                }
                for(int i=1;i<batches.Count;i++)PendingBatches.Enqueue(batches[i]);
            }

            int missingEffects=missing.Values.Sum(x=>x.Count);
            status=$"APPLIED {ModeName(mode)} ~{seconds:0.#}s · selected {selected.Count} · restarted {resets.Count} active effect(s) · fresh native apply {missingEffects} effect(s)"+(pendingExisting>0?$" · {pendingExisting} already pending":"")+(PendingBatches.Count>0?$" · {PendingBatches.Count} safe replay batch(es) queued next":"")+". No same-effect stacking.";
            return status;
        }
    }

    static void UpdateTracks()
    {
        foreach(var t in Tracks.Where(x=>x.State<2).ToList())
        {
            if(!UnitValid(t)){t.State=3;continue;}
            if(t.State==0)
            {
                if(TryFindRecord(t.Unit,t.Ability,out uint rec)){t.Record=rec;t.State=1;t.MissingStable=0;}
                else if(AgeSec(t)>=3.0&&!IntegratedFrameDispatcherCore.NativeQueueBusy())t.State=3;
                continue;
            }
            if(SignatureMatches(t.Record,t.Ability,t.Unit)){t.MissingStable=0;continue;}
            if(TryFindRecord(t.Unit,t.Ability,out uint moved)){t.Record=moved;t.MissingStable=0;continue;}
            t.MissingStable++;
            if(t.MissingStable>=EXPIRY_MISSING_SAMPLES)t.State=2;
        }
    }

    static bool StartNextPendingBatch()
    {
        if(PendingBatches.Count==0||IntegratedFrameDispatcherCore.NativeQueueBusy())return true;
        var b=PendingBatches.Peek();
        if(!StartBatch(b,out string q)){status="WARNING — pending Hero Effect batch failed: "+q;return false;}
        PendingBatches.Dequeue();return true;
    }

    static string HoldSummary(HeroAbilityHold a)
    {
        int waiting=Tracks.Count(t=>t.Ability==a.Id&&t.State==0),active=Tracks.Count(t=>t.Ability==a.Id&&t.State==1);
        if(!a.Held)return $"{a.Name}: ready";
        return $"{a.Name}: ~{a.Seconds:0.#}s held · waiting {waiting} · active {active}";
    }

    static void ReleaseIdleHolds()
    {
        foreach(var a in new[]{Issyl,Gray})
        {
            if(!a.Held)continue;
            if(Tracks.Any(t=>t.Ability==a.Id&&t.State<2)||PendingUses(a.Id))continue;
            if(!RestoreHold(a))status=$"WARNING — {a.Name} tracking ended but config restore failed. Restart BRZE before changing {a.Name} duration.";
        }
        Tracks.RemoveAll(t=>t.State>=2&&!((t.Ability==ISSYL_ID?Issyl:Gray).Held));
    }

    public static string Tick()
    {
        lock(Sync)
        {
            if(!Attach())return status;
            UpdateTracks();StartNextPendingBatch();ReleaseIdleHolds();
            int live=Tracks.Count(t=>t.State<2);
            if(live==0&&PendingBatches.Count==0&&status.StartsWith("APPLIED",StringComparison.OrdinalIgnoreCase))status="READY — previous Hero Effect group(s) finished and configs restored.";
            else if(live>0||PendingBatches.Count>0)status=$"{HoldSummary(Issyl)}   |   {HoldSummary(Gray)}   |   tracked {live} · pending batches {PendingBatches.Count}. Reapply same buff = restart clock; no stack.";
            return status;
        }
    }

    public static string Status(){lock(Sync)return status;}

    public static void Shutdown()
    {
        lock(Sync)
        {
            PendingBatches.Clear();
            if(Attach()){RestoreHold(Issyl);RestoreHold(Gray);}
            Tracks.Clear();ResetRuntimeStateNoWrite();DetachHandle();processId=0;
            status="READY — select unit(s), choose duration, then APPLY.";
        }
    }
}
