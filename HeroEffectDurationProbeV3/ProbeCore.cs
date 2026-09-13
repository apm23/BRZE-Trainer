using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectDurationProbeV3;

internal sealed class FieldStats
{
    public int Offset { get; init; }
    public uint ControlStart { get; set; }
    public uint ControlLast { get; set; }
    public int ControlChanges { get; set; }
    public int ControlFlips { get; set; }
    public int ControlDir { get; set; }

    public uint PreEffect { get; set; }
    public uint EffectStart { get; set; }
    public uint EffectLast { get; set; }
    public int EffectChanges { get; set; }
    public int EffectFlips { get; set; }
    public int EffectDir { get; set; }
    public uint Expiry { get; set; }
    public bool ExpirySet { get; set; }

    static float F(uint raw)=>BitConverter.Int32BitsToSingle(unchecked((int)raw));
    static string V(uint raw)
    {
        float f=F(raw);
        string fs=float.IsFinite(f)?f.ToString("0.#####"):"nan/inf";
        return $"0x{raw:X8}/{unchecked((int)raw)}/{fs}";
    }

    public double Score()
    {
        double s=0;
        if(ControlChanges==0 && EffectChanges>=2)s+=70;
        s+=EffectChanges*2.2;
        s-=EffectFlips*7.0;
        s-=ControlChanges*3.5;
        s-=ControlFlips*2.0;
        if(EffectChanges>=5 && EffectFlips<=1)s+=35;
        if(EffectStart!=PreEffect)s+=12;
        if(ExpirySet && Expiry==PreEffect)s+=30;
        if(Offset==0x460)s+=5;
        return s;
    }

    public string Format()
    {
        string pin=Offset==0x460?"PIN ":"    ";
        string exp=ExpirySet?V(Expiry):"-";
        return $"{pin}Unit+0x{Offset:X3} | score {Score(),6:0.0} | CTRL chg {ControlChanges,3} flip {ControlFlips,2} {V(ControlStart)} -> {V(ControlLast)} | PRE {V(PreEffect)} | FX chg {EffectChanges,3} flip {EffectFlips,2} {V(EffectStart)} -> {V(EffectLast)} | EXP {exp}";
    }
}

internal sealed record ProbeSnapshot(string Game,string Phase,IReadOnlyList<string> Lines);

