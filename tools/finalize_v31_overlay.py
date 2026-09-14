from pathlib import Path

# V31 is UI/packaging only on top of the already-proven V30 gameplay layer.
# It must not mutate any game-side core, dispatcher, duration/reset logic, or hooks.

ui=Path('FinalV18MainForm.cs')
s=ui.read_text(encoding='utf-8')

repls=[
    ('Text="BRZE Trainer — V30 Diagnostics";','Text="BRZE Trainer — V31 Diagnostics";'),
    ('Text="BRZE Trainer — V30 Clean";','Text="BRZE Trainer — V31 Clean";'),
    ('V30  ·  HERO RESET + COPY UNIT  ·  INTEGRATED','V31  ·  ALT+W OVERLAY + HERO RESET + COPY UNIT  ·  INTEGRATED')
]
for old,new in repls:
    if old not in s:
        raise SystemExit('V31 title marker missing: '+old)
    s=s.replace(old,new,1)

shown='Shown+=(_,_)=>{GameGate.Probe(true);TickTrainer();};'
if shown not in s:
    raise SystemExit('V31 could not locate MainForm shown hook')
s=s.replace(shown,shown+'\n        OverlayHotkeyController.Attach(this);',1)

ui.write_text(s,encoding='utf-8')
print('V31 generated: V30 mechanics unchanged + Alt+W non-activating translucent TopMost overlay')
