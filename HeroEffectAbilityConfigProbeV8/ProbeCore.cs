using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectAbilityConfigProbeV8;

internal readonly record struct ProbeSnapshot(string Game,string Phase,bool Done,IReadOnlyList<string> Lines);

internal sealed class FieldStat
{
    public required int Offset { get; init; }
    public uint FirstRaw { get; private set; }
    public uint LastRaw { get; private set; }
    public int Seen { get; private set; }
    public int Changes { get; private set; }
    public int Flips { get; private set; }
    int lastDir;
    long sumAbs;
    long minAbs=long.MaxValue;
    long maxAbs;

    static float F(uint x)=>BitConverter.Int32BitsToSingle(unchecked((int)x));
    static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);

    public void Add(uint raw)
    {
        if(Seen==0){FirstRaw=LastRaw=raw;Seen=1;return;}
        Seen++;
        if(raw==LastRaw)return;
        long d=(long)unchecked((int)raw)-unchecked((int)LastRaw);
        int dir=d>0?1:d<0?-1:0;
        if(dir!=0&&lastDir!=0&&dir!=lastDir)Flips++;
        if(dir!=0)lastDir=dir;
        long a=Math.Abs(d);
        if(a<minAbs)minAbs=a;
        if(a>maxAbs)maxAbs=a;
        sumAbs+=a;
        Changes++;
        LastRaw=raw;
    }

    public bool NumericLike()
    {
        int i=unchecked((int)FirstRaw);
        if(i>=0&&i<=1000000)return true;
        float f=F(FirstRaw);
        return Finite(f)&&Math.Abs(f)<=100000f;
    }

    public string Line()
    {
        float a=F(FirstRaw),b=F(LastRaw);
        string d=Changes==0?"static":$"avgΔ {(double)sumAbs/Changes:0.##} minΔ {(minAbs==long.MaxValue?0:minAbs)} maxΔ {maxAbs}";
        return $"+0x{Offset:X3} | seen {Seen,4} chg {Changes,4} flip {Flips,3} | 0x{FirstRaw:X8}->0x{LastRaw:X8} | int {unchecked((int)FirstRaw)}->{unchecked((int)LastRaw)} | float {a:0.#####}->{b:0.#####} | {d}";
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
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    const int ROOT_1E4=0x1E4,ROOT_1E8=0x1E8,ROOT_20C=0x20C,ROOT_210=0x210,ROOT_214=0x214;
    const int ROOT_SCAN=0x600;
    const int ABILITY_OFF=0x058,TARGET_OFF=0x17C,CONFIG_PTR_OFF=0x1F4;
    const int CONFIG_SCAN=0x200;
    const uint ISSYL_ID=0xA5,GRAYBACK_ID=0xC0;
    const int EXPIRY_STABLE=4;

    static readonly int[] lifecycle={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210,ROOT_214};
    static readonly int[] transientRoots={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210};
    static readonly int[] focus={0x000,0x008,0x00C,0x014,0x058,0x0A0,0x0E0,0x0E8,0x100,0x110,0x120,0x168};
    static readonly object sync=new();

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status="not attached";

    static uint unit,unitDef,owner,parentAddr,abilityId,configAddr;
    static string parentPath="not found";
    static readonly Dictionary<int,uint> baseline=new(),startRoots=new(),endRoots=new();
    static readonly Dictionary<int,FieldStat> stats=new();
    static bool armed,active,done;
    static int waitPolls,effectPolls,expiryStable,configSamples,configReadFailures;
    static long startStamp,endStamp,parentFoundStamp;
    static string phase="IDLE — select one clean target and ARM";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;
    static string AbilityName(uint id)=>id==ISSYL_ID?"ISSYL HASTE (A5)":id==GRAYBACK_ID?"GRAYBACK (C0)":$"UNKNOWN 0x{id:X}";

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

    static Dictionary<int,uint> Roots()
    {
        var d=new Dictionary<int,uint>();
        foreach(int o in lifecycle)d[o]=ReadU32((long)unit+o);
        return d;
    }

    static bool Same(Dictionary<int,uint>a,Dictionary<int,uint>b)=>lifecycle.All(o=>a[o]==b[o]);
    static bool EffectPattern(Dictionary<int,uint> now)=>lifecycle.Count(o=>now[o]!=baseline[o])>=2&&transientRoots.Any(o=>Ptr(now[o])&&now[o]!=baseline[o]);

    static bool Signature(uint addr,out uint id)
    {
        id=0;
        if(!Ptr(addr)||ReadU32((long)addr+TARGET_OFF)!=unit)return false;
        uint x=ReadU32((long)addr+ABILITY_OFF);
        if(x!=ISSYL_ID&&x!=GRAYBACK_ID)return false;
        id=x;return true;
    }

    static bool FindParent(Dictionary<int,uint> roots,out uint addr,out uint id,out string path)
    {
        addr=0;id=0;path="not found";
        var seen=new HashSet<uint>();
        foreach(int ro in transientRoots)
        {
            uint r=roots[ro];
            if(!Ptr(r)||!seen.Add(r))continue;
            if(Signature(r,out id)){addr=r;path=$"Unit+0x{ro:X3}";return true;}
            var b=new byte[ROOT_SCAN];
            if(!ReadExact(r,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=U32(b,o);
                if(!Ptr(p)||!seen.Add(p))continue;
                if(Signature(p,out id)){addr=p;path=$"Unit+0x{ro:X3}->+0x{o:X3}";return true;}
            }
        }
        return false;
    }

    static FieldStat Stat(int off)
    {
        if(stats.TryGetValue(off,out var s))return s;
        s=new FieldStat{Offset=off};stats[off]=s;return s;
    }

    static void SampleConfig()
    {
        if(configAddr==0)return;
        var b=new byte[CONFIG_SCAN];
        if(!ReadExact(configAddr,b)){configReadFailures++;return;}
        configSamples++;
        for(int o=0;o<=b.Length-4;o+=4)Stat(o).Add(U32(b,o));
    }

    static void Begin(Dictionary<int,uint> roots)
    {
        active=true;done=false;effectPolls=expiryStable=configSamples=configReadFailures=0;
        parentAddr=abilityId=configAddr=0;parentPath="not found";stats.Clear();
        startStamp=Stopwatch.GetTimestamp();endStamp=parentFoundStamp=0;
        startRoots.Clear();foreach(var kv in roots)startRoots[kv.Key]=kv.Value;
        phase="Hero-effect lifecycle detected — locating A5/C0 + target signature...";
    }

    static void Finish(Dictionary<int,uint> roots)
    {
        active=false;done=true;endStamp=Stopwatch.GetTimestamp();
        endRoots.Clear();foreach(var kv in roots)endRoots[kv.Key]=kv.Value;
        phase=parentAddr==0?"COMPLETE — no A5/C0 signature parent captured. COPY REPORT.":$"COMPLETE — {AbilityName(abilityId)} config sampled. COPY REPORT.";
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
            baseline.Clear();foreach(var kv in Roots())baseline[kv.Key]=kv.Value;
            startRoots.Clear();endRoots.Clear();stats.Clear();
            armed=true;active=false;done=false;waitPolls=effectPolls=expiryStable=configSamples=configReadFailures=0;
            parentAddr=abilityId=configAddr=0;parentPath="not found";startStamp=endStamp=parentFoundStamp=0;
            return phase=$"ARMED Unit*=0x{unit:X8}. Cast ORIGINAL Issyl Haste OR ORIGINAL Grayback effect once on this target. V8 auto-identifies A5/C0.";
        }
    }

    public static void Tick()
    {
        lock(sync)
        {
            if(!armed||done)return;
            if(!Attach()){phase="waiting for game";return;}
            if(!PinnedUnitValid()){phase="PINNED TARGET INVALID — RESET and repeat.";armed=false;active=false;return;}
            var roots=Roots();
            if(!active)
            {
                waitPolls++;
                if(EffectPattern(roots))Begin(roots);
                else phase=$"ARMED — waiting for Issyl A5 or Grayback C0 lifecycle... ({waitPolls} polls)";
                return;
            }

            effectPolls++;
            if(parentAddr==0&&FindParent(roots,out uint p,out uint id,out string path))
            {
                parentAddr=p;abilityId=id;parentPath=path;parentFoundStamp=Stopwatch.GetTimestamp();
                uint cfg=ReadU32((long)parentAddr+CONFIG_PTR_OFF);
                if(Ptr(cfg))configAddr=cfg;
                SampleConfig();
                uint cfgId=configAddr==0?0:ReadU32(configAddr);
                phase=$"{AbilityName(abilityId)} PARENT LOCKED 0x{parentAddr:X8}; parent+0x1F4=0x{configAddr:X8}; config+0x000=0x{cfgId:X}. Sampling every 20 ms.";
            }
            else if(parentAddr!=0)SampleConfig();

            if(Same(roots,baseline))expiryStable++;else expiryStable=0;
            if(expiryStable>=EXPIRY_STABLE)Finish(roots);
            else if(parentAddr==0)phase=$"EFFECT ACTIVE — searching A5/C0+target parent... polls {effectPolls}";
            else phase=$"EFFECT ACTIVE — {AbilityName(abilityId)}, config samples {configSamples}, read failures {configReadFailures}, expiry stable {expiryStable}/{EXPIRY_STABLE}.";
        }
    }

    static double Ms(long a,long b)
    {
        if(a==0)return 0;
        long e=b==0?Stopwatch.GetTimestamp():b;
        return (e-a)*1000.0/Stopwatch.Frequency;
    }

    static string RootLine(int o)=>$"ROOT Unit+0x{o:X3}: BASE 0x{(baseline.TryGetValue(o,out var a)?a:0):X8} | START 0x{(startRoots.TryGetValue(o,out var b)?b:0):X8} | END 0x{(endRoots.TryGetValue(o,out var c)?c:0):X8}";

    static IReadOnlyList<string> Report()
    {
        double wall=Ms(startStamp,endStamp);
        uint cfgId=configAddr==0?0:ReadU32(configAddr);
        uint e0=stats.TryGetValue(0x0E0,out var e0s)?e0s.FirstRaw:0;
        var l=new List<string>
        {
            "=== BRZE HERO EFFECT ABILITY CONFIG PROBE V8 ===",
            $"Pinned Unit*=0x{unit:X8} UnitDef*=0x{unitDef:X8} owner={owner}",
            $"State: {(done?"COMPLETE":active?"ACTIVE":armed?"ARMED":"IDLE")}",
            $"Observed effect wall time: {wall:0.0} ms | effect polls: {effectPolls}",
            $"Detected ability: {AbilityName(abilityId)}",
            $"Signature parent: 0x{parentAddr:X8} | path: {parentPath} | discovery delay: {Ms(startStamp,parentFoundStamp):0.0} ms",
            $"Config pointer parent+0x1F4: 0x{configAddr:X8} | config+0x000 now: 0x{cfgId:X8} | expected ability: 0x{abilityId:X8}",
            $"Config samples: {configSamples} | read failures: {configReadFailures}",
            $"FOCUS config+0x0E0: 0x{e0:X8} / int {unchecked((int)e0)} | wall/config ratio: {(e0>0?wall/e0:0):0.000000}",
            "",
            "=== ROOT LIFECYCLE ==="
        };
        foreach(int o in lifecycle)l.Add(RootLine(o));
        l.Add("");l.Add("=== CONFIG FOCUS OFFSETS ===");
        foreach(int o in focus)
            l.Add(stats.TryGetValue(o,out var s)?s.Line():$"+0x{o:X3} | not sampled");
        l.Add("");l.Add("=== CONFIG DYNAMIC FIELDS ===");
        foreach(var s in stats.Values.Where(x=>x.Changes>0).OrderByDescending(x=>x.Changes).ThenBy(x=>x.Flips).ThenBy(x=>x.Offset).Take(80))l.Add(s.Line());
        if(!stats.Values.Any(x=>x.Changes>0))l.Add("(none — config record remained static for full effect lifecycle)");
        l.Add("");l.Add("=== CONFIG STATIC NUMERIC CONSTANTS ===");
        foreach(var s in stats.Values.Where(x=>x.Changes==0&&x.NumericLike()).OrderBy(x=>unchecked((int)x.FirstRaw)).ThenBy(x=>x.Offset).Take(120))l.Add(s.Line());
        l.Add("");
        l.Add("Interpretation: V8 compares the same parent+0x1F4 ability/config object across Issyl A5 and Grayback C0. The key confirmation is whether config+0x000 follows the detected ability ID and whether config+0x0E0 differs in a way consistent with their very different natural lifetimes. READ ONLY — no writes from V8.");
        return l;
    }

    public static ProbeSnapshot Snapshot()
    {
        lock(sync)
        {
            string game=Attach()?status:"waiting for game";
            return new ProbeSnapshot(game,phase,done,Report());
        }
    }

    public static string Reset()
    {
        lock(sync)
        {
            armed=active=done=false;waitPolls=effectPolls=expiryStable=configSamples=configReadFailures=0;
            unit=unitDef=owner=parentAddr=abilityId=configAddr=0;parentPath="not found";baseline.Clear();startRoots.Clear();endRoots.Clear();stats.Clear();
            startStamp=endStamp=parentFoundStamp=0;
            return phase="RESET — select one clean target and ARM";
        }
    }

    public static void Shutdown(){lock(sync){Detach();}}
}