internal static class ProbeCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_READ=0x0010;
    const uint PROCESS_QUERY_INFORMATION=0x0400;
    const uint ACCESS=PROCESS_VM_READ|PROCESS_QUERY_INFORMATION;
    const int RVA_SELECTION_LIST=0x441708;
    const int UNIT_SCAN_SIZE=0x500;
    const int OFF_DEF=0x74;
    const int OFF_OWNER=0x240;

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;
    static DateTime lastTry;
    static string status="not attached";

    static uint unit;
    static uint unitDef;
    static uint owner;
    static readonly List<FieldStats> fields=new();
    static bool controlStarted,controlDone,preCaptured,effectStarted,expiryMarked;
    static int controlSamples,effectSamples;
    static string phase="IDLE — select ONE clean idle unit, then run 15s CONTROL";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static uint U32(byte[] b,int o)=>BitConverter.ToUInt32(b,o);

    static bool ReadExact(long address,byte[] buffer)
    {
        if(h==IntPtr.Zero)return false;
        return ReadProcessMemory(h,A(address),buffer,buffer.Length,out var n)&&n.ToInt64()==buffer.Length;
    }

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach();
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<300)return false;
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
        var n=new byte[0x0C];
        if(!ReadExact(node,n))return (0,count);
        return (U32(n,0x08),count);
    }

    static byte[]? ReadUnit()
    {
        if(!Attach()||unit==0)return null;
        var b=new byte[UNIT_SCAN_SIZE];
        if(!ReadExact(unit,b))return null;
        if(U32(b,OFF_DEF)!=unitDef||U32(b,OFF_OWNER)!=owner)return null;
        return b;
    }

    static void Update(uint prev,uint now,ref int changes,ref int flips,ref int dir)
    {
        if(now==prev)return;
        changes++;
        long d=(long)unchecked((int)now)-unchecked((int)prev);
        int nd=d>0?1:d<0?-1:0;
        if(nd!=0&&dir!=0&&nd!=dir)flips++;
        if(nd!=0)dir=nd;
    }

    public static string StartControl()
    {
        if(!Attach())return phase="CONTROL failed — "+status;
        var (u,count)=FirstSelectedUnit();
        if(u==0||count!=1)return phase=$"CONTROL blocked — select exactly ONE normal idle unit (selected {count})";
        unit=u;
        var b=new byte[UNIT_SCAN_SIZE];
        if(!ReadExact(unit,b))return phase="CONTROL failed — cannot read Unit*";
        unitDef=U32(b,OFF_DEF);owner=U32(b,OFF_OWNER);
        if(unitDef==0)return phase="CONTROL blocked — invalid UnitDef pointer";
        fields.Clear();
        for(int o=0;o<=UNIT_SCAN_SIZE-4;o+=4)
        {
            uint raw=U32(b,o);
            fields.Add(new FieldStats{Offset=o,ControlStart=raw,ControlLast=raw});
        }
        controlSamples=0;effectSamples=0;controlStarted=true;controlDone=false;preCaptured=false;effectStarted=false;expiryMarked=false;
        return phase=$"CONTROL STARTED Unit*=0x{unit:X8}. Keep unit completely idle for 15 seconds.";
    }

    public static string SampleControl()
    {
        if(!controlStarted||controlDone)return phase;
        var b=ReadUnit();
        if(b==null)return phase="CONTROL failed — held Unit* changed/unreadable";
        foreach(var f in fields)
        {
            uint raw=U32(b,f.Offset);
            int c=f.ControlChanges,fl=f.ControlFlips,d=f.ControlDir;
            Update(f.ControlLast,raw,ref c,ref fl,ref d);
            f.ControlChanges=c;f.ControlFlips=fl;f.ControlDir=d;f.ControlLast=raw;
        }
        controlSamples++;
        return phase=$"CONTROL sampling {controlSamples}/60 — no buff, no movement, no attack.";
    }

    public static string FinishControl()
    {
        if(!controlStarted)return phase="CONTROL not started";
        controlDone=true;
        return phase=$"CONTROL DONE ({controlSamples} samples). Next: CAPTURE PRE-EFFECT while unit is still clean.";
    }

    public static string CapturePreEffect()
    {
        if(!controlDone)return phase="PRE-EFFECT blocked — finish CONTROL first";
        var b=ReadUnit();
        if(b==null)return phase="PRE-EFFECT failed — held Unit* changed/unreadable";
        foreach(var f in fields)f.PreEffect=U32(b,f.Offset);
        preCaptured=true;effectStarted=false;expiryMarked=false;effectSamples=0;
        return phase="PRE-EFFECT captured. NOW cast ORIGINAL Grayback ONCE on this same unit, then immediately press START EFFECT WATCH.";
    }

    public static string StartEffect()
    {
        if(!preCaptured)return phase="EFFECT blocked — capture PRE-EFFECT first";
        var b=ReadUnit();
        if(b==null)return phase="EFFECT failed — held Unit* changed/unreadable";
        foreach(var f in fields)
        {
            uint raw=U32(b,f.Offset);
            f.EffectStart=raw;f.EffectLast=raw;f.EffectChanges=0;f.EffectFlips=0;f.EffectDir=0;f.ExpirySet=false;f.Expiry=0;
        }
        effectStarted=true;expiryMarked=false;effectSamples=0;
        return phase="EFFECT WATCH STARTED. Do not move/attack/recast. Mark expiry the instant Grayback visibly ends.";
    }

    public static string SampleEffect()
    {
        if(!effectStarted||expiryMarked)return phase;
        var b=ReadUnit();
        if(b==null)return phase="EFFECT failed — held Unit* changed/unreadable";
        foreach(var f in fields)
        {
            uint raw=U32(b,f.Offset);
            int c=f.EffectChanges,fl=f.EffectFlips,d=f.EffectDir;
            Update(f.EffectLast,raw,ref c,ref fl,ref d);
            f.EffectChanges=c;f.EffectFlips=fl;f.EffectDir=d;f.EffectLast=raw;
        }
        effectSamples++;
        return phase=$"EFFECT sampling {effectSamples}. Waiting for natural visible expiry.";
    }

    public static ProbeSnapshot MarkExpired(int top=100)
    {
        if(!effectStarted)return new(status,"MARK blocked — effect watch not started",new[]{"Complete CONTROL -> PRE-EFFECT -> cast Grayback -> START EFFECT WATCH first."});
        var b=ReadUnit();
        if(b==null)return new(status,"MARK failed — held Unit* changed/unreadable",Array.Empty<string>());
        foreach(var f in fields){f.Expiry=U32(b,f.Offset);f.ExpirySet=true;}
        expiryMarked=true;
        phase=$"EXPIRED MARKED after {effectSamples} effect samples. COPY REPORT.";
        return Snapshot(top);
    }

    static bool Interesting(FieldStats f)
    {
        if(f.Offset==0x460)return true;
        if(f.ControlChanges>0||f.EffectChanges>0||f.EffectStart!=f.PreEffect)return true;
        return false;
    }

    public static ProbeSnapshot Snapshot(int top=100)
    {
        Attach();
        if(fields.Count==0)return new(status,phase,new[]{"No data yet. Start 15s CONTROL on one idle unit."});
        var ranked=fields.Where(Interesting).OrderByDescending(f=>f.Offset==0x460).ThenByDescending(f=>f.Score()).Take(top).Select(f=>f.Format()).ToList();
        if(ranked.Count==0)ranked.Add("No changing direct Unit fields yet.");
        return new(status,phase,ranked);
    }

    public static string Reset()
    {
        unit=unitDef=owner=0;fields.Clear();controlStarted=controlDone=preCaptured=effectStarted=expiryMarked=false;controlSamples=effectSamples=0;
        return phase="IDLE — select ONE clean idle unit, then run 15s CONTROL";
    }

    public static void Shutdown()=>Detach();
}
