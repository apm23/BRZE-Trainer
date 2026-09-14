using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectSignatureProbeV6;

internal readonly record struct ProbeSnapshot(string Game,string Phase,bool Done,IReadOnlyList<string> Lines);

internal sealed class FieldStat
{
    public required int Offset { get; init; }
    public uint FirstRaw { get; private set; }
    public uint LastRaw { get; private set; }
    public int Seen { get; private set; }
    public int Changes { get; private set; }
    public int Flips { get; private set; }
    public int LastDir { get; private set; }
    public long SumAbsDelta { get; private set; }
    public long MinAbsDelta { get; private set; }=long.MaxValue;
    public long MaxAbsDelta { get; private set; }

    static float F(uint raw)=>BitConverter.Int32BitsToSingle(unchecked((int)raw));
    static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    public void Add(uint raw)
    {
        if(Seen==0){FirstRaw=LastRaw=raw;Seen=1;return;}
        Seen++;
        if(raw==LastRaw)return;
        long d=(long)unchecked((int)raw)-unchecked((int)LastRaw);
        int dir=d>0?1:d<0?-1:0;
        if(dir!=0&&LastDir!=0&&dir!=LastDir)Flips++;
        if(dir!=0)LastDir=dir;
        long a=Math.Abs(d);
        if(a<MinAbsDelta)MinAbsDelta=a;
        if(a>MaxAbsDelta)MaxAbsDelta=a;
        SumAbsDelta+=a;
        Changes++;
        LastRaw=raw;
    }

    public double Score()
    {
        if(Seen<2||Changes==0)return double.NegativeInfinity;
        double rate=(double)Changes/Math.Max(1,Seen-1);
        double s=Changes*2.0+rate*180.0-Flips*45.0;
        if(Flips==0)s+=180;
        if(rate>=0.50)s+=80;
        if(rate>=0.80)s+=60;
        if(Ptr(FirstRaw)&&Ptr(LastRaw))s-=180;
        return s;
    }

    public bool StaticDurationLike(int totalSamples)
    {
        if(Seen!=totalSamples||Seen==0||Changes!=0||FirstRaw==0||Ptr(FirstRaw))return false;
        int i=unchecked((int)FirstRaw);
        if(i>0&&i<=600000)return true;
        float f=F(FirstRaw);
        return Finite(f)&&f>=0.01f&&f<=600f;
    }

    public string DynamicLine()
    {
        float a=F(FirstRaw),b=F(LastRaw);
        double rate=(double)Changes/Math.Max(1,Seen-1)*100.0;
        string d=Changes==0?"-":$"avgΔ {(double)SumAbsDelta/Changes:0.##} minΔ {(MinAbsDelta==long.MaxValue?0:MinAbsDelta)} maxΔ {MaxAbsDelta}";
        return $"score {Score(),7:0.0} | +0x{Offset:X3} | seen {Seen,4} chg {Changes,4} ({rate,5:0.0}%) flip {Flips,3} | 0x{FirstRaw:X8}->0x{LastRaw:X8} | int {unchecked((int)FirstRaw)}->{unchecked((int)LastRaw)} | float {a:0.#####}->{b:0.#####} | {d}";
    }

    public string StaticLine()
    {
        float f=F(FirstRaw);
        return $"+0x{Offset:X3} | raw 0x{FirstRaw:X8} | int {unchecked((int)FirstRaw)} | float {f:0.#####} | seen {Seen}";
    }
}

