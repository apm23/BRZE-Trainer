using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class InstantDeathCore
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

    // InterfaceMouse::unit-under-cursor path, BRZE 1.60 target binary.
    // 0x535EFB converts current cursor coordinates then calls 0x5D4888 at 0x535F27.
    // We wrap only that call: native hit testing still decides the hovered Unit*.
    const int RVA_HOVER_QUERY_CALL=0x135F27;
    const int RVA_HOVER_QUERY=0x1D4888;
    const int RVA_PLAYER_RELATION=0x1848E8;
    const int RVA_LOCAL_ID=0x4416D0;
    const int OFF_DEF=0x74,OFF_OWNER=0x240,OFF_HP=0x404,OFF_STAMINA=0x408;
    const uint DEATH_SENTINEL=0xFF000000u;
    static readonly byte[] OriginalCall={0xE8,0x5C,0xE9,0x09,0x00}; // call 0x5D4888 from preferred VA 0x535F27

    static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static bool installed,lastEnabled;
    static string error="";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool WriteRaw(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool WriteCode(long a,byte[] b)
    {
        if(h==IntPtr.Zero)return false;
        var x=A(a);
        if(!VirtualProtectEx(h,x,(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;
        bool ok=WriteRaw(a,b);
        if(ok)FlushInstructionCache(h,x,(UIntPtr)b.Length);
        VirtualProtectEx(h,x,(UIntPtr)b.Length,old,out _);
        return ok;
    }
    static void U32(List<byte>b,uint v)=>b.AddRange(BitConverter.GetBytes(v));
    static void I32(List<byte>b,int v)=>b.AddRange(BitConverter.GetBytes(v));
    static void Rel(List<byte>b,int at,long fromNext,long target)
    {
        var q=BitConverter.GetBytes(unchecked((int)(target-fromNext)));
        for(int i=0;i<4;i++)b[at+i]=q[i];
    }

    static bool Attach()
    {
        try{if(p!=null&&!p.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach(false);
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0)return false;
        p=ps[0];
        try{moduleBase=p.MainModule!.BaseAddress.ToInt64();}catch{p=null;return false;}
        h=OpenProcess(ACCESS,false,p.Id);
        return h!=IntPtr.Zero;
    }

    static byte[] BuildWrapper(long stub)
    {
        // Wrapper is called in place of the native stdcall 0x5D4888 call.
        // Forward the original three stack arguments exactly, let BRZE resolve the hovered
        // unit, then kill only a valid non-allied player unit. Preserve the Unit* return.
        var b=new List<byte>();
        b.Add(0x55);                              // push ebp
        b.AddRange(new byte[]{0x8B,0xEC});        // mov ebp,esp
        b.AddRange(new byte[]{0x83,0xEC,0x04});   // sub esp,4 (saved Unit*)
        b.AddRange(new byte[]{0xFF,0x75,0x10});   // push [ebp+10] arg3
        b.AddRange(new byte[]{0xFF,0x75,0x0C});   // push [ebp+0C] arg2
        b.AddRange(new byte[]{0xFF,0x75,0x08});   // push [ebp+08] arg1
        b.Add(0xE8);int callQuery=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x89,0x45,0xFC});   // mov [ebp-4],eax
        b.AddRange(new byte[]{0x85,0xC0});        // test eax,eax
        b.AddRange(new byte[]{0x0F,0x84});int jFinish0=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB8,0x74,0x00,0x00,0x00,0x00}); // cmp [eax+74],0
        b.AddRange(new byte[]{0x0F,0x84});int jFinishDef=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x88,0x40,0x02,0x00,0x00}); // ecx=[eax+240] owner
        b.AddRange(new byte[]{0x83,0xF9,0x0A});   // cmp ecx,10
        b.AddRange(new byte[]{0x0F,0x87});int jFinishOwner=b.Count;I32(b,0);
        b.Add(0x51);                              // push target owner (arg2)
        b.AddRange(new byte[]{0xFF,0x35});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // push localId (arg1)
        b.Add(0xE8);int callRelation=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x85,0xC0});        // relation!=0 => same/allied
        b.AddRange(new byte[]{0x0F,0x85});int jFinishFriendly=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x45,0xFC});   // eax=saved Unit*
        b.AddRange(new byte[]{0xC7,0x80,0x04,0x04,0x00,0x00});U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xC7,0x80,0x08,0x04,0x00,0x00});U32(b,DEATH_SENTINEL);
        int finish=b.Count;
        b.AddRange(new byte[]{0x8B,0x45,0xFC});   // preserve native Unit* return
        b.AddRange(new byte[]{0x8B,0xE5,0x5D});   // mov esp,ebp / pop ebp
        b.AddRange(new byte[]{0xC2,0x0C,0x00});   // ret 0x0C, same as native query

        Rel(b,callQuery,stub+callQuery+4,moduleBase+RVA_HOVER_QUERY);
        Rel(b,callRelation,stub+callRelation+4,moduleBase+RVA_PLAYER_RELATION);
        Rel(b,jFinish0,stub+jFinish0+4,stub+finish);
        Rel(b,jFinishDef,stub+jFinishDef+4,stub+finish);
        Rel(b,jFinishOwner,stub+jFinishOwner+4,stub+finish);
        Rel(b,jFinishFriendly,stub+jFinishFriendly+4,stub+finish);
        return b.ToArray();
    }

    static bool EnsureInstalled()
    {
        if(installed)return true;
        if(!Attach())return false;
        long target=moduleBase+RVA_HOVER_QUERY_CALL;
        var now=new byte[OriginalCall.Length];
        if(!ReadExact(target,now)||!System.Linq.Enumerable.SequenceEqual(now,OriginalCall))
        {
            error="hover query call mismatch (restart BRZE / close other trainers)";
            return false;
        }
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)256,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
        if(cave==IntPtr.Zero){error="death cave allocation failed";return false;}
        long stub=cave.ToInt64();
        var body=BuildWrapper(stub);
        if(!WriteRaw(stub,body)){error="death cave write failed";return false;}
        var patch=new byte[5];patch[0]=0xE8;
        Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,patch,1,4);
        if(!WriteCode(target,patch)){error="death call-wrapper install failed";return false;}
        installed=true;error="";return true;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "DEATH: waiting for Battle_Realms_F.exe...";
        if(enabled)
        {
            if(!EnsureInstalled())return "DEATH NOT ARMED | "+error;
            lastEnabled=true;
            return "DEATH: native hover query ARMED | enemy-only | sentinel FF000000";
        }
        if(lastEnabled&&installed)
        {
            WriteCode(moduleBase+RVA_HOVER_QUERY_CALL,OriginalCall);
            installed=false;
            if(cave!=IntPtr.Zero){VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;}
        }
        lastEnabled=false;
        return "DEATH: OFF";
    }

    public static void Stop()=>Detach(true);
    static void Detach(bool restore)
    {
        if(h!=IntPtr.Zero)
        {
            if(restore&&installed)WriteCode(moduleBase+RVA_HOVER_QUERY_CALL,OriginalCall);
            if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);
            CloseHandle(h);
        }
        h=IntPtr.Zero;cave=IntPtr.Zero;p=null;moduleBase=0;installed=false;lastEnabled=false;error="";
    }
}
