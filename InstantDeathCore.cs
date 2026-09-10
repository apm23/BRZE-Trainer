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

    // BRZE 1.60 InterfaceMouse helper: call at 0x535F27 -> 0x5D4888.
    // V4 keeps the same single call-site probe but exposes live telemetry so runtime can
    // tell us exactly whether the call fires, what Unit* it returns, and whether our write fires.
    const int RVA_HOVER_QUERY_CALL=0x135F27;
    const int RVA_HOVER_QUERY=0x1D4888;
    const int RVA_LOCAL_ID=0x4416D0;
    const int OFF_DEF=0x74,OFF_OWNER=0x240,OFF_HP=0x404,OFF_STAMINA=0x408;
    const uint DEATH_SENTINEL=0xFF000000u;
    static readonly byte[] OriginalCall={0xE8,0x5C,0xE9,0x09,0x00};

    // Cave layout: wrapper at +0, telemetry at +0x300.
    const int TELE_OFF=0x300;
    const int T_CALLS=0x00,T_UNIT=0x04,T_OWNER=0x08,T_WRITES=0x0C;

    static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero;
    static Process? p;
    static long moduleBase,telemetry;
    static bool installed,lastEnabled;
    static string error="";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
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

    static byte[] BuildWrapper(long stub,long tele)
    {
        var b=new List<byte>();
        b.Add(0x55);                              // push ebp
        b.AddRange(new byte[]{0x8B,0xEC});        // mov ebp,esp
        b.AddRange(new byte[]{0x83,0xEC,0x04});   // local saved Unit*
        b.AddRange(new byte[]{0xFF,0x75,0x10});   // arg3
        b.AddRange(new byte[]{0xFF,0x75,0x0C});   // arg2
        b.AddRange(new byte[]{0xFF,0x75,0x08});   // arg1
        b.Add(0xE8);int callQuery=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x89,0x45,0xFC});   // save eax Unit*

        // Telemetry is intentionally simple/atomic enough for diagnostic reads.
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)(tele+T_CALLS)); // inc calls
        b.Add(0xA3);U32(b,(uint)(tele+T_UNIT));                       // last Unit*=eax
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)(tele+T_OWNER));U32(b,0xFFFFFFFFu);

        b.AddRange(new byte[]{0x85,0xC0});
        b.AddRange(new byte[]{0x0F,0x84});int jFinish0=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB8,0x74,0x00,0x00,0x00,0x00});
        b.AddRange(new byte[]{0x0F,0x84});int jFinishDef=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x88,0x40,0x02,0x00,0x00});        // ecx=owner
        b.AddRange(new byte[]{0x89,0x0D});U32(b,(uint)(tele+T_OWNER)); // expose owner
        b.AddRange(new byte[]{0x83,0xF9,0x0A});
        b.AddRange(new byte[]{0x0F,0x87});int jFinishOwner=b.Count;I32(b,0);

        // V3's native relation helper was not runtime-proven. V4 removes that uncertain
        // filter and uses only the hard local-owner guard. This deliberately tests one
        // hypothesis: if native query returns a non-local unit, the write must fire.
        b.AddRange(new byte[]{0x3B,0x0D});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // cmp ecx,[localId]
        b.AddRange(new byte[]{0x0F,0x84});int jFinishLocal=b.Count;I32(b,0);

        b.AddRange(new byte[]{0x8B,0x45,0xFC});
        b.AddRange(new byte[]{0xC7,0x80,0x04,0x04,0x00,0x00});U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xC7,0x80,0x08,0x04,0x00,0x00});U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)(tele+T_WRITES));

        int finish=b.Count;
        b.AddRange(new byte[]{0x8B,0x45,0xFC});
        b.AddRange(new byte[]{0x8B,0xE5,0x5D});
        b.AddRange(new byte[]{0xC2,0x0C,0x00});

        Rel(b,callQuery,stub+callQuery+4,moduleBase+RVA_HOVER_QUERY);
        Rel(b,jFinish0,stub+jFinish0+4,stub+finish);
        Rel(b,jFinishDef,stub+jFinishDef+4,stub+finish);
        Rel(b,jFinishOwner,stub+jFinishOwner+4,stub+finish);
        Rel(b,jFinishLocal,stub+jFinishLocal+4,stub+finish);
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
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)1024,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
        if(cave==IntPtr.Zero){error="death cave allocation failed";return false;}
        long stub=cave.ToInt64();telemetry=stub+TELE_OFF;
        var init=new byte[16];Array.Copy(BitConverter.GetBytes(0xFFFFFFFFu),0,init,T_OWNER,4);
        if(!WriteRaw(telemetry,init)){error="death telemetry init failed";return false;}
        var body=BuildWrapper(stub,telemetry);
        if(body.Length>=TELE_OFF){error="death wrapper exceeded telemetry boundary";return false;}
        if(!WriteRaw(stub,body)){error="death cave write failed";return false;}
        var patch=new byte[5];patch[0]=0xE8;
        Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,patch,1,4);
        if(!WriteCode(target,patch)){error="death call-wrapper install failed";return false;}
        installed=true;error="";return true;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "DEATH V4: waiting for Battle_Realms_F.exe...";
        if(enabled)
        {
            if(!EnsureInstalled())return "DEATH V4 NOT ARMED | "+error;
            lastEnabled=true;
            uint calls=R32(telemetry+T_CALLS),unit=R32(telemetry+T_UNIT),owner=R32(telemetry+T_OWNER),writes=R32(telemetry+T_WRITES);
            uint hpNow=unit!=0?R32((long)unit+OFF_HP):0;
            string own=owner==0xFFFFFFFFu?"-":owner.ToString();
            return $"DEATH V4: ARMED | qcalls:{calls} last:0x{unit:X8} owner:{own} writes:{writes} hpNow:0x{hpNow:X8}";
        }
        if(lastEnabled&&installed)
        {
            WriteCode(moduleBase+RVA_HOVER_QUERY_CALL,OriginalCall);
            installed=false;
            if(cave!=IntPtr.Zero){VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;}
            telemetry=0;
        }
        lastEnabled=false;
        return "DEATH V4: OFF";
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
        h=IntPtr.Zero;cave=IntPtr.Zero;p=null;moduleBase=0;telemetry=0;installed=false;lastEnabled=false;error="";
    }
}
