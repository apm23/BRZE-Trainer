using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectDurationWriteProbeV9;

internal readonly record struct ProbeSnapshot(string Game,string Phase,bool Done,IReadOnlyList<string> Lines);

internal enum ProbeStage
{
    Idle,
    BaselineArmed,
    BaselineActive,
    ReadyForPatch,
    TestArmed,
    TestActive,
    Complete,
    Error
}

internal static class ProbeCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_OPERATION=0x0008;
    const uint PROCESS_VM_READ=0x0010;
    const uint PROCESS_VM_WRITE=0x0020;
    const uint PROCESS_QUERY_INFORMATION=0x0400;
    const uint ACCESS=PROCESS_VM_OPERATION|PROCESS_VM_READ|PROCESS_VM_WRITE|PROCESS_QUERY_INFORMATION;

    const int RVA_SELECTION_LIST=0x441708;
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    const int ROOT_1E4=0x1E4,ROOT_1E8=0x1E8,ROOT_20C=0x20C,ROOT_210=0x210,ROOT_214=0x214;
    const int ROOT_SCAN=0x600;
    const int ABILITY_OFF=0x058,TARGET_OFF=0x17C,CONFIG_PTR_OFF=0x1F4;
    const int CONFIG_DURATION_OFF=0x0E0;
    const uint ISSYL_ID=0xA5;
    const uint ORIGINAL_DURATION=15000;
    const uint PATCH_DURATION=30000;
    const int EXPIRY_STABLE=4;

    static readonly int[] lifecycle={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210,ROOT_214};
    static readonly int[] transientRoots={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210};
    static readonly object sync=new();

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status="not attached";

    static ProbeStage stage=ProbeStage.Idle;
    static string phase="IDLE — select one clean target and ARM BASELINE";

    static uint unit,unitDef,owner;
    static uint configAddr;
    static uint baselineParent,testParent;
    static string baselinePath="not found",testPath="not found";
    static readonly Dictionary<int,uint> rootsBase=new();
    static int waitPolls,effectPolls,expiryStable;
    static long effectStartStamp;
    static double baselineWallMs,testWallMs;
    static bool patchApplied,restoreAttempted,restoreOk,testConfigMatched;
    static string lastWrite="none";

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

    static bool WriteU32(long address,uint value)
    {
        if(h==IntPtr.Zero)return false;
        byte[] b=BitConverter.GetBytes(value);
        if(!WriteProcessMemory(h,A(address),b,b.Length,out var n)||n.ToInt64()!=b.Length)return false;
        return ReadU32(address)==value;
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
        if(h==IntPtr.Zero){status="OpenProcess READ/WRITE failed";return false;}
        status=$"attached PID {process.Id} base 0x{moduleBase:X8}";
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

    static Dictionary<int,uint> Roots(uint u)
    {
        var d=new Dictionary<int,uint>();
        foreach(int o in lifecycle)d[o]=ReadU32((long)u+o);
        return d;
    }

    static bool RootsClean(Dictionary<int,uint> r)=>transientRoots.All(o=>r[o]==0);
    static bool SameRoots(Dictionary<int,uint>a,Dictionary<int,uint>b)=>lifecycle.All(o=>a[o]==b[o]);
    static bool EffectPattern(Dictionary<int,uint> now)=>lifecycle.Count(o=>now[o]!=rootsBase[o])>=2&&transientRoots.Any(o=>Ptr(now[o])&&now[o]!=rootsBase[o]);
    static bool PinnedUnitValid()=>unit!=0&&h!=IntPtr.Zero&&ReadU32((long)unit+OFF_DEF)==unitDef&&ReadU32((long)unit+OFF_OWNER)==owner;

    static bool Signature(uint addr)
    {
        return Ptr(addr)&&ReadU32((long)addr+ABILITY_OFF)==ISSYL_ID&&ReadU32((long)addr+TARGET_OFF)==unit;
    }

    static bool FindParent(Dictionary<int,uint> roots,out uint addr,out string path)
    {
        addr=0;path="not found";
        var seen=new HashSet<uint>();
        foreach(int ro in transientRoots)
        {
            uint r=roots[ro];
            if(!Ptr(r)||!seen.Add(r))continue;
            if(Signature(r)){addr=r;path=$"Unit+0x{ro:X3}";return true;}
            var b=new byte[ROOT_SCAN];
            if(!ReadExact(r,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=U32(b,o);
                if(!Ptr(p)||!seen.Add(p))continue;
                if(Signature(p)){addr=p;path=$"Unit+0x{ro:X3}->+0x{o:X3}";return true;}
            }
        }
        return false;
    }

    static bool CaptureConfig(uint parent,out uint cfg)
    {
        cfg=ReadU32((long)parent+CONFIG_PTR_OFF);
        if(!Ptr(cfg))return false;
        return ReadU32(cfg)==ISSYL_ID;
    }

    static bool PinSelectedCleanTarget(out string error)
    {
        error="";
        if(!Attach()){error="game attach failed — "+status;return false;}
        var (u,c)=FirstSelectedUnit();
        if(c!=1||u==0){error=$"select exactly ONE clean target (selected {c})";return false;}
        uint def=ReadU32((long)u+OFF_DEF),own=ReadU32((long)u+OFF_OWNER);
        if(def==0){error="selected object has no UnitDef";return false;}
        var r=Roots(u);
        if(!RootsClean(r)){error="target is not clean — transient hero-effect roots already active";return false;}
        unit=u;unitDef=def;owner=own;
        rootsBase.Clear();foreach(var kv in r)rootsBase[kv.Key]=kv.Value;
        waitPolls=effectPolls=expiryStable=0;
        return true;
    }

    static double ElapsedMs(long start)
    {
        if(start==0)return 0;
        return (Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
    }

    static void EnterError(string message)
    {
        if(patchApplied)RestoreInternal();
        stage=ProbeStage.Error;
        phase="ERROR — "+message;
    }

    static bool RestoreInternal()
    {
        restoreAttempted=true;
        if(configAddr==0||!Attach()){restoreOk=false;lastWrite="restore failed: no config/game";return false;}
        if(ReadU32(configAddr)!=ISSYL_ID){restoreOk=false;lastWrite="restore blocked: config ID is not A5";return false;}
        uint cur=ReadU32((long)configAddr+CONFIG_DURATION_OFF);
        if(cur==ORIGINAL_DURATION)
        {
            patchApplied=false;restoreOk=true;lastWrite="restore not needed: already 15000";return true;
        }
        if(cur!=PATCH_DURATION)
        {
            restoreOk=false;lastWrite=$"restore blocked: unexpected duration {cur}";return false;
        }
        bool ok=WriteU32((long)configAddr+CONFIG_DURATION_OFF,ORIGINAL_DURATION);
        restoreOk=ok;
        if(ok)patchApplied=false;
        lastWrite=ok?"RESTORED config+0x0E0 30000 -> 15000":"restore write/verify FAILED";
        return ok;
    }

    public static string ArmBaseline()
    {
        lock(sync)
        {
            if(patchApplied&&!RestoreInternal())return phase="ARM blocked — previous patch could not be restored";
            if(!PinSelectedCleanTarget(out string error))return phase="ARM BASELINE blocked — "+error;
            configAddr=baselineParent=testParent=0;baselinePath=testPath="not found";
            baselineWallMs=testWallMs=0;patchApplied=false;restoreAttempted=restoreOk=testConfigMatched=false;lastWrite="none";
            stage=ProbeStage.BaselineArmed;
            return phase=$"BASELINE ARMED Unit*=0x{unit:X8}. Select Issyl and cast ORIGINAL Haste ONCE on this target.";
        }
    }

    public static string PatchAndArmTest()
    {
        lock(sync)
        {
            if(stage!=ProbeStage.ReadyForPatch)return phase="PATCH blocked — finish the baseline Issyl pass first";
            if(!Attach())return phase="PATCH blocked — "+status;
            if(configAddr==0||ReadU32(configAddr)!=ISSYL_ID)return phase="PATCH blocked — A5 config identity no longer valid";
            uint cur=ReadU32((long)configAddr+CONFIG_DURATION_OFF);
            if(cur!=ORIGINAL_DURATION)return phase=$"PATCH blocked — expected config duration 15000, found {cur}";
            if(!PinSelectedCleanTarget(out string error))return phase="PATCH blocked — "+error;
            if(!WriteU32((long)configAddr+CONFIG_DURATION_OFF,PATCH_DURATION))return phase="PATCH FAILED — config+0x0E0 write/verify failed; nothing else was changed";
            patchApplied=true;restoreAttempted=restoreOk=false;lastWrite="PATCHED config+0x0E0 15000 -> 30000";
            testParent=0;testPath="not found";testConfigMatched=false;effectStartStamp=0;
            stage=ProbeStage.TestArmed;
            return phase=$"PATCH VERIFIED: A5 duration is now 30000. TEST ARMED Unit*=0x{unit:X8}. Cast ORIGINAL Issyl Haste ONCE on this target.";
        }
    }

    public static string RestoreNow()
    {
        lock(sync)
        {
            bool ok=RestoreInternal();
            if(ok&&stage==ProbeStage.TestArmed)stage=ProbeStage.ReadyForPatch;
            return phase=ok?"RESTORE OK — Issyl config duration is 15000 again.":"RESTORE FAILED/BLOCKED — "+lastWrite;
        }
    }

    public static void Tick()
    {
        lock(sync)
        {
            if(stage is ProbeStage.Idle or ProbeStage.ReadyForPatch or ProbeStage.Complete or ProbeStage.Error)return;
            if(!Attach()){phase="waiting for game";return;}
            if(!PinnedUnitValid()){EnterError("pinned target became invalid");return;}
            var roots=Roots(unit);

            if(stage==ProbeStage.BaselineArmed)
            {
                waitPolls++;
                if(EffectPattern(roots))
                {
                    stage=ProbeStage.BaselineActive;effectStartStamp=Stopwatch.GetTimestamp();effectPolls=expiryStable=0;
                    phase="BASELINE EFFECT ACTIVE — locating A5+target parent/config...";
                }
                else phase=$"BASELINE ARMED — waiting for original Issyl Haste... ({waitPolls} polls)";
                return;
            }

            if(stage==ProbeStage.BaselineActive)
            {
                effectPolls++;
                if(baselineParent==0&&FindParent(roots,out uint p,out string path))
                {
                    baselineParent=p;baselinePath=path;
                    if(CaptureConfig(p,out uint cfg))
                    {
                        configAddr=cfg;
                        uint d=ReadU32((long)configAddr+CONFIG_DURATION_OFF);
                        phase=$"BASELINE A5 CONFIG LOCKED 0x{configAddr:X8}; duration={d}. Waiting natural expiry...";
                    }
                }
                if(SameRoots(roots,rootsBase))expiryStable++;else expiryStable=0;
                if(expiryStable>=EXPIRY_STABLE)
                {
                    baselineWallMs=ElapsedMs(effectStartStamp);
                    if(configAddr==0){EnterError("baseline ended but A5 config was not captured");return;}
                    if(ReadU32(configAddr)!=ISSYL_ID){EnterError("captured config no longer identifies A5");return;}
                    uint d=ReadU32((long)configAddr+CONFIG_DURATION_OFF);
                    if(d!=ORIGINAL_DURATION){EnterError($"baseline config+0x0E0 expected 15000 but found {d}");return;}
                    stage=ProbeStage.ReadyForPatch;
                    phase=$"BASELINE COMPLETE {baselineWallMs:0.0} ms. A5 config duration=15000 CONFIRMED. Select ONE clean target, then click PATCH 30000 + ARM TEST.";
                }
                else if(baselineParent==0)phase=$"BASELINE ACTIVE — searching A5+target parent... polls {effectPolls}";
                return;
            }

            if(stage==ProbeStage.TestArmed)
            {
                if(ReadU32(configAddr)!=ISSYL_ID||ReadU32((long)configAddr+CONFIG_DURATION_OFF)!=PATCH_DURATION)
                {
                    EnterError("30000 patch is no longer intact before test cast");return;
                }
                waitPolls++;
                if(EffectPattern(roots))
                {
                    stage=ProbeStage.TestActive;effectStartStamp=Stopwatch.GetTimestamp();effectPolls=expiryStable=0;
                    phase="2X TEST EFFECT ACTIVE — locating A5+target parent and measuring natural expiry...";
                }
                else phase=$"2X TEST ARMED — waiting for original Issyl Haste... ({waitPolls} polls)";
                return;
            }

            if(stage==ProbeStage.TestActive)
            {
                effectPolls++;
                if(testParent==0&&FindParent(roots,out uint p,out string path))
                {
                    testParent=p;testPath=path;
                    if(CaptureConfig(p,out uint cfg))testConfigMatched=cfg==configAddr&&ReadU32((long)cfg+CONFIG_DURATION_OFF)==PATCH_DURATION;
                }
                if(SameRoots(roots,rootsBase))expiryStable++;else expiryStable=0;
                if(expiryStable>=EXPIRY_STABLE)
                {
                    testWallMs=ElapsedMs(effectStartStamp);
                    bool restored=RestoreInternal();
                    stage=restored?ProbeStage.Complete:ProbeStage.Error;
                    phase=restored?$"TEST COMPLETE {testWallMs:0.0} ms — config AUTO-RESTORED to 15000. COPY REPORT.":"TEST ended but automatic restore FAILED/BLOCKED — use RESTORE NOW and inspect report.";
                }
                else phase=$"2X TEST ACTIVE — elapsed {ElapsedMs(effectStartStamp)/1000.0:0.0}s; parent {(testParent==0?"searching":$"0x{testParent:X8}")}; waiting natural expiry.";
            }
        }
    }

    static IReadOnlyList<string> Report()
    {
        uint cfgId=configAddr==0?0:ReadU32(configAddr);
        uint cfgDur=configAddr==0?0:ReadU32((long)configAddr+CONFIG_DURATION_OFF);
        double ratio=baselineWallMs>0&&testWallMs>0?testWallMs/baselineWallMs:0;
        return new List<string>
        {
            "=== BRZE HERO EFFECT DURATION WRITE PROBE V9 ===",
            $"Stage: {stage}",
            $"Pinned Unit*=0x{unit:X8} UnitDef*=0x{unitDef:X8} owner={owner}",
            $"Baseline parent: 0x{baselineParent:X8} | path: {baselinePath}",
            $"A5 config: 0x{configAddr:X8} | config+0x000 now 0x{cfgId:X8}",
            $"Baseline nominal config+0x0E0: {ORIGINAL_DURATION}",
            $"Test nominal config+0x0E0: {PATCH_DURATION}",
            $"Current config+0x0E0: {cfgDur}",
            $"Baseline natural wall lifetime: {baselineWallMs:0.0} ms",
            $"2X-patched natural wall lifetime: {testWallMs:0.0} ms",
            $"Observed test/baseline ratio: {ratio:0.000000}x",
            $"Test parent: 0x{testParent:X8} | path: {testPath} | same config+30000 verified: {testConfigMatched}",
            $"Patch applied now: {patchApplied}",
            $"Restore attempted: {restoreAttempted} | restore OK: {restoreOk}",
            $"Last write status: {lastWrite}",
            "",
            "Expected proof condition: test lifetime should be approximately 2x baseline if config+0x0E0 is the duration parameter.",
            "Safety: V9 writes ONLY the discovered Issyl A5 config+0x0E0 after strict identity/value guards; no hooks, injection, native replay, or repeated effect application."
        };
    }

    public static ProbeSnapshot Snapshot()
    {
        lock(sync)
        {
            Attach();
            bool done=stage is ProbeStage.Complete or ProbeStage.Error;
            return new ProbeSnapshot(status,phase,done,Report());
        }
    }

    public static string Reset()
    {
        lock(sync)
        {
            if(patchApplied)RestoreInternal();
            stage=ProbeStage.Idle;phase="IDLE — select one clean target and ARM BASELINE";
            unit=unitDef=owner=configAddr=baselineParent=testParent=0;baselinePath=testPath="not found";
            rootsBase.Clear();waitPolls=effectPolls=expiryStable=0;effectStartStamp=0;baselineWallMs=testWallMs=0;
            patchApplied=false;restoreAttempted=restoreOk=testConfigMatched=false;lastWrite="none";
            return phase;
        }
    }

    public static void Shutdown()
    {
        lock(sync)
        {
            if(patchApplied)RestoreInternal();
            Detach();
        }
    }
}
