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
    readonly CheckBox f5 = new() { Text = "F5 No Stamina Consumption (selected only) — HOOK TEST", AutoSize = true };
    readonly CheckBox f6 = new() { Text = "F6 No Damage (selected only) — HOOK TEST", AutoSize = true };
    readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training (TEST 2)", AutoSize = true };
    readonly Label status = new() { AutoSize = false, Height = 54, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    readonly bool[] held = new bool[12];

    public MainForm()
    {
        Text = "BRZE Trainer 1.60 — Hook Test"; ClientSize = new Size(570, 400); StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
        panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6, f7 });
        panel.Controls.Add(new Label { Text = "F7 building skills/upgrades — not enabled yet", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F8 Instant Building — pending", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F9 Enable / Disable ALL implemented cheats", AutoSize = true });
        panel.Controls.Add(new Label { Text = "F10 Unlimited Horses + Wolves — pending", AutoSize = true, ForeColor = Color.DimGray });
        Controls.Add(panel); Controls.Add(status);
        Native.Start(); timer.Tick += (_, _) => TickTrainer(); timer.Start(); FormClosed += (_, _) => Native.Stop();
    }

    void Toggle(int vk, int idx, Action action) { bool now = (Native.GetAsyncKeyState(vk) & 0x8000) != 0; if (now && !held[idx]) action(); held[idx] = now; }
    void SetAllImplemented(bool e) { f1.Checked=e; f2.Checked=e; f3.Checked=e; f4.Checked=e; f5.Checked=e; f6.Checked=e; f7.Checked=e; }
    void TickTrainer()
    {
        Toggle(0x70,1,()=>f1.Checked=!f1.Checked); Toggle(0x71,2,()=>f2.Checked=!f2.Checked); Toggle(0x72,3,()=>f3.Checked=!f3.Checked);
        Toggle(0x73,4,()=>f4.Checked=!f4.Checked); Toggle(0x74,5,()=>f5.Checked=!f5.Checked); Toggle(0x75,6,()=>f6.Checked=!f6.Checked);
        Toggle(0x76,7,()=>f7.Checked=!f7.Checked);
        Toggle(0x78,9,()=>{ bool all=f1.Checked&&f2.Checked&&f3.Checked&&f4.Checked&&f5.Checked&&f6.Checked&&f7.Checked; SetAllImplemented(!all); });
        Native.SetHooks(f5.Checked, f6.Checked);
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
    const int RVA_UNIT_POOL=0x4796A0,UNIT_STRIDE=0x818,UNIT_COUNT=2000,OFF_DEF=0x74,OFF_OWNER=0x240,OFF_SEL_A=0x3A8,OFF_SEL_B=0x3AC,OFF_HP=0x404,OFF_ST=0x408;
    const int RVA_ADD_HEALTH=0x1CCD4D,RVA_ADD_STAMINA=0x1CCDFB;
    const int RVA_BUILDING_POOL=0x4814E0,BUILDING_STRIDE=0x6A4,BUILDING_COUNT=500,OFF_BUILD_OWNER=0x84,OFF_TRAIN_TYPE=0x488,OFF_TRAIN_PROGRESS=0x490,OFF_TRAIN_GATE=0x4B8,OFF_BUILD_SPECIAL=0x68C,RVA_TRAIN_SPECIAL_GLOBAL=0x46779C;
    const uint TRAIN_COMPLETE_FIXED=0x00640000;

    static readonly object attachLock=new(); static IntPtr h=IntPtr.Zero; static Process? p; static long moduleBase;
    static volatile uint localId; static volatile bool wantStamina,wantHp,running; static Thread? topupThread;
    static DateTime lastUnitCache=DateTime.MinValue,lastBuildingScan=DateTime.MinValue; static UnitInfo[] localUnits=Array.Empty<UnitInfo>();
    static readonly byte[] unitRaw=new byte[UNIT_COUNT*UNIT_STRIDE],buildingRaw=new byte[BUILDING_COUNT*BUILDING_STRIDE];
    static int selectedLocked,trainingBuildings; static IntPtr cave=IntPtr.Zero; static long hpFlag,stFlag; static bool hooksInstalled;
    static readonly byte[] HpOriginal={0x55,0x8B,0xEC,0x83,0xE4,0xF8,0x56,0x8B,0xF1,0x57};
    static readonly byte[] StOriginal={0x55,0x8B,0xEC,0x56,0x8B,0xF1,0x57,0x56};

    readonly struct UnitInfo { public readonly long Addr; public readonly uint MaxHp,MaxSt; public UnitInfo(long a,uint hp,uint st){Addr=a;MaxHp=hp;MaxSt=st;} }
    public static void Start(){ if(running)return; running=true; topupThread=new Thread(TopupLoop){IsBackground=true,Name="BRZE-Selected-Topup"}; topupThread.Start(); }
    public static void Stop(){ running=false; try{topupThread?.Join(300);}catch{} Detach(); }

    static bool ReadExact(long a,byte[] b){IntPtr hh=h;return hh!=IntPtr.Zero&&ReadProcessMemory(hh,new IntPtr(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;}
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b){IntPtr hh=h;return hh!=IntPtr.Zero&&WriteProcessMemory(hh,new IntPtr(a),b,b.Length,out var n)&&n.ToInt64()==b.Length;}
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
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
        if(h!=IntPtr.Zero){ if(hooksInstalled){WriteCode(moduleBase+RVA_ADD_HEALTH,HpOriginal);WriteCode(moduleBase+RVA_ADD_STAMINA,StOriginal);} if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE); CloseHandle(h);}
        h=IntPtr.Zero;p=null;cave=IntPtr.Zero;hooksInstalled=false;localUnits=Array.Empty<UnitInfo>();
    }
    static bool WriteCode(long addr,byte[] data){if(h==IntPtr.Zero)return false;VirtualProtectEx(h,new IntPtr(addr),(UIntPtr)data.Length,PAGE_EXECUTE_READWRITE,out uint old);bool ok=WriteBytes(addr,data);FlushInstructionCache(h,new IntPtr(addr),(UIntPtr)data.Length);VirtualProtectEx(h,new IntPtr(addr),(UIntPtr)data.Length,old,out _);return ok;}
    static void I32(List<byte>b,int v){b.AddRange(BitConverter.GetBytes(v));} static void U32(List<byte>b,uint v){b.AddRange(BitConverter.GetBytes(v));}
    static void PatchRel(List<byte>b,int at,long fromNext,long to){var x=BitConverter.GetBytes(unchecked((int)(to-fromNext)));for(int i=0;i<4;i++)b[at+i]=x[i];}

    static byte[] BuildHook(long stub,long flag,long target,byte[] original,int backOffset)
    {
        var b=new List<byte>();
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)flag);b.Add(0); b.AddRange(new byte[]{0x0F,0x84});int jFlag=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x7C,0x24,0x04,0x00,0x0F,0x8D});int jPos=b.Count;I32(b,0);
        b.Add(0x50);b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));
        b.AddRange(new byte[]{0x39,0x81,0x40,0x02,0x00,0x00,0x0F,0x85});int jOwner=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB9,0xA8,0x03,0x00,0x00,0x01,0x0F,0x85});int jA=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xB9,0xAC,0x03,0x00,0x00,0x01,0x0F,0x85});int jB=b.Count;I32(b,0);
        b.Add(0x58);b.AddRange(new byte[]{0xC2,0x04,0x00});
        int popOriginal=b.Count;b.Add(0x58);int originalLabel=b.Count;b.AddRange(original);b.Add(0xE9);int jBack=b.Count;I32(b,0);
        PatchRel(b,jFlag,stub+jFlag+4,stub+originalLabel);PatchRel(b,jPos,stub+jPos+4,stub+originalLabel);
        PatchRel(b,jOwner,stub+jOwner+4,stub+popOriginal);PatchRel(b,jA,stub+jA+4,stub+popOriginal);PatchRel(b,jB,stub+jB+4,stub+popOriginal);
        PatchRel(b,jBack,stub+jBack+4,target+backOffset);return b.ToArray();
    }
    static byte[] JmpPatch(long target,long stub,int len){var b=new byte[len];b[0]=0xE9;Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,b,1,4);for(int i=5;i<len;i++)b[i]=0x90;return b;}

    static bool EnsureHooks()
    {
        if(hooksInstalled)return true;if(!Attach())return false;
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)512,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);if(cave==IntPtr.Zero)return false;
        long c=cave.ToInt64();hpFlag=c;stFlag=c+4;long hpStub=c+32,stStub=c+160;
        W32(hpFlag,0);W32(stFlag,0);
        byte[] hs=BuildHook(hpStub,hpFlag,moduleBase+RVA_ADD_HEALTH,HpOriginal,HpOriginal.Length);byte[] ss=BuildHook(stStub,stFlag,moduleBase+RVA_ADD_STAMINA,StOriginal,StOriginal.Length);
        if(!WriteBytes(hpStub,hs)||!WriteBytes(stStub,ss))return false;
        if(!WriteCode(moduleBase+RVA_ADD_HEALTH,JmpPatch(moduleBase+RVA_ADD_HEALTH,hpStub,HpOriginal.Length)))return false;
        if(!WriteCode(moduleBase+RVA_ADD_STAMINA,JmpPatch(moduleBase+RVA_ADD_STAMINA,stStub,StOriginal.Length)))return false;
        hooksInstalled=true;return true;
    }
    public static void SetHooks(bool stamina,bool hp){wantStamina=stamina;wantHp=hp;if(!Attach())return;if((stamina||hp)&&EnsureHooks()){W32(hpFlag,hp?1u:0u);W32(stFlag,stamina?1u:0u);}else if(hooksInstalled){W32(hpFlag,0);W32(stFlag,0);}}

    static bool RefreshUnits(bool force=false)
    {
        if(!Attach())return false;if(!force&&(DateTime.UtcNow-lastUnitCache).TotalMilliseconds<1000)return true;uint lid=R32(moduleBase+RVA_LOCAL_ID),pool=R32(moduleBase+RVA_UNIT_POOL);if(pool==0||!ReadExact(pool,unitRaw))return false;
        var found=new List<UnitInfo>(256);for(int i=0;i<UNIT_COUNT;i++){int o=i*UNIT_STRIDE;uint def=BitConverter.ToUInt32(unitRaw,o+OFF_DEF);if(def==0||BitConverter.ToUInt32(unitRaw,o+OFF_OWNER)!=lid)continue;uint mh=R32((long)def+0x6C),ms=R32((long)def+0x80);if(mh==0&&ms==0)continue;found.Add(new UnitInfo((long)pool+o,Fixed16(mh),Fixed16(ms)));}
        localId=lid;localUnits=found.ToArray();lastUnitCache=DateTime.UtcNow;return true;
    }
    static void TopupLoop()
    {
        var sel=new byte[8];var cur=new byte[8];while(running){if(!wantHp&&!wantStamina){selectedLocked=0;Thread.Sleep(100);continue;}if(!RefreshUnits()){Thread.Sleep(200);continue;}
            int count=0;uint lid=localId;foreach(var u in localUnits){if(!ReadExact(u.Addr+OFF_SEL_A,sel))continue;if(BitConverter.ToUInt32(sel,0)!=1||BitConverter.ToUInt32(sel,4)!=1)continue;if(R32(u.Addr+OFF_OWNER)!=lid)continue;count++;
                if(!ReadExact(u.Addr+OFF_HP,cur))continue;uint hp=BitConverter.ToUInt32(cur,0),st=BitConverter.ToUInt32(cur,4);if(wantHp&&u.MaxHp!=0&&hp<u.MaxHp)W32(u.Addr+OFF_HP,u.MaxHp);if(wantStamina&&u.MaxSt!=0&&st<u.MaxSt)W32(u.Addr+OFF_ST,u.MaxSt);}
            selectedLocked=count;Thread.Sleep(100);}
    }

    static void ApplyInstantUnitTraining(uint lid)
    {
        if((DateTime.UtcNow-lastBuildingScan).TotalMilliseconds<25)return;lastBuildingScan=DateTime.UtcNow;uint pool=R32(moduleBase+RVA_BUILDING_POOL);if(pool==0||!ReadExact(pool,buildingRaw)){trainingBuildings=0;return;}
        uint specialGlobal=R32(moduleBase+RVA_TRAIN_SPECIAL_GLOBAL);int active=0;for(int i=0;i<BUILDING_COUNT;i++){int o=i*BUILDING_STRIDE;if(BitConverter.ToUInt32(buildingRaw,o+OFF_BUILD_OWNER)!=lid)continue;uint type=BitConverter.ToUInt32(buildingRaw,o+OFF_TRAIN_TYPE);if(type==0xFFFFFFFF)continue;
            uint gate=BitConverter.ToUInt32(buildingRaw,o+OFF_TRAIN_GATE),special=BitConverter.ToUInt32(buildingRaw,o+OFF_BUILD_SPECIAL);if((specialGlobal!=0&&special!=0)||gate==0xFFFFFFFF)continue;active++;uint prog=BitConverter.ToUInt32(buildingRaw,o+OFF_TRAIN_PROGRESS);if(prog<TRAIN_COMPLETE_FIXED)W32((long)pool+o+OFF_TRAIN_PROGRESS,TRAIN_COMPLETE_FIXED);}trainingBuildings=active;
    }
    public static string Apply(bool rice,bool water,bool yinYang,bool pop,bool instantTrain)
    {
        if(!Attach())return "Waiting for Battle_Realms_F.exe...";uint lid=R32(moduleBase+RVA_LOCAL_ID),playerPtr=R32(moduleBase+RVA_PLAYER_PTR);if(playerPtr==0)return "Attached, waiting for match/player data...";localId=lid;long player=(long)playerPtr+(long)lid*PLAYER_STRIDE;
        if(rice)W32(player+OFF_RICE,50000);if(water)W32(player+OFF_WATER,50000);if(yinYang){W32(player+OFF_YIN,10);W32(player+OFF_YANG,10);}if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,9_999_999);if(instantTrain)ApplyInstantUnitTraining(lid);else trainingBuildings=0;
        return $"Attached | hook:{hooksInstalled} selected:{selectedLocked} training:{trainingBuildings} | F5:{wantStamina} F6:{wantHp} F7:{instantTrain}";
    }
}
