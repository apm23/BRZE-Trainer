from pathlib import Path

# V33 is UI/hotkey only on top of runtime-proven V30 gameplay.
ui=Path('FinalV18MainForm.cs')
s=ui.read_text(encoding='utf-8')
repls=[
    ('Text="BRZE Trainer — V30 Diagnostics";','Text="BRZE Trainer — V33 Diagnostics";'),
    ('Text="BRZE Trainer — V30 Clean";','Text="BRZE Trainer — V33 Clean";'),
    ('V30  ·  HERO RESET + COPY UNIT  ·  INTEGRATED','V33  ·  FINAL PREMIUM OVERLAY + HERO RESET + COPY UNIT + UNIT CHANGER  ·  INTEGRATED')
]
for old,new in repls:
    if old not in s:
        raise SystemExit('V33 title marker missing: '+old)
    s=s.replace(old,new,1)
shown='Shown+=(_,_)=>{GameGate.Probe(true);TickTrainer();};'
if shown not in s:
    raise SystemExit('V33 could not locate MainForm shown hook')
s=s.replace(shown,shown+'\n        OverlayHotkeyControllerV33.Attach(this);',1)
ui.write_text(s,encoding='utf-8')
print('V33 generated: V30 gameplay untouched + 91% premium Alt+W deck + visible duration/offset + target policy + full mini Unit Changer')
