using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectChildProbeV7;

internal readonly record struct ProbeSnapshot(string Game,string Phase,bool Done,IReadOnlyList<string> Lines);

internal sealed class FieldStat
{
    public required string Key { get; init; }
    public uint FirstRaw { get; private set; }
    public uint LastRaw { get; private set; }
    public int Seen { get; private set; }
    public int Changes { get; private set; }
    public int Flips { get; private set; }
    int lastDir;
    long sumAbs;
    long minAbs=long.MaxValue;
    long maxAbs;
    int pointerSamples;

    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;
    static float F(uint x)=>BitConverter.Int32BitsToSingle(unchecked((int)x));
    static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);

    public void Add(uint raw)
    {
        if(Seen==0){FirstRaw=LastRaw=raw;Seen=1;if(Ptr(raw))pointerSamples++;return;}
        Seen++; if(Ptr(raw))pointerSamples++;
        if(raw==LastRaw)return;
        long d=(long)unchecked((int)raw)-unchecked((int)LastRaw);
        int dir=d>0?1:d<0?-1:0;
        if(dir!=0&&lastDir!=0&&dir!=lastDir)Flips++;
        if(dir!=0)lastDir=dir;
        long a=Math.Abs(d); if(a<minAbs)minAbs=a; if(a>maxAbs)maxAbs=a; sumAbs+=a;
        Changes++; LastRaw=raw;
    }

    public double Score()
    {
        if(Seen<2||Changes==0)return double.NegativeInfinity;
        double rate=(double)Changes/Math.Max(1,Seen-1);
        double s=Changes*2.0+rate*220.0-Flips*55.0;
        if(Flips==0)s+=220;
        if(rate>=0.25)s+=45;
        if(rate>=0.50)s+=80;
        if(rate>=0.80)s+=100;
        if(Changes>=12)s+=40;
        if(pointerSamples>Seen*0.75)s-=220;
        return s;
    }

    public bool StaticDurationLike(int total)
    {
        if(Seen<Math.Max(4,total*3/4)||Changes!=0||FirstRaw==0||pointerSamples>Seen*0.75)return false;
        int i=unchecked((int)FirstRaw);
        if(i>0&&i<=1000000)return true;
        float f=F(FirstRaw);
        return Finite(f)&&f>=0.001f&&f<=1000f;
    }

    public string DynamicLine()
    {
        double rate=(double)Changes/Math.Max(1,Seen-1)*100.0;
        float a=F(FirstRaw),b=F(LastRaw);
        string d=Changes==0?"-":$"avgΔ {(double)sumAbs/Changes:0.##} minΔ {(minAbs==long.MaxValue?0:minAbs)} maxΔ {maxAbs}";
        return $"score {Score(),7:0.0} | {Key,-48} | seen {Seen,4} chg {Changes,4} ({rate,5:0.0}%) flip {Flips,3} | 0x{FirstRaw:X8}->0x{LastRaw:X8} | int {unchecked((int)FirstRaw)}->{unchecked((int)LastRaw)} | float {a:0.#####}->{b:0.#####} | {d}";
    }

    public string StaticLine()
    {
        float f=F(FirstRaw);
        return $"{Key,-48} | raw 0x{FirstRaw:X8} | int {unchecked((int)FirstRaw)} | float {f:0.#####} | seen {Seen}";
    }
}

internal sealed class ChildNode
{
    public required uint Address { get; init; }
    public List<int> ParentOffsets { get; }=new();
    public int ReadFailures { get; set; }
    public string Label=>"PARENT["+string.Join(",",ParentOffsets.Select(x=>$"+0x{x:X3}"))+"]";
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
    const int OFF_DEF=0x74, OFF_OWNER=0x240;
    const int ROOT_1E4=0x1E4, ROOT_1E8=0x1E8, ROOT_20C=0x20C, ROOT_210=0x210, ROOT_214=0x214;
    const int ROOT_SCAN=0x600, PARENT_SCAN=0x240, CHILD_SCAN=0x200;
    const int ABILITY_OFF=0x058, TARGET_OFF=0x17C;
    const uint ISSYL_ID=0xA5;
    const int MAX_CHILDREN=24, EXPIRY_STABLE=4;

