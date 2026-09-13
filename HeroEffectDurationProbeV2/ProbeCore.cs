using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectDurationProbeV2;

internal sealed class Candidate
{
    public required string Label { get; init; }
    public required uint Address { get; init; }
    public required uint BaselineRaw { get; init; }
    public required uint EffectRaw { get; init; }
    public required int Depth { get; init; }
    public bool FromNewObject { get; init; }
    public uint LastRaw { get; set; }
    public int Changes { get; set; }
    public int SameSamples { get; set; }
    public int DirectionFlips { get; set; }
    public int LastDirection { get; set; }
    public int UnreadableSamples { get; set; }
    public bool ExpiryMarked { get; set; }
    public bool ExpiryReadable { get; set; }
    public uint ExpiryRaw { get; set; }
    public double ExpiryBonus { get; set; }

    static float F(uint raw) => BitConverter.Int32BitsToSingle(unchecked((int)raw));
    static string FS(uint raw)
    {
        float f = F(raw);
        return float.IsFinite(f) ? f.ToString("0.#####") : "nan/inf";
    }

    public string Format(uint current, double score)
    {
        string expiry = !ExpiryMarked ? "" : ExpiryReadable
            ? $" | expiry 0x{ExpiryRaw:X8}/{FS(ExpiryRaw)}"
            : " | expiry UNREADABLE";
        return $"score {score,6:0.0} | d{Depth} {(FromNewObject ? "NEW" : "   ")} | {Label,-43} @0x{Address:X8} raw 0x{current:X8} | int {unchecked((int)current),11} | float {FS(current),11} | base {FS(BaselineRaw),11} | effect {FS(EffectRaw),11} | chg {Changes,3} flip {DirectionFlips,2}{expiry}";
    }
}

internal sealed record ProbeSnapshot(string Game, string Phase, IReadOnlyList<string> Lines);
internal sealed record NodeSnapshot(uint Address, string Path, int Depth, byte[] Bytes);

