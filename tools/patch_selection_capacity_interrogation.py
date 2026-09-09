from pathlib import Path
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')
# F4 becomes the isolated max-pop + selection-capacity interrogation switch.
s=s.replace('F4 Unlimited Population (9,999,999)', 'F4 Max Population + Selection Capacity 120 — INTERROGATION')
# Add proven BRZE 1.60 selection addresses.
needle='const int RVA_SELECT_ONE=0x1A70C6,RVA_SELECT_BOX=0x1A78CC;'
repl=needle+'\n    const int RVA_SELECTION_CAP_CMP_IMM=0x1A7006,RVA_SELECTION_GROWTH=0x44172C,RVA_TEMPSEL_A_GROWTH=0x4417A8,RVA_TEMPSEL_B_GROWTH=0x4417D0;'
assert needle in s
s=s.replace(needle,repl,1)
# In Apply(), immediately after attach succeeds, F4 writes population. Extend that same F4 path.
needle='if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,9_999_999);'
repl='if(pop){W32(moduleBase+RVA_MAX_UNITS+lid*4,99_999_999); W8(moduleBase+RVA_SELECTION_CAP_CMP_IMM,0x78); W32(moduleBase+RVA_SELECTION_GROWTH,120); W32(moduleBase+RVA_TEMPSEL_A_GROWTH,120); W32(moduleBase+RVA_TEMPSEL_B_GROWTH,120);}'
assert needle in s
s=s.replace(needle,repl,1)
p.write_text(s,encoding='utf-8')
print('patched F4 interrogation: maxpop=99,999,999; manual cap=120; active/temp growth=120')
