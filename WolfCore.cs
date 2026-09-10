using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class WolfCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint allocationType,uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr addr,UIntPtr size,uint newProtect,out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x10|0x20|0x8|0x400;
    const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_EXECUTE_READWRITE=0x40;

    // Current BRZE UnitGiveWolfToUnit:
    // 4C56FE mov eax,[esi+63C]        current owned-wolf count
    // 4C5704 cmp eax,[ecx+1B8]        UnitDef.NumOwnedWolves
    // 4C570A jge 4C574F               reject if native cap reached
    // ESI = master unit; [ESI+240] = owner player id.
    // Patch only the 6-byte CMP and preserve the original following JGE.
    const int RVA_WOLF_CAP_CMP=0x0C5704;
    const int RVA_LOCAL_ID=0x4416D0;
    const int OFF_OWNER=0x240;
    const int OFF_CURRENT_WOLVES=0x63C;
    const int OFF_DEF_MAX_WOLVES=0x1B8;

    static readonly byte[] Original={0x3B,0x81,0xB8,0x01,0x00,0x00};
    static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero;
    static Process? p;
    static long moduleBase,ownerAddr,currentAddr,maxAddr,bypassAddr;
    static bool installed;
    static string error="";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool WriteCode(long a,byte[] b)
    {
        if(h==IntPtr.Zero)return false;
        if(!VirtualProtectEx(h,A(a),(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;
        bool ok=WriteBytes(a,b);
        FlushInstructionCache(h,A(a),(UIntPtr)b.Length);
        VirtualProtectEx(h,A(a),(UIntPtr)b.Length,old,out _);
        return ok;
    }
    static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    static void I32(List<byte>b,int v)=>b.AddRange(BitConverter.GetBytes(v));
    static void U32(List<byte>b,uint v)=>b.AddRange(BitConverter.GetBytes(v));

    static bool Attach()
    {
        try{if(p!=null&&!p.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach();
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0)return false;
        p=ps[0];
        try{moduleBase=p.MainModule!.BaseAddress.ToInt64();}catch{p=null;return false;}
        h=OpenProcess(ACCESS,false,p.Id);
        return h!=IntPtr.Zero;
    }

    static byte[] BuildCave(long stub)
    {
        var b=new List<byte>();

        // Save EDX only. EAX/ECX/ESI must remain exactly as the native compare expects.
        b.Add(0x52); // push edx
        b.AddRange(new byte[]{0x8B,0x96});U32(b,OFF_OWNER);       // mov edx,[esi+240]
        b.AddRange(new byte[]{0x89,0x15});U32(b,(uint)ownerAddr); // telemetry owner
        b.Add(0xA3);U32(b,(uint)currentAddr);                    // telemetry current from eax
        b.AddRange(new byte[]{0x3B,0x15});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // cmp edx,[localId]
        b.AddRange(new byte[]{0x0F,0x85});int jNonLocal=b.Count;I32(b,0);

        // Local player: record native max, then manufacture flags for "well below cap".
        b.AddRange(new byte[]{0x8B,0x91});U32(b,OFF_DEF_MAX_WOLVES); // mov edx,[ecx+1B8]
        b.AddRange(new byte[]{0x89,0x15});U32(b,(uint)maxAddr);
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)bypassAddr);
        b.Add(0x5A);                                             // restore edx before final flags
        b.Add(0x3D);U32(b,0x7FFFFFFFu);                          // cmp eax,7fffffff -> native JGE will not reject
        b.Add(0xE9);int jBackLocal=b.Count;I32(b,0);

        // Non-local/AI: execute the exact original compare and keep native cap behavior.
        int nonLocal=b.Count;
        b.AddRange(new byte[]{0x8B,0x91});U32(b,OFF_DEF_MAX_WOLVES);
        b.AddRange(new byte[]{0x89,0x15});U32(b,(uint)maxAddr);
        b.Add(0x5A);
        b.AddRange(Original);
        b.Add(0xE9);int jBackNative=b.Count;I32(b,0);

        long back=moduleBase+RVA_WOLF_CAP_CMP+Original.Length; // original JGE at 4C570A
        void Patch(int at,long to)
        {
            byte[] x=BitConverter.GetBytes(unchecked((int)(to-(stub+at+4))));
            for(int i=0;i<4;i++)b[at+i]=x[i];
        }
        Patch(jNonLocal,stub+nonLocal);
        Patch(jBackLocal,back);
        Patch(jBackNative,back);
        return b.ToArray();
    }

    static bool Install()
    {
        if(installed)return true;
        error="";
        long site=moduleBase+RVA_WOLF_CAP_CMP;
        var now=new byte[Original.Length];
        if(!ReadExact(site,now)||!Same(now,Original)){error="wolf cap bytes mismatch";return false;}

        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)0x1000,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
        if(cave==IntPtr.Zero){error="VirtualAllocEx failed";return false;}
        long stub=cave.ToInt64();
        ownerAddr=stub+0x200;currentAddr=stub+0x204;maxAddr=stub+0x208;bypassAddr=stub+0x20C;
        if(!WriteBytes(ownerAddr,new byte[16])){error="wolf telemetry init failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        byte[] code=BuildCave(stub);
        if(!WriteBytes(stub,code)){error="wolf cave write failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}

        var patch=new List<byte>{0xE9};I32(patch,unchecked((int)(stub-(site+5))));patch.Add(0x90);
        if(!WriteCode(site,patch.ToArray())){error="wolf patch failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        installed=true;
        return true;
    }

    static void Uninstall()
    {
        if(h==IntPtr.Zero){installed=false;cave=IntPtr.Zero;return;}
        if(installed)WriteCode(moduleBase+RVA_WOLF_CAP_CMP,Original);
        installed=false;
        if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);
        cave=IntPtr.Zero;ownerAddr=currentAddr=maxAddr=bypassAddr=0;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "WOLVES: waiting for Battle_Realms_F.exe...";
        if(!enabled)
        {
            if(installed)Uninstall();
            return "WOLVES: OFF — native NumOwnedWolves cap";
        }
        if(!Install())return "WOLVES: ERROR — "+error;
        uint owner=R32(ownerAddr),cur=R32(currentAddr),max=R32(maxAddr),hits=R32(bypassAddr);
        return $"WOLVES: ON local-only | last owner:{owner} current:{cur} nativeMax:{max} bypassHits:{hits}";
    }

    public static void Stop()=>Detach();
    static void Detach()
    {
        try{Uninstall();}catch{}
        if(h!=IntPtr.Zero)CloseHandle(h);
        h=IntPtr.Zero;p=null;moduleBase=0;installed=false;cave=IntPtr.Zero;ownerAddr=currentAddr=maxAddr=bypassAddr=0;error="";
    }
}
