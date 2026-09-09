from pathlib import Path
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')
# UI: add legacy controls
s=s.replace('readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training — LEGACY F4 EXACT REMAP", AutoSize = true };', '''readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training — LEGACY F4 EXACT REMAP", AutoSize = true };
    readonly CheckBox legacyTower = new() { Text = "Legacy Infinite Watchtowers — experimental remap slot", AutoSize = true };
    readonly CheckBox pausePeasant = new() { Text = "Legacy F9 Pause Peasant Production (F12)", AutoSize = true };
    readonly CheckBox demolish = new() { Text = "Demolition Mode (F11) — BRZE native enable flag", AutoSize = true };
    readonly CheckBox horses = new() { Text = "Maximum Horses / instant horse respawn", AutoSize = true };''')
s=s.replace('ClientSize = new Size(570, 400)', 'ClientSize = new Size(650, 520)')
s=s.replace('panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6, f7 });', 'panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6, f7, legacyTower, pausePeasant, demolish, horses });')
s=s.replace('panel.Controls.Add(new Label { Text = "F10 Maximum Wolves — PUSH TEST (old trainer remap; horses pending)", AutoSize = true });', '''panel.Controls.Add(new Label { Text = "F10 Maximum Wolves — old trainer exact +0x250 remap", AutoSize = true });
        panel.Controls.Add(new Label { Text = "Delete = Instant Build/Repair/Research/BattleGear | PageDown = Instant Death", AutoSize = true });
        panel.Controls.Add(new Label { Text = "PageUp = toggle Health + Stamina together (old trainer behavior)", AutoSize = true });''')
# expand held
s=s.replace('readonly bool[] held = new bool[12];','readonly bool[] held = new bool[20];')
# Tick additions
s=s.replace('Toggle(0x76,7,()=>f7.Checked=!f7.Checked); Toggle(0x77,8,()=>Native.InstantSelectedBuilding());', '''Toggle(0x76,7,()=>f7.Checked=!f7.Checked); Toggle(0x77,8,()=>Native.InstantSelectedBuilding());
        Toggle(0x2E,11,()=>Native.InstantSelectedBuilding());
        Toggle(0x22,12,()=>Native.InstantDeathSelected());
        Toggle(0x21,13,()=>{bool n=!(f5.Checked&&f6.Checked);f5.Checked=n;f6.Checked=n;});
        Toggle(0x7A,14,()=>demolish.Checked=!demolish.Checked);
        Toggle(0x7B,15,()=>pausePeasant.Checked=!pausePeasant.Checked);''')
s=s.replace('Native.SetHooks(f5.Checked, f6.Checked, f7.Checked);\n        status.Text = Native.Apply(f1.Checked,f2.Checked,f3.Checked,f4.Checked,f7.Checked);', '''Native.SetHooks(f5.Checked, f6.Checked, f7.Checked);
        Native.ApplyLegacyRuntime(pausePeasant.Checked,demolish.Checked,horses.Checked,legacyTower.Checked);
        status.Text = Native.Apply(f1.Checked,f2.Checked,f3.Checked,f4.Checked,f7.Checked);''')
# constants
s=s.replace('const int RVA_ADD_HEALTH=0x1CCD4D,RVA_ADD_STAMINA=0x1CCDFB,RVA_TRAIN_PROGRESS_READ=0x0D5DDB;', '''const int RVA_ADD_HEALTH=0x1CCD4D,RVA_ADD_STAMINA=0x1CCDFB,RVA_TRAIN_PROGRESS_READ=0x0D5DDB;
    const int RVA_SELECT_ONE=0x1A70C6,RVA_SELECT_BOX=0x1A78CC;
    const int RVA_PEASANT_CREATION=0x467AF4,RVA_DEMOLISH_ENABLE=0x3D7A1C;
    const int RVA_CFG_BASE_PTR=0x43FF2C,RVA_CFG_INDEX_PTR=0x440034;''')
# fields and originals
s=s.replace('static int selectedLocked,trainingBuildings; static IntPtr cave=IntPtr.Zero; static long hpFlag,stFlag,trainFlag; static bool hooksInstalled;', 'static int selectedLocked,trainingBuildings; static IntPtr cave=IntPtr.Zero; static long hpFlag,stFlag,trainFlag; static bool hooksInstalled; static uint horseOriginal; static bool horseSaved;')
s=s.replace('static readonly byte[] StOriginal={0x55,0x8B,0xEC,0x56,0x8B,0xF1,0x57,0x56}; static readonly byte[] TrainOriginal=', 'static readonly byte[] StOriginal={0x55,0x8B,0xEC,0x56,0x8B,0xF1,0x57,0x56};\n    static readonly byte[] SelectOriginal={0xC7,0x86,0xA8,0x03,0x00,0x00,0x01,0x00,0x00,0x00}; static readonly byte[] TrainOriginal=')
# detach restore selection hooks
s=s.replace('WriteCode(moduleBase+RVA_TRAIN_PROGRESS_READ,TrainOriginal);}', 'WriteCode(moduleBase+RVA_TRAIN_PROGRESS_READ,TrainOriginal);WriteCode(moduleBase+RVA_SELECT_ONE,SelectOriginal);WriteCode(moduleBase+RVA_SELECT_BOX,SelectOriginal);}')
# insert selection hook builder before JmpPatch
marker='    static byte[] JmpPatch(long target,long stub,int len)'
sel=r'''    static byte[] BuildSelectionRefillHook(long stub,long target)
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

'''
s=s.replace(marker,sel+marker)
# replace EnsureHooks with extended version
start=s.index('    static bool EnsureHooks()')
end=s.index('    public static void SetHooks',start)
ensure=r'''    static bool EnsureHooks()
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
'''
s=s[:start]+ensure+s[end:]
# insert legacy runtime and instant death before InstantSelectedBuilding
marker='    public static void InstantSelectedBuilding()'
legacy=r'''    public static void ApplyLegacyRuntime(bool pause,bool demo,bool horse,bool tower)
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

'''
s=s.replace(marker,legacy+marker)
p.write_text(s,encoding='utf-8')
