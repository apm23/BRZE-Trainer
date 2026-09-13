from pathlib import Path
p=Path('UnitCloneLab/Program.cs')
s=p.read_text(encoding='utf-8')

def rep(a,b):
    global s
    if a not in s:
        raise SystemExit('clone safety marker missing: '+a[:160])
    s=s.replace(a,b,1)

rep('    static bool installed;\n    static string status = "not attached";',
    '    static bool installed;\n    static byte[]? hookPatch;\n    static string status = "not attached";')
rep('''        var patch = new List<byte> { 0xE9 }; I32(patch, unchecked((int)((c + STUB) - (site + 5)))); patch.Add(0x90);\n        if (!WriteCode(site, patch.ToArray()))''',
'''        var patch = new List<byte> { 0xE9 }; I32(patch, unchecked((int)((c + STUB) - (site + 5)))); patch.Add(0x90);\n        hookPatch = patch.ToArray();\n        if (!WriteCode(site, hookPatch))''')
rep('''                var now = new byte[6];\n                if (ReadExact(site, now) && now.Length == 6 && now[0] == 0xE9) WriteCode(site, FrameOriginal);''',
'''                var now = new byte[6];\n                if (hookPatch != null && ReadExact(site, now) && Same(now, hookPatch)) WriteCode(site, FrameOriginal);''')
rep('''        process = null; h = IntPtr.Zero; cave = IntPtr.Zero; moduleBase = 0; installed = false; status = "not attached";''',
'''        process = null; h = IntPtr.Zero; cave = IntPtr.Zero; moduleBase = 0; installed = false; hookPatch = null; status = "not attached";''')
p.write_text(s,encoding='utf-8')
print('Unit Clone Lab safety layer applied: exact hook ownership restore')
