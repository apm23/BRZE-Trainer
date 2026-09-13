from pathlib import Path
import re

# V19 is a thin layer over the locked V18.3 final.
# Scope: UI clipping polish + Instant Death press/hold control + target-mode switch.

# -----------------------------------------------------------------------------
# 1) Compact ToggleSwitch rendering so hotkey text never clips in the 5-row grid.
# -----------------------------------------------------------------------------
p=Path('MergedProgram.cs')
s=p.read_text(encoding='utf-8')

def rep_src(old,new):
    global s
    if old not in s:
        raise SystemExit('V19 MergedProgram marker missing: '+old[:240])
    s=s.replace(old,new,1)

rep_src('        AutoSize=false;Width=190;Height=48;',
        '        AutoSize=false;Width=190;Height=40;')
rep_src('        var mainRect=new Rectangle(14,5,112,21);',
        '        var mainRect=new Rectangle(14,2,112,18);')
rep_src('            TextRenderer.DrawText(g,hotkey,hf,new Rectangle(14,27,112,15),Color.FromArgb(150,160,174),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);',
        '            TextRenderer.DrawText(g,hotkey,hf,new Rectangle(14,20,112,14),Color.FromArgb(150,160,174),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);')
rep_src('        var track=new Rectangle(132,12,46,24);',
        '        var track=new Rectangle(132,8,46,24);')
rep_src('        using(var thumb=new SolidBrush(Color.White))g.FillEllipse(thumb,cx-9,15,18,18);',
        '        using(var thumb=new SolidBrush(Color.White))g.FillEllipse(thumb,cx-9,11,18,18);')
Path('MergedProgram.cs').write_text(s,encoding='utf-8')

# -----------------------------------------------------------------------------
# 2) Extend only the proven V6/V7 hover-kill core with a remote target policy:
#    0 enemy-only (stock proven relation filter), 1 all player units incl. local.
# -----------------------------------------------------------------------------
p=Path('InstantDeathCore.cs')
c=p.read_text(encoding='utf-8')

def rep_core(old,new):
    global c
    if old not in c:
        raise SystemExit('V19 InstantDeathCore marker missing: '+old[:260])
    c=c.replace(old,new,1)

rep_core('    static long moduleBase,lastUnitAddr,lastOwnerAddr,writesAddr,modeAddr;',
         '    static long moduleBase,lastUnitAddr,lastOwnerAddr,writesAddr,modeAddr,targetModeAddr;')

rep_core('''        b.AddRange(new byte[]{0x83,0xF9,0x09,0x0F,0x87});int jBadLocal=b.Count;I32(b,0);\n\n        b.Add(0x50); // target owner''',
'''        b.AddRange(new byte[]{0x83,0xF9,0x09,0x0F,0x87});int jBadLocal=b.Count;I32(b,0);\n\n        // V19 target policy: when targetMode != 0, skip the alliance filter.\n        // Owner/Unit validation remains intact, so this broadens only from enemy-only\n        // to all normal player-owned units, including the local player's units.\n        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)targetModeAddr);b.Add(0x00);\n        b.AddRange(new byte[]{0x0F,0x85});int jKillAll=b.Count;I32(b,0);\n\n        b.Add(0x50); // target owner''')

rep_core('''        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x85});int jAllied=b.Count;I32(b,0);\n\n        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_HP);U32(b,DEATH_SENTINEL);''',
'''        b.AddRange(new byte[]{0x85,0xC0,0x0F,0x85});int jAllied=b.Count;I32(b,0);\n\n        int killTarget=b.Count;\n        b.AddRange(new byte[]{0xC7,0x87});U32(b,OFF_HP);U32(b,DEATH_SENTINEL);''')

rep_core('''        PatchRel(b,jBadLocal,stub+jBadLocal+4,stub+finishActive);\n        PatchRel(b,jAllied,stub+jAllied+4,stub+finishActive);''',
'''        PatchRel(b,jBadLocal,stub+jBadLocal+4,stub+finishActive);\n        PatchRel(b,jKillAll,stub+jKillAll+4,stub+killTarget);\n        PatchRel(b,jAllied,stub+jAllied+4,stub+finishActive);''')

rep_core('''        lastUnitAddr=stub+0x300;lastOwnerAddr=stub+0x304;writesAddr=stub+0x308;modeAddr=stub+0x30C;\n        if(!WriteBytes(lastUnitAddr,new byte[16]))''',
'''        lastUnitAddr=stub+0x300;lastOwnerAddr=stub+0x304;writesAddr=stub+0x308;modeAddr=stub+0x30C;targetModeAddr=stub+0x310;\n        if(!WriteBytes(lastUnitAddr,new byte[20]))''')

