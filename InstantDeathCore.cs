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
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x10|0x20|0x8|0x400;
    const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_EXECUTE_READWRITE=0x40;

    // Runtime-proven V6 target architecture. V7 keeps the exact same native cursor query,
    // owner/alliance filtering and sentinel write, but adds a remote mode gate:
    // 0 = idle, 1 = BURST/eraser, 2 = SINGLE one-render-frame trigger.
    const int RVA_FRAME_MOUSE_DRAW=0x135C43;
    const int RVA_INTERFACE_MOUSE=0x443640;
    const int RVA_CURSOR_UNIT_QUERY=0x135EFB;
    const int RVA_RELATION=0x1848E8;
    const int RVA_LOCAL_ID=0x4416D0;
    const int OFF_DEF=0x74,OFF_OWNER=0x240,OFF_HP=0x404,OFF_STAMINA=0x408;
    const uint DEATH_SENTINEL=0xFF000000u;
    const uint MODE_IDLE=0,MODE_BURST=1,MODE_SINGLE=2;

    static readonly byte[] Original={0x55,0x8B,0xEC,0x83,0xEC,0x5C};
    static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero;
    static Process? p;
    static long moduleBase,lastUnitAddr,lastOwnerAddr,writesAddr,modeAddr;
    static bool installed;
    static string error="";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
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
    static void PatchRel(List<byte>b,int at,long fromNext,long to)
    {
        byte[] x=BitConverter.GetBytes(unchecked((int)(to-fromNext)));
        for(int i=0;i<4;i++)b[at+i]=x[i];
    }

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
        var b=new List<byte>{0x9C,0x60}; // pushfd, pushad

        // Remote gate. EBX is safe because pushad restores it.
        b.AddRange(new byte[]{0x8B,0x1D});U32(b,(uint)modeAddr); // mov ebx,[mode]
        b.AddRange(new byte[]{0x85,0xDB,0x0F,0x84});int jIdle=b.Count;I32(b,0);

        // Telemetry is refreshed only when a trigger is active.
        b.Add(0xC7);b.Add(0x05);U32(b,(uint)lastUnitAddr);U32(b,0);
        b.Add(0xC7);b.Add(0x05);U32(b,(uint)lastOwnerAddr);U32(b,0xFFFFFFFFu);

        // Same runtime-proven V6 live cursor query.
        b.Add(0xB9);U32(b,(uint)(moduleBase+RVA_INTERFACE_MOUSE));
        b.AddRange(new byte[]{0x6A,0x00,0x6A,0x01,0xE8});
        int callQuery=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x84});int jNull=b.Count;I32(b,0);
        b.Add(0xA3);U32(b,(uint)lastUnitAddr);
        b.AddRange(new byte[]{0x8B,0xF8});
        b.AddRange(new byte[]{0x83,0xBF});U32(b,OFF_DEF);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jNoDef=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x87});U32(b,OFF_OWNER);
        b.Add(0xA3);U32(b,(uint)lastOwnerAddr);
        b.AddRange(new byte[]{0x83,0xF8,0x09,0x0F,0x87});int jBadOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x0D});U32(b,(uint)(moduleBase+RVA_LOCAL_ID));
        b.AddRange(new byte[]{0x83,0xF9,0x09,0x0F,0x87});int jBadLocal=b.Count;I32(b,0);

        b.Add(0x50); // target owner
        b.Add(0x51); // local id
        b.Add(0xE8);int callRel=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x85});int jAllied=b.Count;I32(b,0);

        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_HP);U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_STAMINA);U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)writesAddr);

        // Every active path lands here. SINGLE consumes itself after exactly one render-frame
        // query even if the cursor was on ground/friendly; BURST remains mode 1.
        int finishActive=b.Count;
        b.AddRange(new byte[]{0x83,0xFB,0x02,0x0F,0x85});int jKeepMode=b.Count;I32(b,0);
        b.Add(0xC7);b.Add(0x05);U32(b,(uint)modeAddr);U32(b,MODE_IDLE);

        int done=b.Count;
        b.AddRange(new byte[]{0x61,0x9D});
        b.AddRange(Original);
        b.Add(0xE9);int jBack=b.Count;I32(b,0);

        PatchRel(b,callQuery,stub+callQuery+4,moduleBase+RVA_CURSOR_UNIT_QUERY);
        PatchRel(b,callRel,stub+callRel+4,moduleBase+RVA_RELATION);
        PatchRel(b,jIdle,stub+jIdle+4,stub+done);
        PatchRel(b,jNull,stub+jNull+4,stub+finishActive);
        PatchRel(b,jNoDef,stub+jNoDef+4,stub+finishActive);
        PatchRel(b,jBadOwner,stub+jBadOwner+4,stub+finishActive);
        PatchRel(b,jBadLocal,stub+jBadLocal+4,stub+finishActive);
        PatchRel(b,jAllied,stub+jAllied+4,stub+finishActive);
        PatchRel(b,jKeepMode,stub+jKeepMode+4,stub+done);
        PatchRel(b,jBack,stub+jBack+4,moduleBase+RVA_FRAME_MOUSE_DRAW+Original.Length);
        return b.ToArray();
    }

    static bool Install()
    {
        if(installed)return true;
        error="";
        long site=moduleBase+RVA_FRAME_MOUSE_DRAW;
        var now=new byte[Original.Length];
        if(!ReadExact(site,now)||!Same(now,Original)){error="frame hook bytes mismatch";return false;}

        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)0x1000,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
        if(cave==IntPtr.Zero){error="VirtualAllocEx failed";return false;}
        long stub=cave.ToInt64();
        lastUnitAddr=stub+0x300;lastOwnerAddr=stub+0x304;writesAddr=stub+0x308;modeAddr=stub+0x30C;
        if(!WriteBytes(lastUnitAddr,new byte[16])){error="state init failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        W32(lastOwnerAddr,0xFFFFFFFFu);
        byte[] code=BuildCave(stub);
        if(!WriteBytes(stub,code)){error="cave write failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}

        var patch=new List<byte>{0xE9};I32(patch,unchecked((int)(stub-(site+5))));patch.Add(0x90);
        if(!WriteCode(site,patch.ToArray())){error="frame hook patch failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        installed=true;
        return true;
    }

    static void Uninstall()
    {
        if(h==IntPtr.Zero){installed=false;cave=IntPtr.Zero;return;}
        if(installed)WriteCode(moduleBase+RVA_FRAME_MOUSE_DRAW,Original);
        installed=false;
        if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);
        cave=IntPtr.Zero;lastUnitAddr=lastOwnerAddr=writesAddr=modeAddr=0;
    }

    public static bool TriggerSingle()
    {
        if(!Attach()||!Install())return false;
        return W32(modeAddr,MODE_SINGLE);
    }

    public static string Tick(bool burstEnabled)
    {
        if(!Attach())return "DEATH V7: waiting for Battle_Realms_F.exe...";

        if(burstEnabled)
        {
            if(!Install())return "DEATH V7: ERROR — "+error;
            W32(modeAddr,MODE_BURST);
        }
        else if(installed)
        {
            // Do not cancel a freshly requested SINGLE pulse from this same UI tick.
            if(R32(modeAddr)==MODE_BURST)W32(modeAddr,MODE_IDLE);
        }

        if(!installed)return "DEATH V7: IDLE | SINGLE=PageDown/button | BURST=OFF";
        uint mode=R32(modeAddr),u=R32(lastUnitAddr),owner=R32(lastOwnerAddr),writes=R32(writesAddr);
        string own=owner==0xFFFFFFFFu?"-":owner.ToString();
        string m=mode==MODE_BURST?"BURST":mode==MODE_SINGLE?"SINGLE-PENDING":"IDLE";
        return $"DEATH V7: {m} | hover:0x{u:X8} owner:{own} writes:{writes} | enemy-only / no select";
    }

    public static void Stop()=>Detach();
    static void Detach()
    {
        try{Uninstall();}catch{}
        if(h!=IntPtr.Zero)CloseHandle(h);
        h=IntPtr.Zero;p=null;moduleBase=0;installed=false;cave=IntPtr.Zero;lastUnitAddr=lastOwnerAddr=writesAddr=modeAddr=0;error="";
    }
}
