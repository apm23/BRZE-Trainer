from pathlib import Path

p = Path('HorseCore.cs')
if p.exists():
    raise SystemExit('HorseCore.cs already exists')

p.write_text(r'''using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class HorseCore
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
    const int RVA_HORSE_SLOT_LOOP=0x0D5482;
    // Native loop: slot0=building+0x5AC, stride=0x1C, EDX counts 6..1.
    // base = ESI + EDX*0x1C - 0x654; owner = base+0x84 => ESI+EDX*0x1C-0x5D0.
    const int OWNER_DYNAMIC_BIAS=0x5D0;
    static readonly byte[] Original={0x83,0x3E,0x00,0x8D,0x41,0x01};

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
        // Wand runtime diff proves this exact native loop owns the six Stable horse slots.
        // For every iteration, reconstruct the Stable owner from current ESI + EDX.
        // Local player only: force current slot occupied=1, then run original cmp/lea.
        var b=new List<byte>();
        b.Add(0x53);                                     // push ebx
        b.AddRange(new byte[]{0x8B,0xC2});              // mov eax,edx
        b.AddRange(new byte[]{0x6B,0xC0,0x1C});         // imul eax,eax,0x1C
        b.AddRange(new byte[]{0x8D,0x84,0x06});I32(b,-OWNER_DYNAMIC_BIAS); // lea eax,[esi+eax-0x5D0]
        b.AddRange(new byte[]{0x8B,0x1D});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // ebx=localId
        b.AddRange(new byte[]{0x39,0x18});              // cmp [eax],ebx
        b.AddRange(new byte[]{0x0F,0x85});int jSkip=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xC7,0x06,0x01,0x00,0x00,0x00}); // [esi]=1
        int skip=b.Count;
        b.Add(0x5B);                                     // pop ebx
        b.AddRange(Original);                            // cmp [esi],0 ; lea eax,[ecx+1]
        b.Add(0xE9);int jBack=b.Count;I32(b,0);
        Rel(b,jSkip,stub+jSkip+4,stub+skip);
        Rel(b,jBack,stub+jBack+4,target+Original.Length);
        return b.ToArray();
    }

    static bool EnsureInstalled()
    {
        if(installed)return true;if(!Attach())return false;
        long target=moduleBase+RVA_HORSE_SLOT_LOOP;
        var now=new byte[Original.Length];if(!ReadExact(target,now)||!System.Linq.Enumerable.SequenceEqual(now,Original)){error="horse loop byte mismatch";return false;}
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)256,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);if(cave==IntPtr.Zero){error="horse cave allocation failed";return false;}
        long stub=cave.ToInt64();var body=BuildStub(stub,target);if(!WriteRaw(stub,body)){error="horse cave write failed";return false;}
        var patch=new byte[Original.Length];patch[0]=0xE9;Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,patch,1,4);patch[5]=0x90;
        if(!WriteCode(target,patch)){error="horse detour install failed";return false;}
        installed=true;error="";return true;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "HORSE: waiting for Battle_Realms_F.exe...";
        if(enabled){if(!EnsureInstalled())return "HORSE NOT ARMED | "+error;lastEnabled=true;return "HORSE: Wand six-slot Stable port ARMED (local player)";}
        if(lastEnabled&&installed){WriteCode(moduleBase+RVA_HORSE_SLOT_LOOP,Original);installed=false;if(cave!=IntPtr.Zero){VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;}}
        lastEnabled=false;return "HORSE: OFF";
    }

    public static void Stop()=>Detach(true);
    static void Detach(bool restore){if(h!=IntPtr.Zero){if(restore&&installed)WriteCode(moduleBase+RVA_HORSE_SLOT_LOOP,Original);if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);CloseHandle(h);}h=IntPtr.Zero;cave=IntPtr.Zero;p=null;moduleBase=0;installed=false;lastEnabled=false;error="";}
}
''', encoding='utf-8')
print('HorseCore.cs generated')
