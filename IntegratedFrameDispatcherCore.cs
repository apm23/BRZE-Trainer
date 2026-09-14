using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal readonly record struct IntegratedCopyResult(bool Ok,string Message);
internal readonly record struct IntegratedUnitSnap(uint Address,uint Type,uint Owner,float X,float Y);
internal readonly record struct IntegratedFrameSnapshot(string Game,string Queue,string Copy);

/// <summary>
/// V20 single owner for BRZE render-frame hook RVA 0x135C43.
/// It serializes the three proven consumers of that site:
///   - V19 Instant Death cursor action
///   - Replay V2 target ability calls
///   - Unit Clone Lab native position spawns
/// No CreateRemoteThread; native calls execute only on the game/render thread.
/// </summary>
internal static class IntegratedFrameDispatcherCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint allocationType,uint protect);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint freeType);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr addr,UIntPtr size,uint newProtect,out uint oldProtect);
    [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x10|0x20|0x8|0x400;
    const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_EXECUTE_READWRITE=0x40;

    public const int RVA_FRAME_MOUSE_DRAW=0x135C43;
    const int RVA_SELECTION_LIST=0x441708;
    const int RVA_INTERFACE_MOUSE=0x443640;
    const int RVA_CURSOR_UNIT_QUERY=0x135EFB;
    const int RVA_RELATION=0x1848E8;
    const int RVA_LOCAL_ID=0x4416D0;
    const int RVA_APPLY_TARGET_ABILITY=0x1F0C32;
    const int RVA_SPAWN_AT_XY=0x0C4A1C;

    const int OFF_DEF=0x74,OFF_OWNER=0x240,OFF_X=0x28,OFF_Y=0x2C,OFF_HP=0x404,OFF_STAMINA=0x408;
    const uint DEATH_SENTINEL=0xFF000000u;
    const uint DEATH_IDLE=0,DEATH_BURST=1,DEATH_SINGLE=2;
    const uint QUEUE_IDLE=0,QUEUE_REPLAY=1,QUEUE_SPAWN=2;
    const int MAX_SELECTED=120,MAX_COPY=120;

    public const uint GRAYBACK_RUNTIME_ABILITY=0xC0;
    public const uint ISSYL_RUNTIME_ABILITY=0xA5;

    static readonly byte[] FrameOriginal={0x55,0x8B,0xEC,0x83,0xEC,0x5C};
    static readonly byte[] ApplyPrologue={0x55,0x8B,0xEC,0x56,0x8B,0xF1};
    static readonly byte[] SpawnPrologue={0x55,0x8B,0xEC,0x83,0xEC,0x4C};

    // One cave, one hook, independent state regions.
    const int D_MODE=0x600,D_TARGET_ALL=0x604,D_LAST_UNIT=0x608,D_LAST_OWNER=0x60C,D_WRITES=0x610;
    const int Q_TYPE=0x620,Q_COUNT=0x624,Q_INDEX=0x628,Q_A1=0x62C,Q_A2=0x630,Q_MODE=0x634,Q_CALLS=0x638;
    const int Q_SUCCESS=0x63C,Q_FAIL=0x640,Q_OUT_ID=0x644,Q_LAST_OUT_ID=0x648;
    const int ENTRIES=0x1000,FXSCRATCH=0x4000,CAVE_SIZE=0x6000;
    const int SPAWN_ENTRY_SIZE=16;

    static readonly object Sync=new();
    static readonly List<IntegratedUnitSnap> copied=new();
    static Process? process;
    static IntPtr h=IntPtr.Zero,cave=IntPtr.Zero;
    static long moduleBase;
    static bool installed;
    static byte[]? hookPatch;
    static string status="not attached",lastQueue="QUEUE: idle",copyStatus="No copied selection yet.";
    static DateTime lastTry;
    static int pasteSerial;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static float RF32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToSingle(b,0):0f;}
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
    static bool Same(byte[] a,byte[] b)=>a.Length==b.Length&&a.SequenceEqual(b);

    static bool WriteCode(long a,byte[] b)
    {
        if(h==IntPtr.Zero)return false;
        if(!VirtualProtectEx(h,A(a),(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;
        bool ok=WriteBytes(a,b);
        if(ok)FlushInstructionCache(h,A(a),(UIntPtr)b.Length);
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

    static bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        DetachRuntime(false);
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<150)return false;
        lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0){status="waiting for Battle_Realms_F.exe";return false;}
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}
        catch{process=null;status="cannot resolve module base";return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        if(h==IntPtr.Zero){status="OpenProcess failed";return false;}
        status=$"attached PID {process.Id} base 0x{moduleBase:X8}";
        return true;
    }

    static List<uint> SelectedUnits()
    {
        var result=new List<uint>();
        if(!Attach())return result;
        long list=moduleBase+RVA_SELECTION_LIST;
        uint count=R32(list+0x18),node=R32(list);
        var seenNodes=new HashSet<uint>();var seenUnits=new HashSet<uint>();
        int limit=(int)Math.Min(count,MAX_SELECTED);
        for(int i=0;i<limit&&node!=0;i++)
        {
            if(!seenNodes.Add(node))break;
            uint next=R32((long)node),unit=R32((long)node+0x08);
            if(unit!=0&&seenUnits.Add(unit))result.Add(unit);
            node=next;
        }
        return result;
    }

    static byte[] BuildStub(long stub)
    {
        long dMode=stub+D_MODE,dAll=stub+D_TARGET_ALL,dLastUnit=stub+D_LAST_UNIT,dLastOwner=stub+D_LAST_OWNER,dWrites=stub+D_WRITES;
        long qType=stub+Q_TYPE,qCount=stub+Q_COUNT,qIndex=stub+Q_INDEX,qA1=stub+Q_A1,qA2=stub+Q_A2,qMode=stub+Q_MODE,qCalls=stub+Q_CALLS;
        long qSuccess=stub+Q_SUCCESS,qFail=stub+Q_FAIL,qOut=stub+Q_OUT_ID,qLastOut=stub+Q_LAST_OUT_ID,entries=stub+ENTRIES,fx=stub+FXSCRATCH;
        uint apply=(uint)(moduleBase+RVA_APPLY_TARGET_ABILITY),spawn=(uint)(moduleBase+RVA_SPAWN_AT_XY);

        var b=new List<byte>();
        b.Add(0x9C);b.Add(0x60); // pushfd / pushad
        b.AddRange(new byte[]{0x0F,0xAE,0x05});U32(b,(uint)fx); // fxsave

        // ----- Instant Death consumer -------------------------------------------------
        b.AddRange(new byte[]{0x8B,0x1D});U32(b,(uint)dMode); // ebx = death mode
        b.AddRange(new byte[]{0x85,0xDB,0x0F,0x84});int jDeathIdle=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)dLastUnit);U32(b,0);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)dLastOwner);U32(b,0xFFFFFFFFu);
        b.Add(0xB9);U32(b,(uint)(moduleBase+RVA_INTERFACE_MOUSE));
        b.AddRange(new byte[]{0x6A,0x00,0x6A,0x01,0xE8});int callQuery=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x84});int jDeathNull=b.Count;I32(b,0);
        b.Add(0xA3);U32(b,(uint)dLastUnit);
        b.AddRange(new byte[]{0x8B,0xF8}); // edi=unit
        b.AddRange(new byte[]{0x83,0xBF});U32(b,OFF_DEF);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jDeathNoDef=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x87});U32(b,OFF_OWNER);
        b.Add(0xA3);U32(b,(uint)dLastOwner);
        b.AddRange(new byte[]{0x83,0xF8,0x09,0x0F,0x87});int jDeathBadOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x0D});U32(b,(uint)(moduleBase+RVA_LOCAL_ID));
        b.AddRange(new byte[]{0x83,0xF9,0x09,0x0F,0x87});int jDeathBadLocal=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)dAll);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x85});int jDeathKillAll=b.Count;I32(b,0);
        b.Add(0x50);b.Add(0x51); // target owner, local id
        b.Add(0xE8);int callRelation=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x85});int jDeathAllied=b.Count;I32(b,0);
        int deathKill=b.Count;
        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_HP);U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_STAMINA);U32(b,DEATH_SENTINEL);
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)dWrites);
        int deathFinish=b.Count;
        b.AddRange(new byte[]{0x83,0xFB,0x02,0x0F,0x85});int jDeathKeep=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)dMode);U32(b,DEATH_IDLE);
        int afterDeath=b.Count;

        // ----- Shared queued native consumer -----------------------------------------
        b.Add(0xA1);U32(b,(uint)qType);
        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x84});int jQueueIdle=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xF8,0x01,0x0F,0x84});int jReplay=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xF8,0x02,0x0F,0x84});int jSpawn=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)qType);U32(b,QUEUE_IDLE);
        b.Add(0xE9);int jUnknownRestore=b.Count;I32(b,0);

        // Replay V2 path. Same proven helper and max four selected units/frame.
        int replayStart=b.Count;
        b.AddRange(new byte[]{0xBB,0x04,0x00,0x00,0x00});
        int replayLoop=b.Count;
        b.Add(0xA1);U32(b,(uint)qIndex);
        b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)qCount);
        b.AddRange(new byte[]{0x3B,0xC2,0x0F,0x83});int jReplayFinished=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0xC8,0xC1,0xE1,0x02,0x81,0xC1});U32(b,(uint)entries);
        b.AddRange(new byte[]{0x8B,0x31,0x85,0xF6,0x0F,0x84});int jReplaySkip=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0xCE,0xFF,0x35});U32(b,(uint)qA1);
        b.Add(0xB8);U32(b,apply);b.AddRange(new byte[]{0xFF,0xD0,0xFF,0x05});U32(b,(uint)qCalls);
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)qMode);b.Add(0x02);
        b.AddRange(new byte[]{0x0F,0x85});int jReplayAfterSecond=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0xCE,0xFF,0x35});U32(b,(uint)qA2);
        b.Add(0xB8);U32(b,apply);b.AddRange(new byte[]{0xFF,0xD0,0xFF,0x05});U32(b,(uint)qCalls);
        int replayAfterSecond=b.Count;
        int replaySkip=b.Count;
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)qIndex);
        b.Add(0x4B);
        b.AddRange(new byte[]{0x0F,0x85});int jReplayLoop=b.Count;I32(b,0);
        b.Add(0xA1);U32(b,(uint)qIndex);b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)qCount);
        b.AddRange(new byte[]{0x3B,0xC2,0x0F,0x82});int jReplayKeep=b.Count;I32(b,0);
        b.Add(0xE9);int jReplayDone=b.Count;I32(b,0);

        // Copy Unit native position-spawn path. Same proven wrapper and four spawns/frame.
        int spawnStart=b.Count;
        b.AddRange(new byte[]{0xBB,0x04,0x00,0x00,0x00});
        int spawnLoop=b.Count;
        b.AddRange(new byte[]{0x8B,0x0D});U32(b,(uint)qIndex);
        b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)qCount);
        b.AddRange(new byte[]{0x3B,0xCA,0x0F,0x83});int jSpawnFinished=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0xC1,0xC1,0xE0,0x04});b.Add(0x05);U32(b,(uint)entries);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)qOut);U32(b,0x0000FFFFu);
        b.AddRange(new byte[]{0xFF,0x70,0x0C,0xFF,0x70,0x08});
        b.Add(0x68);U32(b,(uint)qOut);
        b.AddRange(new byte[]{0xFF,0x70,0x04,0xFF,0x30});
        b.Add(0xB8);U32(b,spawn);b.AddRange(new byte[]{0xFF,0xD0});
        b.Add(0xA1);U32(b,(uint)qOut);b.Add(0xA3);U32(b,(uint)qLastOut);
        b.AddRange(new byte[]{0x3D,0xFF,0xFF,0x00,0x00,0x0F,0x84});int jSpawnFail=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)qSuccess);
        b.Add(0xEB);int jSpawnAfterStat8=b.Count;b.Add(0);
        int spawnFailLabel=b.Count;
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)qFail);
        int spawnAfterStat=b.Count;
        b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)qIndex);
        b.Add(0x4B);
        b.AddRange(new byte[]{0x0F,0x85});int jSpawnLoop=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x0D});U32(b,(uint)qIndex);b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)qCount);
        b.AddRange(new byte[]{0x3B,0xCA,0x0F,0x82});int jSpawnKeep=b.Count;I32(b,0);
        b.Add(0xE9);int jSpawnDone=b.Count;I32(b,0);

        int finishQueue=b.Count;
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)qType);U32(b,QUEUE_IDLE);
        int restore=b.Count;
        b.AddRange(new byte[]{0x0F,0xAE,0x0D});U32(b,(uint)fx);
        b.Add(0x61);b.Add(0x9D);
        b.AddRange(FrameOriginal);
        b.Add(0xE9);int jBack=b.Count;I32(b,0);

        PatchRel(b,callQuery,stub+callQuery+4,moduleBase+RVA_CURSOR_UNIT_QUERY);
        PatchRel(b,callRelation,stub+callRelation+4,moduleBase+RVA_RELATION);
        PatchRel(b,jDeathIdle,stub+jDeathIdle+4,stub+afterDeath);
        PatchRel(b,jDeathNull,stub+jDeathNull+4,stub+deathFinish);
        PatchRel(b,jDeathNoDef,stub+jDeathNoDef+4,stub+deathFinish);
        PatchRel(b,jDeathBadOwner,stub+jDeathBadOwner+4,stub+deathFinish);
        PatchRel(b,jDeathBadLocal,stub+jDeathBadLocal+4,stub+deathFinish);
        PatchRel(b,jDeathKillAll,stub+jDeathKillAll+4,stub+deathKill);
        PatchRel(b,jDeathAllied,stub+jDeathAllied+4,stub+deathFinish);
        PatchRel(b,jDeathKeep,stub+jDeathKeep+4,stub+afterDeath);

        PatchRel(b,jQueueIdle,stub+jQueueIdle+4,stub+restore);
        PatchRel(b,jReplay,stub+jReplay+4,stub+replayStart);
        PatchRel(b,jSpawn,stub+jSpawn+4,stub+spawnStart);
        PatchRel(b,jUnknownRestore,stub+jUnknownRestore+4,stub+restore);
        PatchRel(b,jReplayFinished,stub+jReplayFinished+4,stub+finishQueue);
        PatchRel(b,jReplaySkip,stub+jReplaySkip+4,stub+replaySkip);
        PatchRel(b,jReplayAfterSecond,stub+jReplayAfterSecond+4,stub+replayAfterSecond);
        PatchRel(b,jReplayLoop,stub+jReplayLoop+4,stub+replayLoop);
        PatchRel(b,jReplayKeep,stub+jReplayKeep+4,stub+restore);
        PatchRel(b,jReplayDone,stub+jReplayDone+4,stub+finishQueue);
        PatchRel(b,jSpawnFinished,stub+jSpawnFinished+4,stub+finishQueue);
        PatchRel(b,jSpawnFail,stub+jSpawnFail+4,stub+spawnFailLabel);
        b[jSpawnAfterStat8]=unchecked((byte)(spawnAfterStat-(jSpawnAfterStat8+1)));
        PatchRel(b,jSpawnLoop,stub+jSpawnLoop+4,stub+spawnLoop);
        PatchRel(b,jSpawnKeep,stub+jSpawnKeep+4,stub+restore);
        PatchRel(b,jSpawnDone,stub+jSpawnDone+4,stub+finishQueue);
        PatchRel(b,jBack,stub+jBack+4,moduleBase+RVA_FRAME_MOUSE_DRAW+FrameOriginal.Length);
        return b.ToArray();
    }

    static bool Install()
    {
        if(installed)return true;
        if(!Attach())return false;
        long site=moduleBase+RVA_FRAME_MOUSE_DRAW;
        var now=new byte[FrameOriginal.Length];
        if(!ReadExact(site,now)||!Same(now,FrameOriginal))
        {
            status="FRAME HOOK BUSY/MISMATCH — close other BRZE hook tools and refresh";
            return false;
        }
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)CAVE_SIZE,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
        if(cave==IntPtr.Zero){status="VirtualAllocEx failed";return false;}
        long c=cave.ToInt64();
        if((c+FXSCRATCH)%16!=0){status="FX scratch alignment failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        if(!WriteBytes(c+D_MODE,new byte[0x80])){status="dispatcher state init failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        W32(c+D_LAST_OWNER,0xFFFFFFFFu);W32(c+Q_OUT_ID,0xFFFFu);W32(c+Q_LAST_OUT_ID,0xFFFFu);
        var code=BuildStub(c);
        if(!WriteBytes(c,code)){status="dispatcher stub write failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return false;}
        hookPatch=new byte[6];hookPatch[0]=0xE9;Array.Copy(BitConverter.GetBytes(unchecked((int)(c-(site+5)))),0,hookPatch,1,4);hookPatch[5]=0x90;
        if(!WriteCode(site,hookPatch)){status="dispatcher frame hook patch failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;hookPatch=null;return false;}
        installed=true;status="V20 SHARED FRAME DISPATCHER ACTIVE";return true;
    }

    static bool ValidateApply()
    {
        var b=new byte[ApplyPrologue.Length];
        return ReadExact(moduleBase+RVA_APPLY_TARGET_ABILITY,b)&&Same(b,ApplyPrologue);
    }

    static bool ValidateSpawn()
    {
        var b=new byte[SpawnPrologue.Length];
        return ReadExact(moduleBase+RVA_SPAWN_AT_XY,b)&&Same(b,SpawnPrologue);
    }

    static bool QueueBusy()
    {
        if(!installed||cave==IntPtr.Zero)return false;
        return R32(cave.ToInt64()+Q_TYPE)!=QUEUE_IDLE;
    }

    // ---------------- Instant Death facade ------------------------------------------
    public static bool DeathTriggerSingle(bool killAll)
    {
        lock(Sync)
        {
            if(!Attach()||!Install())return false;
            long c=cave.ToInt64();W32(c+D_TARGET_ALL,killAll?1u:0u);return W32(c+D_MODE,DEATH_SINGLE);
        }
    }

    public static string DeathTick(bool burstEnabled,bool killAll)
    {
        lock(Sync)
        {
            if(!Attach())return "DEATH V20: waiting for Battle_Realms_F.exe...";
            if(burstEnabled)
            {
                if(!Install())return "DEATH V20: ERROR — "+status;
                long c=cave.ToInt64();W32(c+D_TARGET_ALL,killAll?1u:0u);W32(c+D_MODE,DEATH_BURST);
            }
            else if(installed&&cave!=IntPtr.Zero)
            {
                long c=cave.ToInt64();W32(c+D_TARGET_ALL,killAll?1u:0u);
                if(R32(c+D_MODE)==DEATH_BURST)W32(c+D_MODE,DEATH_IDLE);
            }
            if(!installed||cave==IntPtr.Zero)return $"DEATH V20: IDLE | press=single / hold=burst | target:{(killAll?"ALL":"ENEMY")}";
            long x=cave.ToInt64();uint mode=R32(x+D_MODE),u=R32(x+D_LAST_UNIT),owner=R32(x+D_LAST_OWNER),writes=R32(x+D_WRITES);
            string own=owner==0xFFFFFFFFu?"-":owner.ToString();
            string m=mode==DEATH_BURST?"BURST-HOLD":mode==DEATH_SINGLE?"SINGLE-PENDING":"IDLE";
            return $"DEATH V20: {m} | hover:0x{u:X8} owner:{own} writes:{writes} | {(killAll?"ALL-UNITS":"ENEMY-ONLY")} / shared dispatcher";
        }
    }

    // ---------------- Replay V2 facade ----------------------------------------------
    public static string QueueReplaySelected(uint ability1,uint ability2,string label)
    {
        lock(Sync)
        {
            var units=SelectedUnits();
            if(units.Count==0)return $"{label}: no units selected.";
            if(!Attach())return $"{label}: game not attached.";
            if(!ValidateApply())return $"{label}: APPLY helper bytes mismatch.";
            if(!Install())return $"{label}: blocked — {status}";
            long c=cave.ToInt64();
            if(R32(c+Q_TYPE)!=QUEUE_IDLE)return $"{label}: shared native queue busy.";
            var bytes=new byte[units.Count*4];
            for(int i=0;i<units.Count;i++)Buffer.BlockCopy(BitConverter.GetBytes(units[i]),0,bytes,i*4,4);
            if(!WriteBytes(c+ENTRIES,bytes))return $"{label}: queue write failed.";
            bool both=ability2!=uint.MaxValue;
            W32(c+Q_COUNT,(uint)units.Count);W32(c+Q_INDEX,0);W32(c+Q_CALLS,0);W32(c+Q_A1,ability1);W32(c+Q_A2,both?ability2:uint.MaxValue);W32(c+Q_MODE,both?2u:1u);
            W32(c+Q_SUCCESS,0);W32(c+Q_FAIL,0);W32(c+Q_TYPE,QUEUE_REPLAY);
            lastQueue=$"{label}: queued {units.Count} selected";
            return lastQueue;
        }
    }

    // ---------------- Copy Unit facade ----------------------------------------------
    public static IntegratedCopyResult CopySelection()
    {
        lock(Sync)
        {
            if(!Attach())return new(false,"COPY failed — game not attached.");
            long list=moduleBase+RVA_SELECTION_LIST;uint count=R32(list+0x18),node=R32(list);
            if(count==0||node==0){copied.Clear();copyStatus="Nothing selected.";return new(false,copyStatus);}
            var tmp=new List<IntegratedUnitSnap>();var seenNodes=new HashSet<uint>();var seenUnits=new HashSet<uint>();
            int limit=(int)Math.Min(count,MAX_COPY);
            for(int i=0;i<limit&&node!=0;i++)
            {
                if(!seenNodes.Add(node))break;
                uint next=R32((long)node),unit=R32((long)node+0x08);
                if(unit!=0&&seenUnits.Add(unit))
                {
                    uint def=R32((long)unit+OFF_DEF);uint type=def==0?0xFFFFFFFFu:R32(def);uint owner=R32((long)unit+OFF_OWNER);
                    float x=RF32((long)unit+OFF_X),y=RF32((long)unit+OFF_Y);
                    if(def!=0&&type!=0xFFFFFFFFu&&!float.IsNaN(x)&&!float.IsNaN(y)&&!float.IsInfinity(x)&&!float.IsInfinity(y))tmp.Add(new(unit,type,owner,x,y));
                }
                node=next;
            }
            copied.Clear();copied.AddRange(tmp);pasteSerial=0;
            if(copied.Count==0){copyStatus="Selection resolved, but no valid units were captured.";return new(false,copyStatus);}
            var groups=copied.GroupBy(x=>new{x.Type,x.Owner}).Select(g=>$"type 0x{g.Key.Type:X} owner {g.Key.Owner} ×{g.Count()}").Take(5).ToArray();
            copyStatus=$"COPIED {copied.Count}/{count} selected units · heroes/unique/story allowed\r\n"+string.Join("  |  ",groups)+(copied.GroupBy(x=>new{x.Type,x.Owner}).Count()>5?"  |  ...":"");
            return new(true,copyStatus);
        }
    }

    public static string PasteCopied(float sideOffset)
    {
        lock(Sync)
        {
            if(copied.Count==0)return "PASTE ignored — COPY SELECTED first.";
            if(!Attach())return "PASTE blocked — game not attached.";
            if(!ValidateSpawn())return "PASTE blocked — spawn wrapper bytes mismatch.";
            if(!Install())return "PASTE blocked — "+status;
            long c=cave.ToInt64();
            if(R32(c+Q_TYPE)!=QUEUE_IDLE)return "PASTE busy — shared native queue is active.";
            pasteSerial++;float dx=sideOffset*pasteSerial;int n=Math.Min(copied.Count,MAX_COPY);
            var bytes=new byte[n*SPAWN_ENTRY_SIZE];
            for(int i=0;i<n;i++)
            {
                var s=copied[i];int o=i*SPAWN_ENTRY_SIZE;
                Buffer.BlockCopy(BitConverter.GetBytes(s.Type),0,bytes,o,4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.Owner),0,bytes,o+4,4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.X+dx),0,bytes,o+8,4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.Y),0,bytes,o+12,4);
            }
            if(!WriteBytes(c+ENTRIES,bytes))return "PASTE failed — queue write failed.";
            W32(c+Q_COUNT,(uint)n);W32(c+Q_INDEX,0);W32(c+Q_SUCCESS,0);W32(c+Q_FAIL,0);W32(c+Q_OUT_ID,0xFFFFu);W32(c+Q_LAST_OUT_ID,0xFFFFu);W32(c+Q_CALLS,0);
            W32(c+Q_TYPE,QUEUE_SPAWN);
            lastQueue=$"COPY UNIT paste #{pasteSerial}: {n} units, +X {dx:0.0}";
            return lastQueue;
        }
    }

    public static void ClearCopied()
    {
        lock(Sync)
        {
            if(QueueBusy())return;
            copied.Clear();copyStatus="No copied selection yet.";pasteSerial=0;
        }
    }

    public static IntegratedFrameSnapshot Snapshot()
    {
        lock(Sync)
        {
            if(!Attach())return new("GAME: waiting for Battle_Realms_F.exe",lastQueue,copyStatus);
            string g=$"GAME: {status}";
            if(!installed||cave==IntPtr.Zero)return new(g,lastQueue,copyStatus);
            long c=cave.ToInt64();uint type=R32(c+Q_TYPE),count=R32(c+Q_COUNT),index=R32(c+Q_INDEX),calls=R32(c+Q_CALLS),ok=R32(c+Q_SUCCESS),fail=R32(c+Q_FAIL),id=R32(c+Q_LAST_OUT_ID);
            string q=type==QUEUE_REPLAY?$"QUEUE: REPLAY {(type!=0?"ACTIVE":"DONE")} {index}/{count} · native calls:{calls}":type==QUEUE_SPAWN?$"QUEUE: COPY UNIT ACTIVE {index}/{count} · success:{ok} fail:{fail} last:0x{id:X4}":$"QUEUE: IDLE · last: {lastQueue}";
            return new(g,q,copyStatus);
        }
    }

    public static bool NativeQueueBusy(){lock(Sync){return Attach()&&installed&&cave!=IntPtr.Zero&&R32(cave.ToInt64()+Q_TYPE)!=QUEUE_IDLE;}}

    public static void StopAll()
    {
        lock(Sync)DetachRuntime(true);
    }

    static void DetachRuntime(bool clearCopy)
    {
        try
        {
            if(h!=IntPtr.Zero&&installed&&hookPatch!=null)
            {
                long site=moduleBase+RVA_FRAME_MOUSE_DRAW;var now=new byte[hookPatch.Length];
                if(ReadExact(site,now)&&Same(now,hookPatch))WriteCode(site,FrameOriginal);
            }
            if(h!=IntPtr.Zero&&cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);
            if(h!=IntPtr.Zero)CloseHandle(h);
        }
        catch{}
        process=null;h=IntPtr.Zero;cave=IntPtr.Zero;moduleBase=0;installed=false;hookPatch=null;status="not attached";lastTry=DateTime.MinValue;lastQueue="QUEUE: idle";
        if(clearCopy){copied.Clear();copyStatus="No copied selection yet.";pasteSerial=0;}
    }
}