using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BRZETrainer;

internal static class Program
{
    [STAThread] static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new MainForm()); }
}

internal sealed class MainForm : Form
{
    readonly CheckBox f1 = new() { Text = "F1 Infinite Rice", AutoSize = true };
    readonly CheckBox f2 = new() { Text = "F2 Infinite Water", AutoSize = true };
    readonly CheckBox f3 = new() { Text = "F3 Infinite Yin + Yang", AutoSize = true };
    readonly CheckBox f4 = new() { Text = "F4 Unlimited Population (9,999,999)", AutoSize = true };
    readonly CheckBox f5 = new() { Text = "F5 No Stamina Loss (selected only) — SAFE DELTA V62", AutoSize = true };
    readonly CheckBox f6 = new() { Text = "F6 No Damage (selected only) — SAFE DELTA V62", AutoSize = true };
    readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training — LEGACY F4 EXACT REMAP", AutoSize = true };
    readonly CheckBox legacyTower = new() { Text = "Legacy Infinite Watchtowers — experimental remap slot", AutoSize = true };
    readonly CheckBox pausePeasant = new() { Text = "Legacy F9 Pause Peasant Production (F12)", AutoSize = true };
    readonly CheckBox demolish = new() { Text = "Demolition Mode (F11) — BRZE native enable flag", AutoSize = true };
    readonly CheckBox horses = new() { Text = "Maximum Horses / instant horse respawn", AutoSize = true };
    readonly Label status = new() { AutoSize = false, Height = 54, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    readonly bool[] held = new bool[20];

    public MainForm()
    {
        Text = "BRZE Trainer 1.60 — Hook Test"; ClientSize = new Size(650, 520); StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
        panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6, f7, legacyTower, pausePeasant, demolish, horses });
        panel.Controls.Add(new Label { Text = "F7 building skills/upgrades — not enabled yet", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F8 Instant Build/Repair/Research — PUSH TEST (selected building)", AutoSize = true });
        panel.Controls.Add(new Label { Text = "F9 Enable / Disable ALL implemented cheats", AutoSize = true });
        panel.Controls.Add(new Label { Text = "F10 Maximum Wolves — old trainer exact +0x250 remap", AutoSize = true });
        panel.Controls.Add(new Label { Text = "Delete = Instant Build/Repair/Research/BattleGear | PageDown = Instant Death", AutoSize = true });
        panel.Controls.Add(new Label { Text = "PageUp = toggle Health + Stamina together (old trainer behavior)", AutoSize = true });
        Controls.Add(panel); Controls.Add(status);
        Native.Start(); timer.Tick += (_, _) => TickTrainer(); timer.Start(); FormClosed += (_, _) => Native.Stop();
    }

    void Toggle(int vk, int idx, Action action) { bool now = (Native.GetAsyncKeyState(vk) & 0x8000) != 0; if (now && !held[idx]) action(); held[idx] = now; }
    void SetAllImplemented(bool e) { f1.Checked=e; f2.Checked=e; f3.Checked=e; f4.Checked=e; f5.Checked=e; f6.Checked=e; f7.Checked=e; }
    void TickTrainer()
    {
        Toggle(0x70,1,()=>f1.Checked=!f1.Checked); Toggle(0x71,2,()=>f2.Checked=!f2.Checked); Toggle(0x72,3,()=>f3.Checked=!f3.Checked);
        Toggle(0x73,4,()=>f4.Checked=!f4.Checked); Toggle(0x74,5,()=>f5.Checked=!f5.Checked); Toggle(0x75,6,()=>f6.Checked=!f6.Checked);
        Toggle(0x76,7,()=>f7.Checked=!f7.Checked); Toggle(0x77,8,()=>Native.InstantSelectedBuilding());
        Toggle(0x2E,11,()=>Native.InstantSelectedBuilding());
        Toggle(0x22,12,()=>Native.InstantDeathSelected());
        Toggle(0x21,13,()=>{bool n=!(f5.Checked&&f6.Checked);f5.Checked=n;f6.Checked=n;});
        Toggle(0x7A,14,()=>demolish.Checked=!demolish.Checked);
        Toggle(0x7B,15,()=>pausePeasant.Checked=!pausePeasant.Checked);
        Toggle(0x78,9,()=>{ bool all=f1.Checked&&f2.Checked&&f3.Checked&&f4.Checked&&f5.Checked&&f6.Checked&&f7.Checked; SetAllImplemented(!all); }); Toggle(0x79,10,()=>Native.MaxWolves());
        Native.SetHooks(f5.Checked, f6.Checked, f7.Checked);
        Native.ApplyLegacyRuntime(pausePeasant.Checked,demolish.Checked,horses.Checked,legacyTower.Checked);
        status.Text = Native.Apply(f1.Checked,f2.Checked,f3.Checked,f4.Checked,f7.Checked);
    }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint allocationType,uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr addr,UIntPtr size,uint newProtect,out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);

