from pathlib import Path

# V34 is the last pre-final polish layer on top of runtime-proven V30 gameplay.
# It changes only UI/refresh orchestration. Core gameplay source files remain locked.
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
print('V34 generated: 0.1 COPY offset steps + full trainer/BRZE runtime rebind + V33 premium deck preserved')
