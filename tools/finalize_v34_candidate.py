from pathlib import Path

# V34 is the last pre-final polish layer on top of runtime-proven V30 gameplay.
# It changes UI/refresh orchestration and retires unreachable legacy duplicate
# mutation paths that were superseded by the dedicated runtime cores.
ui=Path('FinalV18MainForm.cs')
s=ui.read_text(encoding='utf-8')

repls=[
    ('Text="BRZE Trainer — V30 Diagnostics";','Text="BRZE Trainer — V34 Diagnostics";'),
    ('Text="BRZE Trainer — V30 Clean";','Text="BRZE Trainer — V34 Clean";'),
    ('V30  ·  HERO RESET + COPY UNIT  ·  INTEGRATED','V34  ·  FINAL CANDIDATE + HARD BRZE REBIND  ·  INTEGRATED')
]
for old,new in repls:
    if old not in s:
        raise SystemExit('V34 title marker missing: '+old)
    s=s.replace(old,new,1)

shown='Shown+=(_,_)=>{GameGate.Probe(true);TickTrainer();};'
if shown not in s:
    raise SystemExit('V34 could not locate MainForm shown hook')
s=s.replace(shown,shown+'\n        OverlayHotkeyControllerV34.Attach(this);',1)

# Upgrade HOME/REFRESH TRAINER into a complete trainer<->BRZE runtime rebind.
# This does NOT restart or alter BRZE itself. It safely tears down every trainer-owned
# handle/hook/cave/config hold, forces a fresh process/gate probe, then lets the normal
# 100 ms tick rebuild only the features that are still enabled in the UI.
a=s.find('    void RefreshTrainer()')
b=s.find('    string ActiveList()',a)
if a<0 or b<0:
    raise SystemExit('V34 could not locate RefreshTrainer block')
refresh=r'''    void RefreshTrainer()
    {
        timer.Stop();

        // New V30/V20 integrated consumers first: restore Hero duration holds,
        // cancel queued native work, restore the one shared frame hook, close handles.
        try{HeroEffectDirectCoreV30.Shutdown();}catch{}
        try{IntegratedFrameDispatcherCore.StopAll();}catch{}

        // Legacy/integrated cores: restore all trainer-owned patches and handles.
        try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;
        try{PausePeasantCore.Stop();}catch{}
        try{WolfCore.Stop();}catch{}
        try{RevealMapCore.Stop(false);}catch{}
        try{InstantDeathCore.Stop();}catch{}
        try{StaminaCore.Stop();}catch{}
        try{HorseCore.Stop();}catch{}
        try{HookCore.Stop();}catch{}
        try{SelectionCore.Stop();}catch{}
        try{Native.Detach();}catch{}

        // Forget stale readiness/hotkey edges and refresh the Windows process object.
        // The following GameGate force probe re-reads PID/base/player pointers from BRZE.
        runtimeWasReady=false;
        Array.Clear(held,0,held.Length);
        try
        {
            foreach(var gp in System.Diagnostics.Process.GetProcessesByName("Battle_Realms_F"))
            {
                try{gp.Refresh();}catch{}finally{try{gp.Dispose();}catch{}}
            }
        }
        catch{}

        GameSnapshot gate;
        try{gate=GameGate.Probe(true);}catch{gate=new GameSnapshot(0,"○ BRZE RE-PROBE FAILED");}
        nextStatusUtc=DateTime.MinValue;
        timer.Start();
        SetNotice(gate.Ready
            ? "FULL REFRESH — BRZE link re-probed; every trainer hook/handle will rebuild cleanly; toggles preserved; COPY buffer cleared"
            : "FULL REFRESH — all trainer bindings cleared; waiting for fresh BRZE battle data");
        // Intentionally no recursive TickTrainer() here. The next 100 ms timer tick
        // performs the clean rebuild after HOME has been released.
    }

'''
s=s[:a]+refresh+s[b:]
ui.write_text(s,encoding='utf-8')

# Retire old Native implementations that are no longer reachable from the integrated
# MainForm but were still compiled beside their dedicated replacements. Keeping two
# possible writers for the same BRZE instruction/data address is unnecessary risk.
mp=Path('MergedProgram.cs')
m=mp.read_text(encoding='utf-8')

# 1) Native.DetachUnlocked must NOT restore old HP/stamina/training/selection hooks;
# HookCore owns those sites now. Preserve only the active Native resource state and
# safely restore the population cap if this Native instance changed it.
da=m.find('    static void DetachUnlocked()')
db=m.find('    static bool WriteCode(',da)
if da<0 or db<0:
    raise SystemExit('V34 could not locate Native.DetachUnlocked legacy block')