    const uint Access=0x10|0x20|0x8|0x400; const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_EXECUTE_READWRITE=0x40;
    const int RVA_PLAYER_PTR=0x4416A0,RVA_LOCAL_ID=0x4416D0,PLAYER_STRIDE=0x5E8,OFF_RICE=0xD8,OFF_WATER=0xDC,OFF_YIN=0x2E8,OFF_YANG=0x2EC,RVA_MAX_UNITS=0x467B90;
    const int RVA_UNIT_POOL=0x4796A0,UNIT_STRIDE=0x818,UNIT_COUNT=2000,OFF_DEF=0x74,OFF_OWNER=0x240,OFF_SEL_A=0x3A8,OFF_SEL_B=0x3AC,OFF_HP=0x404,OFF_ST=0x408,RVA_SELECTION_LIST=0x441708,RVA_SELECTED_BUILDING_A=0x4417D4,RVA_SELECTED_BUILDING_B=0x4417D8;
    const int RVA_ADD_HEALTH=0x1CCD4D,RVA_ADD_STAMINA=0x1CCDFB,RVA_TRAIN_PROGRESS_READ=0x0D5DDB;
    const int RVA_SELECT_ONE=0x1A70C6,RVA_SELECT_BOX=0x1A78CC;
    const int RVA_PEASANT_CREATION=0x467AF4,RVA_DEMOLISH_ENABLE=0x3D7A1C;
    const int RVA_CFG_BASE_PTR=0x43FF2C,RVA_CFG_INDEX_PTR=0x440034;
    const int RVA_BUILDING_POOL=0x4814E0,BUILDING_STRIDE=0x6A4,BUILDING_COUNT=500,OFF_BUILD_OWNER=0x84,OFF_TRAIN_TYPE=0x488,OFF_TRAIN_PROGRESS=0x490,OFF_TRAIN_GATE=0x4B8,OFF_BUILD_SPECIAL=0x68C,RVA_TRAIN_SPECIAL_GLOBAL=0x46779C;
    const uint TRAIN_COMPLETE_FIXED=0x00640000;

    static readonly object attachLock=new(); static IntPtr h=IntPtr.Zero; static Process? p; static long moduleBase;
    static volatile uint localId; static volatile bool wantStamina,wantHp,running; static Thread? topupThread;
    static DateTime lastUnitCache=DateTime.MinValue,lastBuildingScan=DateTime.MinValue; static UnitInfo[] localUnits=Array.Empty<UnitInfo>();
    static readonly byte[] unitRaw=new byte[UNIT_COUNT*UNIT_STRIDE],buildingRaw=new byte[BUILDING_COUNT*BUILDING_STRIDE];
    static int selectedLocked,trainingBuildings; static IntPtr cave=IntPtr.Zero; static long hpFlag,stFlag,trainFlag; static bool hooksInstalled; static uint horseOriginal; static bool horseSaved; static int remoteTrainState=-1; static string hookError="";
    static readonly byte[] HpOriginal={0x55,0x8B,0xEC,0x83,0xE4,0xF8,0x56,0x8B,0xF1,0x57};
    static readonly byte[] StOriginal={0x55,0x8B,0xEC,0x56,0x8B,0xF1,0x57,0x56};
    static readonly byte[] SelectOriginal={0xC7,0x86,0xA8,0x03,0x00,0x00,0x01,0x00,0x00,0x00}; static readonly byte[] TrainOriginal={0x8B,0x83,0x90,0x04,0x00,0x00};

