from pathlib import Path

# 1) Keep the already-generated V10/V11 UI class as an unused fallback so the
# new integrated MainForm can live in its own source file without duplicating the type.
p = Path('MergedProgram.cs')
s = p.read_text(encoding='utf-8')
class_marker = 'internal sealed class MainForm : Form'
ctor_marker = '    public MainForm()'
if class_marker not in s or ctor_marker not in s:
    raise SystemExit('V13: expected generated MainForm markers missing')
s = s.replace(class_marker, 'internal sealed class LegacyMainForm : Form', 1)
s = s.replace(ctor_marker, '    public LegacyMainForm()', 1)
p.write_text(s, encoding='utf-8')

# 2) Extract the exact game-side V5 runtime-proven core. No reimplementation.
v5 = Path('UnitChangerTrainerV5/Program.cs').read_text(encoding='utf-8')
start = v5.find('internal readonly record struct CoreSnapshot')
if start < 0:
    raise SystemExit('V13: V5 CoreSnapshot marker missing')
core = v5[start:]
if 'internal static class CompletionCore' not in core:
    raise SystemExit('V13: V5 CompletionCore marker missing')
core = core.replace('internal static class CompletionCore', 'internal static class UnitChangerCore', 1)
header = '''using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing System.Linq;\nusing System.Runtime.InteropServices;\nusing System.Text;\n\nnamespace BRZETrainer;\n\n'''
Path('UnitChangerCore.cs').write_text(header + core, encoding='utf-8')

# 3) Prevent recursive Home-hotkey refresh. Refresh finishes by restarting the
# timer; the next normal 100 ms tick performs the clean rebind.
ui = Path('FinalIntegratedMainForm.cs')
us = ui.read_text(encoding='utf-8')
old = '''        timer.Start();\n        SetNotice("REFRESH TRAINER — runtime cores re-resolved; toggle states preserved");\n        TickTrainer();\n    }'''
new = '''        timer.Start();\n        SetNotice("REFRESH TRAINER — runtime cores re-resolved; toggle states preserved");\n        // Do not call TickTrainer recursively here: Home may still be physically held.\n        // The next timer tick performs the rebind with the preserved UI state.\n    }'''
if old not in us:
    raise SystemExit('V13: refresh tail marker missing')
us = us.replace(old, new, 1)
ui.write_text(us, encoding='utf-8')

print('V13 integrated source generated: V5 core extracted unchanged; refresh recursion guard applied')