start=c.find('    public static bool TriggerSingle()')
end=c.find('    public static void Stop()=>Detach();',start)
if start<0 or end<0:
    raise SystemExit('V19 InstantDeathCore public control block missing')
new_controls=r'''    public static bool TriggerSingle()=>TriggerSingle(false);

    public static bool TriggerSingle(bool killAll)
    {
        if(!Attach()||!Install())return false;
        W32(targetModeAddr,killAll?1u:0u);
        return W32(modeAddr,MODE_SINGLE);
    }

    public static string Tick(bool burstEnabled)=>Tick(burstEnabled,false);

    public static string Tick(bool burstEnabled,bool killAll)
    {
        if(!Attach())return "DEATH V19: waiting for Battle_Realms_F.exe...";

        if(burstEnabled)
        {
            if(!Install())return "DEATH V19: ERROR — "+error;
            W32(targetModeAddr,killAll?1u:0u);
            W32(modeAddr,MODE_BURST);
        }
        else if(installed)
        {
            W32(targetModeAddr,killAll?1u:0u);
            // Do not cancel a freshly requested SINGLE pulse from this same UI tick.
            if(R32(modeAddr)==MODE_BURST)W32(modeAddr,MODE_IDLE);
        }

        if(!installed)return $"DEATH V19: IDLE | press=single / hold=burst | target:{(killAll?"ALL":"ENEMY")}";
        uint mode=R32(modeAddr),u=R32(lastUnitAddr),owner=R32(lastOwnerAddr),writes=R32(writesAddr);
        string own=owner==0xFFFFFFFFu?"-":owner.ToString();
        string m=mode==MODE_BURST?"BURST-HOLD":mode==MODE_SINGLE?"SINGLE-PENDING":"IDLE";
        string target=killAll?"ALL-UNITS":"ENEMY-ONLY";
        return $"DEATH V19: {m} | hover:0x{u:X8} owner:{own} writes:{writes} | {target} / no select";
    }

'''
c=c[:start]+new_controls+c[end:]

c=c.replace('cave=IntPtr.Zero;lastUnitAddr=lastOwnerAddr=writesAddr=modeAddr=0;',
            'cave=IntPtr.Zero;lastUnitAddr=lastOwnerAddr=writesAddr=modeAddr=targetModeAddr=0;')
c=c.replace('lastUnitAddr=lastOwnerAddr=writesAddr=modeAddr=0;error="";',
            'lastUnitAddr=lastOwnerAddr=writesAddr=modeAddr=targetModeAddr=0;error="";')
Path('InstantDeathCore.cs').write_text(c,encoding='utf-8')

# -----------------------------------------------------------------------------
# 3) V18.3 UI -> V19.
# -----------------------------------------------------------------------------
p=Path('FinalV18MainForm.cs')
u=p.read_text(encoding='utf-8')

def must(old,new,count=1):
    global u
    if old not in u:
        raise SystemExit('V19 UI marker missing: '+old[:320])
    u=u.replace(old,new,count)

# Version/title only.
must('Text="BRZE Trainer — V18.3 Diagnostics";','Text="BRZE Trainer — V19 Diagnostics";')
must('Text="BRZE Trainer — V18.3 Clean";','Text="BRZE Trainer — V19 Clean";')
must('V18.3  ·  QUICK UNIT TABS','V19  ·  HOLD DEATH + CLEAN LAYOUT')

# Give the header six more pixels. SYSTEM STATUS remains the same scrollable panel;
# only its available viewport becomes trivially smaller.
old_rows='shell.RowStyles.Add(new RowStyle(SizeType.Absolute,58));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,58));'
new_rows='shell.RowStyles.Add(new RowStyle(SizeType.Absolute,64));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,58));'
must(old_rows,new_rows)

# Building label and compact summary: these are non-scroll UI and must be fully visible.
must('selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,72));\n        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,340));',
     'selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,82));\n        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,330));')
u=u.replace('profileSummary.Text=$"{ActiveProfiles()} buildings active  ·  {TotalSlots()} total outputs";',
            'profileSummary.Text=$"{ActiveProfiles()} ACTIVE  ·  {TotalSlots()} OUT";')

# Give S1..S9 enough width while keeping the checkbox compact.
u=u.replace('row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));\n        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));',
            'row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,34));\n        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,26));')

