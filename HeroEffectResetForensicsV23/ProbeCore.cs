using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;

namespace BRZEHeroEffectResetForensicsV23;

internal readonly record struct ScanResult(bool Ok,string Summary,string Report);

internal static class ProbeCore
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

    static IEnumerable<Instruction> Decode(byte[] bytes,ulong ip)
    {
        var decoder=Decoder.Create(32,new ByteArrayCodeReader(bytes));decoder.IP=ip;
        while(decoder.CanDecode)
        {
            var ins=decoder.Decode();if(ins.Length==0)yield break;yield return ins;
        }
    }

    static bool IsTrackedDisp(Instruction i,ulong d)
    {
        if(i.MemoryDisplSize==0)return false;
        return i.MemoryDisplacement64==d;
    }

    static bool IsMemWriteTo(Instruction i,ulong d)
    {
        if(i.Op0Kind!=OpKind.Memory||!IsTrackedDisp(i,d))return false;
        return i.Mnemonic is Mnemonic.Mov or Mnemonic.Movzx or Mnemonic.Movsx or Mnemonic.And or Mnemonic.Or or Mnemonic.Xor or Mnemonic.Add or Mnemonic.Sub or Mnemonic.Inc or Mnemonic.Dec or Mnemonic.Cmpxchg;
    }

    static string InsLine(Instruction i)
    {
        string flags="";
        if(IsTrackedDisp(i,TEARDOWN_OFF))flags+=" [+194]";
        if(IsTrackedDisp(i,ABILITY_OFF))flags+=" [+058]";
        if(IsTrackedDisp(i,TARGET_OFF))flags+=" [+17C]";
        if(IsTrackedDisp(i,CONFIG_OFF))flags+=" [+1F4]";
        if(IsMemWriteTo(i,TEARDOWN_OFF))flags+=" WRITE194";
        if(i.FlowControl==FlowControl.Call)flags+=" CALL";
        if(i.FlowControl==FlowControl.Return)flags+=" RET";
        return $"0x{i.IP-moduleBase:X6}  {i.Mnemonic,-9} len={i.Length,2}{flags}";
    }

    sealed class Candidate
    {
        public ulong Ip;
        public int Score;
        public bool Write194;
        public HashSet<ulong> Disps=new();
        public int Calls;
        public List<string> Lines=new();
    }

    static List<Candidate> ScanText(TextSection text)
    {
        ulong baseIp=(ulong)moduleBase+text.Rva;
        var all=Decode(text.Bytes,baseIp).ToList();
        var refs=new List<int>();
        for(int i=0;i<all.Count;i++)if(IsTrackedDisp(all[i],TEARDOWN_OFF))refs.Add(i);
        var result=new List<Candidate>();
        foreach(int idx in refs)
        {
            int a=Math.Max(0,idx-18),b=Math.Min(all.Count-1,idx+28);
            var c=new Candidate{Ip=all[idx].IP};
            for(int j=a;j<=b;j++)
            {
                var x=all[j];
                foreach(ulong d in new[]{0x58ul,0x17Cul,0x194ul,0x1F4ul})if(IsTrackedDisp(x,d))c.Disps.Add(d);
                if(x.FlowControl==FlowControl.Call)c.Calls++;
                if(IsMemWriteTo(x,TEARDOWN_OFF))c.Write194=true;
            }
            c.Score=(c.Write194?20:5)+c.Disps.Count*6+Math.Min(c.Calls,6);
            c.Lines.Add(InsLine(all[idx]));
            result.Add(c);
        }
        return result.OrderByDescending(x=>x.Score).ThenBy(x=>x.Ip).Take(24).ToList();
    }

    static List<string> ScanVtable(uint vtable,TextSection text)
    {
        var lines=new List<string>();ulong textLo=(ulong)moduleBase+text.Rva,textHi=textLo+text.Size;
        for(int slot=0;slot<48;slot++)
        {
            uint fn=R32((long)vtable+slot*4);if(fn<textLo||fn>=textHi)continue;
            int off=(int)(fn-textLo);int n=Math.Min(256,text.Bytes.Length-off);if(n<=0)continue;
            var buf=new byte[n];Buffer.BlockCopy(text.Bytes,off,buf,0,n);
            var ins=Decode(buf,fn).Take(48).ToList();
            var hits=new HashSet<ulong>();bool w194=false;int calls=0;
            foreach(var x in ins)
            {
                foreach(ulong d in new[]{0x58ul,0x17Cul,0x194ul,0x1F4ul})if(IsTrackedDisp(x,d))hits.Add(d);
                if(IsMemWriteTo(x,TEARDOWN_OFF))w194=true;
                if(x.FlowControl==FlowControl.Call)calls++;
                if(x.FlowControl==FlowControl.Return)break;
            }
            if(hits.Count==0&&calls==0)continue;
            string hs=string.Join(",",hits.Select(x=>$"+0x{x:X}"));
            lines.Add($"VT[{slot,2}] -> RVA 0x{fn-(uint)moduleBase:X6} | hits [{hs}] | write194={w194} | calls={calls}");
        }
        return lines;
    }

    public static ScanResult ScanActiveIssyl()
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

            var vt=ScanVtable(vtable,text);var refs=ScanText(text);
            var sb=new StringBuilder();
            sb.AppendLine("=== BRZE HERO EFFECT RESET FORENSICS V23 ===");
            sb.AppendLine("MODE: STRICT READ-ONLY — no hooks, no writes, no native calls");
            sb.AppendLine($"PID={process!.Id} moduleBase=0x{moduleBase:X8} .text RVA=0x{text.Rva:X} size=0x{text.Size:X}");
            sb.AppendLine($"Selected Unit*=0x{unit:X8} UnitDef*=0x{def:X8} owner={owner}");
            sb.AppendLine($"A5 parent=0x{record:X8} via {path}");
            sb.AppendLine($"parent+0x000 vtable/type=0x{vtable:X8}");
            sb.AppendLine($"parent+0x194 current=0x{teardown:X8} ({teardown})");
            sb.AppendLine($"parent+0x1F4 config=0x{config:X8} config ID=0x{R32(config):X8} duration={R32((long)config+0xE0)}");
            sb.AppendLine();sb.AppendLine("--- VTABLE METHODS WITH RELEVANT MEMORY/CALL ACTIVITY ---");
            if(vt.Count==0)sb.AppendLine("(none in first 48 slots)");else foreach(var x in vt)sb.AppendLine(x);
            sb.AppendLine();sb.AppendLine("--- TOP .text REFERENCES TO EFFECT parent+0x194 ---");
            if(refs.Count==0)sb.AppendLine("(no decoded +0x194 references found)");
            else foreach(var c in refs)
            {
                string ds=string.Join(",",c.Disps.Select(x=>$"+0x{x:X}"));
                sb.AppendLine($"score={c.Score,2} RVA=0x{c.Ip-(ulong)moduleBase:X6} write194={c.Write194} calls={c.Calls} nearby=[{ds}]");
                foreach(var l in c.Lines)sb.AppendLine("  "+l);
            }
            sb.AppendLine();sb.AppendLine("Interpretation target: identify a native/virtual cleanup path that owns natural expiry before any reset write/call is attempted.");
            return new(true,$"LOCKED A5 parent 0x{record:X8}; found {vt.Count} vtable candidates and {refs.Count} ranked +0x194 refs.",sb.ToString());
        }
        catch(Exception ex){return new(false,"Scan error: "+ex.Message,ex.ToString());}
        finally{Detach();}
    }
}
