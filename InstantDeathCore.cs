using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class InstantDeathCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x10|0x20|0x8|0x400;

    // V5 hypothesis, evidence-driven from exact legacy PageDown autopsy + current BRZE xrefs.
    // Current absolute 0x7DD858 (RVA 0x3DD858) is assigned a Unit* at 0x5E4451 and native
    // code compares it directly with unit-pool slots and reads fields including owner +0x240.
    // Earlier runtime observer snapshots also saw this pointer move between two non-local units.
    // We test ONLY this one pointer; no unit-pool scan and no code detour.
    const int RVA_CURRENT_UNIT_PTR=0x3DD858;
    const int RVA_UNIT_POOL_PTR=0x4796A0;
    const int RVA_LOCAL_ID=0x4416D0;
    const int UNIT_STRIDE=0x818,UNIT_COUNT=2000;
    const int OFF_DEF=0x74,OFF_OWNER=0x240,OFF_HP=0x404,OFF_STAMINA=0x408;
    const uint DEATH_SENTINEL=0xFF000000u;

    static IntPtr h=IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static ulong ticks,writes;
    static uint lastUnit,lastOwner=0xFFFFFFFFu,lastBefore,lastAfter,lastIndex=0xFFFFFFFFu;
    static bool lastValid,lastEnemy;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool W32(long a,uint v)
    {
        var b=BitConverter.GetBytes(v);
        return h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,4,out var n)&&n.ToInt64()==4;
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

    static bool ValidateUnit(uint unit,uint pool,out uint index)
    {
        index=0xFFFFFFFFu;
        if(unit==0||pool==0||unit<pool)return false;
        ulong delta=(ulong)unit-pool;
        if(delta%(ulong)UNIT_STRIDE!=0)return false;
        ulong ix=delta/(ulong)UNIT_STRIDE;
        if(ix>=UNIT_COUNT)return false;
        index=(uint)ix;
        return R32((long)unit+OFF_DEF)!=0;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "DEATH V5: waiting for Battle_Realms_F.exe...";
        if(!enabled)
        {
            lastUnit=0;lastOwner=0xFFFFFFFFu;lastBefore=lastAfter=0;lastIndex=0xFFFFFFFFu;lastValid=false;lastEnemy=false;
            return "DEATH V5: OFF";
        }

        ticks++;
        uint unit=R32(moduleBase+RVA_CURRENT_UNIT_PTR);
        uint pool=R32(moduleBase+RVA_UNIT_POOL_PTR);
        uint lid=R32(moduleBase+RVA_LOCAL_ID);
        lastUnit=unit;
        lastValid=ValidateUnit(unit,pool,out uint index);
        lastIndex=index;
        lastOwner=0xFFFFFFFFu;lastEnemy=false;lastBefore=lastAfter=0;

        if(lastValid)
        {
            uint owner=R32((long)unit+OFF_OWNER);
            lastOwner=owner;
            // Valid player owners are 0..9 in the native 10-player arrays. Never touch local.
            lastEnemy=owner<10u&&owner!=lid;
            lastBefore=R32((long)unit+OFF_HP);
            if(lastEnemy)
            {
                bool a=W32((long)unit+OFF_HP,DEATH_SENTINEL);
                bool b=W32((long)unit+OFF_STAMINA,DEATH_SENTINEL);
                if(a&&b)writes++;
            }
            lastAfter=R32((long)unit+OFF_HP);
        }

        string own=lastOwner==0xFFFFFFFFu?"-":lastOwner.ToString();
        string ix=lastIndex==0xFFFFFFFFu?"-":lastIndex.ToString();
        return $"DEATH V5: ON | ptr:0x{lastUnit:X8} valid:{lastValid} slot:{ix} owner:{own} enemy:{lastEnemy} writes:{writes} hp:{lastBefore:X8}->{lastAfter:X8}";
    }

    public static void Stop()=>Detach();
    static void Detach()
    {
        if(h!=IntPtr.Zero)CloseHandle(h);
        h=IntPtr.Zero;p=null;moduleBase=0;ticks=0;writes=0;lastUnit=0;lastOwner=0xFFFFFFFFu;lastBefore=lastAfter=0;lastIndex=0xFFFFFFFFu;lastValid=false;lastEnemy=false;
    }
}