detach=r'''    static void DetachUnlocked()
    {
        if(h!=IntPtr.Zero)
        {
            // Population is still owned by Native.Apply. Restore it only if the exact
            // trainer sentinel is still present, then let the next enabled tick reapply.
            try
            {
                if(maxPopSaved&&originalMaxPopPlayer<10)
                {
                    long a=moduleBase+RVA_MAX_UNITS+(long)originalMaxPopPlayer*4;
                    if(R32(a)==9_999_999u)W32(a,originalMaxPop);
                }
            }
            catch{}
            if(cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);
            CloseHandle(h);
        }
        h=IntPtr.Zero;p=null;cave=IntPtr.Zero;hooksInstalled=false;localUnits=Array.Empty<UnitInfo>();remoteTrainState=-1;hookError="";
        maxPopSaved=false;originalMaxPop=0;originalMaxPopPlayer=0xFFFFFFFF;
    }
'''
m=m[:da]+detach+m[db:]

# 2) Remove the old hook installer entry point. HookCore is the sole owner of these
# fixed instruction sites in the integrated build. Keep a compile-compatible no-op
# facade for the unused LegacyMainForm only.
ha=m.find('    static bool EnsureHooks()')
hb=m.find('    static bool RefreshUnits',ha)
if ha<0 or hb<0:
    raise SystemExit('V34 could not locate legacy Native hook installer block')
hook_retired=r'''    // V34: legacy Native hook installer retired. HookCore owns HP/training/selection;
    // StaminaCore owns stamina. This method remains only for the unused LegacyMainForm.
    public static void SetHooks(bool stamina,bool hp,bool training)
    {
        wantStamina=stamina;wantHp=hp;remoteTrainState=training?1:0;
        hookError="legacy Native hooks retired — dedicated cores active";
    }

'''
m=m[:ha]+hook_retired+m[hb:]

# 3) Old ApplyLegacyRuntime wrote the same peasant-creation global now owned by
# PausePeasantCore (and old horse/demo paths were superseded too). Retire it entirely.
la=m.find('    public static void ApplyLegacyRuntime(')
lb=m.find('    public static void InstantDeathSelected()',la)
if la<0 or lb<0:
    raise SystemExit('V34 could not locate legacy ApplyLegacyRuntime block')
legacy_retired=r'''    // V34: superseded by PausePeasantCore / HorseCore; no fixed BRZE writes remain here.
    public static void ApplyLegacyRuntime(bool pause,bool demo,bool horse,bool tower) { }

'''
m=m[:la]+legacy_retired+m[lb:]
mp.write_text(m,encoding='utf-8')

# Generate the V34 overlay from the already runtime-approved V33 deck.
ov=Path('OverlayHotkeyControllerV33.cs').read_text(encoding='utf-8')
ov=ov.replace('V33','V34')
ov=ov.replace('const int HOTKEY_ID=0x3377;','const int HOTKEY_ID=0x3477;',1)

old='var om=FlatButton("−4",46,Off);var op=FlatButton("+4",46,Off);var orst=FlatButton("8",48,Off);'
new='var om=FlatButton("−0.1",46,Off);var op=FlatButton("+0.1",46,Off);var orst=FlatButton("8.0",48,Off);'
if old not in ov:
    raise SystemExit('V34 copy-offset button marker missing')
ov=ov.replace(old,new,1)

old='om.Click+=(_,_)=>{copyOffset=Math.Max(4f,copyOffset-4f);UpdateOffset();};op.Click+=(_,_)=>{copyOffset=Math.Min(64f,copyOffset+4f);UpdateOffset();};orst.Click+=(_,_)=>{copyOffset=8f;UpdateOffset();};'
new='om.Click+=(_,_)=>{copyOffset=Math.Max(0.1f,(float)Math.Round(copyOffset-0.1f,1));UpdateOffset();};op.Click+=(_,_)=>{copyOffset=Math.Min(64f,(float)Math.Round(copyOffset+0.1f,1));UpdateOffset();};orst.Click+=(_,_)=>{copyOffset=8f;UpdateOffset();};'
if old not in ov:
    raise SystemExit('V34 copy-offset handler marker missing')
ov=ov.replace(old,new,1)

# Keep one decimal visible even at 0.1 so cramped-map placement is explicit.
if 'void UpdateOffset()=>offsetValue.Text=$"+X  {copyOffset:0.0}";' not in ov:
    raise SystemExit('V34 offset readout marker missing')

Path('OverlayHotkeyControllerV34.cs').write_text(ov,encoding='utf-8')
print('V34 generated: 0.1 COPY offset + hard BRZE rebind + dormant duplicate Native writers retired + V33 deck preserved')
