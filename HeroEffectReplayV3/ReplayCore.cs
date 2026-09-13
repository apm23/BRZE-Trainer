using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectReplayV3;

internal readonly record struct RuntimeSnapshot(string Game, string Queue);
internal readonly record struct CapturedUnit(uint Ptr, uint Def, uint Owner);

internal static class ReplayCore
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

    const int RVA_SELECTION_LIST=0x441708;
    const int RVA_FRAME_MOUSE_DRAW=0x135C43;
    const int RVA_APPLY_TARGET_ABILITY=0x1F0C32;
    const int OFF_DEF=0x74, OFF_OWNER=0x240;
    const int MAX_SELECTED=120;

    public const uint GRAYBACK_RUNTIME_ABILITY=0xC0;
    public const uint ISSYL_RUNTIME_ABILITY=0xA5;

    static readonly byte[] FrameOriginal={0x55,0x8B,0xEC,0x83,0xEC,0x5C};
    static readonly byte[] ApplyPrologue={0x55,0x8B,0xEC,0x56,0x8B,0xF1};

    const int ACTIVE=0x400,COUNT=0x404,INDEX=0x408,A1=0x40C,A2=0x410,MODE=0x414,CALLS=0x418;
    const int ENTRIES=0x800,FXSCRATCH=0x2000,CAVE_SIZE=0x4000;

    static readonly object sync=new();
    static readonly List<CapturedUnit> heldTargets=new();
    static Process? process; static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero; static long moduleBase;
    static bool installed; static byte[]? hookPatch; static string status="not attached",lastQueue="QUEUE: idle"; static DateTime lastTry;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
    static bool Same(byte[] a,byte[] b)=>a.Length==b.Length&&a.SequenceEqual(b);

    static bool WriteCode(long a,byte[] b)
    {
        if(h==IntPtr.Zero)return false;
        if(!VirtualProtectEx(h,A(a),(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;
        bool ok=WriteBytes(a,b); FlushInstructionCache(h,A(a),(UIntPtr)b.Length); VirtualProtectEx(h,A(a),(UIntPtr)b.Length,old,out _); return ok;
    }
    static void I32(List<byte>b,int v)=>b.AddRange(BitConverter.GetBytes(v));
    static void U32(List<byte>b,uint v)=>b.AddRange(BitConverter.GetBytes(v));
    static void PatchRel(List<byte>b,int at,long fromNext,long to){var x=BitConverter.GetBytes(unchecked((int)(to-fromNext)));for(int i=0;i<4;i++)b[at+i]=x[i];}

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        DetachRuntime(); if((DateTime.UtcNow-lastTry).TotalMilliseconds<300)return false; lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F"); if(ps.Length==0){status="waiting for Battle_Realms_F.exe";return false;} process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}catch{process=null;status="cannot resolve module base";return false;}
        h=OpenProcess(ACCESS,false,process.Id); if(h==IntPtr.Zero){status="OpenProcess failed";return false;} status=$"attached PID {process.Id} base 0x{moduleBase:X8}"; return true;
    }

    static List<uint> SelectedUnits()
    {
        var result=new List<uint>(); if(!Attach())return result; long list=moduleBase+RVA_SELECTION_LIST; uint count=R32(list+0x18),node=R32(list);
        var seenNodes=new HashSet<uint>(); var seenUnits=new HashSet<uint>(); int limit=(int)Math.Min(count,MAX_SELECTED);
        for(int i=0;i<limit&&node!=0;i++)
        {
            if(!seenNodes.Add(node))break; uint next=R32((long)node+0x00),unit=R32((long)node+0x08);
            if(unit!=0&&seenUnits.Add(unit))result.Add(unit); node=next;
        }
        return result;
    }

    static byte[] BuildStub(long stub)
    {
        long active=stub+ACTIVE,count=stub+COUNT,index=stub+INDEX,a1=stub+A1,a2=stub+A2,mode=stub+MODE,calls=stub+CALLS,entries=stub+ENTRIES,fx=stub+FXSCRATCH;
        uint apply=(uint)(moduleBase+RVA_APPLY_TARGET_ABILITY); var b=new List<byte>();
        b.Add(0x9C);b.Add(0x60); b.AddRange(new byte[]{0x0F,0xAE,0x05});U32(b,(uint)fx);
        b.Add(0xA1);U32(b,(uint)active); b.AddRange(new byte[]{0x85,0xC0,0x0F,0x84});int jInactive=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xBB,0x04,0x00,0x00,0x00});
        int loop=b.Count;
        b.Add(0xA1);U32(b,(uint)index); b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)count); b.AddRange(new byte[]{0x3B,0xC2,0x0F,0x83});int jFinished=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0xC8,0xC1,0xE1,0x02,0x81,0xC1});U32(b,(uint)entries); b.AddRange(new byte[]{0x8B,0x31,0x85,0xF6,0x0F,0x84});int jSkip=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0xCE,0xFF,0x35});U32(b,(uint)a1); b.Add(0xB8);U32(b,apply);b.AddRange(new byte[]{0xFF,0xD0,0xFF,0x05});U32(b,(uint)calls);
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)mode);b.Add(0x02); b.AddRange(new byte[]{0x0F,0x85});int jAfterSecond=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0xCE,0xFF,0x35});U32(b,(uint)a2); b.Add(0xB8);U32(b,apply);b.AddRange(new byte[]{0xFF,0xD0,0xFF,0x05});U32(b,(uint)calls);
        int afterSecond=b.Count,skip=b.Count;
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)index); b.Add(0x4B); b.AddRange(new byte[]{0x0F,0x85});int jLoop=b.Count;I32(b,0);
        b.Add(0xA1);U32(b,(uint)index); b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)count); b.AddRange(new byte[]{0x3B,0xC2,0x0F,0x82});int jKeep=b.Count;I32(b,0);
        int finished=b.Count; b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)active);U32(b,0);
        int restore=b.Count; b.AddRange(new byte[]{0x0F,0xAE,0x0D});U32(b,(uint)fx); b.Add(0x61);b.Add(0x9D); b.AddRange(FrameOriginal); b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jInactive,stub+jInactive+4,stub+restore); PatchRel(b,jFinished,stub+jFinished+4,stub+finished); PatchRel(b,jSkip,stub+jSkip+4,stub+skip);
        PatchRel(b,jAfterSecond,stub+jAfterSecond+4,stub+afterSecond); PatchRel(b,jLoop,stub+jLoop+4,stub+loop); PatchRel(b,jKeep,stub+jKeep+4,stub+restore); PatchRel(b,jBack,stub+jBack+4,moduleBase+RVA_FRAME_MOUSE_DRAW+FrameOriginal.Length);
        return b.ToArray();
    }

    static bool InstallHook()
    {
        if(installed)return true; if(!Attach())return false;
        var ap=new byte[ApplyPrologue.Length]; if(!ReadExact(moduleBase+RVA_APPLY_TARGET_ABILITY,ap)||!Same(ap,ApplyPrologue)){status="APPLY helper bytes mismatch";return false;}
        long site=moduleBase+RVA_FRAME_MOUSE_DRAW; var now=new byte[FrameOriginal.Length]; if(!ReadExact(site,now)||!Same(now,FrameOriginal)){status="FRAME HOOK BUSY/MISMATCH — close main trainer / Clone Lab / Sniffer / V2";return false;}
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)CAVE_SIZE,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE); if(cave==IntPtr.Zero){status="VirtualAllocEx failed";return false;}
        long c=cave.ToInt64(); if((c+FXSCRATCH)%16!=0){status="FX scratch alignment failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        if(!WriteBytes(c+ACTIVE,new byte[0x40])||!WriteBytes(c,BuildStub(c))){status="cave init failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        hookPatch=new byte[6];hookPatch[0]=0xE9;Array.Copy(BitConverter.GetBytes(unchecked((int)(c-(site+5)))),0,hookPatch,1,4);hookPatch[5]=0x90;
        if(!WriteCode(site,hookPatch)){status="frame hook patch failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;hookPatch=null;return false;}
        installed=true;status="REPLAY V3 DURATION HOOK ACTIVE";return true;
    }

    static string QueuePointers(IReadOnlyList<uint> units,uint ability1,uint ability2,string label)
    {
        if(units.Count==0)return $"{label}: no valid targets."; if(!InstallHook())return $"{label}: blocked — {status}"; long c=cave.ToInt64();
        if(R32(c+ACTIVE)!=0)return $"{label}: previous queue still active.";
        var bytes=new byte[units.Count*4];for(int i=0;i<units.Count;i++)Buffer.BlockCopy(BitConverter.GetBytes(units[i]),0,bytes,i*4,4); if(!WriteBytes(c+ENTRIES,bytes))return $"{label}: queue write failed.";
        bool both=ability2!=uint.MaxValue;W32(c+COUNT,(uint)units.Count);W32(c+INDEX,0);W32(c+CALLS,0);W32(c+A1,ability1);W32(c+A2,both?ability2:uint.MaxValue);W32(c+MODE,both?2u:1u);W32(c+ACTIVE,1);
        lastQueue=$"{label}: queued {units.Count} target(s)";return lastQueue;
    }

    public static string QueueSelected(uint ability1,uint ability2,string label)
    {
        lock(sync){return QueuePointers(SelectedUnits(),ability1,ability2,label);}
    }

    public static int CaptureSelectedTargets()
    {
        lock(sync)
        {
            heldTargets.Clear();
            foreach(uint unit in SelectedUnits())
            {
                uint def=R32((long)unit+OFF_DEF), owner=R32((long)unit+OFF_OWNER);
                if(def!=0)heldTargets.Add(new CapturedUnit(unit,def,owner));
            }
            return heldTargets.Count;
        }
    }

    static List<uint> ValidHeldTargets()
    {
        var valid=new List<uint>(heldTargets.Count);
        foreach(var x in heldTargets)
        {
            if(x.Ptr==0)continue;
            if(R32((long)x.Ptr+OFF_DEF)!=x.Def)continue;
            if(R32((long)x.Ptr+OFF_OWNER)!=x.Owner)continue;
            valid.Add(x.Ptr);
        }
        return valid;
    }

    public static string QueueHeld(uint ability1,uint ability2,string label)
    {
        lock(sync)
        {
            if(!Attach())return $"{label}: game not attached.";
            var valid=ValidHeldTargets();
            return QueuePointers(valid,ability1,ability2,label);
        }
    }

    public static bool IsQueueActive()
    {
        lock(sync)
        {
            if(!Attach()||!installed||cave==IntPtr.Zero)return false;
            return R32(cave.ToInt64()+ACTIVE)!=0;
        }
    }

    public static int HeldCount { get { lock(sync)return heldTargets.Count; } }
    public static void ClearHeld(){lock(sync)heldTargets.Clear();}

    public static RuntimeSnapshot Snapshot()
    {
        lock(sync)
        {
            if(!Attach())return new("GAME: waiting for Battle_Realms_F.exe",lastQueue); string g=$"GAME: {status}"; if(!installed||cave==IntPtr.Zero)return new(g,lastQueue);
            long c=cave.ToInt64();uint active=R32(c+ACTIVE),count=R32(c+COUNT),index=R32(c+INDEX),calls=R32(c+CALLS),a1=R32(c+A1),a2=R32(c+A2),mode=R32(c+MODE);
            string q=$"QUEUE: {(active!=0?"ACTIVE":"DONE")} {index}/{count} | native calls:{calls} | a1:0x{a1:X}"+(mode==2?$" a2:0x{a2:X}":""); return new(g,q);
        }
    }

    public static void ResetRuntime(){lock(sync)DetachRuntime();}
    static void DetachRuntime()
    {
        try
        {
            if(h!=IntPtr.Zero&&installed&&hookPatch!=null){long site=moduleBase+RVA_FRAME_MOUSE_DRAW;var now=new byte[hookPatch.Length];if(ReadExact(site,now)&&Same(now,hookPatch))WriteCode(site,FrameOriginal);}
            if(h!=IntPtr.Zero&&cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE); if(h!=IntPtr.Zero)CloseHandle(h);
        }catch{}
        process=null;h=IntPtr.Zero;cave=IntPtr.Zero;moduleBase=0;installed=false;hookPatch=null;heldTargets.Clear();status="not attached";lastTry=DateTime.MinValue;
    }
}
