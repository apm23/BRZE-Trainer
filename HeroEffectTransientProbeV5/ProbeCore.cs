using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectTransientProbeV5;

internal readonly record struct ProbeSnapshot(string Game,string Phase,bool Done,IReadOnlyList<string> Lines);

internal sealed class FieldStat
{
    public required string Key { get; init; }
    public uint FirstRaw { get; set; }
    public uint LastRaw { get; set; }
    public int Seen { get; set; }
    public int Changes { get; set; }
    public int Flips { get; set; }
    public int LastDir { get; set; }
    public long SumAbsDelta { get; set; }
    public long MinAbsDelta { get; set; }=long.MaxValue;
    public long MaxAbsDelta { get; set; }
    public int PointerLikeSamples { get; set; }

    static float F(uint raw)=>BitConverter.Int32BitsToSingle(unchecked((int)raw));
    static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    public void Add(uint raw)
    {
        if(Seen==0)
        {
            FirstRaw=LastRaw=raw;Seen=1;
            if(Ptr(raw))PointerLikeSamples++;
            return;
        }
        if(Ptr(raw))PointerLikeSamples++;
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

    public double DynamicScore()
    {
        if(Seen<2||Changes==0)return double.NegativeInfinity;
        double rate=(double)Changes/Math.Max(1,Seen-1);
        double s=Changes*2.0+rate*140.0-Flips*28.0;
        if(Flips==0)s+=95;
        if(rate>=0.50)s+=55;
        if(rate>=0.80)s+=35;
        if(PointerLikeSamples>Seen*0.75)s-=130;
        if(Changes>=8)s+=25;
        return s;
    }

    public bool StaticDurationLike()
    {
        if(Seen==0||Changes!=0||FirstRaw==0||PointerLikeSamples>Seen*0.75)return false;
        int i=unchecked((int)FirstRaw);
        if(i>0&&i<=600000)return true;
        float f=F(FirstRaw);
        return Finite(f)&&f>=0.05f&&f<=600f;
    }

    public string FormatDynamic()
    {
        float ff=F(FirstRaw),lf=F(LastRaw);
        double rate=(double)Changes/Math.Max(1,Seen-1)*100.0;
        string delta=Changes==0?"-":$"avgΔ {(double)SumAbsDelta/Changes:0.##} minΔ {(MinAbsDelta==long.MaxValue?0:MinAbsDelta)} maxΔ {MaxAbsDelta}";
        return $"score {DynamicScore(),7:0.0} | {Key,-48} | seen {Seen,4} chg {Changes,4} ({rate,5:0.0}%) flip {Flips,3} | 0x{FirstRaw:X8}->{LastRaw:X8} | int {unchecked((int)FirstRaw)}->{unchecked((int)LastRaw)} | float {ff:0.#####}->{lf:0.#####} | {delta}";
    }

    public string FormatStatic()
    {
        float f=F(FirstRaw);
        return $"{Key,-48} | raw 0x{FirstRaw:X8} | int {unchecked((int)FirstRaw)} | float {f:0.#####} | seen {Seen}";
    }
}

internal sealed record TrackedNode(uint Address,string Label,int Size);

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
    const int CHILD_SCAN=0x240;
    const int CHILD_DISCOVERY=0x180;
    const int MAX_CHILDREN=28;
    const int EXPIRY_STABLE_SAMPLES=3;

