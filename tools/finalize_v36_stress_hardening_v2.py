from pathlib import Path

src=Path('tools/finalize_v36_stress_hardening.py').read_text(encoding='utf-8')
old="""marker='''        runtimeWasReady=true;\\n        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);'''"""
new="""marker='''        runtimeWasReady=true;'''"""
if old not in src:
    raise SystemExit('V36 v2: old runtime marker definition missing')
src=src.replace(old,new,1)
old2="""        runtimeWasReady=true;\\n        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);'''\ns=rep(s,marker,settle,'runtime activation marker missing')"""
new2="""        runtimeWasReady=true;'''\ns=rep(s,marker,settle,'runtime activation marker missing')"""
if old2 not in src:
    raise SystemExit('V36 v2: old settle tail missing')
src=src.replace(old2,new2,1)
exec(compile(src,'finalize_v36_stress_hardening.v2.generated.py','exec'),{'__name__':'__main__'})
