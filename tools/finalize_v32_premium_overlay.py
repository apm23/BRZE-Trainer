from pathlib import Path

# V32 is strictly UI/hotkey/packaging on top of runtime-proven V30 gameplay.
ui=Path('FinalV18MainForm.cs')
s=ui.read_text(encoding='utf-8')

repls=[
    ('Text="BRZE Trainer — V30 Diagnostics";','Text="BRZE Trainer — V32 Diagnostics";'),
    ('Text="BRZE Trainer — V30 Clean";','Text="BRZE Trainer — V32 Clean";'),
    ('V30  ·  HERO RESET + COPY UNIT  ·  INTEGRATED','V32  ·  PREMIUM ALT+W OVERLAY + HERO RESET + COPY UNIT  ·  INTEGRATED')
]
for old,new in repls:
    if old not in s:
        raise SystemExit('V32 title marker missing: '+old)
    s=s.replace(old,new,1)

shown='Shown+=(_,_)=>{GameGate.Probe(true);TickTrainer();};'
if shown not in s:
    raise SystemExit('V32 could not locate MainForm shown hook')
s=s.replace(shown,shown+'\n        OverlayHotkeyControllerV32.Attach(this);',1)
ui.write_text(s,encoding='utf-8')
print('V32 generated: V30 gameplay untouched + robust global Alt+W + separate premium 87% no-activate control deck')
