from pathlib import Path

src=Path('tools/audit_v36_stress_hardening.py').read_text(encoding='utf-8')
old="if 'profileRow.Controls.Add(ucProfile' in ov:\n    raise SystemExit('V36 AUDIT visible Unit Changer building ComboBox survived')"
new="if 'profileRow.Controls.Add(ucProfile,' in ov:\n    raise SystemExit('V36 AUDIT visible Unit Changer building ComboBox survived')"
if old not in src:
    raise SystemExit('V36 audit v2: building ComboBox check marker missing')
src=src.replace(old,new,1)

# Require the actual hidden ComboBox variable to be added as a visible control;
# do not reject the new label-based ucProfileValue/ucUnitValue controls by prefix.
old2="if 'panel.Controls.Add(cb' in ov:\n    raise SystemExit('V36 AUDIT visible Unit Changer unit ComboBox survived')"
new2="if 'panel.Controls.Add(cb,' in ov:\n    raise SystemExit('V36 AUDIT visible Unit Changer unit ComboBox survived')"
if old2 not in src:
    raise SystemExit('V36 audit v2: unit ComboBox check marker missing')
src=src.replace(old2,new2,1)

exec(compile(src,'audit_v36_stress_hardening.v2.generated.py','exec'),{'__name__':'__main__'})