    static readonly int[] LifecycleOffsets={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210,ROOT_214};
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
    static readonly List<TrackedNode> nodes=new();
    static readonly Dictionary<string,FieldStat> fields=new(StringComparer.Ordinal);
    static string phase="IDLE — select one clean target and ARM";
    static bool armed,active,done;
    static int waitSamples,effectSamples,expiryStable;
    static long effectStartStamp,effectEndStamp;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);
    static bool PlausiblePtr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

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

    static bool PinnedUnitValid()
    {
        if(unit==0||h==IntPtr.Zero)return false;
        return ReadU32((long)unit+OFF_DEF)==unitDef&&ReadU32((long)unit+OFF_OWNER)==owner;
    }

    static Dictionary<int,uint> CaptureRoots()
    {
        var d=new Dictionary<int,uint>();
        foreach(int off in LifecycleOffsets)d[off]=ReadU32((long)unit+off);
        return d;
    }

    static bool SameRoots(Dictionary<int,uint> a,Dictionary<int,uint> b)
    {
        foreach(int off in LifecycleOffsets)
            if(!a.TryGetValue(off,out uint av)||!b.TryGetValue(off,out uint bv)||av!=bv)return false;
        return true;
    }

    static bool EffectPattern(Dictionary<int,uint> now)
    {
        int changed=0;
        foreach(int off in LifecycleOffsets)
            if(now[off]!=baselineRoots[off])changed++;
        bool transientPtr=
            (PlausiblePtr(now[ROOT_1E4])&&now[ROOT_1E4]!=baselineRoots[ROOT_1E4])||
            (PlausiblePtr(now[ROOT_1E8])&&now[ROOT_1E8]!=baselineRoots[ROOT_1E8])||
            (PlausiblePtr(now[ROOT_20C])&&now[ROOT_20C]!=baselineRoots[ROOT_20C])||
            (PlausiblePtr(now[ROOT_210])&&now[ROOT_210]!=baselineRoots[ROOT_210]);
        return changed>=2&&transientPtr;
    }

    static void AddNode(uint address,string label,int size,HashSet<uint> seen)
    {
        if(!PlausiblePtr(address)||!seen.Add(address))return;
        var probe=new byte[Math.Min(size,0x40)];
        if(!ReadExact(address,probe))return;
        nodes.Add(new TrackedNode(address,label,size));
    }

    static void BuildPinnedNodeSet(Dictionary<int,uint> roots)
    {
        nodes.Clear();
        var seen=new HashSet<uint>();
        AddNode(roots[ROOT_1E4],"R1E4",ROOT_SCAN,seen);
        AddNode(roots[ROOT_1E8],"R1E8",ROOT_SCAN,seen);
        AddNode(roots[ROOT_20C],"R20C",ROOT_SCAN,seen);
        AddNode(roots[ROOT_210],"R210",ROOT_SCAN,seen);

        var rootsCopy=nodes.ToArray();
        int childCount=0;
        foreach(var n in rootsCopy)
        {
            var b=new byte[Math.Min(CHILD_DISCOVERY,n.Size)];
            if(!ReadExact(n.Address,b))continue;
            for(int o=0;o<=b.Length-4&&childCount<MAX_CHILDREN;o+=4)
            {
                uint p=U32(b,o);
                if(!PlausiblePtr(p)||seen.Contains(p))continue;
                int before=nodes.Count;
                AddNode(p,$"{n.Label}->+0x{o:X3}",CHILD_SCAN,seen);
                if(nodes.Count>before)childCount++;
            }
            if(childCount>=MAX_CHILDREN)break;
        }
    }

    static FieldStat Stat(string key)
    {
        if(fields.TryGetValue(key,out var s))return s;
        s=new FieldStat{Key=key};fields[key]=s;return s;
    }

    static void SamplePinnedNodes()
    {
        foreach(var n in nodes)
        {
            var b=new byte[n.Size];
            if(!ReadExact(n.Address,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint raw=U32(b,o);
                Stat($"{n.Label}+0x{o:X3}").Add(raw);
            }
        }
        effectSamples++;
    }

    static void BeginEffect(Dictionary<int,uint> roots)
    {
        active=true;done=false;expiryStable=0;effectSamples=0;
        effectStartStamp=Stopwatch.GetTimestamp();
        startRoots.Clear();foreach(var kv in roots)startRoots[kv.Key]=kv.Value;
        fields.Clear();
        BuildPinnedNodeSet(roots);
        SamplePinnedNodes();
        phase=$"EFFECT AUTO-DETECTED. Pinned {nodes.Count} transient nodes. Sampling every 50 ms; no clicks needed until COMPLETE.";
    }

    static void FinishEffect(Dictionary<int,uint> roots)
    {
        active=false;done=true;effectEndStamp=Stopwatch.GetTimestamp();
        endRoots.Clear();foreach(var kv in roots)endRoots[kv.Key]=kv.Value;
        phase=$"COMPLETE — natural lifecycle returned to baseline. {effectSamples} samples captured. Click COPY REPORT.";
    }

    public static string Arm()
    {
        lock(sync)
        {
            if(!Attach())return phase="ARM failed — "+status;
            var (u,c)=FirstSelectedUnit();
            if(c!=1||u==0)return phase=$"ARM blocked — select exactly ONE clean target unit (selected {c})";
            unit=u;unitDef=ReadU32((long)u+OFF_DEF);owner=ReadU32((long)u+OFF_OWNER);
            if(unitDef==0)return phase="ARM blocked — selected object has no UnitDef";
            baselineRoots.Clear();foreach(var kv in CaptureRoots())baselineRoots[kv.Key]=kv.Value;
            startRoots.Clear();endRoots.Clear();nodes.Clear();fields.Clear();
            armed=true;active=false;done=false;waitSamples=0;effectSamples=0;expiryStable=0;
            phase=$"ARMED Unit*=0x{unit:X8}. You may now select Issyl/Grayback and cast the ORIGINAL buff on this target. Start/end are automatic.";
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
                phase="PINNED TARGET INVALID — target disappeared/reused. RESET and repeat.";
                armed=false;active=false;return;
            }
            var roots=CaptureRoots();
            if(!active)
            {
                waitSamples++;
                if(EffectPattern(roots))BeginEffect(roots);
                else phase=$"ARMED — waiting for effect lifecycle... ({waitSamples} polls). Cast original Issyl/Grayback whenever ready.";
                return;
            }

            SamplePinnedNodes();
            if(SameRoots(roots,baselineRoots))expiryStable++;else expiryStable=0;
            if(expiryStable>=EXPIRY_STABLE_SAMPLES)FinishEffect(roots);
            else phase=$"EFFECT ACTIVE — samples {effectSamples}, pinned nodes {nodes.Count}. Auto-expiry stable {expiryStable}/{EXPIRY_STABLE_SAMPLES}.";
        }
    }

    static string RootLine(int off)
    {
        uint b=baselineRoots.TryGetValue(off,out var x)?x:0;
        uint s=startRoots.TryGetValue(off,out var y)?y:0;
        uint e=endRoots.TryGetValue(off,out var z)?z:0;
        return $"ROOT Unit+0x{off:X3}: BASE 0x{b:X8} | START 0x{s:X8} | END 0x{e:X8}";
    }

    static double EffectMs()
    {
        if(effectStartStamp==0)return 0;
        long end=effectEndStamp==0?Stopwatch.GetTimestamp():effectEndStamp;
        return (end-effectStartStamp)*1000.0/Stopwatch.Frequency;
    }

    static IReadOnlyList<string> BuildReport()
    {
        var lines=new List<string>
        {
            "=== BRZE HERO EFFECT TRANSIENT PROBE V5 ===",
            $"Pinned Unit*=0x{unit:X8} UnitDef*=0x{unitDef:X8} owner={owner}",
            $"State: {(done?"COMPLETE":active?"ACTIVE":armed?"ARMED":"IDLE")}",
            $"Observed effect wall time: {EffectMs():0.0} ms | effect samples: {effectSamples} | pinned nodes: {nodes.Count}",
            "",
            "=== AUTOMATIC ROOT LIFECYCLE ==="
        };
        foreach(int off in LifecycleOffsets)lines.Add(RootLine(off));

        lines.Add("");
        lines.Add("=== PINNED TRANSIENT NODES ===");
        foreach(var n in nodes)lines.Add($"{n.Label,-24} addr 0x{n.Address:X8} scan 0x{n.Size:X}");

        lines.Add("");
        lines.Add("=== DYNAMIC FIELDS — CHANGING FIRST ===");
        var dyn=fields.Values.Where(f=>f.Changes>0).OrderByDescending(f=>f.DynamicScore()).ThenByDescending(f=>f.Changes).Take(160).ToList();
        if(dyn.Count==0)lines.Add("(none)");else foreach(var f in dyn)lines.Add(f.FormatDynamic());

        lines.Add("");
        lines.Add("=== STATIC EFFECT-OBJECT CONSTANTS — DURATION-LIKE ONLY ===");
        var stat=fields.Values.Where(f=>f.StaticDurationLike()).OrderBy(f=>Math.Abs(unchecked((int)f.FirstRaw))).Take(80).ToList();
        if(stat.Count==0)lines.Add("(none)");else foreach(var f in stat)lines.Add(f.FormatStatic());

        lines.Add("");
        lines.Add("Interpretation priority: dynamic fields with high change-rate + flip 0, whose total change/scale matches observed effect wall time. Static section is secondary for fixed duration/expiry constants.");
        return lines;
    }

    public static ProbeSnapshot Snapshot()
    {
        lock(sync)
        {
            string game=Attach()?status:"waiting for Battle_Realms_F.exe";
            return new ProbeSnapshot(game,phase,done,BuildReport());
        }
    }

    public static string Reset()
    {
        lock(sync)
        {
            unit=unitDef=owner=0;baselineRoots.Clear();startRoots.Clear();endRoots.Clear();nodes.Clear();fields.Clear();
            armed=active=done=false;waitSamples=effectSamples=expiryStable=0;effectStartStamp=effectEndStamp=0;
            return phase="IDLE — select one clean target and ARM";
        }
    }

    public static void Shutdown(){lock(sync){Detach();}}
}
