using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectDurationProbe;

internal sealed class Candidate
{
    public required string Label { get; init; }
    public required uint Address { get; init; }
    public uint BaselineRaw { get; init; }
    public uint EffectRaw { get; init; }
    public uint LastRaw { get; set; }
    public int Changes { get; set; }
    public int SameSamples { get; set; }
    public int DirectionFlips { get; set; }
    public int LastDirection { get; set; }

    public string Format(uint current)
    {
        float f = BitConverter.Int32BitsToSingle(unchecked((int)current));
        float fb = BitConverter.Int32BitsToSingle(unchecked((int)BaselineRaw));
        float fe = BitConverter.Int32BitsToSingle(unchecked((int)EffectRaw));
        string fs = float.IsFinite(f) ? f.ToString("0.#####") : "nan/inf";
        string fbs = float.IsFinite(fb) ? fb.ToString("0.#####") : "nan/inf";
        string fes = float.IsFinite(fe) ? fe.ToString("0.#####") : "nan/inf";
        return $"{Label,-25} @0x{Address:X8} raw 0x{current:X8} | int {unchecked((int)current),11} | float {fs,12} | base {fbs,12} | effect {fes,12} | changes {Changes,3} flips {DirectionFlips,2}";
    }
}

internal sealed record ProbeSnapshot(string Game, string Phase, IReadOnlyList<string> Lines);

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
    const int POINTER_SCAN_SIZE = 0x180;
    const int MAX_POINTER_REGIONS = 160;
    const int MAX_CANDIDATES = 900;

    static Process? process;
    static IntPtr h = IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status = "not attached";

    static uint baselineUnit;
    static byte[]? baselineUnitBytes;
    static readonly Dictionary<uint,(int unitOffset,byte[] bytes)> baselineRegions = new();
    static readonly List<Candidate> candidates = new();
    static string phase = "IDLE — capture baseline on ONE clean selected unit";
    static int samples;

    static IntPtr A(long x) => new(unchecked((int)(uint)x));

    static bool ReadExact(long address, byte[] buffer)
    {
        if(h == IntPtr.Zero) return false;
        return ReadProcessMemory(h, A(address), buffer, buffer.Length, out var n) && n.ToInt64() == buffer.Length;
    }

    static uint U32(byte[] b, int o) => BitConverter.ToUInt32(b, o);

    static bool Attach()
    {
        try { if(process != null && !process.HasExited && h != IntPtr.Zero) return true; } catch { }
        Detach();
        if((DateTime.UtcNow-lastTry).TotalMilliseconds < 300) return false;
        lastTry = DateTime.UtcNow;
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length == 0) { status = "waiting for Battle_Realms_F.exe"; return false; }
        process = ps[0];
        try { moduleBase = process.MainModule!.BaseAddress.ToInt64(); }
        catch { process = null; status = "cannot resolve module base"; return false; }
        h = OpenProcess(ACCESS, false, process.Id);
        if(h == IntPtr.Zero) { status = "OpenProcess READ failed"; return false; }
        status = $"READ-ONLY attached PID {process.Id} base 0x{moduleBase:X8}";
        return true;
    }

    static void Detach()
    {
        try { if(h != IntPtr.Zero) CloseHandle(h); } catch { }
        process = null; h = IntPtr.Zero; moduleBase = 0; status = "not attached";
    }

    static (uint unit,uint count) FirstSelectedUnit()
    {
        if(!Attach()) return default;
        var list = new byte[0x1C];
        if(!ReadExact(moduleBase + RVA_SELECTION_LIST, list)) return default;
        uint node = U32(list,0x00), count = U32(list,0x18);
        if(node == 0 || count == 0) return (0,count);
        var n = new byte[0x0C];
        if(!ReadExact(node,n)) return (0,count);
        return (U32(n,0x08),count);
    }

    static bool PlausiblePtr(uint p) => p >= 0x00010000 && p < 0x7FFF0000;

    static Dictionary<uint,(int unitOffset,byte[] bytes)> ReadPointerRegions(byte[] unitBytes)
    {
        var result = new Dictionary<uint,(int,byte[])>();
        for(int o=0;o<=unitBytes.Length-4 && result.Count<MAX_POINTER_REGIONS;o+=4)
        {
            uint p = U32(unitBytes,o);
            if(!PlausiblePtr(p) || result.ContainsKey(p)) continue;
            var b = new byte[POINTER_SCAN_SIZE];
            if(ReadExact(p,b)) result[p]=(o,b);
        }
        return result;
    }

    static bool LooksInteresting(uint baseRaw, uint effectRaw)
    {
        if(baseRaw == effectRaw) return false;
        int bi = unchecked((int)baseRaw), ei = unchecked((int)effectRaw);
        if(Math.Abs((long)ei) < 100000000L || Math.Abs((long)bi) < 100000000L) return true;
        float bf = BitConverter.Int32BitsToSingle(bi), ef = BitConverter.Int32BitsToSingle(ei);
        bool bfOk = float.IsFinite(bf) && Math.Abs(bf) < 10000000f;
        bool efOk = float.IsFinite(ef) && Math.Abs(ef) < 10000000f;
        return bfOk || efOk;
    }

    public static string CaptureBaseline()
    {
        if(!Attach()) return phase = "BASELINE failed — "+status;
        var (unit,count)=FirstSelectedUnit();
        if(unit==0) return phase=$"BASELINE failed — select exactly ONE normal unit (selected count {count})";
        if(count!=1) return phase=$"BASELINE blocked — selected count is {count}; select exactly ONE unit";
        var ub=new byte[UNIT_SCAN_SIZE];
        if(!ReadExact(unit,ub)) return phase="BASELINE failed — cannot read selected Unit*";
        baselineUnit=unit; baselineUnitBytes=ub;
        baselineRegions.Clear();
        foreach(var kv in ReadPointerRegions(ub)) baselineRegions[kv.Key]=kv.Value;
        candidates.Clear(); samples=0;
        return phase=$"BASELINE captured Unit*=0x{unit:X8} | direct {UNIT_SCAN_SIZE} bytes | readable pointer regions {baselineRegions.Count}. NOW apply ONE V2 effect only.";
    }

    public static string CaptureEffectDiff()
    {
        if(baselineUnit==0 || baselineUnitBytes==null) return phase="EFFECT DIFF blocked — capture baseline first";
        if(!Attach()) return phase="EFFECT DIFF failed — "+status;
        var nowUnit=new byte[UNIT_SCAN_SIZE];
        if(!ReadExact(baselineUnit,nowUnit)) return phase="EFFECT DIFF failed — baseline Unit* no longer readable";

        candidates.Clear(); samples=0;
        var seenAddress=new HashSet<uint>();

        for(int o=0;o<=UNIT_SCAN_SIZE-4;o+=4)
        {
            uint a=U32(baselineUnitBytes,o), b=U32(nowUnit,o);
            if(!LooksInteresting(a,b)) continue;
            uint addr=baselineUnit+(uint)o;
            if(seenAddress.Add(addr)) candidates.Add(new Candidate{Label=$"Unit+0x{o:X3}",Address=addr,BaselineRaw=a,EffectRaw=b,LastRaw=b});
        }

        var nowRegions=ReadPointerRegions(nowUnit);
        var allPtrs=new HashSet<uint>(baselineRegions.Keys);
        foreach(var p in nowRegions.Keys) allPtrs.Add(p);

        foreach(uint p in allPtrs)
        {
            bool hadBase=baselineRegions.TryGetValue(p,out var br);
            bool hasNow=nowRegions.TryGetValue(p,out var nr);
            if(!hasNow)
            {
                var b=new byte[POINTER_SCAN_SIZE];
                if(!ReadExact(p,b)) continue;
                nr=(hadBase?br.unitOffset:-1,b); hasNow=true;
            }
            byte[] before=hadBase?br.bytes:new byte[POINTER_SCAN_SIZE];
            byte[] after=nr.bytes;
            int unitOff=hadBase?br.unitOffset:nr.unitOffset;
            for(int o=0;o<=Math.Min(before.Length,after.Length)-4;o+=4)
            {
                uint a=U32(before,o), b=U32(after,o);
                if(!LooksInteresting(a,b)) continue;
                uint addr=p+(uint)o;
                if(!seenAddress.Add(addr)) continue;
                string root=unitOff>=0?$"[Unit+0x{unitOff:X3}]->":"[newptr]->";
                candidates.Add(new Candidate{Label=$"{root}+0x{o:X3}",Address=addr,BaselineRaw=a,EffectRaw=b,LastRaw=b});
                if(candidates.Count>=MAX_CANDIDATES) break;
            }
            if(candidates.Count>=MAX_CANDIDATES) break;
        }

        return phase=$"EFFECT DIFF captured: {candidates.Count} changed dword candidates. Start WATCH and leave the unit alone until visible buff expires.";
    }

    public static ProbeSnapshot Sample(int top=80)
    {
        Attach();
        if(candidates.Count==0) return new(status,phase,new[]{"No candidates yet. Capture BASELINE, apply ONE V2 effect, then Capture EFFECT DIFF."});
        samples++;
        var scored=new List<(Candidate c,uint raw,double score)>();
        foreach(var c in candidates)
        {
            var b=new byte[4];
            if(!ReadExact(c.Address,b)) continue;
            uint raw=U32(b,0);
            if(raw!=c.LastRaw)
            {
                c.Changes++;
                long d=(long)unchecked((int)raw)-unchecked((int)c.LastRaw);
                int dir=d>0?1:d<0?-1:0;
                if(dir!=0 && c.LastDirection!=0 && dir!=c.LastDirection) c.DirectionFlips++;
                if(dir!=0)c.LastDirection=dir;
                c.SameSamples=0;
            }
            else c.SameSamples++;
            c.LastRaw=raw;
            float f=BitConverter.Int32BitsToSingle(unchecked((int)raw));
            float fe=BitConverter.Int32BitsToSingle(unchecked((int)c.EffectRaw));
            bool finite=float.IsFinite(f)&&float.IsFinite(fe);
            double smoothBonus=c.DirectionFlips==0?8:Math.Max(0,5-c.DirectionFlips);
            double activeBonus=c.Changes*4.0;
            double floatBonus=finite && Math.Abs(f)<1000000f?2:0;
            double score=activeBonus+smoothBonus+floatBonus-Math.Min(c.SameSamples,20)*0.15;
            scored.Add((c,raw,score));
        }
        var lines=scored.OrderByDescending(x=>x.score).ThenBy(x=>x.c.DirectionFlips).Take(top)
            .Select(x=>$"score {x.score,6:0.0} | "+x.c.Format(x.raw)).ToList();
        phase=$"WATCH sample {samples} | candidates {candidates.Count}. Smooth one-direction changing fields near top are timer candidates; observe until visible buff expires.";
        return new(status,phase,lines);
    }

    public static string Reset()
    {
        baselineUnit=0; baselineUnitBytes=null; baselineRegions.Clear(); candidates.Clear(); samples=0;
        phase="IDLE — capture baseline on ONE clean selected unit";
        return phase;
    }

    public static void Shutdown()=>Detach();
}
