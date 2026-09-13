from pathlib import Path
import re

src=Path('tools/finalize_v19_hold_death_layout.py').read_text(encoding='utf-8')
pattern=r'''# State for press = single, hold >=260 ms = burst until release\.\nanchor=.*?must\(anchor,insert\)'''
replacement='''# State for press = single, hold >=260 ms = burst until release.\n# V18.x generator may keep notice/lastStatus on one source line, so anchor only\n# on the stable lastStatus token rather than whitespace/line formatting.\nanchor='string lastStatus="";'\ninsert=''' + "'''" + '''string lastStatus="";\n    bool deathMouseHeld;\n    bool deathHoldBurst;\n    DateTime deathPressUtc=DateTime.MinValue;\n    const int DeathHoldMs=260;''' + "'''" + '''\nmust(anchor,insert)'''
patched,n=re.subn(pattern,replacement,src,count=1,flags=re.S)
if n!=1:
    raise SystemExit('V19 v2 wrapper could not patch state marker')
exec(compile(patched,'finalize_v19_hold_death_layout_v2.generated.py','exec'),{'__name__':'__main__'})
