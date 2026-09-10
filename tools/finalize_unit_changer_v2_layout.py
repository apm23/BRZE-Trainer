from pathlib import Path

p = Path('UnitChangerTrainerV2/Program.cs')
s = p.read_text(encoding='utf-8')
repls = {
    'ClientSize = new Size(1080, 820);': 'ClientSize = new Size(1080, 940);',
    'MinimumSize = new Size(1030, 780);': 'MinimumSize = new Size(1030, 900);',
    'root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));': 'root.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));',
}
for a,b in repls.items():
    if a not in s:
        raise SystemExit(f'missing layout anchor: {a}')
    s = s.replace(a,b,1)
p.write_text(s, encoding='utf-8')
print('Unit Changer V2 no-clipping layout applied')
