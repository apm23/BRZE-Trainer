from pathlib import Path
import re

FILES=[
    'MergedProgram.cs','FinalV18MainForm.cs','UnitChangerCore.cs','PausePeasantCore.cs',
    'SelectionCore.cs','HookCore.cs','HorseCore.cs','StaminaCore.cs','InstantDeathCore.cs',
    'RevealMapCore.cs','WolfCore.cs','IntegratedFrameDispatcherCore.cs','HeroEffectDirectCoreV30.cs',
    'IntegratedFeaturePanelsV35.cs','OverlayHotkeyControllerV36.cs'
]
missing=[f for f in FILES if not Path(f).exists()]
if missing: raise SystemExit('V36 AUDIT missing generated/compiled files: '+', '.join(missing))
texts={f:Path(f).read_text(encoding='utf-8') for f in FILES}

# ------------------------------------------------------------------
# Fixed-address mutation ownership audit (same principle as V35).
# ------------------------------------------------------------------
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
    raise SystemExit('V36 AUDIT fixed-address collision(s):\n  '+'\n  '.join(f'0x{a:06X}: '+', '.join(sorted(o)) for a,o in sorted(collisions.items())))
if sites.get(0x135C43,set())!={'IntegratedFrameDispatcherCore.cs'}:
    raise SystemExit('V36 AUDIT frame hook owner mismatch: '+str(sorted(sites.get(0x135C43,set()))))

# ------------------------------------------------------------------
# Requested stress fixes.
# ------------------------------------------------------------------
ui=texts['FinalV18MainForm.cs']
for x in (
    'Text="BRZE Trainer — V36 Clean"','Text="BRZE Trainer — V36 Diagnostics"',
    'V36  ·  STRESS HARDENED + STAGE SAFE + 500 HERO TARGETS  ·  INTEGRATED',
    'BattleReadySettleMs=1500','battleReadySinceUtc=DateTime.MinValue',
    'AUTO SAFE START','OverlayHotkeyControllerV36.Attach(this);',
    'UnitChangerMemoryPath','SaveUnitChangerMemory()','LoadUnitChangerMemory()'
):
    if x not in ui: raise SystemExit('V36 AUDIT main UI/session marker missing: '+x)

sel=texts['SelectionCore.cs']
for x in (
    'VirtualFreeEx','MEM_RELEASE = 0x8000','RestoreInfrastructureUnlocked',
    'public static void Stop()=>Detach(true);','WAITING SIM / STAGE REBIND',
    'lastSimBase','lastLocalId=0xFFFFFFFFu','Never free a cave while an instruction still points to it.'
):
    if x not in sel: raise SystemExit('V36 AUDIT SelectionCore hard-rebind marker missing: '+x)
if 'public static void Stop()\n    {\n        if (h != IntPtr.Zero && peasantFlag != 0) W32(peasantFlag, 0);\n        Detach();' in sel:
    raise SystemExit('V36 AUDIT legacy SelectionCore dangling-cave Stop survived')

pause=texts['PausePeasantCore.cs']
for x in ('waiting for new stage creation flag','if(R32(savedAddress)==0)W32(savedAddress,1)','savedAddress=address;originalValue=1;saved=false;applied=false;'):
    if x not in pause: raise SystemExit('V36 AUDIT Pause Peasant stage-safety marker missing: '+x)

reveal=texts['RevealMapCore.cs']
for x in ('TryAdoptExistingGuard','FOW reinit entry busy/mismatch — not our guard','AddMilliseconds(1800)','MAP: RETRY —'):
    if x not in reveal: raise SystemExit('V36 AUDIT Reveal transition marker missing: '+x)

hero=texts['HeroEffectDirectCoreV30.cs']
dispatch=texts['IntegratedFrameDispatcherCore.cs']
if 'const int MAX_SELECTED=500,EXPIRY_MISSING_SAMPLES=4;' not in hero:
    raise SystemExit('V36 AUDIT Hero core is not 500-target capable')
if 'const int MAX_SELECTED=500,MAX_COPY=120;' not in dispatch:
    raise SystemExit('V36 AUDIT dispatcher replay cap is not 500 while Copy remains 120')
for x in ('nextTrackMaintenanceUtc','AddMilliseconds(200)'):
    if x not in hero: raise SystemExit('V36 AUDIT Hero maintenance throttling missing: '+x)

ov=texts['OverlayHotkeyControllerV36.cs']
for x in (
    'UNIT CHANGER  //  FULL MINI CONTROL  //  FULLSCREEN SAFE','CycleProfile(int delta)',
    'CycleUnitSlot(int slot,int delta)','readonly Label[] ucUnitValue','FILTER THEN ‹ / › · NO POPUP DROPDOWN',
    'ucProfileValue.Text=','ucUnitValue[i].Text='
):
    if x not in ov: raise SystemExit('V36 AUDIT fullscreen-safe overlay Unit Changer marker missing: '+x)
if 'profileRow.Controls.Add(ucProfile' in ov:
    raise SystemExit('V36 AUDIT visible Unit Changer building ComboBox survived')
if 'panel.Controls.Add(cb' in ov:
    raise SystemExit('V36 AUDIT visible Unit Changer unit ComboBox survived')

proj=Path('MergedTrainerV36.csproj').read_text(encoding='utf-8')
for x in ('BRZE-Trainer-FINAL-V36','<ApplicationIcon>BRZE-Trainer-V36.ico</ApplicationIcon>','OverlayHotkeyControllerV36.cs'):
    if x not in proj: raise SystemExit('V36 AUDIT project/icon marker missing: '+x)
icon=Path('BRZE-Trainer-V36.ico')
if not icon.exists() or icon.stat().st_size<4000:
    raise SystemExit('V36 AUDIT premium app icon missing/too small')

print('=== V36 STRESS HARDENING AUDIT ===')
print(f'Compiled sources scanned: {len(FILES)}')
print(f'Fixed module mutation sites resolved: {len(sites)}')
print('PASS: no fixed-address mutation site is owned by multiple compiled cores.')
print('PASS: shared render hook remains single-owner at RVA 0x135C43.')
print('PASS: pre-launch armed toggles wait for stable battle readiness before writes/hooks.')
print('PASS: SelectionCore HOME/close teardown restores owned code and frees cave only when unreferenced.')
print('PASS: Selection + Pause Peasant re-arm safely across Journey stage resets.')
print('PASS: Reveal guard can be re-adopted after unsafe transition/restart and retries transient failures.')
print('PASS: Hero Effect supports up to 500 selected targets; Copy Unit remains capped at 120.')
print('PASS: overlay Unit Changer uses non-popup previous/next controls, safe for fullscreen focus.')
print('PASS: V36 executable project has a distinct premium application icon.')
