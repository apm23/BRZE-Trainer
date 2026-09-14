using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Iced.Intel;

namespace BRZEHeroEffectCurrentTimeSourceV27;

internal readonly record struct ProbeResult(bool Ok,string Summary,string Report);

internal static class CurrentTimeSourceCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x0010|0x0400;
    const int RVA_SELECTION_LIST=0x441708;
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    static readonly int[] ROOTS={0x1E4,0x1E8,0x20C,0x210};
    const int ROOT_SCAN=0x600;
    const int ABILITY_OFF=0x58,TARGET_OFF=0x17C,CONFIG_OFF=0x1F4,STAMP_OFF=0x194;
    const uint A5=0xA5;
    const uint TICK_RVA=0x13A5EB;

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;

    readonly record struct Section(string Name,uint Rva,uint Size,byte[] Bytes);
    readonly record struct CallSite(uint Rva,uint CallerStart,string BoundaryReason,List<Instruction> Instructions);

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}

    static bool Attach(out string error)
    {
        error="";Detach();
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0){error="Battle_Realms_F.exe is not running";return false;}
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}
        catch{error="cannot resolve module base";process=null;return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        if(h==IntPtr.Zero){error="OpenProcess READ failed";process=null;moduleBase=0;return false;}
        return true;
    }

    static void Detach()
    {
        try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}
        h=IntPtr.Zero;process=null;moduleBase=0;
    }

    static (uint unit,uint count) FirstSelected()
    {
        var b=new byte[0x1C];
        if(!ReadExact(moduleBase+RVA_SELECTION_LIST,b))return default;
        uint node=BitConverter.ToUInt32(b,0),count=BitConverter.ToUInt32(b,0x18);
        if(node==0||count==0)return (0,count);
        var nb=new byte[0x0C];if(!ReadExact(node,nb))return (0,count);
        return (BitConverter.ToUInt32(nb,8),count);
    }

    static bool Signature(uint addr,uint unit)=>Ptr(addr)&&R32((long)addr+ABILITY_OFF)==A5&&R32((long)addr+TARGET_OFF)==unit;

    static bool FindA5(uint unit,out uint record,out string path)
    {
        record=0;path="not found";var seen=new HashSet<uint>();
        foreach(int ro in ROOTS)
        {
            uint root=R32((long)unit+ro);
            if(!Ptr(root)||!seen.Add(root))continue;
            if(Signature(root,unit)){record=root;path=$"Unit+0x{ro:X3}";return true;}
            var b=new byte[ROOT_SCAN];if(!ReadExact(root,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=BitConverter.ToUInt32(b,o);
                if(!Ptr(p)||!seen.Add(p))continue;
                if(Signature(p,unit)){record=p;path=$"Unit+0x{ro:X3}->+0x{o:X3}";return true;}
            }
        }
        return false;
    }

    static bool ReadSections(out List<Section> sections,out string error)
    {
        sections=new();error="";
        var dos=new byte[0x1000];if(!ReadExact(moduleBase,dos)){error="cannot read DOS/PE headers";return false;}
        if(dos[0]!=0x4D||dos[1]!=0x5A){error="MZ mismatch";return false;}
        int pe=BitConverter.ToInt32(dos,0x3C);
        var hdr=new byte[0x1000];if(!ReadExact(moduleBase+pe,hdr)){error="cannot read NT headers";return false;}
        if(hdr[0]!=0x50||hdr[1]!=0x45){error="PE mismatch";return false;}
        ushort count=BitConverter.ToUInt16(hdr,6),opt=BitConverter.ToUInt16(hdr,20);
        long table=moduleBase+pe+24+opt;
        var sb=new byte[count*40];if(!ReadExact(table,sb)){error="cannot read section table";return false;}
        for(int i=0;i<count;i++)
        {
            int o=i*40;string name=Encoding.ASCII.GetString(sb,o,8).TrimEnd('\0');
            uint vs=BitConverter.ToUInt32(sb,o+8),rva=BitConverter.ToUInt32(sb,o+12),raw=BitConverter.ToUInt32(sb,o+16);
            uint size=Math.Max(vs,raw);if(size==0||size>0x02000000)continue;
            var bytes=new byte[size];if(!ReadExact(moduleBase+rva,bytes))continue;
            sections.Add(new(name,rva,size,bytes));
        }
        if(!sections.Any(x=>x.Name==".text")){error=".text not found";return false;}
        return true;
    }

    static List<uint> RawDirectCallXrefs(Section text,uint targetRva)
    {
        var r=new List<uint>();
        long target=moduleBase+targetRva;
        for(int i=0;i<=text.Bytes.Length-5;i++)
        {
            if(text.Bytes[i]!=0xE8)continue;
            int rel=BitConverter.ToInt32(text.Bytes,i+1);
            long srcNext=moduleBase+text.Rva+i+5L;
            if(srcNext+rel==target)r.Add(text.Rva+(uint)i);
        }
        return r;
    }

    static List<uint> PointerRefs(IEnumerable<Section> sections,uint targetRva)
    {
        uint target=unchecked((uint)(moduleBase+targetRva));var r=new List<uint>();
        foreach(var s in sections.Where(x=>x.Name!=".text"))
        {
            for(int i=0;i<=s.Bytes.Length-4;i+=4)
                if(BitConverter.ToUInt32(s.Bytes,i)==target)r.Add(s.Rva+(uint)i);
        }
        return r;
    }

    static List<Instruction> Decode(Section text,uint startRva,int maxBytes,int maxIns)
    {
        var list=new List<Instruction>();if(startRva<text.Rva||startRva>=text.Rva+text.Size)return list;
        int off=checked((int)(startRva-text.Rva));int n=Math.Min(maxBytes,text.Bytes.Length-off);if(n<=0)return list;
        var b=new byte[n];Buffer.BlockCopy(text.Bytes,off,b,0,n);
        ulong ip=(ulong)moduleBase+startRva,end=ip+(ulong)n;
        var d=Iced.Intel.Decoder.Create(32,new ByteArrayCodeReader(b));d.IP=ip;
        while(d.IP<end&&list.Count<maxIns)
        {
            var ins=d.Decode();if(ins.Length==0||ins.Code==Code.INVALID)break;list.Add(ins);
        }
        return list;
    }

    static bool Reaches(Section text,uint start,uint marker)
    {
        if(start>marker)return false;
        foreach(var i in Decode(text,start,(int)Math.Min(0x900u,marker-start+64),600))
        {
            uint r=(uint)(i.IP-(ulong)moduleBase);
            if(r==marker)return true;if(r>marker)return false;
            if(i.FlowControl==FlowControl.Return)return false;
        }
        return false;
    }

    static bool ClassicPrologue(byte[] b,int p)
    {
        return p>=0&&p+2<b.Length&&b[p]==0x55&&b[p+1]==0x8B&&b[p+2]==0xEC;
    }

    static uint FindCallerStart(Section text,uint callRva,out string reason)
    {
        int pos=checked((int)(callRva-text.Rva));int min=Math.Max(0,pos-0x800);
        for(int p=pos;p>=min;p--)
        {
            if(!ClassicPrologue(text.Bytes,p))continue;
            uint r=text.Rva+(uint)p;
            if(Reaches(text,r,callRva)){reason=$"validated prologue 0x{r:X6}";return r;}
        }
        reason="no validated classic prologue; using aligned decode fallback";
        uint fallback=callRva>0x100?callRva-0x100:text.Rva;
        for(uint s=fallback;s<callRva;s++)
        {
            var ins=Decode(text,s,(int)(callRva-s+8),120);
            if(ins.Any(x=>(uint)(x.IP-(ulong)moduleBase)==callRva))return s;
        }
        return fallback;
    }

    static CallSite BuildCallSite(Section text,uint callRva)
    {
        uint start=FindCallerStart(text,callRva,out string why);
        var ins=Decode(text,start,(int)Math.Min(0xC00u,callRva-start+0x180),900);
        return new(callRva,start,why,ins);
    }

    static string InstLine(Instruction i,uint callRva)
    {
        uint r=(uint)(i.IP-(ulong)moduleBase);var tags=new List<string>();
        if(r==callRva)tags.Add("<<< CALL TICK");
        string mem="";
        if(i.MemoryDisplSize!=0)
        {
            mem=$" mem=[{i.MemoryBase}+{i.MemoryIndex}*{i.MemoryIndexScale}+0x{i.MemoryDisplacement64:X}]";
            if(i.MemoryBase==Register.None&&i.MemoryIndex==Register.None)tags.Add("ABS_MEM");
        }
        string target="";
        if(i.Op0Kind is OpKind.NearBranch16 or OpKind.NearBranch32 or OpKind.NearBranch64)
        {
            ulong t=i.NearBranchTarget;target=$" target=0x{t:X8}";
            if(t>=(ulong)moduleBase)target+=$"/RVA0x{t-(ulong)moduleBase:X6}";
        }
        return $"RVA 0x{r:X6} {i.Mnemonic,-11} op0={i.Op0Kind,-18} op1={i.Op1Kind,-18}{mem}{target}"+(tags.Count>0?$" [{string.Join(',',tags)}]":"");
    }

    static List<uint> AbsoluteMemorySources(IEnumerable<Instruction> ins,uint callRva)
    {
        var set=new HashSet<uint>();
        foreach(var i in ins)
        {
            uint r=(uint)(i.IP-(ulong)moduleBase);if(r>callRva)break;
            if(i.MemoryDisplSize==0||i.MemoryBase!=Register.None||i.MemoryIndex!=Register.None)continue;
            ulong d=i.MemoryDisplacement64;if(d>=0x10000&&d<0x80000000)set.Add((uint)d);
        }
        return set.OrderBy(x=>x).ToList();
    }

    static string SampleCandidates(List<uint> addresses)
    {
        if(addresses.Count==0)return "  (no absolute memory operands in decoded caller window)\r\n";
        var first=new Dictionary<uint,uint>();
        foreach(uint a in addresses.Take(80))first[a]=R32(a);
        Thread.Sleep(350);
        var sb=new StringBuilder();
        foreach(var kv in first)
        {
            uint b=R32(kv.Key);long delta=(long)b-kv.Value;
            sb.AppendLine($"  addr=0x{kv.Key:X8} v0={kv.Value} v1={b} delta={delta}"+(delta>0&&delta<5000?"  <<< CHANGING CLOCK-LIKE":""));
        }
        return sb.ToString();
    }

    static void DumpCallSite(StringBuilder sb,CallSite cs)
    {
        sb.AppendLine($"CALL xref RVA 0x{cs.Rva:X6}; callerStart=0x{cs.CallerStart:X6}; {cs.BoundaryReason}");
        int idx=cs.Instructions.FindIndex(x=>(uint)(x.IP-(ulong)moduleBase)==cs.Rva);
        if(idx<0){sb.AppendLine("  decode did not land on call marker");sb.AppendLine();return;}
        int lo=Math.Max(0,idx-28),hi=Math.Min(cs.Instructions.Count-1,idx+10);
        for(int i=lo;i<=hi;i++)sb.AppendLine("  "+InstLine(cs.Instructions[i],cs.Rva));
        sb.AppendLine("  -- absolute memory operands sampled 350ms apart --");
        sb.Append(SampleCandidates(AbsoluteMemorySources(cs.Instructions.Take(idx+1),cs.Rva)));
        sb.AppendLine();
    }

    static void DumpPointerRef(StringBuilder sb,List<Section> sections,uint refRva,uint targetRva)
    {
        var s=sections.First(x=>refRva>=x.Rva&&refRva+4<=x.Rva+x.Size);int o=checked((int)(refRva-s.Rva));
        sb.AppendLine($"pointer ref in {s.Name}: RVA 0x{refRva:X6} -> runtime 0x{moduleBase+targetRva:X8}");
        int lo=Math.Max(0,o-32),hi=Math.Min(s.Bytes.Length-4,o+32);
        for(int p=lo;p<=hi;p+=4)
        {
            uint v=BitConverter.ToUInt32(s.Bytes,p);string mark=p==o?" <<< TARGET":"";
            string maybe=v>=(uint)moduleBase&&v<(uint)moduleBase+0x01000000?$" (RVA 0x{v-(uint)moduleBase:X6})":"";
            sb.AppendLine($"  {s.Name}+0x{p:X6}: 0x{v:X8}{maybe}{mark}");
        }
        sb.AppendLine();
    }

    static void RecursiveCallerSummary(StringBuilder sb,Section text,uint targetRva,int depth,HashSet<uint> seen)
    {
        if(depth>3||!seen.Add(targetRva))return;
        var xrefs=RawDirectCallXrefs(text,targetRva);
        sb.AppendLine($"Depth {depth}: target RVA 0x{targetRva:X6} direct CALL xrefs={xrefs.Count}");
        foreach(uint x in xrefs.Take(24))
        {
            uint caller=FindCallerStart(text,x,out string why);
            sb.AppendLine($"  call@0x{x:X6} caller≈0x{caller:X6} ({why})");
        }
        foreach(uint caller in xrefs.Select(x=>FindCallerStart(text,x,out _)).Distinct().Take(12))RecursiveCallerSummary(sb,text,caller,depth+1,seen);
    }

    public static ProbeResult Run()
    {
        try
        {
            if(!Attach(out string err))return new(false,err,err);
            if(!ReadSections(out var sections,out string peErr))return new(false,peErr,peErr);
            var text=sections.First(x=>x.Name==".text");
            var (unit,count)=FirstSelected();

            var sb=new StringBuilder(65536);
            sb.AppendLine("=== BRZE HERO EFFECT CURRENT-TIME SOURCE V27 ===");
            sb.AppendLine("MODE: STRICT READ-ONLY — no hooks, no writes, no native calls");
            sb.AppendLine($"PID={process!.Id} moduleBase=0x{moduleBase:X8} .text RVA=0x{text.Rva:X} size=0x{text.Size:X}");
            sb.AppendLine($"Tick target RVA=0x{TICK_RVA:X6} runtime=0x{moduleBase+TICK_RVA:X8}");
            sb.AppendLine();

            if(count==1&&unit!=0)
            {
                uint def=R32((long)unit+OFF_DEF),owner=R32((long)unit+OFF_OWNER);
                sb.AppendLine($"Selected Unit*=0x{unit:X8} UnitDef*=0x{def:X8} owner={owner}");
                if(FindA5(unit,out uint rec,out string path))
                {
                    uint cfg=R32((long)rec+CONFIG_OFF);
                    sb.AppendLine($"Active A5 record=0x{rec:X8} via {path}; stamp={R32((long)rec+STAMP_OFF)}; config=0x{cfg:X8}; duration={R32((long)cfg+0xE0)}");
                }
                else sb.AppendLine("No active A5+target signature found; static caller scan still proceeds.");
            }
            else sb.AppendLine($"Selected count={count}; active A5 context omitted; static caller scan still proceeds.");
            sb.AppendLine();

            var xrefs=RawDirectCallXrefs(text,TICK_RVA);
            sb.AppendLine($"--- DIRECT E8 CALL XREFS TO RVA 0x{TICK_RVA:X6}: {xrefs.Count} ---");
            foreach(uint x in xrefs.Take(32))DumpCallSite(sb,BuildCallSite(text,x));
            if(xrefs.Count==0)sb.AppendLine("  (none — likely virtual/indirect dispatch; inspect pointer refs below)");
            sb.AppendLine();

            var ptrs=PointerRefs(sections,TICK_RVA);
            sb.AppendLine($"--- RUNTIME POINTER REFS TO TICK FUNCTION OUTSIDE .text: {ptrs.Count} ---");
            foreach(uint p in ptrs.Take(32))DumpPointerRef(sb,sections,p,TICK_RVA);
            if(ptrs.Count==0)sb.AppendLine("  (none)");
            sb.AppendLine();

            sb.AppendLine("--- RECURSIVE DIRECT-CALLER SUMMARY (max depth 3) ---");
            RecursiveCallerSummary(sb,text,TICK_RVA,0,new HashSet<uint>());
            sb.AppendLine();
            sb.AppendLine("--- INTERPRETATION RULE ---");
            sb.AppendLine("Find the real source of the single stack argument consumed as [EBP+8] by RVA 0x13A5EB. Prefer a caller/global value that increases with BRZE game time. Do NOT write record+0x194 again until that source is identified and read directly.");

            return new(true,$"V27 scan complete: {xrefs.Count} direct call xrefs, {ptrs.Count} pointer refs. Copy full report.",sb.ToString());
        }
        catch(Exception ex){return new(false,"V27 error: "+ex.Message,ex.ToString());}
        finally{Detach();}
    }
}
