using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class PausePeasantCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x0010|0x0020|0x0008|0x0400;
    const int RVA_LOCAL_ID=0x4416D0;
    // Current BRZE remap: 10 DWORD creation-enable entries. 0=pause, 1=enabled.
    const int RVA_PEASANT_CREATION=0x467AF4;

    static readonly object sync=new();
    static Process? process;
    static IntPtr h;
    static long moduleBase;
    static long savedAddress;
    static uint originalValue;
    static bool saved;
    static bool applied;
    static DateTime lastTry;

    static bool ReadExact(long address,byte[] data)
        => h!=IntPtr.Zero && ReadProcessMemory(h,new IntPtr(unchecked((int)(uint)address)),data,data.Length,out var n) && n.ToInt64()==data.Length;

    static uint R32(long address)
    {
        var b=new byte[4];
        return ReadExact(address,b)?BitConverter.ToUInt32(b,0):0xFFFFFFFF;
    }

    static bool W32(long address,uint value)
    {
        if(h==IntPtr.Zero)return false;
        var b=BitConverter.GetBytes(value);
        return WriteProcessMemory(h,new IntPtr(unchecked((int)(uint)address)),b,b.Length,out var n) && n.ToInt64()==b.Length;
    }

    static void RestoreUnlocked()
    {
        if(saved&&applied&&h!=IntPtr.Zero&&savedAddress!=0)
        {
            try{W32(savedAddress,originalValue);}catch{}
        }
        saved=false;applied=false;savedAddress=0;originalValue=1;
    }

    static void DetachUnlocked(bool restore)
    {
        if(restore)RestoreUnlocked();
        try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}
        h=IntPtr.Zero;process=null;moduleBase=0;lastTry=DateTime.MinValue;
    }

    static bool EnsureAttached()
    {
        try
        {
            if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;
        }
        catch{}

        DetachUnlocked(false);
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<400)return false;
        lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0)return false;
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}
        catch{process=null;return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        return h!=IntPtr.Zero;
    }

    public static string Tick(bool enabled)
    {
        lock(sync)
        {
            if(!enabled)
            {
                if(h!=IntPtr.Zero||saved)DetachUnlocked(true);
                return "PAUSE PEASANT: OFF";
            }

            if(!EnsureAttached())return "PAUSE PEASANT: ARMED — waiting for BRZE";
            uint lid=R32(moduleBase+RVA_LOCAL_ID);
            if(lid>=10)return "PAUSE PEASANT: waiting for local player";
            long address=moduleBase+RVA_PEASANT_CREATION+(long)lid*4;

            if(!saved||savedAddress!=address)
            {
                RestoreUnlocked();
                uint current=R32(address);
                if(current==0xFFFFFFFF)return "PAUSE PEASANT: creation flag unreadable";
                savedAddress=address;originalValue=current;saved=true;
            }

            uint now=R32(address);
            if(now!=0 && !W32(address,0))return "PAUSE PEASANT: write failed";
            applied=true;
            return $"PAUSE PEASANT: ON — local player {lid} creation disabled";
        }
    }

    public static void Stop()
    {
        lock(sync)DetachUnlocked(true);
    }
}
