from pathlib import Path
import re

FILES=[
    'MergedProgram.cs','FinalV18MainForm.cs','UnitChangerCore.cs','PausePeasantCore.cs',
    'SelectionCore.cs','HookCore.cs','HorseCore.cs','StaminaCore.cs','InstantDeathCore.cs',
    'RevealMapCore.cs','WolfCore.cs','IntegratedFrameDispatcherCore.cs','HeroEffectDirectCoreV30.cs',
    'IntegratedFeaturePanelsV35.cs','OverlayHotkeyControllerV35.cs'
]
missing=[f for f in FILES if not Path(f).exists()]
if missing: raise SystemExit('V35 AUDIT missing generated/compiled files: '+', '.join(missing))
texts={f:Path(f).read_text(encoding='utf-8') for f in FILES}
consts={}
for f,s in texts.items():
    consts[f]={name:int(hx,16) for name,hx in re.findall(r'const\s+int\s+(RVA_[A-Za-z0-9_]+)\s*=\s*0x([0-9A-Fa-f]+)',s)}

MUTATORS=('WriteCode','WriteRaw','WriteBytes','W32')
def first_arg_calls(src,fn):
    out=[];needle=fn+'(';pos=0
    while True:
        i=src.find(needle,pos)
        if i<0: break
        before=src[max(0,i-48):i];j=i+len(needle);depth=0;k=j
        while k<len(src):
            c=src[k]
            if c=='(': depth+=1
            elif c==')':
                if depth==0: break
                depth-=1
            elif c==',' and depth==0: break
            k+=1
        arg=src[j:k].strip()
        if not re.search(r'\b(?:bool|void|int|long|uint|float|byte\[\])\s*$',before): out.append((i,arg))
        pos=i+len(needle)
    return out

def resolve_rva(f,src,callpos,arg):
    m=re.search(r'moduleBase\s*\+\s*(RVA_[A-Za-z0-9_]+)',arg)
    if m and m.group(1) in consts[f]: return (m.group(1),consts[f][m.group(1)])
    if re.fullmatch(r'[A-Za-z_]\w*',arg):
        window=src[max(0,callpos-1800):callpos]
        pats=list(re.finditer(r'\b'+re.escape(arg)+r'\s*=\s*moduleBase\s*\+\s*(RVA_[A-Za-z0-9_]+)',window))
        if pats:
            name=pats[-1].group(1)
            if name in consts[f]: return (name,consts[f][name])
    return None

sites={};rows=[]
for f,src in texts.items():
    for fn in MUTATORS:
        for pos,arg in first_arg_calls(src,fn):
            r=resolve_rva(f,src,pos,arg)
            if not r: continue
            name,val=r;rows.append((val,f,fn,name));sites.setdefault(val,set()).add(f)
collisions={a:o for a,o in sites.items() if len(o)>1}
if collisions:
    raise SystemExit('V35 AUDIT fixed-address collision(s):\n  '+'\n  '.join(f'0x{a:06X}: '+', '.join(sorted(o)) for a,o in sorted(collisions.items())))

frame=0x135C43
if sites.get(frame,set())!={'IntegratedFrameDispatcherCore.cs'}:
    raise SystemExit(f'V35 AUDIT frame hook owner mismatch: {sorted(sites.get(frame,set()))}')

instant=texts['InstantDeathCore.cs']
if 'IntegratedFrameDispatcherCore.DeathTick' not in instant or 'WriteProcessMemory' in instant or 'VirtualAllocEx' in instant:
    raise SystemExit('V35 AUDIT InstantDeathCore is not dispatcher-only')
hero=texts['HeroEffectDirectCoreV30.cs']
if 'IntegratedFrameDispatcherCore.QueueReplayUnits' not in hero or 'RVA_FRAME_MOUSE_DRAW' in hero or 'VirtualAllocEx' in hero:
    raise SystemExit('V35 AUDIT Hero Effect dispatcher ownership invalid')

ui=texts['FinalV18MainForm.cs']
for marker in (
    'HeroEffectDirectCoreV30.Shutdown();','IntegratedFrameDispatcherCore.StopAll();','GameGate.Probe(true)',
    'Array.Clear(held,0,held.Length)','OverlayHotkeyControllerV35.Attach(this);',
    'UnitChangerMemoryPath','unit-changer-v1.json','SaveUnitChangerMemory()','LoadUnitChangerMemory()',
    'System.Text.Json.JsonSerializer','SelectProfile(0);LoadUnitChangerMemory();SetUnitFilter(-1);return box;'
):
    if marker not in ui: raise SystemExit('V35 AUDIT main/hard-refresh/memory missing: '+marker)
if 'HookCore.Tick(false,hp.Checked,training.Checked)' not in ui or 'StaminaCore.Tick(stamina.Checked)' not in ui:
    raise SystemExit('V35 AUDIT stamina ownership changed')

panel=texts['IntegratedFeaturePanelsV35.cs']
for marker in ('Minimum=0.1m','Maximum=64m','Increment=0.1m','DecimalPlaces=1','Value=0.1m','PasteCopied((float)offset.Value)'):
    if marker not in panel: raise SystemExit('V35 AUDIT main COPY offset missing: '+marker)

overlay=texts['OverlayHotkeyControllerV35.cs']
for marker in (
    'Opacity=0.91','−0.1','+0.1','Math.Max(0.1f','float copyOffset=0.1f','copyOffset=0.1f;UpdateOffset();',
    'public bool UserMoved { get; private set; }','EnableHeaderDrag(head)','EnableHeaderDrag(dragTitle)','EnableHeaderDrag(dragSub)',
    'dragCursorStart=Cursor.Position','Location=new Point(x,y);UserMoved=true','overlay.UserMoved?new Rectangle(overlay.Location,overlay.Size)',
    'OverlayHotkeyControllerV35.ReturnGameFocus();','UNIT CHANGER  //  FULL MINI CONTROL'
):
    if marker not in overlay: raise SystemExit('V35 AUDIT movable overlay/default offset missing: '+marker)
if 'CreateRemoteThread' in overlay: raise SystemExit('V35 overlay must not use remote thread creation')

print('=== V35 RUNTIME CONFLICT AUDIT ===')
print(f'Compiled sources scanned: {len(FILES)}')
print(f'Fixed module mutation sites resolved: {len(sites)}')
for val,f,fn,name in sorted(rows): print(f'  0x{val:06X}  {f:<36} {fn:<10} {name}')
print('PASS: no fixed-address mutation site is owned by multiple compiled cores.')
print('PASS: render hook 0x135C43 has exactly one owner: IntegratedFrameDispatcherCore.')
print('PASS: Hero + death native work remains serialized through the shared dispatcher.')
print('PASS: COPY offset is 0.1..64.0 in 0.1 increments and defaults/resets to 0.1 in both UIs.')
print('PASS: Unit Changer profile/output/slot settings persist as UI-only LOCALAPPDATA JSON.')
print('PASS: 91% overlay is draggable from header and preserves user position across hide/show.')
print('PASS: hard refresh still tears down trainer runtime and force re-probes BRZE.')