    static readonly int[] lifecycle={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210,ROOT_214};
    static readonly int[] transientRoots={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210};
    static readonly object sync=new();
    static Process? process; static IntPtr h=IntPtr.Zero; static long moduleBase; static DateTime lastTry; static string status="not attached";
    static uint unit,unitDef,owner,parentAddr; static string parentPath="not found";
    static readonly Dictionary<int,uint> baseline=new(), startRoots=new(), endRoots=new();
    static readonly List<ChildNode> children=new();
    static readonly Dictionary<string,FieldStat> stats=new(StringComparer.Ordinal);
    static bool armed,active,done; static int waitPolls,effectPolls,expiryStable,samples; static long startStamp,endStamp,parentFoundStamp;
    static string phase="IDLE — select one clean target and ARM";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    static bool ReadExact(long address,byte[] buffer)
    {
        if(h==IntPtr.Zero)return false;
        return ReadProcessMemory(h,A(address),buffer,buffer.Length,out var n)&&n.ToInt64()==buffer.Length;
    }
    static uint ReadU32(long address){var b=new byte[4];return ReadExact(address,b)?U32(b,0):0;}

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach(); if((DateTime.UtcNow-lastTry).TotalMilliseconds<200)return false; lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F"); if(ps.Length==0){status="waiting for Battle_Realms_F.exe";return false;}
        process=ps[0]; try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}catch{process=null;status="cannot resolve module base";return false;}
        h=OpenProcess(ACCESS,false,process.Id); if(h==IntPtr.Zero){status="OpenProcess READ failed";return false;}
        status=$"READ-ONLY attached PID {process.Id} base 0x{moduleBase:X8}"; return true;
    }
    static void Detach(){try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{} process=null;h=IntPtr.Zero;moduleBase=0;status="not attached";}

    static (uint unit,uint count) FirstSelectedUnit()
    {
        if(!Attach())return default; var list=new byte[0x1C]; if(!ReadExact(moduleBase+RVA_SELECTION_LIST,list))return default;
        uint node=U32(list,0),count=U32(list,0x18); if(node==0||count==0)return (0,count);
        var nb=new byte[0x0C]; if(!ReadExact(node,nb))return (0,count); return (U32(nb,0x08),count);
    }
    static bool PinnedUnitValid()=>unit!=0&&h!=IntPtr.Zero&&ReadU32((long)unit+OFF_DEF)==unitDef&&ReadU32((long)unit+OFF_OWNER)==owner;
    static Dictionary<int,uint> Roots(){var d=new Dictionary<int,uint>();foreach(int o in lifecycle)d[o]=ReadU32((long)unit+o);return d;}
    static bool Same(Dictionary<int,uint>a,Dictionary<int,uint>b)=>lifecycle.All(o=>a[o]==b[o]);
    static bool EffectPattern(Dictionary<int,uint> now)=>lifecycle.Count(o=>now[o]!=baseline[o])>=2&&transientRoots.Any(o=>Ptr(now[o])&&now[o]!=baseline[o]);
    static bool Signature(uint a)=>Ptr(a)&&ReadU32((long)a+ABILITY_OFF)==ISSYL_ID&&ReadU32((long)a+TARGET_OFF)==unit;

    static bool FindParent(Dictionary<int,uint> roots,out uint addr,out string path)
    {
        addr=0;path="not found";var seen=new HashSet<uint>();
        foreach(int ro in transientRoots)
        {
            uint r=roots[ro]; if(!Ptr(r)||!seen.Add(r))continue;
            if(Signature(r)){addr=r;path=$"Unit+0x{ro:X3}";return true;}
            var b=new byte[ROOT_SCAN]; if(!ReadExact(r,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=U32(b,o); if(!Ptr(p)||!seen.Add(p))continue;
                if(Signature(p)){addr=p;path=$"Unit+0x{ro:X3}->+0x{o:X3}";return true;}
            }
        }
        return false;
    }

    static void BuildChildren()
    {
        children.Clear(); var b=new byte[PARENT_SCAN]; if(!ReadExact(parentAddr,b))return;
        var map=new Dictionary<uint,ChildNode>();
        for(int o=0;o<=b.Length-4;o+=4)
        {
            uint p=U32(b,o); if(!Ptr(p)||p==unit||p==parentAddr)continue;
            if(!map.TryGetValue(p,out var node))
            {
                if(map.Count>=MAX_CHILDREN)continue;
                var probe=new byte[0x20]; if(!ReadExact(p,probe))continue;
                node=new ChildNode{Address=p}; map[p]=node; children.Add(node);
            }
            node.ParentOffsets.Add(o);
        }
    }

    static FieldStat Stat(string key){if(stats.TryGetValue(key,out var s))return s;s=new FieldStat{Key=key};stats[key]=s;return s;}
    static void SampleChildren()
    {
        foreach(var c in children)
        {
            var b=new byte[CHILD_SCAN]; if(!ReadExact(c.Address,b)){c.ReadFailures++;continue;}
            for(int o=0;o<=b.Length-4;o+=4)Stat($"{c.Label}+0x{o:X3}").Add(U32(b,o));
        }
        samples++;
    }

    static void Begin(Dictionary<int,uint> roots)
    {
        active=true;done=false;effectPolls=expiryStable=samples=0;parentAddr=0;parentPath="not found";children.Clear();stats.Clear();
        startStamp=Stopwatch.GetTimestamp();endStamp=parentFoundStamp=0;startRoots.Clear();foreach(var kv in roots)startRoots[kv.Key]=kv.Value;
        phase="ISSYL lifecycle detected — locating A5+target parent...";
    }
    static void Finish(Dictionary<int,uint> roots)
    {
        active=false;done=true;endStamp=Stopwatch.GetTimestamp();endRoots.Clear();foreach(var kv in roots)endRoots[kv.Key]=kv.Value;
        phase=parentAddr==0?"COMPLETE — signature parent not captured. COPY REPORT.":$"COMPLETE — signature parent + {children.Count} child objects tracked. COPY REPORT.";
    }

    public static string Arm()
    {
        lock(sync)
        {
            if(!Attach())return phase="ARM failed — "+status; var (u,c)=FirstSelectedUnit(); if(c!=1||u==0)return phase=$"ARM blocked — select exactly ONE clean target (selected {c})";
            unit=u;unitDef=ReadU32((long)u+OFF_DEF);owner=ReadU32((long)u+OFF_OWNER);if(unitDef==0)return phase="ARM blocked — selected object has no UnitDef";
            baseline.Clear();foreach(var kv in Roots())baseline[kv.Key]=kv.Value;startRoots.Clear();endRoots.Clear();children.Clear();stats.Clear();
            armed=true;active=false;done=false;waitPolls=effectPolls=expiryStable=samples=0;parentAddr=0;parentPath="not found";startStamp=endStamp=parentFoundStamp=0;
            return phase=$"ARMED Unit*=0x{unit:X8}. Select Issyl and cast ORIGINAL Haste once on this target. V7 is automatic.";
        }
    }

    public static void Tick()
    {
        lock(sync)
        {
            if(!armed||done)return;if(!Attach()){phase="waiting for game";return;}if(!PinnedUnitValid()){phase="PINNED TARGET INVALID — RESET and repeat.";armed=false;active=false;return;}
            var roots=Roots();
            if(!active){waitPolls++;if(EffectPattern(roots))Begin(roots);else phase=$"ARMED — waiting for Issyl... ({waitPolls} polls)";return;}
            effectPolls++;
            if(parentAddr==0&&FindParent(roots,out uint p,out string path))
            {
                parentAddr=p;parentPath=path;parentFoundStamp=Stopwatch.GetTimestamp();BuildChildren();SampleChildren();
                phase=$"A5+TARGET PARENT LOCKED 0x{parentAddr:X8}; {children.Count} readable child objects pinned. Sampling every 20 ms.";
            }
            else if(parentAddr!=0)SampleChildren();
            if(Same(roots,baseline))expiryStable++;else expiryStable=0;
            if(expiryStable>=EXPIRY_STABLE)Finish(roots);
            else if(parentAddr==0)phase=$"EFFECT ACTIVE — searching A5+target parent... polls {effectPolls}";
            else phase=$"EFFECT ACTIVE — parent 0x{parentAddr:X8}, child samples {samples}, expiry stable {expiryStable}/{EXPIRY_STABLE}.";
        }
    }

    static double Ms(long a,long b){if(a==0)return 0;long e=b==0?Stopwatch.GetTimestamp():b;return (e-a)*1000.0/Stopwatch.Frequency;}
    static string RootLine(int o)=>$"ROOT Unit+0x{o:X3}: BASE 0x{(baseline.TryGetValue(o,out var a)?a:0):X8} | START 0x{(startRoots.TryGetValue(o,out var b)?b:0):X8} | END 0x{(endRoots.TryGetValue(o,out var c)?c:0):X8}";

    static IReadOnlyList<string> Report()
    {
        var l=new List<string>{"=== BRZE HERO EFFECT CHILD PROBE V7 ===",$"Pinned Unit*=0x{unit:X8} UnitDef*=0x{unitDef:X8} owner={owner}",$"State: {(done?"COMPLETE":active?"ACTIVE":armed?"ARMED":"IDLE")}",$"Observed effect wall time: {Ms(startStamp,endStamp):0.0} ms | effect polls: {effectPolls}",$"Signature parent: 0x{parentAddr:X8} | path: {parentPath} | discovery delay: {Ms(startStamp,parentFoundStamp):0.0} ms",$"Pinned child objects: {children.Count} | child sample cycles: {samples}","","=== ROOT LIFECYCLE ==="};
        foreach(int o in lifecycle)l.Add(RootLine(o));
        l.Add("");l.Add("=== CHILD POINTER MAP FROM A5+TARGET PARENT ===");
        foreach(var c in children)l.Add($"{c.Label,-42} addr 0x{c.Address:X8} | read failures {c.ReadFailures}");
        l.Add("");l.Add("=== CHILD DYNAMIC FIELDS — MONOTONIC FIRST ===");
        foreach(var s in stats.Values.Where(x=>x.Changes>0).OrderByDescending(x=>x.Score()).ThenBy(x=>x.Key).Take(140))l.Add(s.DynamicLine());
        l.Add("");l.Add("=== CHILD STATIC NUMERIC CONSTANTS — DURATION-LIKE ONLY ===");
        foreach(var s in stats.Values.Where(x=>x.StaticDurationLike(samples)).OrderBy(x=>unchecked((int)x.FirstRaw)).ThenBy(x=>x.Key).Take(120))l.Add(s.StaticLine());
        l.Add("");l.Add("Interpretation rule: only child objects reached from the content-identified A5+target parent are shown. Prefer high-change, flip-0 fields with scale matching the ~10.72 s natural Issyl lifetime. No writes from V7 alone.");
        return l;
    }

    public static ProbeSnapshot Snapshot(){lock(sync){return new ProbeSnapshot(status,phase,done,Report());}}
    public static string Reset(){lock(sync){armed=active=done=false;children.Clear();stats.Clear();baseline.Clear();startRoots.Clear();endRoots.Clear();unit=unitDef=owner=parentAddr=0;phase="IDLE — select one clean target and ARM";return phase;}}
    public static void Shutdown(){lock(sync){Detach();}}
}
