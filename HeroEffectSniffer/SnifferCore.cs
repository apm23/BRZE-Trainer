using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectSniffer;

internal readonly record struct SniffEvent(uint Calls, uint Ability, uint Unit, uint UnitType, uint Owner)
{
    public string Format(string name) =>
        $"{name}: calls {Calls} | ability 0x{Ability:X8} | unit 0x{Unit:X8} | type 0x{UnitType:X8} | owner {(Owner == 0xFFFFFFFFu ? "-" : Owner.ToString())}";
}

internal readonly record struct SniffSnapshot(string Game, SniffEvent Target, SniffEvent Magic);

internal static class SnifferCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h, IntPtr addr, UIntPtr size, uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x0010 | 0x0020 | 0x0008 | 0x0400;
    const uint MEM_COMMIT=0x1000, MEM_RESERVE=0x2000, MEM_RELEASE=0x8000, PAGE_EXECUTE_READWRITE=0x40;

    const int RVA_TARGET_HELPER = 0x1F0C32; // preferred VA 0x5F0C32
    const int RVA_MAGIC_CREATE  = 0x13FFD1; // preferred VA 0x53FFD1
    const int OFF_DEF=0x74, OFF_OWNER=0x240;

    static readonly byte[] TargetOriginal={0x55,0x8B,0xEC,0x56,0x8B,0xF1};
    static readonly byte[] MagicOriginal ={0x55,0x8B,0xEC,0x83,0xEC,0x44};

    const int TARGET_STUB=0x000, MAGIC_STUB=0x200;
    const int TARGET_CALLS=0x800, TARGET_ABILITY=0x804, TARGET_UNIT=0x808, TARGET_TYPE=0x80C, TARGET_OWNER=0x810;
    const int MAGIC_CALLS=0x820, MAGIC_ABILITY=0x824, MAGIC_UNIT=0x828, MAGIC_TYPE=0x82C, MAGIC_OWNER=0x830;
    const int CAVE_SIZE=0x1000;

    static readonly object sync=new();
    static Process? process;
    static IntPtr h=IntPtr.Zero, cave=IntPtr.Zero;
    static long moduleBase;
    static bool installed;
    static byte[]? targetPatch, magicPatch;
    static string status="not attached";

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a, byte[] b)=>h!=IntPtr.Zero && ReadProcessMemory(h,A(a),b,b.Length,out var n) && n.ToInt64()==b.Length;
    static bool WriteBytes(long a, byte[] b)=>h!=IntPtr.Zero && WriteProcessMemory(h,A(a),b,b.Length,out var n) && n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
    static bool Same(byte[] a,byte[] b)=>a.Length==b.Length&&a.SequenceEqual(b);

    static bool WriteCode(long a,byte[] b)
    {
        if(h==IntPtr.Zero)return false;
        if(!VirtualProtectEx(h,A(a),(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;
        bool ok=WriteBytes(a,b);
        FlushInstructionCache(h,A(a),(UIntPtr)b.Length);
        VirtualProtectEx(h,A(a),(UIntPtr)b.Length,old,out _);
        return ok;
    }

    static void I32(List<byte>b,int v)=>b.AddRange(BitConverter.GetBytes(v));
    static void U32(List<byte>b,uint v)=>b.AddRange(BitConverter.GetBytes(v));
    static void PatchRel(List<byte>b,int at,long fromNext,long to)
    {
        var x=BitConverter.GetBytes(unchecked((int)(to-fromNext)));
        for(int i=0;i<4;i++)b[at+i]=x[i];
    }

    static byte[] JmpPatch(long site,long stub)
    {
        var b=new List<byte>{0xE9};I32(b,unchecked((int)(stub-(site+5))));b.Add(0x90);return b.ToArray();
    }

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        DetachRuntime();
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0){status="waiting for Battle_Realms_F.exe";return false;}
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}catch{process=null;status="cannot resolve module base";return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        if(h==IntPtr.Zero){status="OpenProcess failed";return false;}
        status=$"attached PID {process.Id} base 0x{moduleBase:X8}";
        return true;
    }

    static byte[] BuildTargetStub(long stub)
    {
        long tc=stub-TARGET_STUB+TARGET_CALLS, ta=stub-TARGET_STUB+TARGET_ABILITY, tu=stub-TARGET_STUB+TARGET_UNIT;
        long tt=stub-TARGET_STUB+TARGET_TYPE, to=stub-TARGET_STUB+TARGET_OWNER;
        var b=new List<byte>{0x9C,0x60}; // pushfd,pushad
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)tc);
        b.AddRange(new byte[]{0x8B,0x44,0x24,0x28,0xA3});U32(b,(uint)ta); // original arg1 ability
        b.AddRange(new byte[]{0x8B,0x74,0x24,0x18}); // original ECX target from pushad save
        b.AddRange(new byte[]{0x89,0x35});U32(b,(uint)tu);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)tt);U32(b,0xFFFFFFFFu);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)to);U32(b,0xFFFFFFFFu);
        b.AddRange(new byte[]{0x85,0xF6,0x0F,0x84});int jDone=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x86});U32(b,OFF_OWNER);b.Add(0xA3);U32(b,(uint)to);
        b.AddRange(new byte[]{0x8B,0x46,0x74,0x85,0xC0,0x0F,0x84});int jDone2=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x00,0xA3});U32(b,(uint)tt);
        int done=b.Count;
        b.AddRange(new byte[]{0x61,0x9D});
        b.AddRange(TargetOriginal);
        b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jDone,stub+jDone+4,stub+done);PatchRel(b,jDone2,stub+jDone2+4,stub+done);
        PatchRel(b,jBack,stub+jBack+4,moduleBase+RVA_TARGET_HELPER+TargetOriginal.Length);
        return b.ToArray();
    }

    static byte[] BuildMagicStub(long stub)
    {
        long mc=stub-MAGIC_STUB+MAGIC_CALLS, ma=stub-MAGIC_STUB+MAGIC_ABILITY, mu=stub-MAGIC_STUB+MAGIC_UNIT;
        long mt=stub-MAGIC_STUB+MAGIC_TYPE, mo=stub-MAGIC_STUB+MAGIC_OWNER;
        var b=new List<byte>{0x9C,0x60};
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)mc);
        b.AddRange(new byte[]{0x8B,0x44,0x24,0x28,0xA3});U32(b,(uint)ma); // arg1 ability
        b.AddRange(new byte[]{0x8B,0x44,0x24,0x2C,0xA3});U32(b,(uint)mo); // arg2 owner
        b.AddRange(new byte[]{0x8B,0x74,0x24,0x30}); // arg3 Unit*
        b.AddRange(new byte[]{0x89,0x35});U32(b,(uint)mu);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)mt);U32(b,0xFFFFFFFFu);
        b.AddRange(new byte[]{0x85,0xF6,0x0F,0x84});int jDone=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x46,0x74,0x85,0xC0,0x0F,0x84});int jDone2=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x00,0xA3});U32(b,(uint)mt);
        int done=b.Count;
        b.AddRange(new byte[]{0x61,0x9D});
        b.AddRange(MagicOriginal);
        b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jDone,stub+jDone+4,stub+done);PatchRel(b,jDone2,stub+jDone2+4,stub+done);
        PatchRel(b,jBack,stub+jBack+4,moduleBase+RVA_MAGIC_CREATE+MagicOriginal.Length);
        return b.ToArray();
    }

    public static string Install()
    {
        lock(sync)
        {
            if(installed)return "SNIFFER already armed.";
            if(!Attach())return "ARM failed — "+status;
            long ts=moduleBase+RVA_TARGET_HELPER, ms=moduleBase+RVA_MAGIC_CREATE;
            var tnow=new byte[TargetOriginal.Length];var mnow=new byte[MagicOriginal.Length];
            if(!ReadExact(ts,tnow)||!Same(tnow,TargetOriginal))return "ARM blocked — target helper bytes busy/mismatch.";
            if(!ReadExact(ms,mnow)||!Same(mnow,MagicOriginal))return "ARM blocked — magic-create bytes busy/mismatch.";
            cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)CAVE_SIZE,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
            if(cave==IntPtr.Zero)return "ARM failed — VirtualAllocEx.";
            long c=cave.ToInt64();
            if(!WriteBytes(c+TARGET_CALLS,new byte[0x40])){VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return "ARM failed — state init.";}
            W32(c+TARGET_TYPE,0xFFFFFFFF);W32(c+TARGET_OWNER,0xFFFFFFFF);W32(c+MAGIC_TYPE,0xFFFFFFFF);W32(c+MAGIC_OWNER,0xFFFFFFFF);
            if(!WriteBytes(c+TARGET_STUB,BuildTargetStub(c+TARGET_STUB))||!WriteBytes(c+MAGIC_STUB,BuildMagicStub(c+MAGIC_STUB)))
            {VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return "ARM failed — stub write.";}
            targetPatch=JmpPatch(ts,c+TARGET_STUB);magicPatch=JmpPatch(ms,c+MAGIC_STUB);
            if(!WriteCode(ts,targetPatch)){VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;targetPatch=magicPatch=null;return "ARM failed — target patch.";}
            if(!WriteCode(ms,magicPatch))
            {
                var cur=new byte[targetPatch.Length];if(ReadExact(ts,cur)&&Same(cur,targetPatch))WriteCode(ts,TargetOriginal);
                VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;targetPatch=magicPatch=null;return "ARM failed — magic patch; target restored.";
            }
            installed=true;status="SNIFFER ARMED — observation only";Clear();return status;
        }
    }

    public static void Clear()
    {
        lock(sync)
        {
            if(!installed||cave==IntPtr.Zero)return;long c=cave.ToInt64();
            WriteBytes(c+TARGET_CALLS,new byte[0x40]);
            W32(c+TARGET_TYPE,0xFFFFFFFF);W32(c+TARGET_OWNER,0xFFFFFFFF);W32(c+MAGIC_TYPE,0xFFFFFFFF);W32(c+MAGIC_OWNER,0xFFFFFFFF);
        }
    }

    static SniffEvent Event(long c,int calls,int ability,int unit,int type,int owner)=>
        new(R32(c+calls),R32(c+ability),R32(c+unit),R32(c+type),R32(c+owner));

    public static SniffSnapshot Snapshot()
    {
        lock(sync)
        {
            if(!Attach())return new("GAME: waiting for Battle_Realms_F.exe",default,default);
            string g=$"GAME: {status}";
            if(!installed||cave==IntPtr.Zero)return new(g,default,default);
            long c=cave.ToInt64();
            return new(g,Event(c,TARGET_CALLS,TARGET_ABILITY,TARGET_UNIT,TARGET_TYPE,TARGET_OWNER),Event(c,MAGIC_CALLS,MAGIC_ABILITY,MAGIC_UNIT,MAGIC_TYPE,MAGIC_OWNER));
        }
    }

    public static void Stop(){lock(sync)DetachRuntime();}
    static void DetachRuntime()
    {
        try
        {
            if(h!=IntPtr.Zero&&installed)
            {
                long ts=moduleBase+RVA_TARGET_HELPER,ms=moduleBase+RVA_MAGIC_CREATE;
                if(targetPatch!=null){var x=new byte[targetPatch.Length];if(ReadExact(ts,x)&&Same(x,targetPatch))WriteCode(ts,TargetOriginal);}
                if(magicPatch!=null){var x=new byte[magicPatch.Length];if(ReadExact(ms,x)&&Same(x,magicPatch))WriteCode(ms,MagicOriginal);}
            }
            if(h!=IntPtr.Zero&&cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);
            if(h!=IntPtr.Zero)CloseHandle(h);
        }catch{}
        process=null;h=IntPtr.Zero;cave=IntPtr.Zero;moduleBase=0;installed=false;targetPatch=magicPatch=null;status="not attached";
    }
}
