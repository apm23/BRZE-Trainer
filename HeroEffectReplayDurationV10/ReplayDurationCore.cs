using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectReplayDurationV10;

internal readonly record struct LabSnapshot(string Game,string Phase,bool Done,IReadOnlyList<string> Lines);

internal enum LabStage
{
    Idle,
    BaselineArmed,
    BaselineActive,
    ReadyForReplay,
    ReplayArmed,
    ReplayActive,
    Complete,
    Error
}

internal static class ReplayDurationCore
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
    const int ABILITY_OFF=0x058,TARGET_OFF=0x17C,CONFIG_PTR_OFF=0x1F4,CONFIG_DURATION_OFF=0x0E0;
    const uint ISSYL_ID=0xA5;
    const uint ORIGINAL_DURATION=15000;
    const uint DOUBLE_DURATION=30000;
    const int EXPIRY_STABLE=4;

    static readonly int[] lifecycle={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210,ROOT_214};
    static readonly int[] transientRoots={ROOT_1E4,ROOT_1E8,ROOT_20C,ROOT_210};
    static readonly object sync=new();

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status="not attached";

    static LabStage stage=LabStage.Idle;
    static string phase="IDLE — select one clean target and ARM BASELINE/CAPTURE";
    static uint unit,unitDef,owner,configAddr,baselineParent,replayParent;
    static string baselinePath="not found",replayPath="not found";
    static readonly Dictionary<int,uint> rootsBase=new();
    static int waitPolls,effectPolls,expiryStable;
    static long effectStartStamp;
    static double baselineWallMs,replayWallMs;
    static bool patchApplied,restoreAttempted,restoreOk,replayConfigMatched;
    static string lastWrite="none";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    static bool ReadExact(long address,byte[] buffer)
    {
        if(h==IntPtr.Zero)return false;
        return ReadProcessMemory(h,A(address),buffer,buffer.Length,out var n)&&n.ToInt64()==buffer.Length;
    }

    static uint R32(long address)
    {
        var b=new byte[4];
        return ReadExact(address,b)?U32(b,0):0;
    }

    static bool W32(long address,uint value)
    {
        if(h==IntPtr.Zero)return false;
        byte[] b=BitConverter.GetBytes(value);
        if(!WriteProcessMemory(h,A(address),b,b.Length,out var n)||n.ToInt64()!=b.Length)return false;
        return R32(address)==value;
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
        foreach(int o in lifecycle)d[o]=R32((long)u+o);
        return d;
    }

    static bool RootsClean(Dictionary<int,uint> r)=>transientRoots.All(o=>r[o]==0);
    static bool SameRoots(Dictionary<int,uint>a,Dictionary<int,uint>b)=>lifecycle.All(o=>a[o]==b[o]);
    static bool EffectPattern(Dictionary<int,uint> now)=>lifecycle.Count(o=>now[o]!=rootsBase[o])>=2&&transientRoots.Any(o=>Ptr(now[o])&&now[o]!=rootsBase[o]);
    static bool PinnedUnitValid()=>unit!=0&&h!=IntPtr.Zero&&R32((long)unit+OFF_DEF)==unitDef&&R32((long)unit+OFF_OWNER)==owner;

    static bool Signature(uint addr)
    {
        return Ptr(addr)&&R32((long)addr+ABILITY_OFF)==ISSYL_ID&&R32((long)addr+TARGET_OFF)==unit;
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
        cfg=R32((long)parent+CONFIG_PTR_OFF);
        return Ptr(cfg)&&R32(cfg)==ISSYL_ID;
    }

    static bool PinSelectedCleanTarget(out string error)
    {
        error="";
        if(!Attach()){error="game attach failed — "+status;return false;}
        var (u,c)=FirstSelectedUnit();
        if(c!=1||u==0){error=$"select exactly ONE clean target (selected {c})";return false;}
        uint def=R32((long)u+OFF_DEF),own=R32((long)u+OFF_OWNER);
        if(def==0){error="selected object has no UnitDef";return false;}
        var r=Roots(u);
        if(!RootsClean(r)){error="target is not clean — wait until previous hero effect fully expires";return false;}
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

    static bool RestoreInternal()
    {
        restoreAttempted=true;
        if(configAddr==0||!Attach()){restoreOk=false;lastWrite="restore failed: no config/game";return false;}
        if(R32(configAddr)!=ISSYL_ID){restoreOk=false;lastWrite="restore blocked: config ID is not A5";return false;}
        uint cur=R32((long)configAddr+CONFIG_DURATION_OFF);
        if(cur==ORIGINAL_DURATION)
        {
            patchApplied=false;restoreOk=true;lastWrite="restore not needed: already 15000";return true;
        }
        if(cur!=DOUBLE_DURATION)
        {
            restoreOk=false;lastWrite=$"restore blocked: unexpected duration {cur}";return false;
        }
        bool ok=W32((long)configAddr+CONFIG_DURATION_OFF,ORIGINAL_DURATION);
        restoreOk=ok;
        if(ok)patchApplied=false;
        lastWrite=ok?"RESTORED config+0x0E0 30000 -> 15000":"restore write/verify FAILED";
        return ok;
    }

    static void EnterError(string message)
    {
        if(patchApplied)RestoreInternal();
        stage=LabStage.Error;
        phase="ERROR — "+message;
    }

    public static string ArmBaseline()
    {
        lock(sync)
        {
            if(patchApplied&&!RestoreInternal())return phase="BASELINE ARM blocked — previous patch could not be restored";
            if(!PinSelectedCleanTarget(out string error))return phase="BASELINE ARM blocked — "+error;
            configAddr=baselineParent=replayParent=0;baselinePath=replayPath="not found";
            baselineWallMs=replayWallMs=0;patchApplied=false;restoreAttempted=restoreOk=replayConfigMatched=false;lastWrite="none";effectStartStamp=0;
            stage=LabStage.BaselineArmed;
            return phase=$"BASELINE/CAPTURE ARMED Unit*=0x{unit:X8}. Select Issyl and cast ORIGINAL Haste ONCE on this target.";
        }
    }

    public static string ArmReplay2X()
    {
        lock(sync)
        {
            if(stage!=LabStage.ReadyForReplay)return phase="REPLAY ARM blocked — finish baseline/capture first";
            if(!Attach())return phase="REPLAY ARM blocked — "+status;
            if(configAddr==0||R32(configAddr)!=ISSYL_ID)return phase="REPLAY ARM blocked — captured config is not A5";
            uint cur=R32((long)configAddr+CONFIG_DURATION_OFF);
            if(cur!=ORIGINAL_DURATION)return phase=$"REPLAY ARM blocked — expected A5 duration 15000, found {cur}";
            if(!PinSelectedCleanTarget(out string error))return phase="REPLAY ARM blocked — "+error;
            if(!W32((long)configAddr+CONFIG_DURATION_OFF,DOUBLE_DURATION))return phase="REPLAY ARM FAILED — could not patch A5 duration 15000 -> 30000";
            patchApplied=true;restoreAttempted=restoreOk=false;lastWrite="PATCHED config+0x0E0 15000 -> 30000";
            replayParent=0;replayPath="not found";replayConfigMatched=false;effectStartStamp=0;
            stage=LabStage.ReplayArmed;
            return phase=$"V2 REPLAY TEST ARMED Unit*=0x{unit:X8}; A5 duration=30000. NOW click ISSYL 0xA5 in the known-good HeroEffectReplayV2 window. V10 will restore 15000 immediately when the replay lifecycle appears.";
        }
    }

    public static string RestoreNow()
    {
        lock(sync)
        {
            bool ok=RestoreInternal();
            if(ok&&stage==LabStage.ReplayArmed)stage=LabStage.ReadyForReplay;
            return phase=ok?"RESTORE OK — A5 duration is 15000 again.":"RESTORE FAILED/BLOCKED — "+lastWrite;
        }
    }

    public static void Tick()
    {
        lock(sync)
        {
            if(stage is LabStage.Idle or LabStage.ReadyForReplay or LabStage.Complete or LabStage.Error)return;
            if(!Attach()){phase="waiting for game";return;}
            if(!PinnedUnitValid()){EnterError("pinned target became invalid");return;}
            var roots=Roots(unit);

            if(stage==LabStage.BaselineArmed)
            {
                waitPolls++;
                if(EffectPattern(roots))
                {
                    stage=LabStage.BaselineActive;effectStartStamp=Stopwatch.GetTimestamp();effectPolls=expiryStable=0;
                    phase="BASELINE ACTIVE — locating A5 parent/config...";
                }
                else phase=$"BASELINE ARMED — waiting for original Issyl Haste... ({waitPolls} polls)";
                return;
            }

            if(stage==LabStage.BaselineActive)
            {
                effectPolls++;
                if(baselineParent==0&&FindParent(roots,out uint p,out string path))
                {
                    baselineParent=p;baselinePath=path;
                    if(CaptureConfig(p,out uint cfg))
                    {
                        configAddr=cfg;
                        phase=$"A5 CONFIG CAPTURED 0x{configAddr:X8}, duration={R32((long)configAddr+CONFIG_DURATION_OFF)}. Waiting baseline natural expiry...";
                    }
                }
                if(SameRoots(roots,rootsBase))expiryStable++;else expiryStable=0;
                if(expiryStable>=EXPIRY_STABLE)
                {
                    baselineWallMs=ElapsedMs(effectStartStamp);
                    if(configAddr==0||R32(configAddr)!=ISSYL_ID){EnterError("baseline ended but valid A5 config was not captured");return;}
                    uint d=R32((long)configAddr+CONFIG_DURATION_OFF);
                    if(d!=ORIGINAL_DURATION){EnterError($"baseline expected A5 duration 15000, found {d}");return;}
                    stage=LabStage.ReadyForReplay;
                    phase=$"BASELINE COMPLETE {baselineWallMs:0.0} ms. Keep/open known-good Replay V2. Select this clean target again, then click ARM V2 REPLAY 2X here.";
                }
                return;
            }

            if(stage==LabStage.ReplayArmed)
            {
                if(R32(configAddr)!=ISSYL_ID||R32((long)configAddr+CONFIG_DURATION_OFF)!=DOUBLE_DURATION)
                {
                    EnterError("30000 patch is no longer intact before V2 replay");return;
                }
                waitPolls++;
                if(EffectPattern(roots))
                {
                    stage=LabStage.ReplayActive;effectStartStamp=Stopwatch.GetTimestamp();effectPolls=expiryStable=0;
                    if(!RestoreInternal()){EnterError("replay appeared but immediate restore to 15000 failed");return;}
                    phase="V2 REPLAY EFFECT ACTIVE — A5 config already restored to 15000. Measuring natural expiry...";
                }
                else phase=$"V2 REPLAY ARMED — click ISSYL 0xA5 in Replay V2 now. Waiting lifecycle... ({waitPolls} polls)";
                return;
            }

            if(stage==LabStage.ReplayActive)
            {
                effectPolls++;
                if(patchApplied){EnterError("duration patch unexpectedly remained active after replay creation");return;}
                if(R32(configAddr)!=ISSYL_ID||R32((long)configAddr+CONFIG_DURATION_OFF)!=ORIGINAL_DURATION)
                {
                    EnterError("A5 config is not restored to 15000 while replay effect is active");return;
                }
                if(replayParent==0&&FindParent(roots,out uint p,out string path))
                {
                    replayParent=p;replayPath=path;
                    if(CaptureConfig(p,out uint cfg))replayConfigMatched=cfg==configAddr;
                }
                if(SameRoots(roots,rootsBase))expiryStable++;else expiryStable=0;
                if(expiryStable>=EXPIRY_STABLE)
                {
                    replayWallMs=ElapsedMs(effectStartStamp);
                    if(!replayConfigMatched){EnterError("V2 replay parent did not resolve to the captured A5 config");return;}
                    stage=LabStage.Complete;
                    phase=$"COMPLETE — Replay V2 produced {replayWallMs:0.0} ms with temporary 30000 and A5 config remained restored at 15000.";
                }
                return;
            }
        }
    }

    static IReadOnlyList<string> Report()
    {
        uint cfgId=configAddr==0?0:R32(configAddr);
        uint cfgDur=configAddr==0?0:R32((long)configAddr+CONFIG_DURATION_OFF);
        double ratio=baselineWallMs>0?replayWallMs/baselineWallMs:0;
        return new List<string>
        {
            "=== BRZE HERO EFFECT REPLAY DURATION V10 COMPANION ===",
            $"Stage: {stage}",
            $"Pinned Unit*=0x{unit:X8} UnitDef*=0x{unitDef:X8} owner={owner}",
            $"Baseline parent: 0x{baselineParent:X8} | path: {baselinePath}",
            $"Captured A5 config: 0x{configAddr:X8} | config+0x000 now 0x{cfgId:X8}",
            $"Current config+0x0E0: {cfgDur}",
            $"Baseline ORIGINAL Issyl wall lifetime: {baselineWallMs:0.0} ms",
            $"Replay V2 2X wall lifetime: {replayWallMs:0.0} ms",
            $"Observed replay/baseline ratio: {ratio:0.000000}x",
            $"Replay parent: 0x{replayParent:X8} | path: {replayPath} | same config verified: {replayConfigMatched}",
            $"Patch applied now: {patchApplied}",
            $"Restore attempted: {restoreAttempted} | restore OK: {restoreOk}",
            $"Last write status: {lastWrite}",
            "Expected proof: Replay V2 lifetime ~2x baseline, replay parent uses same A5 config, and current config is already back to 15000.",
            "V10 companion performs no hook/injection/native replay itself; known-good Replay V2 remains untouched and performs the one-shot native call."
        };
    }

    public static LabSnapshot Snapshot()
    {
        lock(sync)
        {
            string g=Attach()?"GAME: "+status:"GAME: waiting for Battle_Realms_F.exe";
            return new LabSnapshot(g,phase,stage is LabStage.Complete or LabStage.Error,Report());
        }
    }

    public static string Reset()
    {
        lock(sync)
        {
            if(patchApplied)RestoreInternal();
            stage=LabStage.Idle;phase="IDLE — select one clean target and ARM BASELINE/CAPTURE";
            unit=unitDef=owner=configAddr=baselineParent=replayParent=0;baselinePath=replayPath="not found";rootsBase.Clear();
            waitPolls=effectPolls=expiryStable=0;effectStartStamp=0;baselineWallMs=replayWallMs=0;
            patchApplied=restoreAttempted=restoreOk=replayConfigMatched=false;lastWrite="none";
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
