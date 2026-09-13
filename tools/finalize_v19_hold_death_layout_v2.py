from pathlib import Path
import re

src=Path('tools/finalize_v19_hold_death_layout.py').read_text(encoding='utf-8')
pattern=r'''# State for press = single, hold >=260 ms = burst until release\.\nanchor=.*?must\(anchor,insert\)'''
replacement=r'''# State for press = single, hold >=260 ms = burst until release.
state_pat=r'(?:string\s+notice\s*=\s*""\s*,\s*lastStatus\s*=\s*""\s*;|string\s+lastStatus\s*=\s*""\s*;)'
m=re.search(state_pat,u)
if not m:
    raise SystemExit('V19 could not locate notice/lastStatus state declaration')
state_insert='\n    bool deathMouseHeld;\n    bool deathHoldBurst;\n    DateTime deathPressUtc=DateTime.MinValue;\n    const int DeathHoldMs=260;'
u=u[:m.end()]+state_insert+u[m.end():]'''
patched,n=re.subn(pattern,lambda _m:replacement,src,count=1,flags=re.S)
if n!=1:
    raise SystemExit('V19 v2 wrapper could not patch state block')
exec(compile(patched,'finalize_v19_hold_death_layout_v2.generated.py','exec'),{'__name__':'__main__'})
