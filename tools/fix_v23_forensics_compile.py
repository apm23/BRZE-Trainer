from pathlib import Path

p=Path('HeroEffectResetForensicsV23/ProbeCore.cs')
s=p.read_text(encoding='utf-8')
old='var decoder=Decoder.Create(32,new ByteArrayCodeReader(bytes));decoder.IP=ip;'
new='var decoder=Iced.Intel.Decoder.Create(32,new ByteArrayCodeReader(bytes));decoder.IP=ip;'
if old not in s:
    raise SystemExit('Decoder marker missing')
s=s.replace(old,new,1)
s=s.replace('return $"0x{i.IP-moduleBase:X6}  {i.Mnemonic,-9} len={i.Length,2}{flags}";',
            'return $"0x{i.IP-(ulong)moduleBase:X6}  {i.Mnemonic,-9} len={i.Length,2}{flags}";',1)
p.write_text(s,encoding='utf-8')
print('V23 compile compatibility patch applied')
