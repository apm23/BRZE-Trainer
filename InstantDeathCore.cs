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

    // V6: V5 runtime proved RVA 0x3DD858 is a simulation/scratch Unit* that sweeps enemies.
    // It is deliberately gone.  Instead, execute BRZE's own live InterfaceMouse unit query
    // once per render frame, on the game thread.  That query reads InterfaceMouse +4/+8
    // (the live cursor coordinates), so no selection and no whole-unit-pool scan are involved.
    const int RVA_FRAME_MOUSE_DRAW=0x135C43;       // per-frame function entry, original 55 8B EC 83 EC 5C
    const int RVA_INTERFACE_MOUSE=0x443640;        // static InterfaceMouse object (preferred VA 0x843640)
    const int RVA_CURSOR_UNIT_QUERY=0x135EFB;      // InterfaceMouse helper -> native Unit* query
    const int RVA_RELATION=0x1848E8;               // player relation/alliance helper
    const int RVA_LOCAL_ID=0x4416D0;
    const int OFF_DEF=0x74,OFF_OWNER=0x240,OFF_HP=0x404,OFF_STAMINA=0x408;
    const uint DEATH_SENTINEL=0xFF000000u;

    static readonly byte[] Original={0x55,0x8B,0xEC,0x83,0xEC,0x5C};
    static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero;
    static Process? p;
    static long moduleBase,lastUnitAddr,lastOwnerAddr,writesAddr;
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
        // pushfd/pushad so the render caller sees exactly the original register/flags state.
        var b=new List<byte>{0x9C,0x60};

        // Clear telemetry for this frame.
        b.Add(0xC7);b.Add(0x05);U32(b,(uint)lastUnitAddr);U32(b,0);
        b.Add(0xC7);b.Add(0x05);U32(b,(uint)lastOwnerAddr);U32(b,0xFFFFFFFFu);

        // Unit* u = InterfaceMouse::cursorUnitQuery(1,0), using the live +4/+8 cursor coords.
        b.Add(0xB9);U32(b,(uint)(moduleBase+RVA_INTERFACE_MOUSE)); // mov ecx, InterfaceMouse
        b.AddRange(new byte[]{0x6A,0x00,0x6A,0x01,0xE8});
        int callQuery=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x84});int jNull=b.Count;I32(b,0);
        b.Add(0xA3);U32(b,(uint)lastUnitAddr);                    // lastUnit = eax
        b.AddRange(new byte[]{0x8B,0xF8});                       // edi = eax
        b.AddRange(new byte[]{0x83,0xBF});U32(b,OFF_DEF);b.Add(0); // cmp [edi+74],0
        b.AddRange(new byte[]{0x0F,0x84});int jNoDef=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x87});U32(b,OFF_OWNER);      // eax = owner
        b.Add(0xA3);U32(b,(uint)lastOwnerAddr);
        b.AddRange(new byte[]{0x83,0xF8,0x09,0x0F,0x87});int jBadOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x0D});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // ecx = localId
        b.AddRange(new byte[]{0x83,0xF9,0x09,0x0F,0x87});int jBadLocal=b.Count;I32(b,0);

        // Relation(localId,targetOwner): nonzero = same/allied -> never kill.
        b.Add(0x50); // push target owner
        b.Add(0x51); // push local id
        b.Add(0xE8);int callRel=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x85});int jAllied=b.Count;I32(b,0);

        // Exact old-trainer death sentinel, current BRZE HP/stamina offsets.
        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_HP);U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_STAMINA);U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)writesAddr); // inc writes

        int done=b.Count;
        b.AddRange(new byte[]{0x61,0x9D}); // popad, popfd
        b.AddRange(Original);
        b.Add(0xE9);int jBack=b.Count;I32(b,0);

        PatchRel(b,callQuery,stub+callQuery+4,moduleBase+RVA_CURSOR_UNIT_QUERY);
        PatchRel(b,callRel,stub+callRel+4,moduleBase+RVA_RELATION);
        PatchRel(b,jNull,stub+jNull+4,stub+done);
        PatchRel(b,jNoDef,stub+jNoDef+4,stub+done);
        PatchRel(b,jBadOwner,stub+jBadOwner+4,stub+done);
        PatchRel(b,jBadLocal,stub+jBadLocal+4,stub+done);
        PatchRel(b,jAllied,stub+jAllied+4,stub+done);
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
        lastUnitAddr=stub+0x300;lastOwnerAddr=stub+0x304;writesAddr=stub+0x308;
        if(!WriteBytes(lastUnitAddr,new byte[12])){error="telemetry init failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
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
        cave=IntPtr.Zero;lastUnitAddr=lastOwnerAddr=writesAddr=0;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "DEATH V6: waiting for Battle_Realms_F.exe...";
        if(!enabled)
        {
            if(installed)Uninstall();
            return "DEATH V6: OFF — hover-only cursor query";
        }
        if(!Install())return "DEATH V6: ERROR — "+error;
        uint u=R32(lastUnitAddr),owner=R32(lastOwnerAddr),writes=R32(writesAddr);
        string own=owner==0xFFFFFFFFu?"-":owner.ToString();
        return $"DEATH V6: ON | hover:0x{u:X8} owner:{own} kills:{writes} | NO SELECT / NO SWEEP";
    }

    public static void Stop()=>Detach();
    static void Detach()
    {
        try{Uninstall();}catch{}
        if(h!=IntPtr.Zero)CloseHandle(h);
        h=IntPtr.Zero;p=null;moduleBase=0;installed=false;cave=IntPtr.Zero;error="";
    }
}
