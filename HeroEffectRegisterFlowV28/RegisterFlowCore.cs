using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Iced.Intel;

namespace BRZEHeroEffectRegisterFlowV28;

internal readonly record struct ProbeResult(bool Ok,string Summary,string Report);

internal static class RegisterFlowCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x0010|0x0400;
    const uint TICK_RVA=0x13A5EB;
    const int RVA_SELECTION_LIST=0x441708;
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    const int ABILITY_OFF=0x58,TARGET_OFF=0x17C,CONFIG_OFF=0x1F4,STAMP_OFF=0x194;
    const uint A5=0xA5;
    static readonly int[] ROOTS={0x1E4,0x1E8,0x20C,0x210};
    const int ROOT_SCAN=0x600;

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;

    readonly record struct Section(string Name,uint Rva,uint Size,byte[] Bytes);

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
        var b=new byte[0x1C];if(!ReadExact(moduleBase+RVA_SELECTION_LIST,b))return default;
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

    static bool ReadText(out Section text,out string error)
    {
        text=default;error="";
        var dos=new byte[0x1000];if(!ReadExact(moduleBase,dos)){error="cannot read DOS headers";return false;}
        int pe=BitConverter.ToInt32(dos,0x3C);
        var hdr=new byte[0x1000];if(!ReadExact(moduleBase+pe,hdr)){error="cannot read NT headers";return false;}
        ushort count=BitConverter.ToUInt16(hdr,6),opt=BitConverter.ToUInt16(hdr,20);
        long table=moduleBase+pe+24+opt;var sb=new byte[count*40];if(!ReadExact(table,sb)){error="cannot read section table";return false;}
        for(int i=0;i<count;i++)
        {
            int o=i*40;string name=Encoding.ASCII.GetString(sb,o,8).TrimEnd('\0');
            uint vs=BitConverter.ToUInt32(sb,o+8),rva=BitConverter.ToUInt32(sb,o+12),raw=BitConverter.ToUInt32(sb,o+16);
            uint size=Math.Max(vs,raw);if(name!=".text"||size==0||size>0x01000000)continue;
            var bytes=new byte[size];if(!ReadExact(moduleBase+rva,bytes)){error="cannot read .text";return false;}
            text=new(name,rva,size,bytes);return true;
        }
        error=".text not found";return false;
    }

    static List<Instruction> Decode(Section text,uint startRva,int maxBytes,int maxIns)
    {
        var list=new List<Instruction>();if(startRva<text.Rva||startRva>=text.Rva+text.Size)return list;
        int off=checked((int)(startRva-text.Rva)),n=Math.Min(maxBytes,text.Bytes.Length-off);if(n<=0)return list;
        var b=new byte[n];Buffer.BlockCopy(text.Bytes,off,b,0,n);
        ulong ip=(ulong)moduleBase+startRva,end=ip+(ulong)n;
        var d=Iced.Intel.Decoder.Create(32,new ByteArrayCodeReader(b));d.IP=ip;
        while(d.IP<end&&list.Count<maxIns){var ins=d.Decode();if(ins.Length==0||ins.Code==Code.INVALID)break;list.Add(ins);}
        return list;
    }

    static List<uint> DirectXrefs(Section text,uint targetRva)
    {
        var r=new List<uint>();long target=moduleBase+targetRva;
        for(int i=0;i<=text.Bytes.Length-5;i++)
        {
            if(text.Bytes[i]!=0xE8)continue;
            int rel=BitConverter.ToInt32(text.Bytes,i+1);long next=moduleBase+text.Rva+i+5L;
            if(next+rel==target)r.Add(text.Rva+(uint)i);
        }
        return r;
    }

    static bool Reaches(Section text,uint start,uint marker)
    {
        if(start>marker)return false;
        foreach(var i in Decode(text,start,(int)Math.Min(0x1000u,marker-start+96),900))
        {
            uint r=(uint)(i.IP-(ulong)moduleBase);if(r==marker)return true;if(r>marker)return false;
            if(i.FlowControl==FlowControl.Return)return false;
        }
        return false;
    }

    static uint FindFunctionStart(Section text,uint callRva,out string why)
    {
        int pos=checked((int)(callRva-text.Rva)),min=Math.Max(0,pos-0x1000);
        for(int p=pos;p>=min;p--)
        {
            if(p+2>=text.Bytes.Length||text.Bytes[p]!=0x55||text.Bytes[p+1]!=0x8B||text.Bytes[p+2]!=0xEC)continue;
            uint r=text.Rva+(uint)p;if(Reaches(text,r,callRva)){why=$"validated prologue 0x{r:X6}";return r;}
        }
        why="fallback";return callRva>0x180?callRva-0x180:text.Rva;
    }

    static Register OpReg(Instruction i,int index)=>index switch{0=>i.Op0Register,1=>i.Op1Register,2=>i.Op2Register,3=>i.Op3Register,_=>Register.None};
    static OpKind OpKindAt(Instruction i,int index)=>index switch{0=>i.Op0Kind,1=>i.Op1Kind,2=>i.Op2Kind,3=>i.Op3Kind,_=>OpKind.Register};

    static string Operand(Instruction i,int index)
    {
        var k=OpKindAt(i,index);
        if(k==OpKind.Register)return OpReg(i,index).ToString();
        if(k is OpKind.NearBranch16 or OpKind.NearBranch32 or OpKind.NearBranch64)
        {
            ulong t=i.NearBranchTarget;return t>=(ulong)moduleBase?$"0x{t:X8}(RVA0x{t-(ulong)moduleBase:X6})":$"0x{t:X8}";
        }
        if(k is OpKind.Immediate8 or OpKind.Immediate8_2nd)return $"0x{i.Immediate8:X2}";
        if(k is OpKind.Immediate16)return $"0x{i.Immediate16:X4}";
        if(k is OpKind.Immediate32 or OpKind.Immediate32to64)return $"0x{i.Immediate32:X8}";
        if(k is OpKind.Immediate8to16 or OpKind.Immediate8to32 or OpKind.Immediate8to64)return $"0x{unchecked((byte)i.Immediate8to32):X2}";
        if(k is OpKind.Memory or OpKind.MemorySegSI or OpKind.MemorySegESI or OpKind.MemorySegRSI or OpKind.MemorySegDI or OpKind.MemorySegEDI or OpKind.MemorySegRDI)
        {
            string b=i.MemoryBase==Register.None?"":i.MemoryBase.ToString();
            string ix=i.MemoryIndex==Register.None?"":$"+{i.MemoryIndex}*{i.MemoryIndexScale}";
            string d=i.MemoryDisplSize==0?"":$"+0x{i.MemoryDisplacement64:X}";
            return $"[{b}{ix}{d}]";
        }
        return k.ToString();
    }

    static string InstText(Instruction i,uint callRva)
    {
        uint r=(uint)(i.IP-(ulong)moduleBase);var ops=new List<string>();
        for(int x=0;x<i.OpCount&&x<4;x++)ops.Add(Operand(i,x));
        string tag=r==callRva?"  <<< CALL TARGET":"";
        if(i.MemoryDisplSize!=0&&i.MemoryBase==Register.None&&i.MemoryIndex==Register.None)tag+="  [ABS_MEM]";
        return $"RVA 0x{r:X6}  {i.Mnemonic,-10} {string.Join(", ",ops)}{tag}";
    }

    static bool Same32(Register a,Register b)
    {
        if(a==b)return true;
        static Register Norm(Register r)=>r switch
        {
            Register.AL or Register.AH or Register.AX=>Register.EAX,
            Register.BL or Register.BH or Register.BX=>Register.EBX,
            Register.CL or Register.CH or Register.CX=>Register.ECX,
            Register.DL or Register.DH or Register.DX=>Register.EDX,
            Register.SI=>Register.ESI,Register.DI=>Register.EDI,Register.BP=>Register.EBP,Register.SP=>Register.ESP,
            _=>r
        };
        return Norm(a)==Norm(b);
    }

    static string TraceRegisterSource(List<Instruction> ins,int beforeIndex,Register reg,out uint absAddress)
    {
        absAddress=0;Register cur=reg;var trace=new List<string>{$"start {cur}"};
        for(int i=beforeIndex-1;i>=0&&beforeIndex-i<=90;i--)
        {
            var x=ins[i];
            if(x.Mnemonic==Mnemonic.Mov&&x.Op0Kind==OpKind.Register&&Same32(x.Op0Register,cur))
            {
                if(x.Op1Kind==OpKind.Register){trace.Add($"0x{(uint)(x.IP-(ulong)moduleBase):X6}: {cur} <- {x.Op1Register}");cur=x.Op1Register;continue;}
                if(x.Op1Kind==OpKind.Memory)
                {
                    uint r=(uint)(x.IP-(ulong)moduleBase);
                    if(x.MemoryBase==Register.EBP&&x.MemoryIndex==Register.None&&x.MemoryDisplacement64==8){trace.Add($"0x{r:X6}: {cur} <- [EBP+8]  <<< CALLER ARG");return string.Join(" -> ",trace);}
                    if(x.MemoryBase==Register.None&&x.MemoryIndex==Register.None){absAddress=(uint)x.MemoryDisplacement64;trace.Add($"0x{r:X6}: {cur} <- [0x{absAddress:X8}]  <<< ABS SOURCE");return string.Join(" -> ",trace);}
                    trace.Add($"0x{r:X6}: {cur} <- {Operand(x,1)}  <<< MEMORY SOURCE");return string.Join(" -> ",trace);
                }
                trace.Add($"0x{(uint)(x.IP-(ulong)moduleBase):X6}: {cur} <- {Operand(x,1)}");return string.Join(" -> ",trace);
            }
            if(x.Mnemonic==Mnemonic.Lea&&x.Op0Kind==OpKind.Register&&Same32(x.Op0Register,cur))
            {
                trace.Add($"0x{(uint)(x.IP-(ulong)moduleBase):X6}: {cur} <- LEA {Operand(x,1)}");return string.Join(" -> ",trace);
            }
            if(x.FlowControl==FlowControl.Call&&Same32(cur,Register.EAX))
            {
                trace.Add($"0x{(uint)(x.IP-(ulong)moduleBase):X6}: EAX <- CALL return  <<< FUNCTION RESULT");return string.Join(" -> ",trace);
            }
        }
        trace.Add("source not resolved inside local caller window");return string.Join(" -> ",trace);
    }

    static void DumpCallLink(StringBuilder sb,Section text,uint targetRva,int depth,List<uint> absCandidates)
    {
        var xs=DirectXrefs(text,targetRva);
        sb.AppendLine($"=== DEPTH {depth}: target RVA 0x{targetRva:X6}; direct xrefs={xs.Count} ===");
        foreach(uint call in xs.Take(8))
        {
            uint start=FindFunctionStart(text,call,out string why);
            var ins=Decode(text,start,(int)Math.Min(0x1400u,call-start+0x140),1200);
            int idx=ins.FindIndex(z=>(uint)(z.IP-(ulong)moduleBase)==call);
            sb.AppendLine($"call@0x{call:X6}; caller=0x{start:X6}; {why}");
            if(idx<0){sb.AppendLine("  decode marker missing");continue;}
            int lo=Math.Max(0,idx-42),hi=Math.Min(ins.Count-1,idx+8);
            for(int i=lo;i<=hi;i++)sb.AppendLine("  "+InstText(ins[i],call));

            int push=-1;
            for(int i=idx-1;i>=Math.Max(0,idx-12);i--){if(ins[i].Mnemonic==Mnemonic.Push){push=i;break;}}
            if(push>=0)
            {
                sb.AppendLine($"  ARG-CANDIDATE last PUSH before call: {InstText(ins[push],uint.MaxValue)}");
                if(ins[push].Op0Kind==OpKind.Register)
                {
                    string tr=TraceRegisterSource(ins,push,ins[push].Op0Register,out uint abs);
                    sb.AppendLine("  BACKWARD TRACE: "+tr);
                    if(abs!=0)absCandidates.Add(abs);
                }
                else if(ins[push].Op0Kind==OpKind.Memory)
                {
                    sb.AppendLine("  BACKWARD TRACE: argument is direct memory "+Operand(ins[push],0));
                    if(ins[push].MemoryBase==Register.None&&ins[push].MemoryIndex==Register.None)absCandidates.Add((uint)ins[push].MemoryDisplacement64);
                }
                else sb.AppendLine("  BACKWARD TRACE: push operand "+Operand(ins[push],0));
            }
            else sb.AppendLine("  ARG-CANDIDATE: no PUSH found in last 12 instructions; inspect ECX/register calling convention manually.");
            sb.AppendLine();

            foreach(var z in ins.Take(idx+1))
                if(z.MemoryDisplSize!=0&&z.MemoryBase==Register.None&&z.MemoryIndex==Register.None&&z.MemoryDisplacement64>=0x10000&&z.MemoryDisplacement64<0x80000000)
                    absCandidates.Add((uint)z.MemoryDisplacement64);
        }
        sb.AppendLine();
    }

    static void SampleAbs(StringBuilder sb,List<uint> addresses)
    {
        var uniq=addresses.Distinct().Take(120).ToList();
        sb.AppendLine($"--- ABSOLUTE MEMORY CANDIDATES SAMPLED 350ms APART: {uniq.Count} ---");
        if(uniq.Count==0){sb.AppendLine("(none)");return;}
        var a=new Dictionary<uint,uint>();foreach(uint p in uniq)a[p]=R32(p);
        Thread.Sleep(350);
        foreach(var kv in a)
        {
            uint b=R32(kv.Key);long d=(long)b-kv.Value;
            string mark=d>0&&d<5000?"  <<< CHANGING CLOCK-LIKE":"";
            sb.AppendLine($"0x{kv.Key:X8}: {kv.Value} -> {b} delta={d}{mark}");
        }
    }

    public static ProbeResult Run()
    {
        try
        {
            if(!Attach(out string err))return new(false,err,err);
            if(!ReadText(out var text,out string peErr))return new(false,peErr,peErr);
            var sb=new StringBuilder(90000);
            sb.AppendLine("=== BRZE HERO EFFECT REGISTER FLOW V28 ===");
            sb.AppendLine("MODE: STRICT READ-ONLY — exact operand/register tracing, no hooks/writes/native calls");
            sb.AppendLine($"PID={process!.Id} moduleBase=0x{moduleBase:X8} .text RVA=0x{text.Rva:X} size=0x{text.Size:X}");
            sb.AppendLine($"Tick target RVA=0x{TICK_RVA:X6} runtime=0x{moduleBase+TICK_RVA:X8}");

            var (unit,count)=FirstSelected();
            if(count==1&&unit!=0&&FindA5(unit,out uint rec,out string path))
            {
                uint def=R32((long)unit+OFF_DEF),owner=R32((long)unit+OFF_OWNER),cfg=R32((long)rec+CONFIG_OFF);
                sb.AppendLine($"Selected Unit*=0x{unit:X8} UnitDef*=0x{def:X8} owner={owner}");
                sb.AppendLine($"Active A5 record=0x{rec:X8} via {path}; stamp={R32((long)rec+STAMP_OFF)}; config=0x{cfg:X8}; duration={R32((long)cfg+0xE0)}");
            }
            else sb.AppendLine($"Selected count={count}; active A5 context not required for static caller tracing.");
            sb.AppendLine();

            var abs=new List<uint>();uint target=TICK_RVA;
            for(int depth=0;depth<5;depth++)
            {
                var xs=DirectXrefs(text,target);DumpCallLink(sb,text,target,depth,abs);
                if(xs.Count!=1)break;
                target=FindFunctionStart(text,xs[0],out _);
            }
            SampleAbs(sb,abs);
            sb.AppendLine();
            sb.AppendLine("--- DECISION RULE ---");
            sb.AppendLine("Follow the stack argument consumed by RVA 0x13A5EB upward through the unique caller chain. We want the first concrete source that is either a changing BRZE clock global or a function result whose producer can be identified. Do not write record+0x194 until the source is proven.");
            return new(true,"Register-flow trace complete. Copy the full report.",sb.ToString());
        }
        catch(Exception ex){return new(false,"V28 error: "+ex.Message,ex.ToString());}
        finally{Detach();}
    }
}