internal static class ProbeCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_READ = 0x0010;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const uint ACCESS = PROCESS_VM_READ | PROCESS_QUERY_INFORMATION;

    const int RVA_SELECTION_LIST = 0x441708;
    const int UNIT_SCAN_SIZE = 0x500;
    const int NODE_SCAN_SIZE = 0x200;
    const int MAX_DEPTH = 3;
    const int MAX_NODES = 700;
    const int MAX_CANDIDATES = 3000;

    static Process? process;
    static IntPtr h = IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status = "not attached";

    static uint baselineUnit;
    static Dictionary<uint,NodeSnapshot> baselineGraph = new();
    static Dictionary<uint,NodeSnapshot> effectGraph = new();
    static readonly List<Candidate> candidates = new();
    static string phase = "IDLE — select ONE clean unit and capture baseline";
    static int samples;
    static bool expiryMarked;

    static IntPtr A(long x) => new(unchecked((int)(uint)x));
    static uint U32(byte[] b, int o) => BitConverter.ToUInt32(b,o);

    static bool ReadExact(long address, byte[] buffer)
    {
        if(h == IntPtr.Zero) return false;
        return ReadProcessMemory(h,A(address),buffer,buffer.Length,out var n) && n.ToInt64()==buffer.Length;
    }

    static bool Attach()
    {
        try { if(process != null && !process.HasExited && h != IntPtr.Zero) return true; } catch { }
        Detach();
        if((DateTime.UtcNow-lastTry).TotalMilliseconds < 300) return false;
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
        uint node=U32(list,0x00),count=U32(list,0x18);
        if(node==0||count==0)return (0,count);
        var n=new byte[0x0C];
        if(!ReadExact(node,n))return (0,count);
        return (U32(n,0x08),count);
    }

    static bool PlausiblePtr(uint p)=>p>=0x00010000 && p<0x7FFF0000;

    static Dictionary<uint,NodeSnapshot> CaptureGraph(uint unit)
    {
        var result=new Dictionary<uint,NodeSnapshot>();
        var q=new Queue<(uint address,string path,int depth,int size)>();
        q.Enqueue((unit,"Unit",0,UNIT_SCAN_SIZE));

        while(q.Count>0 && result.Count<MAX_NODES)
        {
            var cur=q.Dequeue();
            if(result.ContainsKey(cur.address))continue;
            var bytes=new byte[cur.size];
            if(!ReadExact(cur.address,bytes))continue;
            var node=new NodeSnapshot(cur.address,cur.path,cur.depth,bytes);
            result[cur.address]=node;
            if(cur.depth>=MAX_DEPTH)continue;

            for(int o=0;o<=bytes.Length-4 && result.Count+q.Count<MAX_NODES;o+=4)
            {
                uint p=U32(bytes,o);
                if(!PlausiblePtr(p) || result.ContainsKey(p))continue;
                string path=cur.depth==0 ? $"[Unit+0x{o:X3}]" : $"{cur.path}->+0x{o:X3}";
                q.Enqueue((p,path,cur.depth+1,NODE_SCAN_SIZE));
            }
        }
        return result;
    }

    static bool NumericInteresting(uint raw)
    {
        int i=unchecked((int)raw);
        if(Math.Abs((long)i)<=10_000_000L)return true;
        float f=BitConverter.Int32BitsToSingle(i);
        if(!float.IsFinite(f))return false;
        float a=Math.Abs(f);
        return a==0f || (a>=0.000001f && a<=100000f);
    }

    static bool ChangedInteresting(uint a,uint b)
    {
        if(a==b)return false;
        return NumericInteresting(a)||NumericInteresting(b);
    }

    static void AddCandidate(HashSet<uint> seen,string label,uint address,uint a,uint b,int depth,bool fromNew)
    {
        if(candidates.Count>=MAX_CANDIDATES || !seen.Add(address))return;
        candidates.Add(new Candidate
        {
            Label=label,Address=address,BaselineRaw=a,EffectRaw=b,Depth=depth,FromNewObject=fromNew,LastRaw=b
        });
    }

    public static string CaptureBaseline()
    {
        if(!Attach())return phase="BASELINE failed — "+status;
        var (unit,count)=FirstSelectedUnit();
        if(unit==0)return phase=$"BASELINE failed — select exactly ONE normal unit (selected {count})";
        if(count!=1)return phase=$"BASELINE blocked — selected count {count}; use exactly ONE unit";
        baselineUnit=unit;
        baselineGraph=CaptureGraph(unit);
        effectGraph.Clear();candidates.Clear();samples=0;expiryMarked=false;
        return phase=$"BASELINE captured Unit*=0x{unit:X8} | recursive graph nodes {baselineGraph.Count} | depth <= {MAX_DEPTH}. NOW cast ONE original hero buff on this unit.";
    }

    public static string CaptureEffectDiff()
    {
        if(baselineUnit==0||baselineGraph.Count==0)return phase="EFFECT DIFF blocked — capture baseline first";
        if(!Attach())return phase="EFFECT DIFF failed — "+status;
        effectGraph=CaptureGraph(baselineUnit);
        candidates.Clear();samples=0;expiryMarked=false;
        var seen=new HashSet<uint>();

        foreach(var now in effectGraph.Values.OrderBy(n=>n.Depth))
        {
            bool hadBase=baselineGraph.TryGetValue(now.Address,out var beforeNode);
            byte[] before=hadBase ? beforeNode!.Bytes : new byte[now.Bytes.Length];
            int lim=Math.Min(before.Length,now.Bytes.Length);
            for(int o=0;o<=lim-4;o+=4)
            {
                uint a=U32(before,o),b=U32(now.Bytes,o);
                if(!ChangedInteresting(a,b))continue;
                string label=now.Depth==0?$"Unit+0x{o:X3}":$"{now.Path}->+0x{o:X3}";
                AddCandidate(seen,label,now.Address+(uint)o,a,b,now.Depth,!hadBase);
            }
            if(candidates.Count>=MAX_CANDIDATES)break;
        }

        return phase=$"EFFECT DIFF: graph {baselineGraph.Count}->{effectGraph.Count} nodes | {candidates.Count} candidates. START WATCH; do not recast. When visible buff disappears, press MARK EFFECT EXPIRED immediately.";
    }

    static double LiveScore(Candidate c,uint raw,bool readable)
    {
        if(!readable)return 15 + Math.Min(c.UnreadableSamples,20)*2 + c.ExpiryBonus;
        double score=c.Changes*1.5 - c.DirectionFlips*4.0 + c.ExpiryBonus;
        if(c.Changes>=3 && c.DirectionFlips<=1)score+=25;
        if(c.FromNewObject)score+=8;
        int ii=unchecked((int)raw),ei=unchecked((int)c.EffectRaw);
        if(Math.Abs((long)ii)<=100000 && Math.Abs((long)ei)<=100000)score+=6;
        float f=BitConverter.Int32BitsToSingle(ii),ef=BitConverter.Int32BitsToSingle(ei);
        if(float.IsFinite(f)&&float.IsFinite(ef)&&Math.Abs(f)<=10000f&&Math.Abs(ef)<=10000f)score+=7;
        score-=Math.Min(c.SameSamples,30)*0.15;
        return score;
    }

    public static ProbeSnapshot Sample(int top=120)
    {
        Attach();
        if(candidates.Count==0)return new(status,phase,new[]{"No candidates. Capture BASELINE -> cast ONE original hero buff -> Capture EFFECT DIFF."});
        samples++;
        var ranked=new List<(Candidate c,uint raw,bool readable,double score)>();
        foreach(var c in candidates)
        {
            var b=new byte[4];
            bool ok=ReadExact(c.Address,b);
            uint raw=ok?U32(b,0):c.LastRaw;
            if(!ok)
            {
                c.UnreadableSamples++;
            }
            else if(raw!=c.LastRaw)
            {
                c.Changes++;
                long d=(long)unchecked((int)raw)-unchecked((int)c.LastRaw);
                int dir=d>0?1:d<0?-1:0;
                if(dir!=0&&c.LastDirection!=0&&dir!=c.LastDirection)c.DirectionFlips++;
                if(dir!=0)c.LastDirection=dir;
                c.SameSamples=0;
                c.LastRaw=raw;
            }
            else c.SameSamples++;
            ranked.Add((c,raw,ok,LiveScore(c,raw,ok)));
        }
        var lines=ranked.OrderByDescending(x=>x.score).ThenBy(x=>x.c.DirectionFlips).Take(top)
            .Select(x=>x.c.Format(x.raw,x.score)).ToList();
        phase=expiryMarked
            ? $"EXPIRED MARKED | samples {samples} | candidates {candidates.Count}. COPY REPORT now."
            : $"WATCH sample {samples} | candidates {candidates.Count}. Do not recast; press MARK EFFECT EXPIRED the instant visible buff ends.";
        return new(status,phase,lines);
    }

    public static ProbeSnapshot MarkExpired(int top=180)
    {
        if(candidates.Count==0)return new(status,"MARK EXPIRED blocked — no candidates",new[]{"Capture a baseline/effect diff first."});
        expiryMarked=true;
        foreach(var c in candidates)
        {
            var b=new byte[4];
            bool ok=ReadExact(c.Address,b);
            c.ExpiryMarked=true;c.ExpiryReadable=ok;
            if(!ok)
            {
                c.ExpiryBonus=110;
                continue;
            }
            uint raw=U32(b,0);c.ExpiryRaw=raw;
            double bonus=0;
            if(raw==c.BaselineRaw)bonus+=100;
            if(c.FromNewObject && raw==0)bonus+=70;
            if(raw!=c.EffectRaw)bonus+=18;
            if(c.DirectionFlips<=1 && c.Changes>=2)bonus+=20;
            float f=BitConverter.Int32BitsToSingle(unchecked((int)raw));
            float bf=BitConverter.Int32BitsToSingle(unchecked((int)c.BaselineRaw));
            if(float.IsFinite(f)&&float.IsFinite(bf)&&Math.Abs(f-bf)<0.0001f)bonus+=30;
            c.ExpiryBonus=bonus;
        }
        phase="EFFECT EXPIRY MARKED — candidates that returned to baseline, hit zero, or became unreadable are boosted. COPY REPORT.";
        return Sample(top);
    }

    public static string Reset()
    {
        baselineUnit=0;baselineGraph.Clear();effectGraph.Clear();candidates.Clear();samples=0;expiryMarked=false;
        phase="IDLE — select ONE clean unit and capture baseline";
        return phase;
    }

    public static void Shutdown()=>Detach();
}
