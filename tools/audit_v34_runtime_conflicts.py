from pathlib import Path
import re
import sys

FILES=[
    'MergedProgram.cs','FinalV18MainForm.cs','UnitChangerCore.cs','PausePeasantCore.cs',
    'SelectionCore.cs','HookCore.cs','HorseCore.cs','StaminaCore.cs','InstantDeathCore.cs',
    'RevealMapCore.cs','WolfCore.cs','IntegratedFrameDispatcherCore.cs','HeroEffectDirectCoreV30.cs',
    'IntegratedFeaturePanelsV30.cs','OverlayHotkeyControllerV34.cs'
]

missing=[f for f in FILES if not Path(f).exists()]
if missing:
    raise SystemExit('V34 AUDIT missing generated/compiled files: '+', '.join(missing))

texts={f:Path(f).read_text(encoding='utf-8') for f in FILES}

# Resolve RVA constants per source.
consts={}
for f,s in texts.items():
    m={}
    for name,hx in re.findall(r'const\s+int\s+(RVA_[A-Za-z0-9_]+)\s*=\s*0x([0-9A-Fa-f]+)',s):
        m[name]=int(hx,16)
    consts[f]=m

# Extract actual fixed-address mutation calls. This intentionally looks at write
# call sites rather than every RVA constant, so shared READ-ONLY globals such as
# LOCAL_ID and SELECTION_LIST do not produce false collision alarms.
MUTATORS=('WriteCode','WriteRaw','WriteBytes','W32')

def first_arg_calls(src,fn):
    out=[]
    needle=fn+'('
    pos=0
    while True:
        i=src.find(needle,pos)
        if i<0: break
        # Ignore the helper declaration itself (e.g. static bool WriteCode(long addr,...)).
        before=src[max(0,i-48):i]
        j=i+len(needle);depth=0;k=j
        while k<len(src):
            c=src[k]
            if c=='(': depth+=1
            elif c==')':
                if depth==0: break
                depth-=1
            elif c==',' and depth==0: break
            k+=1
        arg=src[j:k].strip()
        if not re.search(r'\b(?:bool|void|int|long|uint|float|byte\[\])\s*$',before):
            out.append((i,arg))
        pos=i+len(needle)
    return out

def resolve_rva(f,src,callpos,arg):
    # Direct moduleBase + RVA_NAME expression.
    m=re.search(r'moduleBase\s*\+\s*(RVA_[A-Za-z0-9_]+)',arg)
    if m and m.group(1) in consts[f]: return (m.group(1),consts[f][m.group(1)])
    # Simple local alias: find the nearest preceding "alias = moduleBase + RVA_X".
    if re.fullmatch(r'[A-Za-z_]\w*',arg):
        alias=arg
        window=src[max(0,callpos-1800):callpos]
        pats=list(re.finditer(r'\b'+re.escape(alias)+r'\s*=\s*moduleBase\s*\+\s*(RVA_[A-Za-z0-9_]+)',window))
        if pats:
            name=pats[-1].group(1)
            if name in consts[f]: return (name,consts[f][name])
    return None

sites={}
rows=[]
for f,src in texts.items():
    for fn in MUTATORS:
        for pos,arg in first_arg_calls(src,fn):
            r=resolve_rva(f,src,pos,arg)
            if not r: continue
            name,val=r
            key=val
            rows.append((val,f,fn,name,arg))
            sites.setdefault(key,set()).add(f)

# One fixed patch/write address must never be owned by two compiled cores.
collisions={addr:owners for addr,owners in sites.items() if len(owners)>1}
if collisions:
    detail=[]
    for addr,owners in sorted(collisions.items()): detail.append(f'0x{addr:06X}: '+', '.join(sorted(owners)))
    raise SystemExit('V34 AUDIT fixed-address collision(s):\n  '+'\n  '.join(detail))

# Critical shared-frame architecture lock.
frame=0x135C43
owners=sites.get(frame,set())
if owners!={'IntegratedFrameDispatcherCore.cs'}:
    raise SystemExit(f'V34 AUDIT frame hook owner mismatch at 0x{frame:06X}: {sorted(owners)}')

instant=texts['InstantDeathCore.cs']
if 'IntegratedFrameDispatcherCore.DeathTick' not in instant or 'WriteProcessMemory' in instant or 'VirtualAllocEx' in instant:
    raise SystemExit('V34 AUDIT InstantDeathCore is not the dispatcher-only facade')

hero=texts['HeroEffectDirectCoreV30.cs']
if 'IntegratedFrameDispatcherCore.QueueReplayUnits' not in hero:
    raise SystemExit('V34 AUDIT Hero Effect is not using shared dispatcher replay')
if 'RVA_FRAME_MOUSE_DRAW' in hero or 'VirtualAllocEx' in hero:
    raise SystemExit('V34 AUDIT Hero Effect unexpectedly owns a native hook/cave')

ui=texts['FinalV18MainForm.cs']
# StaminaCore remains the sole active stamina runtime path; HookCore stamina arg must stay false.
if 'HookCore.Tick(false,hp.Checked,training.Checked)' not in ui or 'StaminaCore.Tick(stamina.Checked)' not in ui:
    raise SystemExit('V34 AUDIT stamina ownership changed or duplicate path reintroduced')
# Death policy still runs through the one dispatcher facade.
if 'InstantDeathCore.Tick(deathHoldBurst,killAll.Checked)' not in ui:
    raise SystemExit('V34 AUDIT death target-policy dispatcher call missing')
# Hard refresh must tear down the newest runtime state too, then force a fresh BRZE probe.
for marker in ('HeroEffectDirectCoreV30.Shutdown();','IntegratedFrameDispatcherCore.StopAll();','GameGate.Probe(true)','Array.Clear(held,0,held.Length)'):
    if marker not in ui:
        raise SystemExit('V34 AUDIT hard refresh missing: '+marker)

ov=texts['OverlayHotkeyControllerV34.cs']
for marker in ('Opacity=0.91','−0.1','+0.1','Math.Max(0.1f','offsetValue.Text=$"+X  {copyOffset:0.0}"','UNIT CHANGER  //  FULL MINI CONTROL'):
    if marker not in ov:
        raise SystemExit('V34 AUDIT overlay marker missing: '+marker)

print('=== V34 RUNTIME CONFLICT AUDIT ===')
print(f'Compiled sources scanned: {len(FILES)}')
print(f'Fixed module mutation sites resolved: {len(sites)}')
for val,f,fn,name,arg in sorted(rows):
    print(f'  0x{val:06X}  {f:<36} {fn:<10} {name}')
print('PASS: no fixed-address mutation site is owned by multiple compiled cores.')
print('PASS: render hook 0x135C43 has exactly one owner: IntegratedFrameDispatcherCore.')
print('PASS: Instant Death + Hero native calls are serialized through the shared dispatcher.')
print('PASS: HookCore stamina path remains disabled while StaminaCore owns active stamina behavior.')
print('PASS: V34 hard refresh tears down Hero/dispatcher + legacy cores and force re-probes BRZE.')
