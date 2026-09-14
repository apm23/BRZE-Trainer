using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectContainerProbeV4;

internal sealed record NodeSnapshot(uint Address,string Path,int Depth,byte[] Bytes);
internal sealed record ProbeSnapshot(string Game,string Phase,IReadOnlyList<string> Lines);

internal sealed class FieldStat
{
    public required string Key { get; init; }
    public uint ControlStartRaw { get; set; }
    public uint ControlLastRaw { get; set; }
    public int ControlChanges { get; set; }
    public int ControlFlips { get; set; }
    public int ControlLastDirection { get; set; }
    public int ControlSeen { get; set; }

    public bool HasPre { get; set; }
    public uint PreRaw { get; set; }

    public bool HasEffect { get; set; }
    public uint EffectStartRaw { get; set; }
    public uint EffectLastRaw { get; set; }
    public int EffectChanges { get; set; }
    public int EffectFlips { get; set; }
    public int EffectLastDirection { get; set; }
    public int EffectSeen { get; set; }
    public int EffectMissing { get; set; }
    public bool NewInEffect { get; set; }

    public bool ExpiryMarked { get; set; }
    public bool ExpiryReadable { get; set; }
    public uint ExpiryRaw { get; set; }

    static float F(uint raw)=>BitConverter.Int32BitsToSingle(unchecked((int)raw));
    static string FS(uint raw)
    {
        float f=F(raw);
        return float.IsFinite(f)?f.ToString("0.#####"):"nan/inf";
    }

    public double Score()
    {
        if(!HasEffect)return double.NegativeInfinity;
        double s=EffectChanges*1.8-EffectFlips*4.5-ControlChanges*7.5-ControlFlips*1.5;
        if(ControlChanges==0)s+=45;
        if(NewInEffect)s+=25;
        if(EffectChanges>=5&&EffectFlips<=1)s+=35;
        if(ExpiryMarked)
        {
            if(!ExpiryReadable){s+=NewInEffect?110:65;}
            else
            {
                if(HasPre&&ExpiryRaw==PreRaw)s+=95;
                if(ExpiryRaw==0&&EffectStartRaw!=0)s+=65;
                if(ExpiryRaw!=EffectLastRaw)s+=12;
            }
        }
        if(EffectChanges==0&&NewInEffect&&ExpiryMarked&&!ExpiryReadable)s+=35;
        if(ControlSeen==0)s+=10;
        return s;
    }

