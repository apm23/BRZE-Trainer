from pathlib import Path
p=Path('MergedProgram.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new,name):
    global s
    if old not in s:
        raise SystemExit(f'MergedProgram marker missing: {name}')
    s=s.replace(old,new,1)

rep('readonly CheckBox horses = new() { Text = "Unlimited Horses — native stable stock / instant respawn", AutoSize = true };',
    'readonly CheckBox horses = new() { Text = "Unlimited Horses — pending exact WeMod 1.60 patch (v1 workaround disabled)", AutoSize = true, Enabled = false };',
    'horse checkbox')
rep('readonly CheckBox fastPeasant = new() { Text = "Fast Peasant Spawn — FIXED 1.0 second (local player)", AutoSize = true };',
    'readonly CheckBox fastPeasant = new() { Text = "Fast Peasant Spawn — FIXED 3.0 seconds (local player)", AutoSize = true };',
    'peasant label')
rep('Native.ApplyLegacyRuntime(pausePeasant.Checked,demolish.Checked,horses.Checked,legacyTower.Checked);',
    'Native.ApplyLegacyRuntime(pausePeasant.Checked,demolish.Checked,false,legacyTower.Checked);',
    'disable old horse config')
p.write_text(s,encoding='utf-8')
print('Merged v2 UI patch OK: horse workaround disabled; peasant label 3s')
