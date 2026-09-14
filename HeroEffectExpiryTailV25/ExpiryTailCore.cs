using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;

namespace BRZEHeroEffectExpiryTailV25;

internal readonly record struct ExpiryTailResult(bool Ok,string Summary,string Report);

internal static class ExpiryTailCore
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
    const uint TICK_FN=0x13A5EB;
    const uint STAMP_MARKER=0x13A8B3;
    const uint SIDE_CALL=0x13A349;

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;

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

    static void Detach(){try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{} h=IntPtr.Zero;process=null;moduleBase=0;}

    static (uint unit,uint count) FirstSelected()
    {
        var b=new byte[0x1C];if(!ReadExact(moduleBase+RVA_SELECTION_LIST,b))return default;
        uint node=BitConverter.ToUInt32(b,0),count=BitConverter.ToUInt32(b,0x18);if(node==0||count==0)return (0,count);
        var nb=new byte[0x0C];if(!ReadExact(node,nb))return (0,count);
        return (BitConverter.ToUInt32(nb,8),count);
    }

    static bool Signature(uint addr,uint unit)=>Ptr(addr)&&R32((long)addr+ABILITY_OFF)==A5&&R32((long)addr+TARGET_OFF)==unit;

    static bool FindA5(uint unit,out uint record,out string path)
    {
        record=0;path="not found";var seen=new HashSet<uint>();
        foreach(int ro in ROOTS)
        {
            uint root=R32((long)unit+ro);if(!Ptr(root)||!seen.Add(root))continue;
            if(Signature(root,unit)){record=root;path=$"Unit+0x{ro:X3}";return true;}
            var b=new byte[ROOT_SCAN];if(!ReadExact(root,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=BitConverter.ToUInt32(b,o);if(!Ptr(p)||!seen.Add(p))continue;
                if(Signature(p,unit)){record=p;path=$"Unit+0x{ro:X3}->+0x{o:X3}";return true;}
            }
        }
        return false;
    }

    readonly record struct TextSection(uint Rva,uint Size,byte[] Bytes);

    static bool TryReadText(out TextSection text,out string error)
    {
        text=default;error="";
        var dos=new byte[0x1000];if(!ReadExact(moduleBase,dos)){error="cannot read DOS/PE headers";return false;}
        int pe=BitConverter.ToInt32(dos,0x3C);var hdr=new byte[0x800];if(!ReadExact(moduleBase+pe,hdr)){error="cannot read NT headers";return false;}
        ushort sections=BitConverter.ToUInt16(hdr,6),optSize=BitConverter.ToUInt16(hdr,20);long sh=moduleBase+pe+24+optSize;
        var sec=new byte[sections*40];if(!ReadExact(sh,sec)){error="cannot read section table";return false;}
        for(int i=0;i<sections;i++)
        {
            int o=i*40;string name=Encoding.ASCII.GetString(sec,o,8).TrimEnd('\0');if(name!=".text")continue;
            uint vs=BitConverter.ToUInt32(sec,o+8),rva=BitConverter.ToUInt32(sec,o+12),rs=BitConverter.ToUInt32(sec,o+16),size=Math.Max(vs,rs);
            if(size==0||size>0x01000000){error="invalid .text size";return false;}
            var bytes=new byte[size];if(!ReadExact(moduleBase+rva,bytes)){error="cannot read .text";return false;}
            text=new(rva,size,bytes);return true;
        }
        error=".text not found";return false;
    }

    static List<Instruction> Decode(TextSection t,uint startRva,int maxBytes=0x1400,int maxIns=1500)
    {
        var result=new List<Instruction>();if(startRva<t.Rva||startRva>=t.Rva+t.Size)return result;
        int off=(int)(startRva-t.Rva),n=Math.Min(maxBytes,t.Bytes.Length-off);var buf=new byte[n];Buffer.BlockCopy(t.Bytes,off,buf,0,n);
        ulong ip=(ulong)moduleBase+startRva,end=ip+(ulong)n;var d=Iced.Intel.Decoder.Create(32,new ByteArrayCodeReader(buf));d.IP=ip;
        while(d.IP<end&&result.Count<maxIns){var x=d.Decode();if(x.Length==0||x.Code==Code.INVALID)break;result.Add(x);}
        return result;
    }

    static bool IsNear(OpKind k)=>k is OpKind.NearBranch16 or OpKind.NearBranch32 or OpKind.NearBranch64;
    static uint Rva(Instruction i)=>(uint)(i.IP-(ulong)moduleBase);

    static string BasicLine(Instruction i,TextSection t,string extra="")
    {
        uint r=Rva(i);ulong d=i.MemoryDisplacement64;var tags=new List<string>();
        if(i.MemoryDisplSize!=0)
        {
            if(i.MemoryBase==Register.EBP&&d==8)tags.Add("ARG+8");
            if(d==0x17C)tags.Add("REC+17C");if(d==0x194)tags.Add("REC+194");if(d==0x1F4)tags.Add("REC+1F4");
            if(d==0xE0)tags.Add("DISP+E0");if(d==0xE4)tags.Add("DISP+E4");
        }
        if(r==STAMP_MARKER)tags.Add("STAMP MARKER");
        if(!string.IsNullOrEmpty(extra))tags.Add(extra);
        string mem=i.MemoryDisplSize!=0?$" mem=[{i.MemoryBase}+{i.MemoryIndex}*{i.MemoryIndexScale}+0x{d:X}]":"";
        string branch="";if(IsNear(i.Op0Kind)){ulong target=i.NearBranchTarget;ulong lo=(ulong)moduleBase+t.Rva,hi=lo+t.Size;branch=target>=lo&&target<hi?$" target=RVA 0x{target-(ulong)moduleBase:X6}":$" target=0x{target:X8}";}
        string tag=tags.Count>0?$"  [{string.Join(",",tags)}]":"";
        return $"RVA 0x{r:X6} {i.Mnemonic,-11} op0={i.Op0Kind,-17} op1={i.Op1Kind,-17}{mem}{branch}{tag}";
    }

    static Dictionary<int,string> AnalyzeConfigFlow(List<Instruction> ins)
    {
        var tags=new Dictionary<int,string>();
        var configRegAge=new Dictionary<Register,int>();
        for(int i=0;i<ins.Count;i++)
        {
            foreach(var k in configRegAge.Keys.ToList()){configRegAge[k]++;if(configRegAge[k]>20)configRegAge.Remove(k);}
            var x=ins[i];
            if(x.Mnemonic==Mnemonic.Mov&&x.Op0Kind==OpKind.Register&&x.Op1Kind==OpKind.Memory&&x.MemoryDisplacement64==0x1F4)
            {
                configRegAge[x.Op0Register]=0;tags[i]="LOAD CONFIG PTR";
            }
            if(x.MemoryDisplSize!=0&&(x.MemoryDisplacement64==0xE0||x.MemoryDisplacement64==0xE4)&&configRegAge.ContainsKey(x.MemoryBase))
            {
                string t=x.MemoryDisplacement64==0xE0?"CONFIG+E0 DURATION":"CONFIG+E4";
                tags[i]=tags.TryGetValue(i,out var old)?old+"; "+t:t;
            }
            if(x.Op0Kind==OpKind.Register&&x.Mnemonic is Mnemonic.Mov or Mnemonic.Lea or Mnemonic.Xor or Mnemonic.Add or Mnemonic.Sub)
            {
                if(!(x.Mnemonic==Mnemonic.Mov&&x.Op1Kind==OpKind.Memory&&x.MemoryDisplacement64==0x1F4))configRegAge.Remove(x.Op0Register);
            }
        }
        return tags;
    }

    static void DumpTickTail(StringBuilder sb,TextSection text)
    {
        var all=Decode(text,TICK_FN,0x1600,1800);var flow=AnalyzeConfigFlow(all);
        int marker=all.FindIndex(x=>Rva(x)==STAMP_MARKER);if(marker<0){sb.AppendLine("STAMP marker not found in tick function decode.");return;}
        int firstDuration=-1;
        for(int i=marker;i<all.Count;i++)if(flow.TryGetValue(i,out var t)&&t.Contains("CONFIG+E0")){firstDuration=i;break;}
        sb.AppendLine($"Tick candidate RVA 0x{TICK_FN:X6}; decoded {all.Count} instructions; stamp index={marker}; first CONFIG+E0 index={(firstDuration>=0?firstDuration.ToString():"NONE")}");
        sb.AppendLine("--- Tail from 32 instructions before stamp through first RET after stamp ---");
        int start=Math.Max(0,marker-32),printed=0;
        for(int i=start;i<all.Count;i++)
        {
            string extra=flow.TryGetValue(i,out var t)?t:"";sb.AppendLine("  "+BasicLine(all[i],text,extra));printed++;
            if(i>marker&&all[i].FlowControl==FlowControl.Return)break;
            if(printed>650){sb.AppendLine("  [TRUNCATED after 650 instructions]");break;}
        }
        sb.AppendLine();
        sb.AppendLine("--- Focus windows around CONFIG+E0 / CONFIG+E4 uses after stamp ---");
        var focus=new HashSet<int>();
        for(int i=marker;i<all.Count;i++)if(flow.TryGetValue(i,out var t)&&(t.Contains("CONFIG+E0")||t.Contains("CONFIG+E4")))for(int j=Math.Max(marker,i-18);j<=Math.Min(all.Count-1,i+36);j++)focus.Add(j);
        int prev=-2;foreach(int i in focus.OrderBy(x=>x))
        {
            if(prev>=0&&i>prev+1)sb.AppendLine("  ...");
            string extra=flow.TryGetValue(i,out var t)?t:"";sb.AppendLine("  "+BasicLine(all[i],text,extra));prev=i;
        }
        if(focus.Count==0)sb.AppendLine("  No proven config-register +E0/E4 access found in decoded tail.");
        sb.AppendLine();

        sb.AppendLine("--- Direct CALL targets in the expiry-focused region ---");
        var calls=new SortedSet<uint>();
        int callStart=firstDuration>=0?Math.Max(marker,firstDuration-24):marker;
        int callEnd=firstDuration>=0?Math.Min(all.Count-1,firstDuration+120):Math.Min(all.Count-1,marker+260);
        for(int i=callStart;i<=callEnd;i++)if(all[i].FlowControl==FlowControl.Call&&IsNear(all[i].Op0Kind))
        {
            ulong target=all[i].NearBranchTarget;ulong lo=(ulong)moduleBase+text.Rva,hi=lo+text.Size;if(target>=lo&&target<hi)calls.Add((uint)(target-(ulong)moduleBase));
        }
        foreach(uint c in calls)sb.AppendLine($"  CALL RVA 0x{c:X6}");
        if(calls.Count==0)sb.AppendLine("  (none)");
        sb.AppendLine();
    }

    static bool ClassicPrologue(TextSection t,int p)=>p>=0&&p+2<t.Bytes.Length&&t.Bytes[p]==0x55&&t.Bytes[p+1]==0x8B&&t.Bytes[p+2]==0xEC;
    static uint FindStart(TextSection t,uint near)
    {
        int pos=(int)(near-t.Rva),min=Math.Max(0,pos-0x600);
        for(int p=pos;p>=min;p--)if(ClassicPrologue(t,p))return t.Rva+(uint)p;
        return near;
    }

    static void DumpFunction(StringBuilder sb,TextSection text,uint near,string title)
    {
        uint start=FindStart(text,near);sb.AppendLine($"--- {title}: near RVA 0x{near:X6}, decode start 0x{start:X6} ---");
        var list=Decode(text,start,0x900,500);bool reached=false;int n=0;
        foreach(var x in list)
        {
            if(Rva(x)==near)reached=true;if(!reached&&Rva(x)+0x40<near)continue;
            sb.AppendLine("  "+BasicLine(x,text));n++;
            if(reached&&x.FlowControl==FlowControl.Return)break;
            if(n>220){sb.AppendLine("  [TRUNCATED]");break;}
        }
        sb.AppendLine();
    }

    static void DumpCallXrefs(StringBuilder sb,TextSection text,uint targetRva)
    {
        sb.AppendLine($"--- Direct .text CALL xrefs to RVA 0x{targetRva:X6} ---");
        ulong target=(ulong)moduleBase+targetRva;int hits=0;
        foreach(var x in Decode(text,text.Rva,(int)text.Size,2000000))
        {
            if(x.FlowControl==FlowControl.Call&&IsNear(x.Op0Kind)&&x.NearBranchTarget==target){sb.AppendLine("  "+BasicLine(x,text));hits++;if(hits>=40)break;}
        }
        if(hits==0)sb.AppendLine("  (none)");sb.AppendLine();
    }

    public static ExpiryTailResult Run()
    {
        try
        {
            if(!Attach(out string err))return new(false,err,err);
            var (unit,count)=FirstSelected();if(count!=1||unit==0)return new(false,$"Select exactly ONE unit with ACTIVE Issyl (selected {count}).","");
            uint def=R32((long)unit+OFF_DEF),owner=R32((long)unit+OFF_OWNER);if(def==0)return new(false,"Selected object has no UnitDef.","");
            if(!FindA5(unit,out uint record,out string path))return new(false,"No active A5+target signature found on selected unit.","");
            uint config=R32((long)record+CONFIG_OFF),stamp=R32((long)record+STAMP_OFF);if(!TryReadText(out var text,out string peErr))return new(false,peErr,peErr);

            var sb=new StringBuilder(131072);
            sb.AppendLine("=== BRZE HERO EFFECT EXPIRY TAIL V25 ===");
            sb.AppendLine("MODE: STRICT READ-ONLY — no hooks, no writes, no native calls");
            sb.AppendLine($"PID={process!.Id} moduleBase=0x{moduleBase:X8} .text RVA=0x{text.Rva:X} size=0x{text.Size:X}");
            sb.AppendLine($"Selected Unit*=0x{unit:X8} UnitDef*=0x{def:X8} owner={owner}");
            sb.AppendLine($"A5 parent=0x{record:X8} via {path}");
            sb.AppendLine($"record+0x194={stamp}; record+0x1F4 config=0x{config:X8}; config ID=0x{R32(config):X8}; config+0xE0 duration={R32((long)config+0xE0)}");
            sb.AppendLine();
            DumpTickTail(sb,text);
            DumpFunction(sb,text,SIDE_CALL,"Known side-call from V24 tick body");
            DumpCallXrefs(sb,text,SIDE_CALL);
            sb.AppendLine("--- INTERPRETATION RULE ---");
            sb.AppendLine("We are looking for the exact natural-expiry decision: record start/timestamp (+0x194), ability config duration (+0xE0), and the branch/call that returns the effect to an expired/unlinked state. Do not invoke any candidate yet.");
            return new(true,$"V25 expiry-tail scan complete for A5 parent 0x{record:X8}. Copy the full report.",sb.ToString());
        }
        catch(Exception ex){return new(false,"V25 scan error: "+ex.Message,ex.ToString());}
        finally{Detach();}
    }
}
