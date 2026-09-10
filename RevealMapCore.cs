using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class RevealMapCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr CreateRemoteThread(IntPtr process,IntPtr attrs,UIntPtr stack,IntPtr start,IntPtr param,uint flags,out uint threadId);
    [DllImport("kernel32.dll", SetLastError=true)] static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x2|0x400|0x8|0x20|0x10;
    // BattleScriptInterface::Enable/DisableFogOfWar both dispatch here:
    // EnableFogOfWar -> push 1; DisableFogOfWar -> push 0; call preferred VA 0x50DBD7.
    // The native routine is stdcall-style ret 4, matching CreateRemoteThread's one argument.
    const int RVA_SET_FOG_OF_WAR=0x10DBD7;

    static IntPtr h=IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static bool? appliedReveal;
    static string error="";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));

    static bool Attach()
    {
        try{if(p!=null&&!p.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach(false);
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0)return false;
        p=ps[0];
        try{moduleBase=p.MainModule!.BaseAddress.ToInt64();}catch{p=null;return false;}
        h=OpenProcess(ACCESS,false,p.Id);
        appliedReveal=null;error="";
        return h!=IntPtr.Zero;
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
        if(appliedReveal!=enabled&&!SetReveal(enabled))return "MAP: ERROR — "+error;
        return enabled?"MAP: REVEALED — native fog-of-war disabled":"MAP: normal fog-of-war";
    }

    public static void Stop()=>Detach(true);
    static void Detach(bool restore)
    {
        if(h!=IntPtr.Zero)
        {
            if(restore&&appliedReveal==true)
            {
                try{SetReveal(false);}catch{}
            }
            CloseHandle(h);
        }
        h=IntPtr.Zero;p=null;moduleBase=0;appliedReveal=null;error="";
    }
}
