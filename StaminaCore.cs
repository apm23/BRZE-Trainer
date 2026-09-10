using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class StaminaCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint allocationType,uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr addr,UIntPtr size,uint newProtect,out uint oldProtect);
    [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x10|0x20|0x8|0x400;
    const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_EXECUTE_READWRITE=0x40;
    const int RVA_LOCAL_ID=0x4416D0;
    const int RVA_WEMOD_STAMINA_READ=0x1D3B42;
    const int OFF_OWNER=0x240,OFF_SEL_A=0x3A8,OFF_SEL_B=0x3AC,OFF_STAMINA=0x408;
    const uint WEMOD_RAW_STAMINA=100_000_000u; // observer-proven 0x05F5E100
    static readonly byte[] Original={0x66,0x0F,0x6E,0x9E,0x08,0x04,0x00,0x00}; // movd xmm3,[esi+408]

    static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static bool installed,lastEnabled;
    static string error="";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool WriteRaw(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool WriteCode(long a,byte[] b){if(h==IntPtr.Zero)return false;var x=A(a);if(!VirtualProtectEx(h,x,(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;bool ok=WriteRaw(a,b);if(ok)FlushInstructionCache(h,x,(UIntPtr)b.Length);VirtualProtectEx(h,x,(UIntPtr)b.Length,old,out _);return ok;}
    static void U32(List<byte>b,uint v)=>b.AddRange(BitConverter.GetBytes(v));
    static void I32(List<byte>b,int v)=>b.AddRange(BitConverter.GetBytes(v));
    static void Rel(List<byte>b,int at,long fromNext,long target){var q=BitConverter.GetBytes(unchecked((int)(target-fromNext)));for(int i=0;i<4;i++)b[at+i]=q[i];}

    static bool Attach(){try{if(p!=null&&!p.HasExited&&h!=IntPtr.Zero)return true;}catch{}Detach(false);var ps=Process.GetProcessesByName("Battle_Realms_F");if(ps.Length==0)return false;p=ps[0];try{moduleBase=p.MainModule!.BaseAddress.ToInt64();}catch{p=null;return false;}h=OpenProcess(ACCESS,false,p.Id);return h!=IntPtr.Zero;}

    static byte[] BuildStub(long stub,long target)
    {
        // Runtime Wand tri-diff: RVA 0x1D3B42 is detoured while Unlimited Stamina is ON,
        // replacing the native movd xmm3,[esi+408]. The selected unit's +408 becomes
        // exactly 100,000,000 raw (0x05F5E100). Reproduce that evidence narrowly:
        // local owner + UI/SIM selected only, then execute the original read unchanged.
        // Preserve EFLAGS because native MOVD does not modify flags.
        var b=new List<byte>();
        b.Add(0x9C);                                     // pushfd
        b.Add(0x50);                                     // push eax
        b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // eax=localId
        b.AddRange(new byte[]{0x39,0x86,0x40,0x02,0x00,0x00}); // cmp [esi+240],eax
        b.AddRange(new byte[]{0x0F,0x85});int jSkip=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xBE,0xA8,0x03,0x00,0x00,0x01}); // UI selected?
        b.AddRange(new byte[]{0x0F,0x84});int jWrite=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xBE,0xAC,0x03,0x00,0x00,0x01}); // SIM selected?
        b.AddRange(new byte[]{0x0F,0x85});int jSkip2=b.Count;I32(b,0);
        int write=b.Count;
        b.AddRange(new byte[]{0xC7,0x86,0x08,0x04,0x00,0x00});U32(b,WEMOD_RAW_STAMINA);
        int skip=b.Count;
        b.Add(0x58);                                     // pop eax
        b.Add(0x9D);                                     // popfd
        b.AddRange(Original);                            // native movd xmm3,[esi+408]
        b.Add(0xE9);int jBack=b.Count;I32(b,0);
        Rel(b,jSkip,stub+jSkip+4,stub+skip);
        Rel(b,jWrite,stub+jWrite+4,stub+write);
        Rel(b,jSkip2,stub+jSkip2+4,stub+skip);
        Rel(b,jBack,stub+jBack+4,target+Original.Length);
        return b.ToArray();
    }

    static bool EnsureInstalled()
    {
        if(installed)return true;if(!Attach())return false;
        long target=moduleBase+RVA_WEMOD_STAMINA_READ;
        var now=new byte[Original.Length];
        if(!ReadExact(target,now)||!System.Linq.Enumerable.SequenceEqual(now,Original)){error="stamina read-site byte mismatch (close Wand/WeMod and restart BRZE)";return false;}
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)256,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);if(cave==IntPtr.Zero){error="stamina cave allocation failed";return false;}
        long stub=cave.ToInt64();var body=BuildStub(stub,target);if(!WriteRaw(stub,body)){error="stamina cave write failed";return false;}
        var patch=new byte[Original.Length];patch[0]=0xE9;Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,patch,1,4);for(int i=5;i<patch.Length;i++)patch[i]=0x90;
        if(!WriteCode(target,patch)){error="stamina detour install failed";return false;}
        installed=true;error="";return true;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "STAMINA: waiting for Battle_Realms_F.exe...";
        if(enabled){if(!EnsureInstalled())return "STAMINA NOT ARMED | "+error;lastEnabled=true;return "STAMINA: Wand read-site port ARMED | selected local | raw=100000000";}
        if(lastEnabled&&installed){WriteCode(moduleBase+RVA_WEMOD_STAMINA_READ,Original);installed=false;if(cave!=IntPtr.Zero){VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;}}
        lastEnabled=false;return "STAMINA: OFF";
    }

    public static void Stop()=>Detach(true);
    static void Detach(bool restore){if(h!=IntPtr.Zero){if(restore&&installed)WriteCode(moduleBase+RVA_WEMOD_STAMINA_READ,Original);if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);CloseHandle(h);}h=IntPtr.Zero;cave=IntPtr.Zero;p=null;moduleBase=0;installed=false;lastEnabled=false;error="";}
}
