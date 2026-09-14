using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;

namespace BRZEHeroEffectResetDeepScanV24;

internal readonly record struct DeepScanResult(bool Ok,string Summary,string Report);

internal static class DeepScanCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x0010|0x0400;
    const int RVA_SELECTION_LIST=0x441708;
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    static readonly int[] ROOTS={0x1E4,0x1E8,0x20C,0x210};
    const int ROOT_SCAN=0x600;
    const int ABILITY_OFF=0x58,TARGET_OFF=0x17C,CONFIG_OFF=0x1F4,TEARDOWN_OFF=0x194;
    const uint A5=0xA5;

    static readonly uint[] PRIMARY_CANDIDATES={0x13DA9C,0x13A8B3,0x16B76A,0x2151DE,0x24A7B8};
    static readonly int[] VTABLE_SLOTS={4,5,20,23,24};

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

    static bool Signature(uint addr,uint unit)
    {
        if(!Ptr(addr))return false;
        return R32((long)addr+ABILITY_OFF)==A5&&R32((long)addr+TARGET_OFF)==unit;
    }

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

    readonly record struct TextSection(uint Rva,uint Size,byte[] Bytes);

    static bool TryReadText(out TextSection text,out string error)
    {
        text=default;error="";
        var dos=new byte[0x1000];if(!ReadExact(moduleBase,dos)){error="cannot read DOS/PE headers";return false;}
        if(dos[0]!=0x4D||dos[1]!=0x5A){error="MZ signature mismatch";return false;}
        int pe=BitConverter.ToInt32(dos,0x3C);
        var hdr=new byte[0x800];if(!ReadExact(moduleBase+pe,hdr)){error="cannot read NT headers";return false;}
        if(hdr[0]!=0x50||hdr[1]!=0x45){error="PE signature mismatch";return false;}
        ushort sections=BitConverter.ToUInt16(hdr,6),optSize=BitConverter.ToUInt16(hdr,20);
        long sh=moduleBase+pe+24+optSize;
        var sec=new byte[sections*40];if(!ReadExact(sh,sec)){error="cannot read section table";return false;}
        for(int i=0;i<sections;i++)
        {
            int o=i*40;string name=Encoding.ASCII.GetString(sec,o,8).TrimEnd('\0');
            uint virtualSize=BitConverter.ToUInt32(sec,o+8),rva=BitConverter.ToUInt32(sec,o+12),rawSize=BitConverter.ToUInt32(sec,o+16);
            if(name!=".text")continue;
            uint size=Math.Max(virtualSize,rawSize);if(size==0||size>0x01000000){error=$"invalid .text size 0x{size:X}";return false;}
            var bytes=new byte[size];if(!ReadExact(moduleBase+rva,bytes)){error="cannot read .text";return false;}
            text=new(rva,size,bytes);return true;
        }
        error=".text section not found";return false;
    }

    static List<Instruction> DecodeSlice(TextSection text,uint startRva,int maxBytes,int maxInstructions)
    {
        var result=new List<Instruction>();
        if(startRva<text.Rva||startRva>=text.Rva+text.Size)return result;
        int off=checked((int)(startRva-text.Rva));
        int n=Math.Min(maxBytes,text.Bytes.Length-off);if(n<=0)return result;
        var buf=new byte[n];Buffer.BlockCopy(text.Bytes,off,buf,0,n);
        ulong ip=(ulong)moduleBase+startRva,end=ip+(ulong)n;
        var decoder=Iced.Intel.Decoder.Create(32,new ByteArrayCodeReader(buf));decoder.IP=ip;
        while(decoder.IP<end&&result.Count<maxInstructions)
        {
            var ins=decoder.Decode();
            if(ins.Length==0||ins.Code==Code.INVALID)break;
            result.Add(ins);
        }
        return result;
    }

    static bool ReachesTarget(TextSection text,uint startRva,uint targetRva)
    {
        if(startRva>targetRva||startRva<text.Rva||targetRva>=text.Rva+text.Size)return false;
        int span=checked((int)(targetRva-startRva))+32;
        foreach(var ins in DecodeSlice(text,startRva,Math.Min(span,0x700),500))
        {
            uint rva=(uint)(ins.IP-(ulong)moduleBase);
            if(rva==targetRva)return true;
            if(rva>targetRva)return false;
            if(ins.FlowControl==FlowControl.Return)return false;
        }
        return false;
    }

    static bool IsPrologueAt(byte[] b,int p,out int entry)
    {
        entry=p;
        if(p>=0&&p+2<b.Length&&b[p]==0x55&&b[p+1]==0x8B&&b[p+2]==0xEC)return true;
        if(p>=0&&p+4<b.Length&&b[p]==0x8B&&b[p+1]==0xFF&&b[p+2]==0x55&&b[p+3]==0x8B&&b[p+4]==0xEC){entry=p;return true;}
        return false;
    }

    static bool FindFunctionStart(TextSection text,uint targetRva,out uint startRva,out string reason)
    {
        startRva=targetRva;reason="no validated classic prologue found";
        if(targetRva<text.Rva||targetRva>=text.Rva+text.Size)return false;
        int target=checked((int)(targetRva-text.Rva));int min=Math.Max(0,target-0x500);
        for(int p=target;p>=min;p--)
        {
            if(!IsPrologueAt(text.Bytes,p,out int entry))continue;
            uint rva=text.Rva+(uint)entry;
            if(ReachesTarget(text,rva,targetRva))
            {
                startRva=rva;reason=$"validated classic x86 prologue at RVA 0x{rva:X6}";return true;
            }
        }
        return false;
    }

    static bool IsNear(OpKind k)=>k==OpKind.NearBranch16||k==OpKind.NearBranch32||k==OpKind.NearBranch64;

    static string Line(Instruction i,TextSection text,uint markerRva)
    {
        uint rva=(uint)(i.IP-(ulong)moduleBase);
        var tags=new List<string>();
        ulong d=i.MemoryDisplacement64;
        if(i.MemoryDisplSize!=0)
        {
            if(d==0x58)tags.Add("+058");
            if(d==0x17C)tags.Add("+17C");
            if(d==0x194)tags.Add("+194");
            if(d==0x1F4)tags.Add("+1F4");
        }
        if(rva==markerRva)tags.Add("<<< V23 HIT");
        string branch="";
        if(IsNear(i.Op0Kind))
        {
            ulong t=i.NearBranchTarget;
            ulong lo=(ulong)moduleBase+text.Rva,hi=lo+text.Size;
            branch=t>=lo&&t<hi?$" target=RVA 0x{t-(ulong)moduleBase:X6}":$" target=0x{t:X8}";
        }
        string mem=i.MemoryDisplSize!=0?$" mem=[{i.MemoryBase}+{i.MemoryIndex}*{i.MemoryIndexScale}+0x{d:X}]":"";
        string tag=tags.Count>0?$"  [{string.Join(",",tags)}]":"";
        return $"RVA 0x{rva:X6} len={i.Length,2} {i.Mnemonic,-12} op0={i.Op0Kind,-18} op1={i.Op1Kind,-18}{mem}{branch}{tag}";
    }

    static void DumpContainingFunction(StringBuilder sb,TextSection text,uint markerRva)
    {
        bool found=FindFunctionStart(text,markerRva,out uint start,out string reason);
        sb.AppendLine($"Candidate RVA 0x{markerRva:X6}: boundary={(found?$"0x{start:X6}":"UNRESOLVED")} | {reason}");
        uint decodeStart=found?start:markerRva;
        var ins=DecodeSlice(text,decodeStart,0x900,260);
        bool passedMarker=false;int afterMarker=0;
        foreach(var i in ins)
        {
            uint rva=(uint)(i.IP-(ulong)moduleBase);
            if(rva==markerRva)passedMarker=true;
            if(passedMarker)afterMarker++;
            sb.AppendLine("  "+Line(i,text,markerRva));
            if(passedMarker&&i.FlowControl==FlowControl.Return)break;
            if(passedMarker&&afterMarker>120)break;
        }
        sb.AppendLine();
    }

    static void DumpVtableMethod(StringBuilder sb,TextSection text,uint vtable,int slot)
    {
        uint fn=R32((long)vtable+slot*4);
        ulong lo=(ulong)moduleBase+text.Rva,hi=lo+text.Size;
        sb.AppendLine($"VT[{slot}] ptr=0x{fn:X8}"+(fn>=lo&&fn<hi?$" RVA=0x{fn-(uint)moduleBase:X6}":" OUTSIDE .text"));
        if(fn<lo||fn>=hi){sb.AppendLine();return;}
        uint start=fn-(uint)moduleBase;
        var ins=DecodeSlice(text,start,0x700,180);
        int n=0;
        foreach(var i in ins)
        {
            sb.AppendLine("  "+Line(i,text,uint.MaxValue));n++;
            if(i.FlowControl==FlowControl.Return||n>=100)break;
        }
        sb.AppendLine();
    }

    public static DeepScanResult Run()
    {
        try
        {
            if(!Attach(out string err))return new(false,err,err);
            var (unit,count)=FirstSelected();
            if(count!=1||unit==0)return new(false,$"Select exactly ONE unit with ACTIVE Issyl (selected {count}).","");
            uint def=R32((long)unit+OFF_DEF),owner=R32((long)unit+OFF_OWNER);
            if(def==0)return new(false,"Selected object has no UnitDef.","");
            if(!FindA5(unit,out uint record,out string path))return new(false,"No active A5+target signature found on selected unit.","");
            uint vtable=R32(record),config=R32((long)record+CONFIG_OFF),teardown=R32((long)record+TEARDOWN_OFF);
            if(!TryReadText(out var text,out string peErr))return new(false,peErr,peErr);

            var sb=new StringBuilder(65536);
            sb.AppendLine("=== BRZE HERO EFFECT RESET DEEP SCAN V24 ===");
            sb.AppendLine("MODE: STRICT READ-ONLY — no hooks, no writes, no native calls");
            sb.AppendLine($"PID={process!.Id} moduleBase=0x{moduleBase:X8} .text RVA=0x{text.Rva:X} size=0x{text.Size:X}");
            sb.AppendLine($"Selected Unit*=0x{unit:X8} UnitDef*=0x{def:X8} owner={owner}");
            sb.AppendLine($"A5 parent=0x{record:X8} via {path}");
            sb.AppendLine($"parent vtable/type=0x{vtable:X8} parent+0x194={teardown} parent+0x1F4 config=0x{config:X8} configID=0x{R32(config):X8} duration={R32((long)config+0xE0)}");
            sb.AppendLine();
            sb.AppendLine("--- PART A: V23 TOP CANDIDATE CONTAINING FUNCTIONS ---");
            foreach(uint rva in PRIMARY_CANDIDATES)DumpContainingFunction(sb,text,rva);
            sb.AppendLine("--- PART B: A5 RECORD VTABLE METHODS SEEN IN V23 ---");
            foreach(int slot in VTABLE_SLOTS)DumpVtableMethod(sb,text,vtable,slot);
            sb.AppendLine("--- INTERPRETATION RULE ---");
            sb.AppendLine("Do not call any candidate yet. We want a function boundary/call chain that structurally owns natural expiry or unlink/destruction of this exact effect instance, not a generic +0x194 writer.");
            return new(true,$"Deep scan complete for A5 parent 0x{record:X8}. Copy the full report.",sb.ToString());
        }
        catch(Exception ex){return new(false,"Deep scan error: "+ex.Message,ex.ToString());}
        finally{Detach();}
    }
}
