from pathlib import Path
p=Path('UnitChangerCore.cs')
s=p.read_text(encoding='utf-8')
bad='    public static void Reset(){    public static void Reset(){'
if bad not in s:
    raise SystemExit('V14 repair: duplicate Reset marker not found')
s=s.replace(bad,'    public static void Reset(){',1)
p.write_text(s,encoding='utf-8')
print('V14 generator duplicate Reset marker repaired')
