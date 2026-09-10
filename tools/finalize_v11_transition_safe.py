from pathlib import Path

p=Path('MergedProgram.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('MISSING V10 FRAGMENT:\n'+old)
    s=s.replace(old,new,1)

rep('        allOn.Click+=(_,_)=>SetAll(true);\n        allOff.Click+=(_,_)=>SetAll(false);',
    '        allOn.Click+=(_,_)=>AllOn();\n        allOff.Click+=(_,_)=>AllOff();')

rep('    void SetAll(bool e){rice.Checked=e;water.Checked=e;yinYang.Checked=e;population.Checked=e;training.Checked=e;peasant.Checked=e;hp.Checked=e;stamina.Checked=e;horses.Checked=e;wolves.Checked=e;reveal.Checked=e;burst.Checked=e;}',
'''    void AllOn()
    {
        // Safe master preset: enable the normal/core group only.
        // Horses / Wolves / Death Burst remain fully manual and ALL ON never changes their state.
        rice.Checked=true;water.Checked=true;yinYang.Checked=true;population.Checked=true;
        training.Checked=true;peasant.Checked=true;hp.Checked=true;stamina.Checked=true;reveal.Checked=true;
        SetNotice("ALL ON — Horses / Wolves / Death Burst unchanged (manual)");
    }
    void AllOff()
    {
        rice.Checked=false;water.Checked=false;yinYang.Checked=false;population.Checked=false;
        training.Checked=false;peasant.Checked=false;hp.Checked=false;stamina.Checked=false;
        horses.Checked=false;wolves.Checked=false;reveal.Checked=false;burst.Checked=false;
        SetNotice("ALL OFF — every cheat disabled");
    }''')

rep('            SetAll(!shift);\n            SetNotice(shift?"ALL OFF":"ALL ON");',
    '            if(shift)AllOff();else AllOn();')

rep('        try{WolfCore.Stop();}catch{}try{RevealMapCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}',
    '        // RevealMapCore intentionally stays attached across Journey/menu transitions so its game-thread\n        // FOW reinit guard can normalize fog BEFORE the old map buffers are torn down.\n        try{WolfCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}')

rep('        timer.Stop();\n        try{WolfCore.Stop();}catch{}try{RevealMapCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}',
'''        timer.Stop();
        bool revealSafe=false;try{revealSafe=GameGate.Probe(true).Ready;}catch{}
        try{WolfCore.Stop();}catch{}try{RevealMapCore.Stop(revealSafe);}catch{}try{InstantDeathCore.Stop();}catch{}''')

p.write_text(s,encoding='utf-8')
print('V11 transition-safe Reveal + manual ALL ON exclusions applied OK')
