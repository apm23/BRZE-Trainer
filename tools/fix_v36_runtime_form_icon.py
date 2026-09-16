from pathlib import Path

p=Path('FinalV18MainForm.cs')
s=p.read_text(encoding='utf-8')

# ApplicationIcon embeds the ICO in the executable, but WinForms taskbar uses
# Form.Icon. Bind the main window explicitly to the executable's embedded icon.
marker='Text="BRZE Trainer — V36 Clean";'
marker_diag='Text="BRZE Trainer — V36 Diagnostics";'
needle='try{this.Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);this.ShowIcon=true;}catch{}'

if needle not in s:
    if marker_diag in s:
        s=s.replace(marker_diag,marker_diag+'\n        '+needle,1)
    elif marker in s:
        s=s.replace(marker,marker+'\n        '+needle,1)
    else:
        raise SystemExit('V36 icon fix: MainForm title marker not found')

p.write_text(s,encoding='utf-8')
print('V36 taskbar icon fix applied: MainForm.Icon <- embedded EXE icon')
