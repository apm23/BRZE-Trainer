using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BRZEHeroEffectCurrentTickResetV29;

internal readonly record struct ResetResult(bool Ok,string Summary,string Report);

internal static class CurrentTickResetCore
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x0010|0x0020|0x0008|0x0400;
    const int RVA_SELECTION_LIST=0x441708;
    const int RVA_CURRENT_TICK=0x440A3C;
    const int OFF_DEF=0x74,OFF_OWNER=0x240;
    static readonly int[] ROOTS={0x1E4,0x1E8,0x20C,0x210};
    const int ROOT_SCAN=0x600;
    const int ABILITY_OFF=0x58,TARGET_OFF=0x17C,STAMP_OFF=0x194,CONFIG_OFF=0x1F4;
    const uint A5=0xA5,BASE_DURATION=15000;

    static Process? process;
    static IntPtr h=IntPtr.Zero;
    static long moduleBase;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));
    static bool Ptr(uint p)=>p>=0x00010000&&p<0x7FFF0000;
    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,A(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool W32(long a,uint v)
    {
        var b=BitConverter.GetBytes(v);
        return h!=IntPtr.Zero&&WriteProcessMemory(h,A(a),b,4,out var n)&&n.ToInt64()==4&&R32(a)==v;
    }

    static bool Attach(out string error)
    {
        error="";Detach();
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0){error="Battle_Realms_F.exe is not running";return false;}
        process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}
        catch{error="cannot resolve module base";process=null;return false;}
        h=OpenProcess(ACCESS,false,process.Id);
        if(h==IntPtr.Zero){error="OpenProcess READ/WRITE failed";process=null;moduleBase=0;return false;}
        return true;
    }

    static void Detach()
    {
        try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}
        h=IntPtr.Zero;process=null;moduleBase=0;
    }

    static (uint unit,uint count) FirstSelected()
    {
        var b=new byte[0x1C];
        if(!ReadExact(moduleBase+RVA_SELECTION_LIST,b))return default;
        uint node=BitConverter.ToUInt32(b,0),count=BitConverter.ToUInt32(b,0x18);
        if(node==0||count==0)return (0,count);
        var nb=new byte[0x0C];if(!ReadExact(node,nb))return (0,count);
        return (BitConverter.ToUInt32(nb,8),count);
    }

    static bool Signature(uint addr,uint unit)=>Ptr(addr)&&R32((long)addr+ABILITY_OFF)==A5&&R32((long)addr+TARGET_OFF)==unit;

    static bool FindA5(uint unit,out uint record,out string path)
    {
        record=0;path="not found";var seen=new HashSet<uint>();
        foreach(int ro in ROOTS)
        {
            uint root=R32((long)unit+ro);
            if(!Ptr(root)||!seen.Add(root))continue;
            if(Signature(root,unit)){record=root;path=$"Unit+0x{ro:X3}";return true;}
            var b=new byte[ROOT_SCAN];if(!ReadExact(root,b))continue;
            for(int o=0;o<=b.Length-4;o+=4)
            {
                uint p=BitConverter.ToUInt32(b,o);
                if(!Ptr(p)||!seen.Add(p))continue;
                if(Signature(p,unit)){record=p;path=$"Unit+0x{ro:X3}->+0x{o:X3}";return true;}
            }
        }
        return false;
    }

    public static ResetResult Run()
    {
        var sb=new StringBuilder();
        try
        {
            if(!Attach(out string err))return new(false,err,err);
            var (unit,count)=FirstSelected();
            if(count!=1||unit==0)return new(false,$"Select exactly ONE unit with ACTIVE Issyl (selected {count}).","");
            uint def=R32((long)unit+OFF_DEF),owner=R32((long)unit+OFF_OWNER);
            if(def==0)return new(false,"Selected object has no UnitDef.","");
            if(!FindA5(unit,out uint record,out string path))return new(false,"No active A5+target signature found on selected unit.","");

            uint config=R32((long)record+CONFIG_OFF);
            uint cfgId=Ptr(config)?R32(config):0;
            uint duration=Ptr(config)?R32((long)config+0xE0):0;
            uint oldStamp=R32((long)record+STAMP_OFF);
            uint currentTick=R32(moduleBase+RVA_CURRENT_TICK);
            long elapsed=(long)currentTick-oldStamp;

            sb.AppendLine("=== BRZE HERO EFFECT CURRENT-TICK RESET V29 ===");
            sb.AppendLine("MODE: GUARDED ONE-FIELD WRITE PROOF — DIRECT BRZE CLOCK, NO NATIVE REPLAY / NO STACKING");
            sb.AppendLine($"PID={process!.Id} moduleBase=0x{moduleBase:X8}");
            sb.AppendLine($"Selected Unit*=0x{unit:X8} UnitDef*=0x{def:X8} owner={owner}");
            sb.AppendLine($"A5 record=0x{record:X8} via {path}");
            sb.AppendLine($"signature: ability=0x{R32((long)record+ABILITY_OFF):X8} target=0x{R32((long)record+TARGET_OFF):X8}");
            sb.AppendLine($"config=0x{config:X8} configID=0x{cfgId:X8} duration={duration}");
            sb.AppendLine($"proven current-tick source: module+0x{RVA_CURRENT_TICK:X6} = 0x{moduleBase+RVA_CURRENT_TICK:X8}");
            sb.AppendLine($"old record+0x194 start timestamp={oldStamp}");
            sb.AppendLine($"current BRZE tick={currentTick}");
            sb.AppendLine($"pre-reset elapsed tick delta={elapsed}");

            if(cfgId!=A5)return new(false,"GUARD BLOCK — config identity is not A5.",sb.ToString());
            if(duration!=BASE_DURATION)return new(false,$"GUARD BLOCK — first proof requires stock A5 duration {BASE_DURATION}, found {duration}.",sb.ToString());
            if(oldStamp==0)return new(false,"GUARD BLOCK — start timestamp is zero; effect is not in a normal active state.",sb.ToString());
            if(currentTick==0)return new(false,"GUARD BLOCK — current BRZE tick source returned zero.",sb.ToString());
            if(currentTick<=oldStamp)return new(false,$"GUARD BLOCK — current BRZE tick {currentTick} is not newer than effect start {oldStamp}.",sb.ToString());
            if(elapsed<100)return new(false,$"GUARD BLOCK — effect has only advanced {elapsed} ticks; wait a little before reset proof.",sb.ToString());
            if(elapsed>60000)return new(false,$"GUARD BLOCK — elapsed delta {elapsed} is implausibly large for guarded first proof.",sb.ToString());
            if(!Signature(record,unit))return new(false,"GUARD BLOCK — A5+target signature changed before write.",sb.ToString());
            if(R32(moduleBase+RVA_CURRENT_TICK)!=currentTick)return new(false,"GUARD BLOCK — BRZE current-tick source changed during pre-write validation; retry once while game is stable/paused.",sb.ToString());

            sb.AppendLine($"ACTION: write ONLY record+0x194 = current BRZE tick ({currentTick}).");
            if(!W32((long)record+STAMP_OFF,currentTick))return new(false,"WRITE FAILED — exact timestamp write/readback verification failed.",sb.ToString());

            uint post=R32((long)record+STAMP_OFF);
            bool sigOk=Signature(record,unit);
            uint tickAfter=R32(moduleBase+RVA_CURRENT_TICK);
            sb.AppendLine($"post-write record+0x194={post}");
            sb.AppendLine($"post-write same A5+target signature={sigOk}");
            sb.AppendLine($"current BRZE tick after write={tickAfter}");

            if(post!=currentTick)
            {
                sb.AppendLine("FAIL: record timestamp does not equal the proven current-tick value after write.");
                return new(false,"FAIL — direct current-tick reset did not stick. Restart/reload before more testing.",sb.ToString());
            }
            if(!sigOk)
            {
                sb.AppendLine("FAIL: A5 record signature changed immediately after timestamp write.");
                return new(false,"FAIL — effect signature changed after reset. Restart/reload before more testing.",sb.ToString());
            }

            sb.AppendLine("WRITE PROOF PASS: same active A5 instance now starts at the exact BRZE tick source used by the native tick caller chain.");
            sb.AppendLine("VISUAL PROOF REQUIRED: return to the game and confirm Issyl remains active for approximately one fresh stock lifetime from this reset moment.");
            sb.AppendLine("No native ability helper was called and no second effect instance was created by this tool.");
            return new(true,$"WRITE PASS — A5 start {oldStamp} -> current BRZE tick {currentTick}; verify fresh lifetime visually.",sb.ToString());
        }
        catch(Exception ex){sb.AppendLine(ex.ToString());return new(false,"V29 error: "+ex.Message,sb.ToString());}
        finally{Detach();}
    }
}
