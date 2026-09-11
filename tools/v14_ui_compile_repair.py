from pathlib import Path
p=Path('FinalV14MainForm.cs')
s=p.read_text(encoding='utf-8')
old='BuildStatus(gate,false,"","","","","","","","","","","","")'
new='BuildStatus(gate,false,"","","","","","","","","","","","","")'
if old not in s:
    raise SystemExit('V14 UI repair marker not found')
s=s.replace(old,new,1)
p.write_text(s,encoding='utf-8')
print('V14 standby BuildStatus argument count repaired')
