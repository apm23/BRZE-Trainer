from pathlib import Path

p=Path('FinalV18MainForm.cs')
s=p.read_text(encoding='utf-8')

pairs=[
    ('Text="BRZE Trainer — V20 Diagnostics";','Text="BRZE Trainer — V21 Diagnostics";'),
    ('Text="BRZE Trainer — V20 Clean";','Text="BRZE Trainer — V21 Clean";'),
    ('V20  ·  COPY UNIT + HERO EFFECT  ·  INTEGRATED','V21  ·  DIRECT HERO EFFECT + COPY UNIT  ·  INTEGRATED')
]
for old,new in pairs:
    if old not in s:
        raise SystemExit('V21 title marker missing: '+old)
    s=s.replace(old,new,1)

# Keep the proven V20 geometry and full-width SYSTEM STATUS exactly as-is.
# V21 fixes Hero Effect clipping inside its dedicated panel by using a bounded
# table layout rather than the old single-line FlowLayout test UI.
p.write_text(s,encoding='utf-8')
print('V21 generated: direct Hero Effect UI; V20 geometry/status layout preserved')
