from pathlib import Path

p = Path('Program.cs')
s = p.read_text(encoding='utf-8')

repls = [
    ('F4 Max Population + Selection Capacity 120 — INTERROGATION',
     'F4 Max Population + Selection Capacity 120 — CLEANROOM'),
    ('F5 No Stamina Loss (selected only) — SPECIMEN B NATIVE DELTA',
     'F5 DISABLED — clean-room selection test'),
    ('F6 No Damage (selected only) — SPECIMEN B NATIVE DELTA',
     'F6 DISABLED — clean-room selection test'),
    ('F7 Instant Unit Training — LEGACY F4 EXACT REMAP',
     'F7 DISABLED — clean-room selection test'),
    ('Native.SetHooks(f5.Checked, f6.Checked, f7.Checked);',
     'Native.SetHooks(false,false,false);'),
    ('const int RVA_SELECTION_CAP_CMP_IMM=0x1A7006,RVA_SELECTION_GROWTH=0x44172C,RVA_TEMPSEL_A_GROWTH=0x4417A8,RVA_TEMPSEL_B_GROWTH=0x4417D0;',
     'const int RVA_SELECTION_CAP_CMP_IMM=0x1A7006,RVA_SELECTION_GROWTH=0x44172C,RVA_TEMPSEL_A_GROWTH=0x4417A8,RVA_TEMPSEL_B_GROWTH=0x4417D0;\n    const int RVA_SELECTION_MATCH_INIT_IMM=0x1A6BCC,RVA_TEMPSEL_A_INIT_IMM=0x1A71B1,RVA_TEMPSEL_B_INIT_IMM=0x1A71B9,RVA_SELECTION_REBUILD_IMM=0x1A7299;'),
    ('static int selectedLocked,trainingBuildings; static IntPtr cave=IntPtr.Zero;',
     'static int selectedLocked,trainingBuildings; static bool largeSelectionPatched; static IntPtr cave=IntPtr.Zero;'),
    ('if(rice)W32(player+OFF_RICE,50000);if(water)W32(player+OFF_WATER,50000);if(yinYang){W32(player+OFF_YIN,10);W32(player+OFF_YANG,10);}if(pop){W32(moduleBase+RVA_MAX_UNITS+lid*4,99_999_999); W8(moduleBase+RVA_SELECTION_CAP_CMP_IMM,0x78); W32(moduleBase+RVA_SELECTION_GROWTH,120); W32(moduleBase+RVA_TEMPSEL_A_GROWTH,120); W32(moduleBase+RVA_TEMPSEL_B_GROWTH,120);}',
     'if(rice)W32(player+OFF_RICE,50000);if(water)W32(player+OFF_WATER,50000);if(yinYang){W32(player+OFF_YIN,10);W32(player+OFF_YANG,10);}if(pop){W32(moduleBase+RVA_MAX_UNITS+lid*4,99_999_999);EnableLargeSelection120();}')
]

for old, new in repls:
    if old not in s:
        raise SystemExit(f'missing expected source fragment: {old[:90]}')
    s = s.replace(old, new, 1)

anchor = '    public static string Apply(bool rice,bool water,bool yinYang,bool pop,bool instantTrain)\n'
if anchor not in s:
    raise SystemExit('Apply anchor missing')
method = '''    static void EnableLargeSelection120()\n    {\n        if(largeSelectionPatched||!Attach())return;\n        // CLEANROOM: no HP/stamina/selection-event hooks. Only enlarge the native selection\n        // manager with minimal one-byte immediate patches. Runtime +0x24 writes happen ONCE\n        // only to rescue already-constructed 90-node pools; the native rebuild sites are\n        // changed from initial capacity 90 to 120 so the game does not depend on timer races.\n        bool ok=true;\n        ok &= WriteCode(moduleBase+RVA_SELECTION_CAP_CMP_IMM,new byte[]{0x78});\n        ok &= WriteCode(moduleBase+RVA_SELECTION_MATCH_INIT_IMM,new byte[]{0x78});\n        ok &= WriteCode(moduleBase+RVA_TEMPSEL_A_INIT_IMM,new byte[]{0x78});\n        ok &= WriteCode(moduleBase+RVA_TEMPSEL_B_INIT_IMM,new byte[]{0x78});\n        ok &= WriteCode(moduleBase+RVA_SELECTION_REBUILD_IMM,new byte[]{0x78});\n        ok &= W32(moduleBase+RVA_SELECTION_GROWTH,120u);\n        ok &= W32(moduleBase+RVA_TEMPSEL_A_GROWTH,120u);\n        ok &= W32(moduleBase+RVA_TEMPSEL_B_GROWTH,120u);\n        if(ok)largeSelectionPatched=true; else hookError=\"large-selection cleanroom patch failed\";\n    }\n\n'''
s = s.replace(anchor, method + anchor, 1)

# Make the three unrelated hook checkboxes visibly non-interactive in this specimen.
ctor_anchor = '        Controls.Add(panel); Controls.Add(status);\n'
if ctor_anchor not in s:
    raise SystemExit('constructor anchor missing')
s = s.replace(ctor_anchor, '        f5.Enabled=false; f6.Enabled=false; f7.Enabled=false;\n' + ctor_anchor, 1)

p.write_text(s, encoding='utf-8')
print('patched Program.cs for clean-room selection capacity test')
