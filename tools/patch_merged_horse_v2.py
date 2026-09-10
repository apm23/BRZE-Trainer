from pathlib import Path

p=Path('tools/prepare_merged_v1.py')
s=p.read_text(encoding='utf-8')

def rep(a,b):
    global s
    if a not in s: raise SystemExit('missing expected fragment: '+a)
    s=s.replace(a,b,1)

rep('Native.Start(); timer.Tick += (_, _) => TickTrainer(); timer.Start(); FormClosed += (_, _) => { HookCore.Stop(); SelectionCore.Stop(); Native.Stop(); };',
    'Native.Start(); timer.Tick += (_, _) => TickTrainer(); timer.Start(); FormClosed += (_, _) => { HorseCore.Stop(); HookCore.Stop(); SelectionCore.Stop(); Native.Stop(); };')
rep('Native.ApplyLegacyRuntime(pausePeasant.Checked,demolish.Checked,horses.Checked,legacyTower.Checked);',
    'Native.ApplyLegacyRuntime(pausePeasant.Checked,demolish.Checked,false,legacyTower.Checked);\n        string horseStatus = HorseCore.Tick(horses.Checked);')
rep('status.Text = Native.Apply(f1.Checked,f2.Checked,f3.Checked,f4.Checked,f7.Checked) + "\\r\\n" + hookStatus + "\\r\\n" + selectionStatus;',
    'status.Text = Native.Apply(f1.Checked,f2.Checked,f3.Checked,f4.Checked,f7.Checked) + "\\r\\n" + hookStatus + "\\r\\n" + horseStatus + "\\r\\n" + selectionStatus;')
rep('readonly CheckBox horses = new() { Text = "Unlimited Horses — native stable stock / instant respawn", AutoSize = true };',
    'readonly CheckBox horses = new() { Text = "Unlimited Horses — Wand 6-slot Stable port", AutoSize = true };')
p.write_text(s,encoding='utf-8')

c=Path('MergedTrainer.csproj')
t=c.read_text(encoding='utf-8')
needle='    <Compile Include="HookCore.cs" />\n'
if needle not in t: raise SystemExit('missing HookCore compile item')
t=t.replace(needle,needle+'    <Compile Include="HorseCore.cs" />\n',1)
c.write_text(t,encoding='utf-8')
print('merged wiring patched')