    public string Format()
    {
        string ctrl=ControlSeen==0?"CTRL unseen":$"CTRL chg {ControlChanges,3} flip {ControlFlips,2} 0x{ControlStartRaw:X8}->{ControlLastRaw:X8}";
        string pre=HasPre?$"PRE 0x{PreRaw:X8}/{unchecked((int)PreRaw)}/{FS(PreRaw)}":"PRE absent";
        string fx=HasEffect?$"FX chg {EffectChanges,3} flip {EffectFlips,2} 0x{EffectStartRaw:X8}->{EffectLastRaw:X8}":"FX absent";
        string exp=!ExpiryMarked?"EXP -":ExpiryReadable?$"EXP 0x{ExpiryRaw:X8}/{unchecked((int)ExpiryRaw)}/{FS(ExpiryRaw)}":"EXP UNREADABLE";
        return $"score {Score(),6:0.0} | {(NewInEffect?"NEW":"   ")} | {Key,-66} | {ctrl} | {pre} | {fx} | {exp}";
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
    const int NODE_SCAN=0x180;
    const int MAX_DEPTH=4;
    const int MAX_NODES=260;

    static readonly int[] RootOffsets={0x094,0x098,0x1E4,0x1E8,0x1F4,0x20C,0x210,0x214,0x21C};

    static readonly object sync=new();
    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status="not attached";

    static uint unit;
    static uint unitDef;
    static uint owner;
    static string phase="IDLE — select ONE clean unit";
    static bool controlDone;
    static bool effectStarted;
    static bool expiryMarked;
    static int controlSamples;
    static int effectSamples;

    static readonly Dictionary<string,FieldStat> stats=new(StringComparer.Ordinal);
    static Dictionary<string,uint> preFields=new(StringComparer.Ordinal);
    static Dictionary<int,uint> preRoots=new();
    static Dictionary<int,uint> effectRoots=new();
    static Dictionary<int,uint> expiryRoots=new();

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);

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
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<250)return false;
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

    static bool SameUnitSelected()
    {
        var (u,c)=FirstSelectedUnit();
        if(c!=1||u==0||u!=unit)return false;
        return ReadU32(u+OFF_DEF)==unitDef&&ReadU32(u+OFF_OWNER)==owner;
    }

    static bool PlausiblePtr(uint p)=>p>=0x00010000&&p<0x7FFF0000;

    static Dictionary<int,uint> CaptureRoots(uint u)
    {
        var d=new Dictionary<int,uint>();
        foreach(int off in RootOffsets)d[off]=ReadU32(u+off);
        return d;
    }

    static Dictionary<string,NodeSnapshot> CaptureGraph(uint u)
    {
        var result=new Dictionary<string,NodeSnapshot>(StringComparer.Ordinal);
        var q=new Queue<(uint addr,string path,int depth)>();
        var visited=new HashSet<uint>();
        foreach(int off in RootOffsets)
        {
            uint p=ReadU32(u+off);
            if(PlausiblePtr(p))q.Enqueue((p,$"Unit+0x{off:X3}",1));
        }

        while(q.Count>0&&result.Count<MAX_NODES)
        {
            var cur=q.Dequeue();
            if(!visited.Add(cur.addr))continue;
            var bytes=new byte[NODE_SCAN];
            if(!ReadExact(cur.addr,bytes))continue;
            result[cur.path]=new NodeSnapshot(cur.addr,cur.path,cur.depth,bytes);
            if(cur.depth>=MAX_DEPTH)continue;
            for(int o=0;o<=bytes.Length-4&&result.Count+q.Count<MAX_NODES;o+=4)
            {
                uint p=U32(bytes,o);
                if(!PlausiblePtr(p)||visited.Contains(p))continue;
                q.Enqueue((p,$"{cur.path}->+0x{o:X3}",cur.depth+1));
            }
        }
        return result;
    }

    static bool NumericInteresting(uint raw)
    {
        int i=unchecked((int)raw);
        if(Math.Abs((long)i)<=20_000_000L)return true;
        float f=BitConverter.Int32BitsToSingle(i);
        if(!float.IsFinite(f))return false;
        float a=Math.Abs(f);
        return a==0f||(a>=0.000001f&&a<=200000f);
    }

    static Dictionary<string,uint> Flatten(Dictionary<string,NodeSnapshot> graph)
    {
        var d=new Dictionary<string,uint>(StringComparer.Ordinal);
        foreach(var n in graph.Values)
        {
            for(int o=0;o<=n.Bytes.Length-4;o+=4)
            {
                uint raw=U32(n.Bytes,o);
                if(!NumericInteresting(raw))continue;
                d[$"{n.Path}->field+0x{o:X3}"]=raw;
            }
        }
        return d;
    }

    static FieldStat Get(string key,uint seed,bool control)
    {
        if(stats.TryGetValue(key,out var s))return s;
        s=new FieldStat{Key=key};
        if(control){s.ControlStartRaw=seed;s.ControlLastRaw=seed;}
        stats[key]=s;
        return s;
    }

    static void UpdateDirection(uint oldRaw,uint newRaw,ref int flips,ref int lastDir)
    {
        long d=(long)unchecked((int)newRaw)-unchecked((int)oldRaw);
        int dir=d>0?1:d<0?-1:0;
        if(dir!=0&&lastDir!=0&&dir!=lastDir)flips++;
        if(dir!=0)lastDir=dir;
    }

    public static string StartControl()
    {
        lock(sync)
        {
            if(!Attach())return phase="CONTROL failed — "+status;
            var (u,c)=FirstSelectedUnit();
            if(c!=1||u==0)return phase=$"CONTROL blocked — select exactly ONE normal unit (selected {c})";
            unit=u;unitDef=ReadU32(u+OFF_DEF);owner=ReadU32(u+OFF_OWNER);
            if(unitDef==0)return phase="CONTROL blocked — selected object has no UnitDef";
            stats.Clear();preFields.Clear();preRoots.Clear();effectRoots.Clear();expiryRoots.Clear();
            controlDone=false;effectStarted=false;expiryMarked=false;controlSamples=0;effectSamples=0;
            var f=Flatten(CaptureGraph(unit));
            foreach(var kv in f)
            {
                var s=Get(kv.Key,kv.Value,true);s.ControlSeen=1;
            }
            return phase=$"MOVEMENT CONTROL started Unit*=0x{unit:X8}. Keep this unit WALKING for 15 seconds. NO hero buff.";
        }
    }

    public static string SampleControl()
    {
        lock(sync)
        {
            if(unit==0)return phase;
            if(!SameUnitSelected())return phase="CONTROL PAUSED — selected unit changed; RESET and repeat";
            var now=Flatten(CaptureGraph(unit));
            foreach(var kv in now)
            {
                var s=Get(kv.Key,kv.Value,true);
                if(s.ControlSeen==0){s.ControlStartRaw=kv.Value;s.ControlLastRaw=kv.Value;s.ControlSeen=1;continue;}
                s.ControlSeen++;
                if(kv.Value!=s.ControlLastRaw)
                {
                    int flips=s.ControlFlips,dir=s.ControlLastDirection;
                    UpdateDirection(s.ControlLastRaw,kv.Value,ref flips,ref dir);
                    s.ControlFlips=flips;s.ControlLastDirection=dir;s.ControlChanges++;s.ControlLastRaw=kv.Value;
                }
            }
            controlSamples++;
            return phase=$"MOVEMENT CONTROL sample {controlSamples}. Keep walking; no buffs.";
        }
    }

    public static string FinishControl()
    {
        lock(sync)
        {
            controlDone=true;
            return phase=$"MOVEMENT CONTROL complete ({controlSamples} samples). STOP the unit, then CAPTURE PRE-EFFECT.";
        }
    }

    public static string CapturePreEffect()
    {
        lock(sync)
        {
            if(!controlDone)return phase="PRE-EFFECT blocked — finish movement control first";
            if(!SameUnitSelected())return phase="PRE-EFFECT blocked — selected unit changed";
            preFields=Flatten(CaptureGraph(unit));
            preRoots=CaptureRoots(unit);
            foreach(var kv in preFields)
            {
                var s=Get(kv.Key,kv.Value,false);s.HasPre=true;s.PreRaw=kv.Value;
            }
            return phase=$"PRE-EFFECT captured: {preFields.Count} numeric fields. Cast ORIGINAL Grayback ONCE now, then immediately START EFFECT WATCH.";
        }
    }

    public static string StartEffect()
    {
        lock(sync)
        {
            if(preFields.Count==0)return phase="EFFECT blocked — capture PRE-EFFECT first";
            if(!SameUnitSelected())return phase="EFFECT blocked — selected unit changed";
            effectStarted=true;expiryMarked=false;effectSamples=0;
            effectRoots=CaptureRoots(unit);
            var now=Flatten(CaptureGraph(unit));
            foreach(var kv in now)
            {
                var s=Get(kv.Key,kv.Value,false);
                if(!s.HasPre&&preFields.TryGetValue(kv.Key,out uint pr)){s.HasPre=true;s.PreRaw=pr;}
                s.HasEffect=true;s.EffectStartRaw=kv.Value;s.EffectLastRaw=kv.Value;s.EffectSeen=1;
                s.NewInEffect=!preFields.ContainsKey(kv.Key)&&s.ControlSeen==0;
            }
            return phase=$"EFFECT WATCH started with {now.Count} numeric fields. Do not recast. MARK EXPIRED when visible Grayback ends.";
        }
    }

    public static string SampleEffect()
    {
        lock(sync)
        {
            if(!effectStarted||expiryMarked)return phase;
            if(!SameUnitSelected())return phase="EFFECT WATCH PAUSED — selected unit changed";
            var now=Flatten(CaptureGraph(unit));
            var present=new HashSet<string>(now.Keys,StringComparer.Ordinal);
            foreach(var s in stats.Values.Where(x=>x.HasEffect&&x.EffectSeen>0))
                if(!present.Contains(s.Key))s.EffectMissing++;

            foreach(var kv in now)
            {
                var s=Get(kv.Key,kv.Value,false);
                if(!s.HasEffect)
                {
                    s.HasEffect=true;s.EffectStartRaw=kv.Value;s.EffectLastRaw=kv.Value;s.EffectSeen=1;
                    s.NewInEffect=!preFields.ContainsKey(kv.Key)&&s.ControlSeen==0;
                    if(preFields.TryGetValue(kv.Key,out uint pr)){s.HasPre=true;s.PreRaw=pr;}
                    continue;
                }
                s.EffectSeen++;
                if(kv.Value!=s.EffectLastRaw)
                {
                    int flips=s.EffectFlips,dir=s.EffectLastDirection;
                    UpdateDirection(s.EffectLastRaw,kv.Value,ref flips,ref dir);
                    s.EffectFlips=flips;s.EffectLastDirection=dir;s.EffectChanges++;s.EffectLastRaw=kv.Value;
                }
            }
            effectSamples++;
            return phase=$"EFFECT WATCH sample {effectSamples}. Do not recast; MARK EXPIRED at natural visible expiry.";
        }
    }

    static string RootLine(int off,uint pre,uint fx,uint exp)
        =>$"ROOT Unit+0x{off:X3}: PRE 0x{pre:X8} | FX-start 0x{fx:X8} | EXP 0x{exp:X8}";

    public static ProbeSnapshot MarkExpired(int top=180)
    {
        lock(sync)
        {
            if(!effectStarted)return Snapshot(top);
            expiryMarked=true;
            var now=Flatten(CaptureGraph(unit));
            expiryRoots=CaptureRoots(unit);
            foreach(var s in stats.Values.Where(x=>x.HasEffect))
            {
                s.ExpiryMarked=true;
                if(now.TryGetValue(s.Key,out uint raw)){s.ExpiryReadable=true;s.ExpiryRaw=raw;}
                else{s.ExpiryReadable=false;}
            }
            phase="EXPIRY MARKED — movement-only changes are penalized; effect-only fields/objects returning to baseline or disappearing are boosted. COPY REPORT.";
            return Snapshot(top);
        }
    }

    public static ProbeSnapshot Snapshot(int top=140)
    {
        lock(sync)
        {
            Attach();
            var lines=new List<string>();
            if(preRoots.Count>0||effectRoots.Count>0||expiryRoots.Count>0)
            {
                lines.Add("=== ROOT POINTER LIFECYCLE ===");
                foreach(int off in RootOffsets)
                {
                    preRoots.TryGetValue(off,out uint pr);effectRoots.TryGetValue(off,out uint fx);expiryRoots.TryGetValue(off,out uint ex);
                    lines.Add(RootLine(off,pr,fx,ex));
                }
                lines.Add("");lines.Add("=== RANKED CONTAINER FIELDS ===");
            }
            var ranked=stats.Values.Where(x=>x.HasEffect).OrderByDescending(x=>x.Score()).ThenBy(x=>x.ControlChanges).Take(top);
            lines.AddRange(ranked.Select(x=>x.Format()));
            if(lines.Count==0)lines.Add("No effect candidates yet. Run movement control -> PRE-EFFECT -> original Grayback -> effect watch -> expiry mark.");
            return new(status,phase,lines);
        }
    }

    public static string Reset()
    {
        lock(sync)
        {
            unit=unitDef=owner=0;stats.Clear();preFields.Clear();preRoots.Clear();effectRoots.Clear();expiryRoots.Clear();
            controlDone=false;effectStarted=false;expiryMarked=false;controlSamples=effectSamples=0;
            return phase="IDLE — select ONE clean unit";
        }
    }

    public static void Shutdown(){lock(sync)Detach();}
}