# Replace the old persistent BURST toggle with target policy.
if 'burst=new("Death Burst","End")' not in u:
    raise SystemExit('V19 expected Death Burst field missing')
u=u.replace('burst=new("Death Burst","End")','killAll=new("Kill All","End")',1)
u=re.sub(r'\bburst\b','killAll',u)
u=u.replace('Death Burst','Kill All')
u=u.replace('Add(killAll,"Burst")','Add(killAll,"KillAll")')

# State for press = single, hold >=260 ms = burst until release.
anchor='    string notice="";\n    string lastStatus="";'
insert='''    string notice="";\n    string lastStatus="";\n    bool deathMouseHeld;\n    bool deathHoldBurst;\n    DateTime deathPressUtc=DateTime.MinValue;\n    const int DeathHoldMs=260;'''
must(anchor,insert)

# Action label.
u=u.replace('new ActionButton("SINGLE KILL","PgDn")','new ActionButton("INSTANT DEATH","PgDn")')

# Mouse behavior: MouseDown fires the single immediately; holding transitions to burst.
must('        single.Click+=(_,_)=>SafeSingle();',
'''        single.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left){deathMouseHeld=true;BeginDeathPress();}};\n        single.MouseUp+=(_,e)=>{if(e.Button==MouseButtons.Left){deathMouseHeld=false;UpdateDeathHold();}};\n        single.MouseLeave+=(_,_)=>{if(deathMouseHeld){deathMouseHeld=false;UpdateDeathHold();}};''')

# Replace old one-shot helper with press/hold helpers.
start=u.find('    void SafeSingle()')
end=u.find('    void SafeBuild()',start)
if start<0 or end<0:
    raise SystemExit('V19 SafeSingle block missing')
new_death_ui=r'''    void BeginDeathPress()
    {
        if(!GameGate.Probe(true).Ready){SetNotice("INSTANT DEATH ignored safely — waiting for battle");return;}
        deathPressUtc=DateTime.UtcNow;deathHoldBurst=false;
        InstantDeathCore.TriggerSingle(killAll.Checked);
        SetNotice($"INSTANT DEATH — single fired · target {(killAll.Checked?"ALL UNITS":"ENEMY ONLY")}");
    }

    void UpdateDeathHold()
    {
        bool keyHeld=(Native.GetAsyncKeyState(0x22)&0x8000)!=0; // PageDown
        bool anyHeld=keyHeld||deathMouseHeld;
        if(!anyHeld){deathHoldBurst=false;deathPressUtc=DateTime.MinValue;return;}
        if(deathPressUtc==DateTime.MinValue)deathPressUtc=DateTime.UtcNow;
        if(!deathHoldBurst&&(DateTime.UtcNow-deathPressUtc).TotalMilliseconds>=DeathHoldMs)
        {
            deathHoldBurst=true;
            SetNotice($"INSTANT DEATH BURST — release to stop · target {(killAll.Checked?"ALL UNITS":"ENEMY ONLY")}");
        }
    }

'''
u=u[:start]+new_death_ui+u[end:]

# Replace PageDown edge action with hold-aware key state. End now toggles Kill All.
old_hot='ToggleKey(0x77,8,()=>SafeBuild());ToggleKey(0x2E,11,()=>SafeBuild());ToggleKey(0x22,12,()=>SafeSingle());'
new_hot='''ToggleKey(0x77,8,()=>SafeBuild());ToggleKey(0x2E,11,()=>SafeBuild());\n        bool deathKey=(Native.GetAsyncKeyState(0x22)&0x8000)!=0;\n        if(deathKey&&!held[12])BeginDeathPress();\n        held[12]=deathKey;'''
must(old_hot,new_hot)

# Ensure hold state is refreshed every 100 ms for keyboard and mouse.
home='        ToggleKey(0x24,19,()=>RefreshTrainer()); // Home'
must(home,home+'\n        UpdateDeathHold();')

# Runtime core call: hold state controls burst, switch controls target policy.
must('string deathStatus=InstantDeathCore.Tick(killAll.Checked);',
     'string deathStatus=InstantDeathCore.Tick(deathHoldBurst,killAll.Checked);')

# User-facing semantics for ALL ON/OFF and status text.
u=u.replace('ALL ON — Horses / Wolves / Kill All / Pause / Unit Changer unchanged',
            'ALL ON — Horses / Wolves / Kill All / Pause / Unit Changer unchanged')
u=u.replace('SINGLE KILL','INSTANT DEATH')

p.write_text(u,encoding='utf-8')
print('V19 generated: clipping polish + press single / hold burst + Enemy Only vs Kill All switch')
