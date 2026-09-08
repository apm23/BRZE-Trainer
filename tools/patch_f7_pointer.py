from pathlib import Path
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')
# x86-safe pointer construction: preserve the low 32-bit address instead of checked long->IntPtr conversion.
s=s.replace('new IntPtr(a)', 'new IntPtr(unchecked((int)(uint)a))')
# Keep the diagnostic scan deliberately slow and bounded so a bad candidate cannot hammer the game process.
s=s.replace('TotalMilliseconds<50', 'TotalMilliseconds<500')
s=s.replace('ScanBuildingPointerTable(mgr,lid,seen,512);', 'ScanBuildingPointerTable(mgr,lid,seen,128);')
s=s.replace('off<=0x80', 'off<=0x40')
s=s.replace('ScanBuildingPointerTable(table,lid,seen,512);', 'ScanBuildingPointerTable(table,lid,seen,128);')
# Stop obviously invalid high/low candidates before dereferencing them.
s=s.replace('if(table>=0x10000)ScanBuildingPointerTable(table,lid,seen,128);', 'if(table>=0x10000 && table<=0xFFF00000)ScanBuildingPointerTable(table,lid,seen,128);')
s=s.replace('if(obj<0x10000||!seen.Add(obj))continue;', 'if(obj<0x10000||obj>0xFFF00000||!seen.Add(obj))continue;')
p.write_text(s,encoding='utf-8')
