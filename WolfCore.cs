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
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x10|0x20|0x8|0x400;

    // V8: runtime rejected the V7 UnitGiveWolfToUnit per-unit companion-cap bypass.
    // Re-forensic of the exact old F10 plus current BRZE code proves +0x250 is Wolves Den stock:
    // current 4D4F7B checks building type 0x44 (Wolves Den), then [building+250] against native cap 12;
    // current 4D511C decrements [building+250] when a wolf is released/used.
    // Old trainer F10 wrote ONE BYTE 250 to [current object+0x250]. Preserve that exact value/width.
    // To avoid touching unrelated buildings, only local-player selected buildings whose type is exactly 0x44
    // are captured. Once captured they stay latched while the checkbox is ON, so changing selection is safe.
    const int RVA_LOCAL_ID=0x4416D0;
    const int RVA_SELECTED_BUILDING_A=0x4417D4;
    const int RVA_SELECTED_BUILDING_B=0x4417D8;
    const int OFF_BUILD_TYPE=0x78;
    const int OFF_BUILD_OWNER=0x84;
    const int OFF_WOLF_STOCK=0x250;
    const uint WOLVES_DEN_TYPE=0x44;
    const byte MAX_WOLF_STOCK=250;

    static IntPtr h=IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static readonly HashSet<long> dens=new();
    static bool wasEnabled;
    static uint writes;
    static long lastDen;
    static uint lastBefore,lastAfter;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool W8(long a,byte v)=>WriteBytes(a,new[]{v});

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

    static bool IsLocalWolvesDen(long obj,uint localId)
    {
        if(obj==0||localId>=10)return false;
        return R32(obj+OFF_BUILD_OWNER)==localId && R32(obj+OFF_BUILD_TYPE)==WOLVES_DEN_TYPE;
    }

    static void CaptureSelected(uint localId)
    {
        uint a=R32(moduleBase+RVA_SELECTED_BUILDING_A);
        uint b=R32(moduleBase+RVA_SELECTED_BUILDING_B);
        long la=a,lb=b;
        if(IsLocalWolvesDen(la,localId))dens.Add(la);
        if(lb!=la&&IsLocalWolvesDen(lb,localId))dens.Add(lb);
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "WOLVES V8: waiting for Battle_Realms_F.exe...";
        if(!enabled)
        {
            if(wasEnabled){dens.Clear();writes=0;lastDen=0;lastBefore=lastAfter=0;}
            wasEnabled=false;
            return "WOLVES V8: OFF — Wolves Den stock runs normally";
        }

        if(!wasEnabled)
        {
            dens.Clear();writes=0;lastDen=0;lastBefore=lastAfter=0;
            wasEnabled=true;
        }

        uint localId=R32(moduleBase+RVA_LOCAL_ID);
        if(localId>=10)return $"WOLVES V8: ON | invalid localId:{localId}";

        // Select each local Wolves Den once while ON. It is then latched and remains safe if selection changes.
        CaptureSelected(localId);

        var stale=new List<long>();
        foreach(long den in dens)
        {
            if(!IsLocalWolvesDen(den,localId)){stale.Add(den);continue;}
            uint before=R32(den+OFF_WOLF_STOCK);
            // Safety: the native field is a tiny stock counter (normally 0..12). Refuse implausible objects.
            if(before>0xFFFFu){stale.Add(den);continue;}
            if(W8(den+OFF_WOLF_STOCK,MAX_WOLF_STOCK))
            {
                writes++;
                lastDen=den;
                lastBefore=before;
                lastAfter=R32(den+OFF_WOLF_STOCK);
            }
        }
        foreach(long den in stale)dens.Remove(den);

        if(dens.Count==0)
            return "WOLVES V8: ON | select YOUR Wolves Den once — waiting for type 0x44";
        return $"WOLVES V8: ON | latchedDens:{dens.Count} den:0x{lastDen:X8} stock:{lastBefore}->{lastAfter} writes:{writes} | exact +0x250 byte=250";
    }

    public static void Stop()=>Detach();
    static void Detach()
    {
        dens.Clear();wasEnabled=false;writes=0;lastDen=0;lastBefore=lastAfter=0;
        if(h!=IntPtr.Zero)CloseHandle(h);
        h=IntPtr.Zero;p=null;moduleBase=0;
    }
}
