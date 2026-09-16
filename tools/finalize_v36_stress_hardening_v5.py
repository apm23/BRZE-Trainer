from pathlib import Path
import re

src=Path('tools/finalize_v36_stress_hardening.py').read_text(encoding='utf-8')

# Normalize anchors against the generated V35 sources.
src=src.replace('var ps=Process.GetProcessesByName','var ps = Process.GetProcessesByName')

old="marker='''        runtimeWasReady=true;\\n        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);'''"
new="marker='''        runtimeWasReady=true;'''"
if old not in src:
    raise SystemExit('V36 v5: runtime marker definition missing')
src=src.replace(old,new,1)

old2="        runtimeWasReady=true;\\n        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);'''\ns=rep(s,marker,settle,'runtime activation marker missing')"
new2="        runtimeWasReady=true;'''\ns=rep(s,marker,settle,'runtime activation marker missing')"
if old2 not in src:
    raise SystemExit('V36 v5: settle tail missing')
src=src.replace(old2,new2,1)

# The generated V35 MainForm no longer has the old unitRuntime/ucCount locals.
# Keep the 1.5 s safe-start gate, but render its temporary status using only
# fields/methods that are present in the current V35/V36 MainForm.
old_settle=r'''                unitRuntime.Text=ucCount==0?"Runtime: safe-start settling":$"Runtime: ARMED · {ucCount} slot(s) · safe-start settling";\n                if(DateTime.UtcNow>=nextStatusUtc)\n                {\n                    nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);\n                    string text=BuildStatus(gate,false,"","",ucCount==0?"off":"armed");\n                    text+=$"\\r\\n\\r\\nAUTO SAFE START  battle data settling · {Math.Max(0,(BattleReadySettleMs-elapsed)/1000.0):0.0}s";\n                    if(DateTime.UtcNow<noticeUntilUtc)text+="\\r\\nNOTICE  "+notice;\n                    UpdateStatus(text);\n                }\n                return;\n'''
new_settle=r'''                if(DateTime.UtcNow>=nextStatusUtc)\n                {\n                    nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);\n                    string text=$"AUTO SAFE START · {gate.Label} · battle data settling · {Math.Max(0,(BattleReadySettleMs-elapsed)/1000.0):0.0}s";\n                    if(DateTime.UtcNow<noticeUntilUtc)text+=" · NOTICE "+notice;\n                    UpdateStatus(text);\n                }\n                return;\n'''
if old_settle not in src:
    raise SystemExit('V36 v5: obsolete safe-start status block missing')
src=src.replace(old_settle,new_settle,1)

old3='s=rep(s,\'            if(!SetReveal(true))return "MAP: ERROR — "+error;\',\'            if(!SetReveal(true)){reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(800);return "MAP: RETRY — "+error;}\',\'Reveal retry marker missing\')'
new3='s=rep(s,\'            if(!SetReveal(true))return "MAP: ERROR — "+error;\',\'            if(!SetReveal(true)){string e=error;error="";reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(800);return "MAP: RETRY — "+e;}\',\'Reveal retry marker missing\')'
if old3 not in src:
    raise SystemExit('V36 v5: reveal retry marker missing')
src=src.replace(old3,new3,1)

# Use the checked-in exact 64x64 icon payload and validate it before ICO creation.
pat=r"png_b64='''.*?'''\npng=base64\.b64decode\(png_b64\)"
repl="png_b64=Path('assets/brze_trainer_v36_64.b64').read_text(encoding='ascii').strip()\npng=base64.b64decode(png_b64,validate=True)"
src,n=re.subn(pat,repl,src,count=1,flags=re.S)
if n!=1:
    raise SystemExit('V36 v5: icon payload marker missing')

exec(compile(src,'finalize_v36_stress_hardening.v5.generated.py','exec'),{'__name__':'__main__'})
