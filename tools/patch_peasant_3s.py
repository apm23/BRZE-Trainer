from pathlib import Path
p = Path('SelectionCore.cs')
s = p.read_text(encoding='utf-8')
s = s.replace('PEASANT_FIXED_MS = 1000', 'PEASANT_FIXED_MS = 3000')
s = s.replace('peasant1s:', 'peasant3s:')
p.write_text(s, encoding='utf-8')
