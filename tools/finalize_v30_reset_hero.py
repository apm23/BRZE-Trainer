from pathlib import Path

# V30 is generated on top of the V20 base + V22 explicit-target dispatcher facade.
# It does not create another hook owner. It only switches final version text and
# adds a guard so a later APPLY cannot discard queued mixed BOTH sub-batches.

core=Path('HeroEffectDirectCoreV30.cs')
s=core.read_text(encoding='utf-8')
needle='''        lock(Sync)\n        {\n            if(seconds<1m||seconds>420m)return status="BLOCKED — duration must be 1–420 seconds.";'''
repl='''        lock(Sync)\n        {\n            if(PendingBatches.Count>0)return status="BLOCKED — finishing the previous safe mixed-effect replay batch; retry in a moment.";\n            if(seconds<1m||seconds>420m)return status="BLOCKED — duration must be 1–420 seconds.";'''
if needle not in s:
    raise SystemExit('V30 could not locate Start guard insertion point')
s=s.replace(needle,repl,1)
core.write_text(s,encoding='utf-8')

ui=Path('FinalV18MainForm.cs')
u=ui.read_text(encoding='utf-8')
repls=[
    ('Text="BRZE Trainer — V22 Diagnostics";','Text="BRZE Trainer — V30 Diagnostics";'),
    ('Text="BRZE Trainer — V22 Clean";','Text="BRZE Trainer — V30 Clean";'),
    ('V22  ·  GROUP-SAFE HERO EFFECT + COPY UNIT  ·  INTEGRATED','V30  ·  HERO RESET + COPY UNIT  ·  INTEGRATED')
]
for old,new in repls:
    if old not in u:
        raise SystemExit('V30 title marker missing after V22 dispatcher finalizer: '+old)
    u=u.replace(old,new,1)
ui.write_text(u,encoding='utf-8')
print('V30 generated: proven current-tick reset + missing-only native apply + safe mixed BOTH batching')