    readonly struct UnitInfo { public readonly long Addr; public readonly uint MaxHp,MaxSt; public UnitInfo(long a,uint hp,uint st){Addr=a;MaxHp=hp;MaxSt=st;} }
    // Old trainer PageUp did its HP/stamina work in injected game-side code instead of
    // repeatedly walking every selected unit through ReadProcessMemory/WriteProcessMemory.
    // Our central delta hooks are already event-driven, so external top-up polling is removed.
    public static void Start(){ running=true; }
    public static void Stop(){ running=false; Detach(); }

    static bool ReadExact(long a,byte[] b){IntPtr hh=h;return hh!=IntPtr.Zero&&ReadProcessMemory(hh,new IntPtr(unchecked((int)(uint)a)),b,b.Length,out var n)&&n.ToInt64()==b.Length;}
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b){IntPtr hh=h;return hh!=IntPtr.Zero&&WriteProcessMemory(hh,new IntPtr(unchecked((int)(uint)a)),b,b.Length,out var n)&&n.ToInt64()==b.Length;}
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
    static bool W8(long a,byte v)=>WriteBytes(a,new byte[]{v});
    static uint Fixed16(uint v){ulong x=((ulong)v)<<16;return x>uint.MaxValue?uint.MaxValue:(uint)x;}

    static bool Attach()
    {
        lock(attachLock){try{if(p!=null&&!p.HasExited&&h!=IntPtr.Zero)return true;}catch{} DetachUnlocked();
            var ps=Process.GetProcessesByName("Battle_Realms_F");if(ps.Length==0)return false;p=ps[0];try{moduleBase=p.MainModule!.BaseAddress.ToInt64();}catch{p=null;return false;}
            h=OpenProcess(Access,false,p.Id);lastUnitCache=DateTime.MinValue;lastBuildingScan=DateTime.MinValue;localUnits=Array.Empty<UnitInfo>();hooksInstalled=false;cave=IntPtr.Zero;
            return h!=IntPtr.Zero;}
    }
    public static void Detach(){lock(attachLock)DetachUnlocked();}
    static void DetachUnlocked()
    {
        if(h!=IntPtr.Zero){ if(hooksInstalled){WriteCode(moduleBase+RVA_ADD_HEALTH,HpOriginal);WriteCode(moduleBase+RVA_ADD_STAMINA,StOriginal);WriteCode(moduleBase+RVA_TRAIN_PROGRESS_READ,TrainOriginal);WriteCode(moduleBase+RVA_SELECT_ONE,SelectOriginal);WriteCode(moduleBase+RVA_SELECT_BOX,SelectOriginal);} if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE); CloseHandle(h);}
        h=IntPtr.Zero;p=null;cave=IntPtr.Zero;hooksInstalled=false;localUnits=Array.Empty<UnitInfo>();remoteTrainState=-1;hookError="";
    }
    static bool WriteCode(long addr,byte[] data){if(h==IntPtr.Zero)return false;VirtualProtectEx(h,new IntPtr(addr),(UIntPtr)data.Length,PAGE_EXECUTE_READWRITE,out uint old);bool ok=WriteBytes(addr,data);FlushInstructionCache(h,new IntPtr(addr),(UIntPtr)data.Length);VirtualProtectEx(h,new IntPtr(addr),(UIntPtr)data.Length,old,out _);return ok;}
    static void I32(List<byte>b,int v){b.AddRange(BitConverter.GetBytes(v));} static void U32(List<byte>b,uint v){b.AddRange(BitConverter.GetBytes(v));}
    static void PatchRel(List<byte>b,int at,long fromNext,long to){var x=BitConverter.GetBytes(unchecked((int)(to-fromNext)));for(int i=0;i<4;i++)b[at+i]=x[i];}

    static byte[] BuildHook(long stub,long flag,long target,byte[] original,int backOffset)
    {
        // Safe event hook: preserve the original function prologue.  If the feature is
        // enabled and this call is a negative delta for a selected local unit, return
        // without applying it. Positive deltas/healing remain native. No sentinel writes.
        var b=new List<byte>();
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)flag);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jDisabled=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x7C,0x24,0x04,0x00}); // cmp dword [esp+4],0
        b.AddRange(new byte[]{0x0F,0x8D});int jNonNeg=b.Count;I32(b,0);
        b.Add(0x50); // preserve eax
        b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));
        b.AddRange(new byte[]{0x39,0x81,0x40,0x02,0x00,0x00}); // cmp [ecx+240],eax
        b.AddRange(new byte[]{0x0F,0x85});int jOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB9,0xA8,0x03,0x00,0x00,0x01});
        b.AddRange(new byte[]{0x0F,0x85});int jSelA=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB9,0xAC,0x03,0x00,0x00,0x01});
        b.AddRange(new byte[]{0x0F,0x85});int jSelB=b.Count;I32(b,0);
        b.Add(0x58);b.AddRange(new byte[]{0xC2,0x04,0x00}); // block negative delta
        int popOriginal=b.Count;b.Add(0x58);
        int originalLabel=b.Count;b.AddRange(original);b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jDisabled,stub+jDisabled+4,stub+originalLabel);
        PatchRel(b,jNonNeg,stub+jNonNeg+4,stub+originalLabel);
        PatchRel(b,jOwner,stub+jOwner+4,stub+popOriginal);
        PatchRel(b,jSelA,stub+jSelA+4,stub+popOriginal);
        PatchRel(b,jSelB,stub+jSelB+4,stub+popOriginal);
        PatchRel(b,jBack,stub+jBack+4,target+backOffset);
        return b.ToArray();
    }
    static byte[] BuildTrainingHook(long stub,long flag,long target)
    {
        var b=new List<byte>();
        // Old trainer F4 used a dedicated training progress hook. BRZE 1.60 equivalent at 0x4D5DDB:
        //   mov eax,[ebx+0x490] ; add eax,esi ; mov [ebx+0x490],eax ; cmp eax,0x640000
        // If enabled for our building, force +0x490 to 0x64B540 immediately before that read.
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)flag);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jDisabled=b.Count;I32(b,0);
        b.Add(0x52); // push edx
        b.AddRange(new byte[]{0x8B,0x15});U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // mov edx,[localId]
        b.AddRange(new byte[]{0x39,0x93,0x84,0x00,0x00,0x00}); // cmp [ebx+84],edx
        b.Add(0x5A); // pop edx
        b.AddRange(new byte[]{0x0F,0x85});int jNotOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xC7,0x83,0x90,0x04,0x00,0x00,0x40,0xB5,0x64,0x00}); // [ebx+490]=0x64B540
        int originalLabel=b.Count;b.AddRange(TrainOriginal);
        b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jDisabled,stub+jDisabled+4,stub+originalLabel);
        PatchRel(b,jNotOwner,stub+jNotOwner+4,stub+originalLabel);
        PatchRel(b,jBack,stub+jBack+4,target+TrainOriginal.Length);
        return b.ToArray();
    }

    static byte[] BuildSelectionRefillHook(long stub,long target)
    {
        var b=new List<byte>();
        b.AddRange(SelectOriginal); // preserve selected flag write
        b.Add(0x50); // eax
        b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));
        b.AddRange(new byte[]{0x39,0x86,0x40,0x02,0x00,0x00}); // owner
        b.AddRange(new byte[]{0x0F,0x85});int jDone=b.Count;I32(b,0);
        // HP: if hpFlag != 0, real max = [def+6c]<<16
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)hpFlag);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jSt=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x46,0x74,0x8B,0x40,0x6C,0xC1,0xE0,0x10,0x89,0x86,0x04,0x04,0x00,0x00});
        int staminaLabel=b.Count;
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)stFlag);b.Add(0);
        b.AddRange(new byte[]{0x0F,0x84});int jDone2=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x8B,0x46,0x74,0x8B,0x80,0x80,0x00,0x00,0x00,0xC1,0xE0,0x10,0x89,0x86,0x08,0x04,0x00,0x00});
        int done=b.Count;b.Add(0x58);b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jDone,stub+jDone+4,stub+done);PatchRel(b,jSt,stub+jSt+4,stub+staminaLabel);PatchRel(b,jDone2,stub+jDone2+4,stub+done);PatchRel(b,jBack,stub+jBack+4,target+SelectOriginal.Length);
        return b.ToArray();
    }

    static byte[] JmpPatch(long target,long stub,int len){var b=new byte[len];b[0]=0xE9;Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,b,1,4);for(int i=5;i<len;i++)b[i]=0x90;return b;}

    static bool EnsureHooks()
    {
        if(hooksInstalled)return true;if(!Attach())return false;
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)1536,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);if(cave==IntPtr.Zero)return false;
        long c=cave.ToInt64();hpFlag=c;stFlag=c+4;trainFlag=c+8;long hpStub=c+32,stStub=c+224,trainStub=c+416,sel1Stub=c+640,sel2Stub=c+896;
        long hpTarget=moduleBase+RVA_ADD_HEALTH,stTarget=moduleBase+RVA_ADD_STAMINA,trainTarget=moduleBase+RVA_TRAIN_PROGRESS_READ,s1=moduleBase+RVA_SELECT_ONE,s2=moduleBase+RVA_SELECT_BOX;
        var hn=new byte[HpOriginal.Length];var sn=new byte[StOriginal.Length];var tn=new byte[TrainOriginal.Length];var q1=new byte[SelectOriginal.Length];var q2=new byte[SelectOriginal.Length];
        if(!ReadExact(hpTarget,hn)||!System.Linq.Enumerable.SequenceEqual(hn,HpOriginal)){hookError="F6 byte mismatch";return false;}
        if(!ReadExact(stTarget,sn)||!System.Linq.Enumerable.SequenceEqual(sn,StOriginal)){hookError="F5 byte mismatch";return false;}
        if(!ReadExact(trainTarget,tn)||!System.Linq.Enumerable.SequenceEqual(tn,TrainOriginal)){hookError="F7 byte mismatch";return false;}
        if(!ReadExact(s1,q1)||!System.Linq.Enumerable.SequenceEqual(q1,SelectOriginal)||!ReadExact(s2,q2)||!System.Linq.Enumerable.SequenceEqual(q2,SelectOriginal)){hookError="selection byte mismatch";return false;}
        W32(hpFlag,0);W32(stFlag,0);W32(trainFlag,0);
        byte[] hs=BuildHook(hpStub,hpFlag,hpTarget,HpOriginal,HpOriginal.Length),ss=BuildHook(stStub,stFlag,stTarget,StOriginal,StOriginal.Length),ts=BuildTrainingHook(trainStub,trainFlag,trainTarget),a=BuildSelectionRefillHook(sel1Stub,s1),b=BuildSelectionRefillHook(sel2Stub,s2);
        if(!WriteBytes(hpStub,hs)||!WriteBytes(stStub,ss)||!WriteBytes(trainStub,ts)||!WriteBytes(sel1Stub,a)||!WriteBytes(sel2Stub,b)){hookError="cave write failed";return false;}
        if(!WriteCode(hpTarget,JmpPatch(hpTarget,hpStub,HpOriginal.Length))||!WriteCode(stTarget,JmpPatch(stTarget,stStub,StOriginal.Length))||!WriteCode(trainTarget,JmpPatch(trainTarget,trainStub,TrainOriginal.Length))||!WriteCode(s1,JmpPatch(s1,sel1Stub,SelectOriginal.Length))||!WriteCode(s2,JmpPatch(s2,sel2Stub,SelectOriginal.Length))){hookError="hook install failed";return false;}
        hooksInstalled=true;remoteTrainState=0;hookError="";return true;
    }
    public static void SetHooks(bool stamina,bool hp,bool training){wantStamina=stamina;wantHp=hp;if(!Attach())return;if((stamina||hp||training)&&EnsureHooks()){W32(hpFlag,hp?1u:0u);W32(stFlag,stamina?1u:0u);int t=training?1:0;if(remoteTrainState!=t&&W32(trainFlag,(uint)t))remoteTrainState=t;}else if(hooksInstalled){W32(hpFlag,0);W32(stFlag,0);if(remoteTrainState!=0&&W32(trainFlag,0))remoteTrainState=0;}}

    static bool RefreshUnits(bool force=false)
    {
        if(!Attach())return false;if(!force&&(DateTime.UtcNow-lastUnitCache).TotalMilliseconds<1000)return true;uint lid=R32(moduleBase+RVA_LOCAL_ID),pool=R32(moduleBase+RVA_UNIT_POOL);if(pool==0||!ReadExact(pool,unitRaw))return false;
        var found=new List<UnitInfo>(256);for(int i=0;i<UNIT_COUNT;i++){int o=i*UNIT_STRIDE;uint def=BitConverter.ToUInt32(unitRaw,o+OFF_DEF);if(def==0||BitConverter.ToUInt32(unitRaw,o+OFF_OWNER)!=lid)continue;uint mh=R32((long)def+0x6C),ms=R32((long)def+0x80);if(mh==0&&ms==0)continue;found.Add(new UnitInfo((long)pool+o,Fixed16(mh),Fixed16(ms)));}
        localId=lid;localUnits=found.ToArray();lastUnitCache=DateTime.UtcNow;return true;
    }
    // Intentionally no external selected-unit HP/stamina walker.
    // F5/F6 now cost work only when the game applies stamina/health deltas.

    public static void ApplyLegacyRuntime(bool pause,bool demo,bool horse,bool tower)
    {
        if(!Attach())return;uint lid=R32(moduleBase+RVA_LOCAL_ID);
        // Current exports prove 0 disables and 1 enables peasant creation at [0x867AF4+player*4].
        W32(moduleBase+RVA_PEASANT_CREATION+lid*4,pause?0u:1u);
        // EnableDemolish export writes its argument directly to 0x7D7A1C.
        W32(moduleBase+RVA_DEMOLISH_ENABLE,demo?1u:0u);
        uint cfg=R32(moduleBase+RVA_CFG_BASE_PTR),ip=R32(moduleBase+RVA_CFG_INDEX_PTR);uint ix=ip==0?0:R32(ip);
        if(cfg!=0&&ip!=0){long a=(long)cfg+ix*0x98+0x48;if(horse){if(!horseSaved){horseOriginal=R32(a);horseSaved=true;}W32(a,0);}else if(horseSaved){W32(a,horseOriginal);horseSaved=false;}}
        // Infinite Watchtowers remains isolated until its BRZE counter is runtime-confirmed; never guess-write an address.
    }
    public static void InstantDeathSelected()
    {
        if(!Attach())return;uint lid=R32(moduleBase+RVA_LOCAL_ID);long list=moduleBase+RVA_SELECTION_LIST;uint node=R32(list);int count=(int)Math.Min(R32(list+0x18),512u);
        var seen=new HashSet<uint>();for(int i=0;i<count&&node!=0&&seen.Add(node);i++){uint unit=R32((long)node+8);if(unit!=0&&R32((long)unit+OFF_OWNER)==lid){W32((long)unit+OFF_HP,0);W32((long)unit+OFF_ST,0);}node=R32(node);}
    }

    public static void InstantSelectedBuilding()
    {
        if(!Attach())return; uint lid=R32(moduleBase+RVA_LOCAL_ID);
        uint a=R32(moduleBase+RVA_SELECTED_BUILDING_A),b=R32(moduleBase+RVA_SELECTED_BUILDING_B);
        Boost(a,lid); if(b!=a)Boost(b,lid);
    }

    public static void MaxWolves()
    {
        if(!Attach())return;
        uint lid=R32(moduleBase+RVA_LOCAL_ID);
        uint obj=R32(moduleBase+RVA_SELECTED_BUILDING_A);
        if(obj==0||R32((long)obj+OFF_BUILD_OWNER)!=lid)return;
        // Exact old-trainer F10: read the same primary selected-building pointer used by Delete,
        // then write ONE BYTE 0xFA at object+0x250. BRZE keeps building +0x250 unchanged.
        W8((long)obj+0x250,250);
    }
    static void Boost(uint obj,uint lid)
    {
        if(obj==0||R32((long)obj+0x84)!=lid)return;
        W32((long)obj+0x8E,5000); W32((long)obj+0x492,100); W32((long)obj+0x4BE,100);
    }

    public static string Apply(bool rice,bool water,bool yinYang,bool pop,bool instantTrain)
    {
        if(!Attach())return "Waiting for Battle_Realms_F.exe...";uint lid=R32(moduleBase+RVA_LOCAL_ID),playerPtr=R32(moduleBase+RVA_PLAYER_PTR);if(playerPtr==0)return "Attached, waiting for match/player data...";localId=lid;long player=(long)playerPtr+(long)lid*PLAYER_STRIDE;
        if(rice)W32(player+OFF_RICE,50000);if(water)W32(player+OFF_WATER,50000);if(yinYang){W32(player+OFF_YIN,10);W32(player+OFF_YANG,10);}if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,9_999_999);
        return $"Attached | hooks:{hooksInstalled} selected:{selectedLocked} F7LegacyHook:{instantTrain} | F5:{wantStamina} F6:{wantHp} F7:{instantTrain}"+(hookError.Length==0?"":" | "+hookError);
    }
}
