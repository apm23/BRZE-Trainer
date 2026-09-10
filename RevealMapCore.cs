using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class RevealMapCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint allocationType,uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr addr,UIntPtr size,uint newProtect,out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr CreateRemoteThread(IntPtr process,IntPtr attrs,UIntPtr stack,IntPtr start,IntPtr param,uint flags,out uint threadId);
    [DllImport("kernel32.dll", SetLastError=true)] static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x2|0x400|0x8|0x20|0x10;
    const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_EXECUTE_READWRITE=0x40;

    // Native FOW setter:
    // EnableFogOfWar -> push 1; DisableFogOfWar -> push 0; call preferred VA 0x50DBD7.
    const int RVA_SET_FOG_OF_WAR=0x10DBD7;
    const int RVA_FOG_STATE=0x441D04;

    // Current BRZE FOW/map reinitialization entry, preferred VA 0x50D868.
    // It calls the FOW-buffer teardown routine at 0x50D887 before allocating the next map's buffers.
    // Stock first five bytes: 53 8B DC 51 51.
    // Journey transitions can tear the map down while Reveal left FOW disabled.  A tiny game-thread
    // guard normalizes the state to 1 BEFORE teardown and increments a generation counter.  After
    // reinit, Tick() notices the generation change and re-applies Reveal only after a short settle.
    const int RVA_FOW_REINIT=0x10D868;
    static readonly byte[] FowReinitOriginal={0x53,0x8B,0xDC,0x51,0x51};

    static IntPtr h=IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static bool? appliedReveal;
    static string error="";

    static IntPtr guardCave=IntPtr.Zero;
    static long guardCounterAddr;
    static bool guardInstalled;
    static uint seenGeneration;
    static DateTime reapplyAfterUtc=DateTime.MinValue;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));

    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;

    static bool WriteCode(long addr,byte[] data)
    {
        if(h==IntPtr.Zero)return false;
        if(!VirtualProtectEx(h,A(addr),(UIntPtr)data.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;
        bool ok=WriteBytes(addr,data);
        if(ok)FlushInstructionCache(h,A(addr),(UIntPtr)data.Length);
        VirtualProtectEx(h,A(addr),(UIntPtr)data.Length,old,out _);
        return ok;
    }

    static int Rel32(long fromNext,long to)=>unchecked((int)(to-fromNext));

    static void ResetLocal(bool closeHandle)
    {
        if(closeHandle&&h!=IntPtr.Zero)try{CloseHandle(h);}catch{}
        h=IntPtr.Zero;p=null;moduleBase=0;appliedReveal=null;error="";
        guardCave=IntPtr.Zero;guardCounterAddr=0;guardInstalled=false;seenGeneration=0;
        reapplyAfterUtc=DateTime.MinValue;
    }

    static bool Attach()
    {
        try{if(p!=null&&!p.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        // If the old process has already exited there is nothing left to restore in it.
        ResetLocal(true);
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0)return false;
        p=ps[0];
        try{moduleBase=p.MainModule!.BaseAddress.ToInt64();}catch{p=null;return false;}
        h=OpenProcess(ACCESS,false,p.Id);
        appliedReveal=null;error="";
        return h!=IntPtr.Zero;
    }

    static bool InstallTransitionGuard()
    {
        if(guardInstalled)return true;
        long target=moduleBase+RVA_FOW_REINIT;
        var cur=new byte[FowReinitOriginal.Length];
        if(!ReadExact(target,cur)){error="cannot read FOW reinit entry";return false;}
        for(int i=0;i<cur.Length;i++)if(cur[i]!=FowReinitOriginal[i])
        {
            error="FOW reinit entry not stock — transition guard refused";
            return false;
        }

        guardCave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)0x1000,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
        if(guardCave==IntPtr.Zero){error="transition guard alloc failed: "+Marshal.GetLastWin32Error();return false;}
        long cave=guardCave.ToInt64();
        guardCounterAddr=cave+0x100;
        if(!WriteBytes(guardCounterAddr,new byte[4])){error="transition counter init failed";VirtualFreeEx(h,guardCave,UIntPtr.Zero,MEM_RELEASE);guardCave=IntPtr.Zero;return false;}

        var b=new List<byte>();
        // mov dword ptr [FOW_STATE],1
        b.AddRange(new byte[]{0xC7,0x05});b.AddRange(BitConverter.GetBytes(unchecked((uint)(moduleBase+RVA_FOG_STATE))));b.AddRange(new byte[]{1,0,0,0});
        // inc dword ptr [generation]
        b.AddRange(new byte[]{0xFF,0x05});b.AddRange(BitConverter.GetBytes(unchecked((uint)guardCounterAddr)));
        // displaced stock prologue
        b.AddRange(FowReinitOriginal);
        // jmp target+5
        b.Add(0xE9);b.AddRange(BitConverter.GetBytes(Rel32(cave+b.Count+4,target+FowReinitOriginal.Length)));
        if(!WriteBytes(cave,b.ToArray()))
        {
            error="transition guard cave write failed";VirtualFreeEx(h,guardCave,UIntPtr.Zero,MEM_RELEASE);guardCave=IntPtr.Zero;guardCounterAddr=0;return false;
        }

        var j=new byte[5];j[0]=0xE9;Array.Copy(BitConverter.GetBytes(Rel32(target+5,cave)),0,j,1,4);
        if(!WriteCode(target,j))
        {
            error="transition guard hook write failed";VirtualFreeEx(h,guardCave,UIntPtr.Zero,MEM_RELEASE);guardCave=IntPtr.Zero;guardCounterAddr=0;return false;
        }
        guardInstalled=true;
        seenGeneration=R32(guardCounterAddr);
        error="";
        return true;
    }

    static void UninstallTransitionGuard()
    {
        if(h==IntPtr.Zero||!guardInstalled)return;
        WriteCode(moduleBase+RVA_FOW_REINIT,FowReinitOriginal);
        if(guardCave!=IntPtr.Zero)VirtualFreeEx(h,guardCave,UIntPtr.Zero,MEM_RELEASE);
        guardCave=IntPtr.Zero;guardCounterAddr=0;guardInstalled=false;seenGeneration=0;
    }

    static bool SetReveal(bool reveal)
    {
        // reveal=true means DisableFogOfWar(0); reveal=false means EnableFogOfWar(1).
        IntPtr param=reveal?IntPtr.Zero:new IntPtr(1);
        IntPtr t=CreateRemoteThread(h,IntPtr.Zero,UIntPtr.Zero,A(moduleBase+RVA_SET_FOG_OF_WAR),param,0,out _);
        if(t==IntPtr.Zero){error="CreateRemoteThread failed: "+Marshal.GetLastWin32Error();return false;}
        uint wait=WaitForSingleObject(t,5000);
        CloseHandle(t);
        if(wait!=0){error="native FOW call timeout/status "+wait;return false;}
        appliedReveal=reveal;error="";
        return true;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "MAP: waiting for Battle_Realms_F.exe...";

        if(enabled&&!guardInstalled&&!InstallTransitionGuard())return "MAP: ERROR — "+error;

        if(guardInstalled)
        {
            uint gen=R32(guardCounterAddr);
            if(gen!=seenGeneration)
            {
                seenGeneration=gen;
                // The game-thread guard has already restored FOW state to normal before teardown.
                appliedReveal=false;
                reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(1200);
            }
        }

        if(!enabled)
        {
            if(appliedReveal==true&&!SetReveal(false))return "MAP: ERROR — "+error;
            return "MAP: normal fog-of-war";
        }

        if(DateTime.UtcNow<reapplyAfterUtc)
            return "MAP: TRANSITION SAFE — waiting for new map FOW to settle";

        // If the game itself re-enabled FOW (normal at map init), re-apply the user's armed Reveal.
        uint state=R32(moduleBase+RVA_FOG_STATE);
        if(appliedReveal!=true||state!=0)
        {
            if(!SetReveal(true))return "MAP: ERROR — "+error;
        }
        return "MAP: REVEALED — transition guard armed";
    }

    // safeRestore=true is for a stable in-battle shutdown.  During loading/transition we deliberately
    // avoid calling the heavy native FOW setter; the game-thread reinit guard owns that transition.
    public static void Stop(bool safeRestore)
    {
        if(h==IntPtr.Zero){ResetLocal(false);return;}
        try
        {
            if(safeRestore)
            {
                if(appliedReveal==true&&R32(moduleBase+RVA_FOG_STATE)==0)try{SetReveal(false);}catch{}
                UninstallTransitionGuard();
            }
            // Unsafe close/transition: leave the tiny cave+hook allocated in the BRZE process.
            // It only forces the game's normal FOW state (1) at future FOW reinit and therefore cannot
            // dereference trainer memory after this process exits.  BRZE frees it naturally on exit.
        }
        finally{ResetLocal(true);}
    }

    public static void Stop()=>Stop(true);
}