internal static class ProbeCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_READ=0x0010;
    const uint PROCESS_QUERY_INFORMATION=0x0400;
    const uint ACCESS=PROCESS_VM_READ|PROCESS_QUERY_INFORMATION;

    const int RVA_SELECTION_LIST=0x441708;
    const int OFF_DEF=0x74;
    const int OFF_OWNER=0x240;
    const int ROOT_1E4=0x1E4;
    const int ROOT_1E8=0x1E8;
    const int ROOT_20C=0x20C;
    const int ROOT_210=0x210;
    const int ROOT_214=0x214;
    const int ROOT_SCAN=0x600;
    const int RECORD_SCAN=0x240;
    const int ABILITY_OFF=0x58;
    const int TARGET_OFF=0x17C;
    const uint ISSYL_ID=0xA5;
    const int EXPIRY_STABLE_SAMPLES=4;

    static readonly int[] LifecycleOffsets={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210,ROOT_214};
    static readonly int[] TransientRoots={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210};
    static readonly int[] FocusOffsets={0x018,0x020,0x024,0x028,0x02C,0x030,0x058,0x17C,0x188,0x18C,0x194,0x1F8,0x204,0x210};
    static readonly object sync=new();

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status="not attached";

    static uint unit,unitDef,owner;
    static readonly Dictionary<int,uint> baselineRoots=new();
    static readonly Dictionary<int,uint> startRoots=new();
    static readonly Dictionary<int,uint> endRoots=new();
    static readonly Dictionary<int,FieldStat> stats=new();

    static bool armed,active,done;
    static int waitPolls,effectPolls,expiryStable,recordSamples,recordUnreadable;
    static long effectStartStamp,effectEndStamp,recordFoundStamp;
    static uint recordAddr;
    static string recordPath="not found";
    static string phase="IDLE — select one clean target and ARM";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    static bool ReadExact(long address,byte[] buffer)
    {
        if(h==IntPtr.Zero)return false;
        return ReadProcessMemory(h,A(address),buffer,buffer.Length,out var n)&&n.ToInt64()==buffer.Length;
    }

    static uint ReadU32(long address)
    {
        var b=new byte[4];
        return ReadExact(address,b)?U32(b,0):0;
    }

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach();
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<200)return false;
        lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0){status="waiting for Battle_Realms_F.exe";return false;}
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}
        catch{process=null;status="cannot resolve module base";return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        if(h==IntPtr.Zero){status="OpenProcess READ failed";return false;}
        status=$"READ-ONLY attached PID {process.Id} base 0x{moduleBase:X8}";
        return true;
    }

    static void Detach()
    {
        try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}
        process=null;h=IntPtr.Zero;moduleBase=0;status="not attached";
    }

    static (uint unit,uint count) FirstSelectedUnit()
    {
        if(!Attach())return default;
        var list=new byte[0x1C];
        if(!ReadExact(moduleBase+RVA_SELECTION_LIST,list))return default;
        uint node=U32(list,0),count=U32(list,0x18);
        if(node==0||count==0)return (0,count);
        var nb=new byte[0x0C];
        if(!ReadExact(node,nb))return (0,count);
        return (U32(nb,0x08),count);
    }

    static bool PinnedUnitValid()=>unit!=0&&h!=IntPtr.Zero&&ReadU32((long)unit+OFF_DEF)==unitDef&&ReadU32((long)unit+OFF_OWNER)==owner;

    static Dictionary<int,uint> CaptureRoots()
    {
        var d=new Dictionary<int,uint>();
        foreach(int off in LifecycleOffsets)d[off]=ReadU32((long)unit+off);
        return d;
    }

    static bool SameRoots(Dictionary<int,uint> a,Dictionary<int,uint> b)
    {
        foreach(int off in LifecycleOffsets)if(a[off]!=b[off])return false;
        return true;
    }

    static bool EffectPattern(Dictionary<int,uint> now)
    {
        int changed=0;
        foreach(int off in LifecycleOffsets)if(now[off]!=baselineRoots[off])changed++;
        bool transient=TransientRoots.Any(off=>Ptr(now[off])&&now[off]!=baselineRoots[off]);
        return changed>=2&&transient;
    }

    static bool SignatureMatches(uint addr)
    {
        if(!Ptr(addr))return false;
        return ReadU32((long)addr+ABILITY_OFF)==ISSYL_ID&&ReadU32((long)addr+TARGET_OFF)==unit;
    }

    static bool TryFindRecord(Dictionary<int,uint> roots,out uint addr,out string path)
    {
        addr=0;path="not found";
        var seen=new HashSet<uint>();
        foreach(int rootOff in TransientRoots)
        {
            uint root=roots[rootOff];
            if(!Ptr(root)||!seen.Add(root))continue;
            if(SignatureMatches(root)){addr=root;path=$"Unit+0x{rootOff:X3}";return true;}
            var b=new byte[ROOT_SCAN];
            if(!ReadExact(root,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=U32(b,o);
                if(!Ptr(p)||!seen.Add(p))continue;
                if(SignatureMatches(p))
                {
                    addr=p;path=$"Unit+0x{rootOff:X3}->+0x{o:X3}";return true;
                }
            }
        }
        return false;
    }

    static FieldStat Stat(int off)
    {
        if(stats.TryGetValue(off,out var s))return s;
        s=new FieldStat{Offset=off};stats[off]=s;return s;
    }

    static void SampleRecord()
    {
        if(recordAddr==0)return;
        var b=new byte[RECORD_SCAN];
        if(!ReadExact(recordAddr,b)){recordUnreadable++;return;}
        recordSamples++;
        for(int o=0;o<=b.Length-4;o+=4)Stat(o).Add(U32(b,o));
    }

    static void BeginEffect(Dictionary<int,uint> roots)
    {
        active=true;done=false;effectPolls=0;expiryStable=0;recordSamples=0;recordUnreadable=0;recordAddr=0;recordPath="not found";stats.Clear();
        effectStartStamp=Stopwatch.GetTimestamp();recordFoundStamp=0;
        startRoots.Clear();foreach(var kv in roots)startRoots[kv.Key]=kv.Value;
        phase="ISSYL LIFECYCLE AUTO-DETECTED — locating A5 + target signature...";
    }

    static void FinishEffect(Dictionary<int,uint> roots)
    {
        active=false;done=true;effectEndStamp=Stopwatch.GetTimestamp();
        endRoots.Clear();foreach(var kv in roots)endRoots[kv.Key]=kv.Value;
        phase=recordAddr==0
            ?"COMPLETE — lifecycle ended but A5+target record was not captured. COPY REPORT."
            :$"COMPLETE — A5+target record tracked at 0x{recordAddr:X8}. COPY REPORT.";
    }

    public static string Arm()
    {
        lock(sync)
        {
            if(!Attach())return phase="ARM failed — "+status;
            var (u,c)=FirstSelectedUnit();
            if(c!=1||u==0)return phase=$"ARM blocked — select exactly ONE clean target (selected {c})";
            unit=u;unitDef=ReadU32((long)u+OFF_DEF);owner=ReadU32((long)u+OFF_OWNER);
            if(unitDef==0)return phase="ARM blocked — selected object has no UnitDef";
            baselineRoots.Clear();foreach(var kv in CaptureRoots())baselineRoots[kv.Key]=kv.Value;
            startRoots.Clear();endRoots.Clear();stats.Clear();
            armed=true;active=false;done=false;waitPolls=effectPolls=expiryStable=recordSamples=recordUnreadable=0;
            recordAddr=0;recordPath="not found";effectStartStamp=effectEndStamp=recordFoundStamp=0;
            phase=$"ARMED Unit*=0x{unit:X8}. Now select Issyl and cast ORIGINAL Haste once on this target. V6 handles timing automatically.";
            return phase;
        }
    }

    public static void Tick()
    {
        lock(sync)
        {
            if(!armed||done)return;
            if(!Attach()){phase="waiting for game";return;}
            if(!PinnedUnitValid())
            {
                phase="PINNED TARGET INVALID — RESET and repeat.";armed=false;active=false;return;
            }
            var roots=CaptureRoots();
            if(!active)
            {
                waitPolls++;
                if(EffectPattern(roots))BeginEffect(roots);
                else phase=$"ARMED — waiting for Issyl lifecycle... ({waitPolls} polls).";
                return;
            }

            effectPolls++;
            if(recordAddr==0&&TryFindRecord(roots,out uint found,out string path))
            {
                recordAddr=found;recordPath=path;recordFoundStamp=Stopwatch.GetTimestamp();
                phase=$"SIGNATURE LOCKED: A5 + target Unit* at 0x{recordAddr:X8} via {recordPath}. Tracking record every 20 ms.";
            }
            if(recordAddr!=0)SampleRecord();

            if(SameRoots(roots,baselineRoots))expiryStable++;else expiryStable=0;
            if(expiryStable>=EXPIRY_STABLE_SAMPLES)FinishEffect(roots);
            else if(recordAddr==0)phase=$"EFFECT ACTIVE — searching A5+target record... polls {effectPolls}";
            else phase=$"EFFECT ACTIVE — signature record 0x{recordAddr:X8}, samples {recordSamples}, unreadable {recordUnreadable}, expiry stable {expiryStable}/{EXPIRY_STABLE_SAMPLES}.";
        }
    }

    static double Ms(long a,long b)
    {
        if(a==0)return 0;
        long end=b==0?Stopwatch.GetTimestamp():b;
        return (end-a)*1000.0/Stopwatch.Frequency;
    }

    static string RootLine(int off)
    {
        uint b=baselineRoots.TryGetValue(off,out var x)?x:0;
        uint s=startRoots.TryGetValue(off,out var y)?y:0;
        uint e=endRoots.TryGetValue(off,out var z)?z:0;
        return $"ROOT Unit+0x{off:X3}: BASE 0x{b:X8} | START 0x{s:X8} | END 0x{e:X8}";
    }

    static IReadOnlyList<string> BuildReport()
    {
        var lines=new List<string>
        {
            "=== BRZE HERO EFFECT SIGNATURE PROBE V6 ===",
            $"Pinned Unit*=0x{unit:X8} UnitDef*=0x{unitDef:X8} owner={owner}",
            $"State: {(done?"COMPLETE":active?"ACTIVE":armed?"ARMED":"IDLE")}",
            $"Observed effect wall time: {Ms(effectStartStamp,effectEndStamp):0.0} ms | effect polls: {effectPolls}",
            $"Signature target: ability +0x058 = 0xA5 AND effect +0x17C = Unit*",
            $"Signature record: {(recordAddr==0?"NOT FOUND":$"0x{recordAddr:X8}")} | path at discovery: {recordPath}",
            $"Signature discovery delay: {(recordFoundStamp==0?0:Ms(effectStartStamp,recordFoundStamp)):0.0} ms | record samples: {recordSamples} | unreadable samples: {recordUnreadable}",
            "",
            "=== ROOT LIFECYCLE ===",
            RootLine(ROOT_1E4),RootLine(ROOT_1E8),RootLine(ROOT_20C),RootLine(ROOT_210),RootLine(ROOT_214),
            ""
        };

        if(recordAddr==0)
        {
            lines.Add("No A5+target record was located during this pass. Do not infer a duration field from this report.");
            return lines;
        }

        lines.Add("=== SIGNATURE CHECK / FOCUS OFFSETS ===");
        foreach(int off in FocusOffsets)
        {
            if(stats.TryGetValue(off,out var s))lines.Add(s.Changes>0?s.DynamicLine():s.StaticLine());
        }
        lines.Add("");
        lines.Add("=== DYNAMIC FIELDS — MONOTONIC FIRST ===");
        foreach(var s in stats.Values.Where(x=>x.Changes>0).OrderByDescending(x=>x.Score()).ThenBy(x=>x.Offset).Take(80))lines.Add(s.DynamicLine());
        lines.Add("");
        lines.Add("=== STATIC NUMERIC CONSTANTS IN THE A5+TARGET RECORD ===");
        foreach(var s in stats.Values.Where(x=>x.StaticDurationLike(recordSamples)).OrderBy(x=>x.FirstRaw).ThenBy(x=>x.Offset).Take(80))lines.Add(s.StaticLine());
        lines.Add("");
        lines.Add("Interpretation rule: only consider fields from this content-identified A5+target record. Prefer high-change, flip-0 fields whose scale tracks the ~11 s natural Issyl lifetime. No writes from V6 alone.");
        return lines;
    }

    public static ProbeSnapshot Snapshot()
    {
        lock(sync)
        {
            Attach();
            return new ProbeSnapshot(status,phase,done,BuildReport());
        }
    }

    public static string Reset()
    {
        lock(sync)
        {
            armed=active=done=false;unit=unitDef=owner=recordAddr=0;
            baselineRoots.Clear();startRoots.Clear();endRoots.Clear();stats.Clear();
            waitPolls=effectPolls=expiryStable=recordSamples=recordUnreadable=0;
            effectStartStamp=effectEndStamp=recordFoundStamp=0;recordPath="not found";
            return phase="RESET — select one clean target and ARM";
        }
    }

    public static void Shutdown(){lock(sync){Detach();}}
}
