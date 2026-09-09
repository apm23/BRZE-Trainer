from pathlib import Path

p = Path('Program.cs')
s = p.read_text(encoding='utf-8')
old_label = 'F4 Unlimited Population (9,999,999)'
new_label = 'F4 Population 1000 — pressure bypass pending'
old_write = 'if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,9_999_999);'
new_write = 'if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,1_000);'

if old_label not in s:
    raise SystemExit('guard failed: old population label not found')
if old_write not in s:
    raise SystemExit('guard failed: old population write not found')

s = s.replace(old_label, new_label, 1)
s = s.replace(old_write, new_write, 1)
p.write_text(s, encoding='utf-8')
print('Population stage-1 patch applied: max cap 9,999,999 -> 1,000')
print('NOTE: this does NOT fake used population. Pressure/peasant slowdown bypass requires an independently proven BRZE read site.')
