from pathlib import Path

src=Path('tools/finalize_v36_stress_hardening.py').read_text(encoding='utf-8')
# SelectionCore source keeps spaces around '=' in its Attach() process lookup.
# Normalize the generator's anchor strings before execution.
src=src.replace('var ps=Process.GetProcessesByName','var ps = Process.GetProcessesByName')
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
old3='''s=rep(s,'            if(!SetReveal(true))return "MAP: ERROR — "+error;','            if(!SetReveal(true)){reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(800);return "MAP: RETRY — "+error;}','Reveal retry marker missing')'''
new3='''s=rep(s,'            if(!SetReveal(true))return "MAP: ERROR — "+error;','            if(!SetReveal(true)){string e=error;error="";reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(800);return "MAP: RETRY — "+e;}','Reveal retry marker missing')'''
if old3 not in src:
    raise SystemExit('V36 v2: reveal retry generator marker missing')
src=src.replace(old3,new3,1)
exec(compile(src,'finalize_v36_stress_hardening.v2.generated.py','exec'),{'__name__':'__main__'})
