using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal enum DirectHeroMode { Issyl, Grayback, Both }

internal static class HeroEffectDirectCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll", SetLastError=true)] static extern UIntPtr VirtualQueryEx(IntPtr h,IntPtr addr,out MEMORY_BASIC_INFORMATION mbi,UIntPtr size);

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

    sealed class TargetTrack
    {
        public uint Unit,Def,Owner;
        public readonly uint[] Baseline=new uint[5];
        public int State; // 0 waiting, 1 active, 2 complete, 3 skipped
        public int Stable;
    }

    const uint ACCESS=0x10|0x20|0x8|0x400;
    const uint MEM_COMMIT=0x1000;
    const uint PAGE_GUARD=0x100,PAGE_NOACCESS=0x01;
    const int RVA_SELECTION_LIST=0x441708;
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    static readonly int[] ROOTS={0x1E4,0x1E8,0x20C,0x210,0x214};
    const int CONFIG_DURATION_OFF=0x0E0;
    const uint ISSYL_ID=0xA5,GRAYBACK_ID=0xC0;
    const uint ISSYL_BASE=15000,GRAYBACK_BASE=60000;
    const uint MIN_NOMINAL=1000,MAX_NOMINAL=600000;
    const uint ISSYL_KNOWN=0x227F4664,GRAYBACK_KNOWN=0x227F9470;
    const double ISSYL_BASE_WALL_SEC=10.7271;
    const double GRAYBACK_BASE_WALL_SEC=42.2221;
    const int MAX_SELECTED=120;

    static readonly object Sync=new();
    static readonly List<TargetTrack> targets=new();
    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static bool active,queueDone;
    static uint issylConfig,grayConfig,issylDesired,grayDesired;
    static bool issylPatched,grayPatched;
    static string status="READY — select unit(s), choose duration, then APPLY ISSYL / GRAYBACK / BOTH.";
    static DateTime lastTry;
    static DirectHeroMode activeMode;
    static decimal activeSeconds;
    static long applyStartStamp;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach();
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<150)return false;
        lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0){status="WAITING — Battle_Realms_F.exe is not running.";return false;}
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}
        catch{process=null;status="ERROR — cannot resolve module base.";return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        if(h==IntPtr.Zero){status="ERROR — OpenProcess READ/WRITE failed.";process=null;moduleBase=0;return false;}
        return true;
    }

    static void Detach()
    {
        try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}
        h=IntPtr.Zero;process=null;moduleBase=0;
    }

    static bool ReadExact(long address,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(address),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long address){var b=new byte[4];return ReadExact(address,b)?BitConverter.ToUInt32(b,0):0;}
    static bool W32(long address,uint value)
    {
        if(h==IntPtr.Zero)return false;
        var b=BitConverter.GetBytes(value);
        return WriteProcessMemory(h,A(address),b,4,out var n)&&n.ToInt64()==4&&R32(address)==value;
    }

    static bool Readable(uint protect)
    {
        if((protect&PAGE_GUARD)!=0)return false;
        uint p=protect&0xFF;
        return p!=0&&p!=PAGE_NOACCESS;
    }

    static bool ValidateConfig(uint addr,uint id,uint baseline)=>addr>=0x10000&&R32(addr)==id&&R32((long)addr+CONFIG_DURATION_OFF)==baseline;

    static uint ResolveConfig(uint id,uint baseline,uint preferred)
    {
        if(ValidateConfig(preferred,id,baseline))return preferred;
        uint near=ScanForConfig(id,baseline,0x18000000u,0x30000000u,preferred);
        if(near!=0)return near;
        return ScanForConfig(id,baseline,0x00010000u,0x7FFF0000u,preferred);
    }

    static uint ScanForConfig(uint id,uint baseline,uint low,uint high,uint preferred)
    {
        if(h==IntPtr.Zero)return 0;
        var hits=new List<uint>();
        ulong cursor=low,limit=high;
        int mbiSize=Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();
        const int CHUNK=1024*1024,OVERLAP=0xE4;
        while(cursor<limit)
        {
            if(VirtualQueryEx(h,A((long)cursor),out var mbi,(UIntPtr)mbiSize)==UIntPtr.Zero)break;
            ulong baseAddr=unchecked((uint)mbi.BaseAddress.ToInt64());
            ulong region=mbi.RegionSize.ToUInt64();
            if(region==0){cursor+=0x1000;continue;}
            ulong end=Math.Min(baseAddr+region,limit);
            if(mbi.State==MEM_COMMIT&&Readable(mbi.Protect)&&end>low&&baseAddr<limit)
            {
                ulong start=Math.Max(baseAddr,low);
                for(ulong pos=start;pos<end;pos+=(ulong)CHUNK)
                {
                    int want=(int)Math.Min((ulong)(CHUNK+OVERLAP),end-pos);
                    if(want<CONFIG_DURATION_OFF+4)continue;
                    var buf=new byte[want];
                    if(!ReadProcessMemory(h,A((long)pos),buf,want,out var got)||got.ToInt64()<CONFIG_DURATION_OFF+4)continue;
                    int n=(int)got.ToInt64();
                    for(int o=0;o<=n-(CONFIG_DURATION_OFF+4);o+=4)
                    {
                        if(BitConverter.ToUInt32(buf,o)!=id||BitConverter.ToUInt32(buf,o+CONFIG_DURATION_OFF)!=baseline)continue;
                        ulong a=pos+(ulong)o;if(a>=low&&a<high)hits.Add((uint)a);
                    }
                }
            }
            ulong next=baseAddr+region;cursor=next>cursor?next:cursor+0x1000;
        }
        if(hits.Count==0)return 0;
        uint best=hits[0];ulong bestDist=best>preferred?(ulong)(best-preferred):(ulong)(preferred-best);
        foreach(uint x in hits)
        {
            ulong d=x>preferred?(ulong)(x-preferred):(ulong)(preferred-x);
            if(d<bestDist){best=x;bestDist=d;}
        }
        return ValidateConfig(best,id,baseline)?best:0;
    }

    static uint NominalForSeconds(decimal seconds,uint baseline,double baselineWallSeconds)
    {
        double v=(double)seconds*baseline/baselineWallSeconds;
        uint n=(uint)Math.Round(v,MidpointRounding.AwayFromZero);
        return Math.Clamp(n,MIN_NOMINAL,MAX_NOMINAL);
    }

    static bool PatchOne(uint cfg,uint id,uint baseline,uint desired,out string error)
    {
        error="";
        if(cfg==0||R32(cfg)!=id){error=$"config identity mismatch for 0x{id:X2}";return false;}
        uint cur=R32((long)cfg+CONFIG_DURATION_OFF);
        if(cur!=baseline){error=$"config 0x{id:X2} duration guard failed: expected {baseline}, found {cur}";return false;}
        if(desired==baseline)return true;
        if(!W32((long)cfg+CONFIG_DURATION_OFF,desired)){error=$"write/verify failed for 0x{id:X2}";return false;}
        return true;
    }

    static bool RestoreOne(uint cfg,uint id,uint baseline,uint desired,bool wasPatched)
    {
        if(!wasPatched)return true;
        if(cfg==0||R32(cfg)!=id)return false;
        uint cur=R32((long)cfg+CONFIG_DURATION_OFF);
        if(cur==baseline)return true;
        if(cur!=desired)return false;
        return W32((long)cfg+CONFIG_DURATION_OFF,baseline);
    }

    static bool RestoreAll()
    {
        if(!Attach())return false;
        bool a=RestoreOne(issylConfig,ISSYL_ID,ISSYL_BASE,issylDesired,issylPatched);
        bool b=RestoreOne(grayConfig,GRAYBACK_ID,GRAYBACK_BASE,grayDesired,grayPatched);
        if(a)issylPatched=false;if(b)grayPatched=false;
        return a&&b;
    }

    static uint[] Roots(uint unit)
    {
        var r=new uint[ROOTS.Length];for(int i=0;i<r.Length;i++)r[i]=R32((long)unit+ROOTS[i]);return r;
    }

    static bool RootsClean(uint[] r)=>r[0]==0&&r[1]==0&&r[2]==0&&r[3]==0;
    static bool Same(uint[] a,uint[] b){for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    static bool EffectPattern(uint[] baseline,uint[] now)
    {
        int changed=0;for(int i=0;i<now.Length;i++)if(now[i]!=baseline[i])changed++;
        bool transientChanged=false;for(int i=0;i<4;i++)if(now[i]!=baseline[i]&&Ptr(now[i])){transientChanged=true;break;}
        return changed>=2&&transientChanged;
    }

    static bool CaptureTargets(out string error)
    {
        error="";targets.Clear();
        if(!Attach()){error=status;return false;}
        long list=moduleBase+RVA_SELECTION_LIST;uint count=R32(list+0x18),node=R32(list);
        if(count==0||node==0){error="no units selected";return false;}
        var seenNodes=new HashSet<uint>();var seenUnits=new HashSet<uint>();int limit=(int)Math.Min(count,MAX_SELECTED);
        for(int i=0;i<limit&&node!=0;i++)
        {
            if(!seenNodes.Add(node))break;
            uint next=R32((long)node),unit=R32((long)node+0x08);
            if(unit!=0&&seenUnits.Add(unit))
            {
                uint def=R32((long)unit+OFF_DEF),owner=R32((long)unit+OFF_OWNER);
                if(def!=0)
                {
                    var roots=Roots(unit);
                    if(!RootsClean(roots)){targets.Clear();error=$"selected unit 0x{unit:X8} has an active transient effect — wait until it is clean";return false;}
                    var t=new TargetTrack{Unit=unit,Def=def,Owner=owner};Array.Copy(roots,t.Baseline,roots.Length);targets.Add(t);
                }
            }
            node=next;
        }
        if(targets.Count==0){error="selection contained no valid units";return false;}
        return true;
    }

    static bool UnitValid(TargetTrack t)=>R32((long)t.Unit+OFF_DEF)==t.Def&&R32((long)t.Unit+OFF_OWNER)==t.Owner;
    static double ElapsedSec()=>applyStartStamp==0?0:(Stopwatch.GetTimestamp()-applyStartStamp)/(double)Stopwatch.Frequency;

    public static string Start(DirectHeroMode mode,decimal seconds)
    {
        lock(Sync)
        {
            if(active)return status="BLOCKED — previous Hero Effect apply is still active.";
            if(seconds<1m||seconds>420m)return status="BLOCKED — duration must be 1–420 seconds.";
            if(IntegratedFrameDispatcherCore.NativeQueueBusy())return status="BLOCKED — shared native queue is busy (Copy Unit / Hero Effect).";
            if(!CaptureTargets(out string targetError))return status="BLOCKED — "+targetError;

            bool needIssyl=mode is DirectHeroMode.Issyl or DirectHeroMode.Both;
            bool needGray=mode is DirectHeroMode.Grayback or DirectHeroMode.Both;
            issylConfig=grayConfig=0;issylPatched=grayPatched=false;
            issylDesired=NominalForSeconds(seconds,ISSYL_BASE,ISSYL_BASE_WALL_SEC);
            grayDesired=NominalForSeconds(seconds,GRAYBACK_BASE,GRAYBACK_BASE_WALL_SEC);

            if(needIssyl)
            {
                issylConfig=ResolveConfig(ISSYL_ID,ISSYL_BASE,ISSYL_KNOWN);
                if(issylConfig==0){targets.Clear();return status="BLOCKED — could not safely resolve Issyl A5 config (expected ID A5 / duration 15000).";}
            }
            if(needGray)
            {
                grayConfig=ResolveConfig(GRAYBACK_ID,GRAYBACK_BASE,GRAYBACK_KNOWN);
                if(grayConfig==0){targets.Clear();return status="BLOCKED — could not safely resolve Grayback C0 config (expected ID C0 / duration 60000).";}
            }

            if(needIssyl)
            {
                if(!PatchOne(issylConfig,ISSYL_ID,ISSYL_BASE,issylDesired,out string e)){targets.Clear();return status="BLOCKED — "+e;}
                issylPatched=issylDesired!=ISSYL_BASE;
            }
            if(needGray)
            {
                if(!PatchOne(grayConfig,GRAYBACK_ID,GRAYBACK_BASE,grayDesired,out string e))
                {
                    RestoreAll();targets.Clear();return status="BLOCKED — "+e;
                }
                grayPatched=grayDesired!=GRAYBACK_BASE;
            }

            uint a1=mode==DirectHeroMode.Issyl?ISSYL_ID:GRAYBACK_ID;
            uint a2=mode==DirectHeroMode.Both?ISSYL_ID:uint.MaxValue;
            string label=mode==DirectHeroMode.Issyl?"ISSYL":mode==DirectHeroMode.Grayback?"GRAYBACK":"BOTH";
            string q=IntegratedFrameDispatcherCore.QueueReplaySelected(a1,a2,$"{label} ~{seconds:0.#}s");
            if(!q.Contains("queued",StringComparison.OrdinalIgnoreCase))
            {
                bool restored=RestoreAll();targets.Clear();
                return status=$"APPLY FAILED — {q} | restore {(restored?"OK":"FAILED")}";
            }

            active=true;queueDone=false;activeMode=mode;activeSeconds=seconds;applyStartStamp=Stopwatch.GetTimestamp();
            string nom=mode==DirectHeroMode.Issyl?$"A5 nominal {issylDesired}":mode==DirectHeroMode.Grayback?$"C0 nominal {grayDesired}":$"A5 {issylDesired} / C0 {grayDesired}";
            return status=$"APPLIED {label} queue to {targets.Count} selected unit(s) · target ~{seconds:0.#}s · {nom}. Duration config will stay held until natural expiry.";
        }
    }

    public static string Tick()
    {
        lock(Sync)
        {
            if(!active)return status;
            if(!Attach())
            {
                active=false;targets.Clear();return status="BRZE closed/detached — Hero Effect tracking ended.";
            }
            if(!queueDone&&!IntegratedFrameDispatcherCore.NativeQueueBusy())queueDone=true;

            int waiting=0,running=0,complete=0,skipped=0;
            double elapsed=ElapsedSec();
            foreach(var t in targets)
            {
                if(t.State>=2){if(t.State==2)complete++;else skipped++;continue;}
                if(!UnitValid(t)){t.State=3;skipped++;continue;}
                uint[] now=Roots(t.Unit);
                if(t.State==0)
                {
                    if(EffectPattern(t.Baseline,now)){t.State=1;t.Stable=0;running++;}
                    else if(queueDone&&elapsed>=2.0){t.State=3;skipped++;}
                    else waiting++;
                }
                else
                {
                    if(Same(t.Baseline,now))t.Stable++;else t.Stable=0;
                    if(t.Stable>=4){t.State=2;complete++;}else running++;
                }
            }

            bool allTerminal=waiting==0&&running==0;
            if(allTerminal&&queueDone)
            {
                bool restored=RestoreAll();
                string label=activeMode==DirectHeroMode.Issyl?"ISSYL":activeMode==DirectHeroMode.Grayback?"GRAYBACK":"BOTH";
                active=false;targets.Clear();applyStartStamp=0;
                status=restored?$"DONE — {label} requested ~{activeSeconds:0.#}s · completed {complete}, skipped {skipped}. Duration config restored after natural expiry.":$"WARNING — {label} effect finished, but automatic duration restore FAILED/BLOCKED. Restart BRZE before another Hero Effect apply.";
                return status;
            }

            string mode=activeMode==DirectHeroMode.Issyl?"ISSYL":activeMode==DirectHeroMode.Grayback?"GRAYBACK":"BOTH";
            status=$"{mode} ~{activeSeconds:0.#}s ACTIVE · waiting {waiting} · running {running} · complete {complete} · skipped {skipped} · duration config HELD until natural expiry.";
            return status;
        }
    }

    public static string Status(){lock(Sync)return status;}
    public static bool Busy(){lock(Sync)return active;}

    public static void Shutdown()
    {
        lock(Sync)
        {
            if(active||issylPatched||grayPatched)RestoreAll();
            active=false;queueDone=false;targets.Clear();issylPatched=grayPatched=false;applyStartStamp=0;Detach();
            status="READY — select unit(s), choose duration, then APPLY ISSYL / GRAYBACK / BOTH.";
        }
    }
}
