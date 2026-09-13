from pathlib import Path
import re

src=Path('tools/finalize_v19_hold_death_layout.py').read_text(encoding='utf-8')

# Replace the brittle generated-state anchor with a formatting-tolerant one.
state_block=r'''# State for press = single, hold >=260 ms = burst until release\.\nanchor=.*?must\(anchor,insert\)'''
state_replacement=r'''# State for press = single, hold >=260 ms = burst until release.
state_pat=r'(?:string\s+notice\s*=\s*""\s*,\s*lastStatus\s*=\s*""\s*;|string\s+lastStatus\s*=\s*""\s*;)'
m=re.search(state_pat,u)
if not m:
    raise SystemExit('V19 could not locate notice/lastStatus state declaration')
state_insert='\n    bool deathMouseHeld;\n    bool deathHoldBurst;\n    DateTime deathPressUtc=DateTime.MinValue;\n    const int DeathHoldMs=260;'
u=u[:m.end()]+state_insert+u[m.end():]'''
patched,n=re.subn(state_block,lambda _m:state_replacement,src,count=1,flags=re.S)
if n!=1:
    raise SystemExit('V19 v2 wrapper could not patch state block')

# Do not depend on the exact generated Home line/comment. Insert the hold-state
# updater immediately before the stable F9 polling block instead.
home_block=r'''# Ensure hold state is refreshed every 100 ms for keyboard and mouse\.\nhome=.*?must\(home,home\+'\\n        UpdateDeathHold\(\);'\)'''
home_replacement=r'''# Ensure hold state is refreshed every 100 ms for keyboard and mouse.
fm=re.search(r'(?m)^(\s*)bool\s+f9\s*=\s*\(Native\.GetAsyncKeyState\(0x78\)&0x8000\)!=0;',u)
if not fm:
    raise SystemExit('V19 could not locate stable F9 polling block')
indent=fm.group(1)
u=u[:fm.start()]+indent+'UpdateDeathHold();\n'+u[fm.start():]'''
patched,n2=re.subn(home_block,lambda _m:home_replacement,patched,count=1,flags=re.S)
if n2!=1:
    raise SystemExit('V19 v2 wrapper could not patch hold-update block')

# Make the final death Tick replacement tolerant of generated whitespace.
tick_block=r'''# Runtime core call: hold state controls burst, switch controls target policy\.\nmust\('string deathStatus=InstantDeathCore\.Tick\(killAll\.Checked\);',\s*\n\s*'string deathStatus=InstantDeathCore\.Tick\(deathHoldBurst,killAll\.Checked\);'\)'''
tick_replacement=r'''# Runtime core call: hold state controls burst, switch controls target policy.
tm=re.search(r'InstantDeathCore\.Tick\(\s*killAll\.Checked\s*\)',u)
if not tm:
    raise SystemExit('V19 could not locate InstantDeathCore.Tick(killAll) call')
u=u[:tm.start()]+'InstantDeathCore.Tick(deathHoldBurst,killAll.Checked)'+u[tm.end():]'''
patched,n3=re.subn(tick_block,lambda _m:tick_replacement,patched,count=1,flags=re.S)
if n3!=1:
    raise SystemExit('V19 v2 wrapper could not patch death Tick block')

exec(compile(patched,'finalize_v19_hold_death_layout_v2.generated.py','exec'),{'__name__':'__main__'})
