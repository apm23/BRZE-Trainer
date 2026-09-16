from pathlib import Path
import base64, struct


def req(cond,msg):
    if not cond: raise SystemExit('V36: '+msg)

def rep(s,old,new,msg):
    req(old in s,msg)
    return s.replace(old,new,1)

def between(s,start,end,new,msg):
    a=s.find(start); req(a>=0,msg+' start')
    b=s.find(end,a); req(b>=0,msg+' end')
    return s[:a]+new+s[b:]

# -----------------------------------------------------------------------------
# 1) Main UI/session orchestration: V35 polish + delayed safe start.
#    The delay matters when the trainer is already armed before BRZE enters battle;
#    it avoids installing stage-sensitive runtime pieces on the first transient
#    readable player frame.
# -----------------------------------------------------------------------------
ui=Path('FinalV18MainForm.cs')
s=ui.read_text(encoding='utf-8')
for old,new in [
    ('Text="BRZE Trainer — V35 Diagnostics";','Text="BRZE Trainer — V36 Diagnostics";'),
    ('Text="BRZE Trainer — V35 Clean";','Text="BRZE Trainer — V36 Clean";'),
    ('V35  ·  FINAL RC + 0.1 OFFSET + MOVABLE OVERLAY + UC MEMORY  ·  INTEGRATED','V36  ·  STRESS HARDENED + STAGE SAFE + 500 HERO TARGETS  ·  INTEGRATED'),
    ('OverlayHotkeyControllerV35.Attach(this);','OverlayHotkeyControllerV36.Attach(this);')
]:
    s=rep(s,old,new,'main version marker missing: '+old)

s=rep(s,'    bool runtimeWasReady;\n','    bool runtimeWasReady;\n    DateTime battleReadySinceUtc=DateTime.MinValue;\n    const int BattleReadySettleMs=1500;\n','runtime state marker missing')

# HOME refresh now also restarts the safe-start settling window. SelectionCore.Stop()
# is upgraded below into a real restore/free teardown instead of merely forgetting state.
s=rep(s,'        runtimeWasReady=false;\n        Array.Clear(held,0,held.Length);','        runtimeWasReady=false;\n        battleReadySinceUtc=DateTime.MinValue;\n        Array.Clear(held,0,held.Length);','refresh runtime reset marker missing')

# Gate-not-ready always restarts the settle timer.
s=rep(s,'        if(!gate.Ready)\n        {\n            if(runtimeWasReady){SuspendRuntime();runtimeWasReady=false;}','        if(!gate.Ready)\n        {\n            battleReadySinceUtc=DateTime.MinValue;\n            if(runtimeWasReady){SuspendRuntime();runtimeWasReady=false;}','gate standby marker missing')

# When BRZE becomes readable, require 1.5 s of continuously-ready data before any
# cheat writer/hook starts. This reproduces the stable behavior seen when the user
# opens the trainer only after entering the map.
marker='''        runtimeWasReady=true;\n        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);'''
settle='''        if(!runtimeWasReady)\n        {\n            if(battleReadySinceUtc==DateTime.MinValue)battleReadySinceUtc=DateTime.UtcNow;\n            double elapsed=(DateTime.UtcNow-battleReadySinceUtc).TotalMilliseconds;\n            if(elapsed<BattleReadySettleMs)\n            {\n                unitRuntime.Text=ucCount==0?"Runtime: safe-start settling":$"Runtime: ARMED · {ucCount} slot(s) · safe-start settling";\n                if(DateTime.UtcNow>=nextStatusUtc)\n                {\n                    nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);\n                    string text=BuildStatus(gate,false,"","",ucCount==0?"off":"armed");\n                    text+=$"\\r\\n\\r\\nAUTO SAFE START  battle data settling · {Math.Max(0,(BattleReadySettleMs-elapsed)/1000.0):0.0}s";\n                    if(DateTime.UtcNow<noticeUntilUtc)text+="\\r\\nNOTICE  "+notice;\n                    UpdateStatus(text);\n                }\n                return;\n            }\n        }\n\n        runtimeWasReady=true;\n        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);'''
s=rep(s,marker,settle,'runtime activation marker missing')
ui.write_text(s,encoding='utf-8')

# -----------------------------------------------------------------------------
# 2) Selection / Peasant 3s lifecycle hardening.
#    V35 Stop() only closed the handle and forgot its cave while BRZE code still
#    pointed to that cave. HOME / close+reopen therefore could not reinstall.
#    V36 restores only patches that are still exactly ours, then frees the cave.
#    It also detects fresh stage list roots / list resets and re-arms dynamically.
# -----------------------------------------------------------------------------
p=Path('SelectionCore.cs')
s=p.read_text(encoding='utf-8')
s=rep(s,'    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);\n','    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);\n    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualFreeEx(IntPtr h, IntPtr addr, UIntPtr size, uint freeType);\n','SelectionCore VirtualAlloc marker missing')
s=rep(s,'    const uint PAGE_EXECUTE_READWRITE = 0x40, MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000;','    const uint PAGE_EXECUTE_READWRITE = 0x40, MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, MEM_RELEASE = 0x8000;','SelectionCore allocation constants missing')
s=rep(s,'    static bool infrastructureInstalled, activeArmed, simArmed, headroomArmed;\n    static string error = "";','    static bool infrastructureInstalled, activeArmed, simArmed, headroomArmed;\n    static long lastSimBase;\n    static uint lastLocalId=0xFFFFFFFFu;\n    static string error = "";','SelectionCore state marker missing')
s=rep(s,'        Detach();\n        var ps=Process.GetProcessesByName("Battle_Realms_F");','        Detach(false);\n        var ps=Process.GetProcessesByName("Battle_Realms_F");','SelectionCore Attach detach marker missing')

old='''        uint lid = R32(moduleBase + RVA_LOCAL_ID);\n        long active = moduleBase + RVA_ACTIVE, sim = Sim(lid);\n        if (sim == 0) return Status(fastPeasant, "WAITING SIM");'''
new='''        uint lid = R32(moduleBase + RVA_LOCAL_ID);\n        long active = moduleBase + RVA_ACTIVE, sim = Sim(lid);\n\n        // Journey/stage changes can rebuild the selection lists without changing the BRZE PID.\n        // Do not trust the previous stage's armed booleans: verify the live roots/metadata each tick.\n        bool stageShift=lastLocalId!=0xFFFFFFFFu&&(lid!=lastLocalId||(lastSimBase!=0&&sim!=lastSimBase));\n        if(stageShift){activeArmed=false;simArmed=false;headroomArmed=false;}\n        if(activeArmed&&R32(active+OFF_FIRST)!=SELECTION_LIMIT)activeArmed=false;\n        if(simArmed&&(sim==0||R32(sim+OFF_FIRST)!=SELECTION_LIMIT))simArmed=false;\n        if(headroomArmed&&R32(moduleBase+RVA_EVENT_USED)==0&&R32(moduleBase+RVA_EVENT_REMAIN)!=LOGICAL_WINDOW)headroomArmed=false;\n        lastLocalId=lid;lastSimBase=sim;\n\n        if (sim == 0) return Status(fastPeasant, "WAITING SIM / STAGE REBIND");'''
s=rep(s,old,new,'SelectionCore Tick stage marker missing')

stop_start=s.find('    public static void Stop()')
req(stop_start>=0,'SelectionCore Stop block missing')
stop_end=s.rfind('\n}')
req(stop_end>stop_start,'SelectionCore class end missing')
new_tail=r'''    static void RestoreInfrastructureUnlocked()
    {
        if(h==IntPtr.Zero||moduleBase==0)return;
        try
        {
            if(selectionFlag!=0)W32(selectionFlag,0);
            if(peasantFlag!=0)W32(peasantFlag,0);

            if(infrastructureInstalled)
            {
                int[] listCalls={RVA_SIM_INIT_CALL,RVA_SIM_RESET_CALL,RVA_SORT_A_CALL,RVA_SORT_B_CALL,RVA_SORT_ACTIVE_CALL};
                foreach(int rva in listCalls)
                {
                    long site=moduleBase+rva;
                    if(CallTarget(site)==ctorWrapper)WriteCode(site,Call(site,moduleBase+RVA_LIST_RESET_FN));
                }

                long capSite=moduleBase+RVA_CAP_GATE;
                byte[] ours=Jmp(capSite,capWrapper,13);
                if(Eq(RB(capSite,13),ours))WriteCode(capSite,StockCapBytes());

                long sched=moduleBase+RVA_PEASANT_AUTO_SCHEDULE_CALL;
                if(CallTarget(sched)==peasantWrapper)WriteCode(sched,Call(sched,moduleBase+RVA_PEASANT_SCHEDULE_FN));
                foreach(int rva in new[]{RVA_PEASANT_COUNT_GATE1_CALL,RVA_PEASANT_COUNT_GATE2_CALL})
                {
                    long site=moduleBase+rva;
                    if(CallTarget(site)==peasantCountWrapper)WriteCode(site,Call(site,moduleBase+RVA_PEASANT_POPCOUNT_FN));
                }
            }

            if(Eq(RB(moduleBase+RVA_INIT_REMAIN_IMM,4),EVENT_NEW))WriteCode(moduleBase+RVA_INIT_REMAIN_IMM,EVENT_OLD);
            if(Eq(RB(moduleBase+RVA_RESET_REMAIN_IMM,4),EVENT_NEW))WriteCode(moduleBase+RVA_RESET_REMAIN_IMM,EVENT_OLD);
            if(R32(moduleBase+RVA_EVENT_USED)==0&&R32(moduleBase+RVA_EVENT_REMAIN)==LOGICAL_WINDOW)
                W32(moduleBase+RVA_EVENT_REMAIN,256u);
        }
        catch{}

        // Never free a cave while an instruction still points to it.
        bool referenced=false;
        try
        {
            foreach(int rva in new[]{RVA_SIM_INIT_CALL,RVA_SIM_RESET_CALL,RVA_SORT_A_CALL,RVA_SORT_B_CALL,RVA_SORT_ACTIVE_CALL})
                referenced|=ctorWrapper!=0&&CallTarget(moduleBase+rva)==ctorWrapper;
            referenced|=peasantWrapper!=0&&CallTarget(moduleBase+RVA_PEASANT_AUTO_SCHEDULE_CALL)==peasantWrapper;
            foreach(int rva in new[]{RVA_PEASANT_COUNT_GATE1_CALL,RVA_PEASANT_COUNT_GATE2_CALL})
                referenced|=peasantCountWrapper!=0&&CallTarget(moduleBase+rva)==peasantCountWrapper;
            if(capWrapper!=0)referenced|=Eq(RB(moduleBase+RVA_CAP_GATE,13),Jmp(moduleBase+RVA_CAP_GATE,capWrapper,13));
        }
        catch{referenced=true;}

        if(!referenced&&cave!=IntPtr.Zero)
        {
            try{VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);}catch{}
        }
    }

    public static void Stop()=>Detach(true);

    static void Detach(bool restore)
    {
        if(h!=IntPtr.Zero)
        {
            if(restore)RestoreInfrastructureUnlocked();
            else if(cave!=IntPtr.Zero)
            {
                // Old process / failed attach path: best-effort free only when no live process owns our hook state.
                try{if(p==null||p.HasExited){}else VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);}catch{}
            }
            try{CloseHandle(h);}catch{}
        }
        h=IntPtr.Zero;p=null;moduleBase=0;pid=0;cave=IntPtr.Zero;
        ctorWrapper=capWrapper=peasantWrapper=peasantCountWrapper=selectionFlag=peasantFlag=0;
        infrastructureInstalled=activeArmed=simArmed=headroomArmed=false;
        lastSimBase=0;lastLocalId=0xFFFFFFFFu;
        error="";
    }
'''
s=s[:stop_start]+new_tail+s[stop_end:]
p.write_text(s,encoding='utf-8')

# -----------------------------------------------------------------------------
# 3) Pause Peasant stage safety.
#    Never learn a loading-stage zero as the value to restore. New stage addresses
#    are armed only after the game exposes creation-enable=1; OFF restores 1.
# -----------------------------------------------------------------------------
p=Path('PausePeasantCore.cs')
s=p.read_text(encoding='utf-8')
old=r'''    static void RestoreUnlocked()
    {
        if(saved&&applied&&h!=IntPtr.Zero&&savedAddress!=0)
        {
            try{W32(savedAddress,originalValue);}catch{}
        }
        saved=false;applied=false;savedAddress=0;originalValue=1;
    }
'''
new=r'''    static void RestoreUnlocked()
    {
        if(applied&&h!=IntPtr.Zero&&savedAddress!=0)
        {
            // This flag is explicitly 0=pause, 1=enabled. Never carry a transient
            // loading-stage zero forward as the resume value.
            try{if(R32(savedAddress)==0)W32(savedAddress,1);}catch{}
        }
        saved=false;applied=false;savedAddress=0;originalValue=1;
    }
'''
s=rep(s,old,new,'PausePeasant restore block missing')
old=r'''            if(!saved||savedAddress!=address)
            {
                RestoreUnlocked();
                uint current=R32(address);
                if(current==0xFFFFFFFF)return "PAUSE PEASANT: creation flag unreadable";
                savedAddress=address;originalValue=current;saved=true;
            }

            uint now=R32(address);
            if(now!=0 && !W32(address,0))return "PAUSE PEASANT: write failed";
            applied=true;
            return $"PAUSE PEASANT: ON — local player {lid} creation disabled";'''
new=r'''            if(savedAddress!=address)
            {
                RestoreUnlocked();
                savedAddress=address;originalValue=1;saved=false;applied=false;
            }

            uint now=R32(address);
            if(now==0xFFFFFFFF)return "PAUSE PEASANT: creation flag unreadable";
            if(!saved)
            {
                // During a Journey transition the game can expose zero before the new
                // stage scheduler is initialized. Wait for its native enabled state.
                if(now==0)return "PAUSE PEASANT: ON — waiting for new stage creation flag";
                originalValue=1;saved=true;
            }
            if(now!=0 && !W32(address,0))return "PAUSE PEASANT: write failed";
            applied=true;
            return $"PAUSE PEASANT: ON — local player {lid} creation disabled";'''
s=rep(s,old,new,'PausePeasant stage block missing')
p.write_text(s,encoding='utf-8')

# -----------------------------------------------------------------------------
# 4) Reveal Map transition guard can survive HOME / trainer restart.
#    V35 intentionally left the tiny guard in BRZE on unsafe transitions, but the
#    next trainer instance refused the non-stock entry. V36 recognizes and adopts
#    only the exact guard signature it owns. Transient native-call failures retry.
# -----------------------------------------------------------------------------
p=Path('RevealMapCore.cs')
s=p.read_text(encoding='utf-8')
marker='''    static bool InstallTransitionGuard()
    {'''
adopt=r'''    static bool TryAdoptExistingGuard(long target,byte[] cur)
    {
        if(cur.Length<5||cur[0]!=0xE9)return false;
        long cave=target+5+BitConverter.ToInt32(cur,1);
        if(cave<0x00010000||cave>=0x7FFF0000)return false;
        var stub=new byte[21];
        if(!ReadExact(cave,stub))return false;
        if(stub[0]!=0xC7||stub[1]!=0x05||BitConverter.ToUInt32(stub,2)!=(uint)(moduleBase+RVA_FOG_STATE)||BitConverter.ToUInt32(stub,6)!=1u)return false;
        if(stub[10]!=0xFF||stub[11]!=0x05)return false;
        uint counter=BitConverter.ToUInt32(stub,12);
        for(int i=0;i<FowReinitOriginal.Length;i++)if(stub[16+i]!=FowReinitOriginal[i])return false;
        if(counter<0x00010000||counter>=0x7FFF0000)return false;
        guardCave=A(cave);guardCounterAddr=counter;guardInstalled=true;seenGeneration=R32(guardCounterAddr);
        error="";return true;
    }

'''
req(marker in s,'Reveal install marker missing')
s=s.replace(marker,adopt+marker,1)
old=r'''        for(int i=0;i<cur.Length;i++)if(cur[i]!=FowReinitOriginal[i])
        {
            error="FOW reinit entry not stock — transition guard refused";
            return false;
        }
'''
new=r'''        bool stock=true;for(int i=0;i<cur.Length;i++)if(cur[i]!=FowReinitOriginal[i]){stock=false;break;}
        if(!stock)
        {
            if(TryAdoptExistingGuard(target,cur))return true;
            error="FOW reinit entry busy/mismatch — not our guard";
            return false;
        }
'''
s=rep(s,old,new,'Reveal stock guard block missing')
s=s.replace('reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(1200);','reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(1800);',1)
s=rep(s,'            if(!SetReveal(true))return "MAP: ERROR — "+error;','            if(!SetReveal(true)){reapplyAfterUtc=DateTime.UtcNow.AddMilliseconds(800);return "MAP: RETRY — "+error;}','Reveal retry marker missing')
p.write_text(s,encoding='utf-8')

# -----------------------------------------------------------------------------
# 5) Hero Effect stress capacity.
#    Selection supports 500. Replay queue entry memory has >12 KiB available before
#    FXSCRATCH; 500 replay pointers use only 2000 bytes. Copy Unit remains capped 120.
# -----------------------------------------------------------------------------
p=Path('IntegratedFrameDispatcherCore.cs')
s=p.read_text(encoding='utf-8')
s=rep(s,'    const int MAX_SELECTED=120,MAX_COPY=120;','    const int MAX_SELECTED=500,MAX_COPY=120;','dispatcher MAX_SELECTED marker missing')
p.write_text(s,encoding='utf-8')

p=Path('HeroEffectDirectCoreV30.cs')
s=p.read_text(encoding='utf-8')
s=rep(s,'    const int MAX_SELECTED=120,EXPIRY_MISSING_SAMPLES=4;','    const int MAX_SELECTED=500,EXPIRY_MISSING_SAMPLES=4;','hero MAX_SELECTED marker missing')
s=rep(s,'    static DateTime lastTry;\n    static string status=','    static DateTime lastTry;\n    static DateTime nextTrackMaintenanceUtc=DateTime.MinValue;\n    static string status=','hero maintenance state marker missing')
old=r'''            if(!Attach())return status;
            UpdateTracks();StartNextPendingBatch();ReleaseIdleHolds();
            int live=Tracks.Count(t=>t.State<2);'''
new=r'''            if(!Attach())return status;
            if(DateTime.UtcNow>=nextTrackMaintenanceUtc)
            {
                nextTrackMaintenanceUtc=DateTime.UtcNow.AddMilliseconds(200);
                UpdateTracks();StartNextPendingBatch();ReleaseIdleHolds();
            }
            int live=Tracks.Count(t=>t.State<2);'''
s=rep(s,old,new,'hero Tick maintenance block missing')
s=rep(s,'            Tracks.Clear();ResetRuntimeStateNoWrite();DetachHandle();processId=0;','            Tracks.Clear();ResetRuntimeStateNoWrite();DetachHandle();processId=0;nextTrackMaintenanceUtc=DateTime.MinValue;','hero Shutdown marker missing')
p.write_text(s,encoding='utf-8')

# -----------------------------------------------------------------------------
# 6) Overlay Unit Changer: remove activation-dependent dropdowns from the visible UI.
#    Hidden ComboBoxes remain only as a bridge/model. The player uses previous/next
#    buttons + clan/hero filters, so true fullscreen BRZE keeps focus and does not
#    minimize when configuring units.
# -----------------------------------------------------------------------------
p=Path('OverlayHotkeyControllerV35.cs')
ov=p.read_text(encoding='utf-8').replace('V35','V36')

old='''    readonly ComboBox ucProfile=new(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    readonly ComboBox[] ucUnits=Enumerable.Range(0,9).Select(_=>new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat}).ToArray();
    readonly Button[] ucOn=Enumerable.Range(0,9).Select(_=>new Button()).ToArray();
    readonly Label ucInfo=new();'''
new='''    // Hidden bridge models only; visible Unit Changer uses no dropdown windows.
    readonly ComboBox ucProfile=new(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    readonly ComboBox[] ucUnits=Enumerable.Range(0,9).Select(_=>new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat}).ToArray();
    readonly Label ucProfileValue=new(){AutoEllipsis=true};
    readonly Label[] ucUnitValue=Enumerable.Range(0,9).Select(_=>new Label{AutoEllipsis=true}).ToArray();
    readonly Button[] ucOn=Enumerable.Range(0,9).Select(_=>new Button()).ToArray();
    readonly Label ucInfo=new();'''
ov=rep(ov,old,new,'overlay UC field marker missing')

start='    Panel BuildUnitChangerPage()\n'
end='    void AddFilter(FlowLayoutPanel host,string text,string? group,int width)\n'
page=r'''    Panel BuildUnitChangerPage()
    {
        var page=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        var card=CardPanel("UNIT CHANGER  //  FULL MINI CONTROL  //  FULLSCREEN SAFE",Accent);
        card.Margin=Padding.Empty;page.Controls.Add(card);
        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(14,43,14,12),BackColor=Color.Transparent};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,36));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));

        var profileRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=6,BackColor=Color.Transparent};
        profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,36));profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,36));profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,10));profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));
        profileRow.Controls.Add(new Label{Text="BUILDING",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI Semibold",8.3f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        var prev=FlatButton("‹",32,Off);var next=FlatButton("›",32,Off);prev.Dock=next.Dock=DockStyle.Fill;prev.Margin=next.Margin=new Padding(2);prev.Click+=(_,_)=>CycleProfile(-1);next.Click+=(_,_)=>CycleProfile(1);
        ucProfileValue.Dock=DockStyle.Fill;ucProfileValue.BackColor=Color.FromArgb(44,51,62);ucProfileValue.ForeColor=TextColor;ucProfileValue.Font=new Font("Segoe UI Semibold",8.4f);ucProfileValue.TextAlign=ContentAlignment.MiddleCenter;ucProfileValue.Padding=new Padding(6,0,6,0);
        profileRow.Controls.Add(prev,1,0);profileRow.Controls.Add(ucProfileValue,2,0);profileRow.Controls.Add(next,3,0);
        ucInfo.Dock=DockStyle.Fill;ucInfo.ForeColor=Dim;ucInfo.Font=new Font("Segoe UI",7.6f);ucInfo.TextAlign=ContentAlignment.MiddleRight;ucInfo.AutoEllipsis=true;profileRow.Controls.Add(ucInfo,5,0);outer.Controls.Add(profileRow,0,0);

        var filters=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Padding=Padding.Empty,Margin=Padding.Empty};
        AddFilter(filters,"ALL",null,54);AddFilter(filters,"DRAGON","Dragon",70);AddFilter(filters,"SERPENT","Serpent",72);AddFilter(filters,"LOTUS","Lotus",62);AddFilter(filters,"WOLF","Wolf",58);AddFilter(filters,"HEROES","Heroes",68);outer.Controls.Add(filters,0,1);

        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));for(int r=0;r<3;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,33.333f));
        for(int i=0;i<9;i++)grid.Controls.Add(BuildUnitSlot(i),i%3,i/3);outer.Controls.Add(grid,0,2);

        var bulk=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,BackColor=Color.Transparent};bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));
        var allOn=FlatButton("1–9 ON",92,On);var allOff=FlatButton("ALL OFF",92,Red);allOn.Dock=allOff.Dock=DockStyle.Fill;allOn.Margin=allOff.Margin=new Padding(3);allOn.Click+=(_,_)=>SetAllUnitSlots(true);allOff.Click+=(_,_)=>SetAllUnitSlots(false);bulk.Controls.Add(allOn,0,0);bulk.Controls.Add(allOff,1,0);
        bulk.Controls.Add(new Label{Text="FILTER THEN ‹ / › · NO POPUP DROPDOWN",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI",7.8f),TextAlign=ContentAlignment.MiddleCenter},2,0);
        var back=FlatButton("‹  BACK TO MAIN",150,Off);back.Dock=DockStyle.Fill;back.Margin=new Padding(3);back.Click+=(_,_)=>ShowPage(false);bulk.Controls.Add(back,3,0);outer.Controls.Add(bulk,0,3);
        card.Controls.Add(outer);return page;
    }

'''
ov=between(ov,start,end,page,'overlay BuildUnitChangerPage')

start='    Control BuildUnitSlot(int i)\n'
end='    T? MainField<T>(string field) where T:class\n'
slot=r'''    Control BuildUnitSlot(int i)
    {
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1,BackColor=Color.FromArgb(31,37,46),Padding=new Padding(5),Margin=new Padding(3)};
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,27));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,43));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,28));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,28));
        panel.Controls.Add(new Label{Text=$"S{i+1}",Dock=DockStyle.Fill,ForeColor=TextColor,Font=new Font("Segoe UI Semibold",7.9f),TextAlign=ContentAlignment.MiddleCenter},0,0);
        var on=ucOn[i];on.Text="OFF";on.Dock=DockStyle.Fill;on.Margin=new Padding(2);on.FlatStyle=FlatStyle.Flat;on.FlatAppearance.BorderSize=1;on.FlatAppearance.BorderColor=Color.FromArgb(70,82,101);on.BackColor=Off;on.ForeColor=TextColor;on.Font=new Font("Segoe UI Semibold",7.3f);on.TabStop=false;int slot=i;on.Click+=(_,_)=>ToggleUnitSlot(slot);panel.Controls.Add(on,1,0);
        var left=FlatButton("‹",26,Off);var right=FlatButton("›",26,Off);left.Dock=right.Dock=DockStyle.Fill;left.Margin=right.Margin=new Padding(1);left.Font=right.Font=new Font("Segoe UI Semibold",9f);left.Click+=(_,_)=>CycleUnitSlot(slot,-1);right.Click+=(_,_)=>CycleUnitSlot(slot,1);
        var value=ucUnitValue[i];value.Dock=DockStyle.Fill;value.Margin=new Padding(2);value.BackColor=Color.FromArgb(44,51,62);value.ForeColor=TextColor;value.Font=new Font("Segoe UI",7.5f);value.TextAlign=ContentAlignment.MiddleCenter;value.Padding=new Padding(3,0,3,0);
        panel.Controls.Add(left,2,0);panel.Controls.Add(value,3,0);panel.Controls.Add(right,4,0);return panel;
    }

    void CycleProfile(int delta)
    {
        if(!InitUnitChangerBridge()||ucProfile.Items.Count==0)return;
        int n=ucProfile.Items.Count,ix=ucProfile.SelectedIndex;if(ix<0)ix=0;ix=(ix+delta+n)%n;
        ucProfile.SelectedIndex=ix;UnitProfileChanged();
    }

    void CycleUnitSlot(int slot,int delta)
    {
        if(!InitUnitChangerBridge()||slot<0||slot>=ucUnits.Length)return;
        var cb=ucUnits[slot];if(cb.Items.Count==0)return;
        int n=cb.Items.Count,ix=cb.SelectedIndex;if(ix<0)ix=0;ix=(ix+delta+n)%n;
        cb.SelectedIndex=ix;UnitOutputChanged(slot);
    }

'''
ov=between(ov,start,end,slot,'overlay BuildUnitSlot')

# No focus churn for Unit Changer actions; the overlay remains WS_EX_NOACTIVATE and
# every visible control is now a plain button/label.
ov=ov.replace('        PopulateUnitCombos();\n        OverlayHotkeyControllerV36.ReturnGameFocus();','        PopulateUnitCombos();',1)
ov=ov.replace('        profile.SelectedIndex=ucProfile.SelectedIndex;SyncUnitChanger();OverlayHotkeyControllerV36.ReturnGameFocus();','        profile.SelectedIndex=ucProfile.SelectedIndex;SyncUnitChanger();',1)
ov=ov.replace('ons[i].Checked=!ons[i].Checked;SyncUnitChanger();OverlayHotkeyControllerV36.ReturnGameFocus();','ons[i].Checked=!ons[i].Checked;SyncUnitChanger();',1)
ov=ov.replace('SyncUnitChanger();SetStatus(value?"UNIT CHANGER: S1–S9 ON":"UNIT CHANGER: S1–S9 OFF");OverlayHotkeyControllerV36.ReturnGameFocus();','SyncUnitChanger();SetStatus(value?"UNIT CHANGER: S1–S9 ON":"UNIT CHANGER: S1–S9 OFF");',1)
ov=ov.replace('outs[i].SelectedIndex=e.MainIndex;SyncUnitChanger();OverlayHotkeyControllerV36.ReturnGameFocus();','outs[i].SelectedIndex=e.MainIndex;SyncUnitChanger();',1)

start='    void SyncUnitChanger()\n'
end='    Panel CardPanel(string title,Color accent)\n'
sync=r'''    void SyncUnitChanger()
    {
        if(!InitUnitChangerBridge())return;
        var profile=MainField<ComboBox>("profileSelect");var ons=MainField<CheckBox[]>("slotOn");var outs=MainField<ComboBox[]>("slotOut");if(profile==null||ons==null||outs==null)return;
        ucSyncing=true;
        if(ucProfile.SelectedIndex!=profile.SelectedIndex&&profile.SelectedIndex>=0)ucProfile.SelectedIndex=profile.SelectedIndex;
        ucProfileValue.Text=ucProfile.SelectedIndex>=0&&ucProfile.SelectedIndex<ucProfile.Items.Count?(ucProfile.Items[ucProfile.SelectedIndex]?.ToString()??"BUILDING"):"BUILDING";
        for(int i=0;i<9;i++)
        {
            bool on=ons[i].Checked;ucOn[i].Text=on?"ON":"OFF";ucOn[i].BackColor=on?On:Off;
            int mainIndex=outs[i].SelectedIndex;var cur=ucUnits[i].SelectedItem as UnitEntry;if(cur==null||cur.MainIndex!=mainIndex)PopulateUnitCombo(i,mainIndex);
            cur=ucUnits[i].SelectedItem as UnitEntry;ucUnitValue[i].Text=cur?.Name??"—";
        }
        ucSyncing=false;
        string mode=MainField<Label>("unitMode")?.Text??"";string summary=MainField<Label>("profileSummary")?.Text??"";ucInfo.Text=string.IsNullOrWhiteSpace(mode)?summary:$"{mode} · {summary}";
    }

'''
ov=between(ov,start,end,sync,'overlay SyncUnitChanger')
Path('OverlayHotkeyControllerV36.cs').write_text(ov,encoding='utf-8')

# -----------------------------------------------------------------------------
# 7) Premium trainer icon. The source art was generated specifically for the trainer;
#    the executable icon uses a compact 64x64 PNG-in-ICO so it stays visibly distinct
#    from the original BRZE executable even in the Windows taskbar.
# -----------------------------------------------------------------------------
png_b64='''iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAjLklEQVR42s2bZ7Al13Hff33OpBvevS+HzRnYRV4sMiiSCAQhkAQJkhKDWLIkKtiWrGAFWypVwZI/qOwPcllySS5BpiiJBUFMYhQDQBCBELEgAGKRNucX74s3TjzHH2ZeWBKkJFvl8lTNu3PDmzndp7tPn/7/W4JyzVIcIiAI/5zD/h9880875P/oqT/wKx9wO2ddeCFKMoyxxV3kB9xN1m628Z4i3/80+Z6H/3NUay0bxvGP3+eNnv8DNWPX3zliLUoJvThhz3iN/oqPqA12IIK1YPIRYYzFWrDkr8ZYjF09838x1mKLN8bmp9341GK0dvUDe6moIqCUQomAtYUy8t/YtXvlH6bGYm1+J2OluF7/n1V1KLWuFKUEQRARHNdmtDoJV27u48qtVZ493cBz9JrAqwqwxXtrVxWQj8hYi8FizbpA1q4r/JJBr03qqoLtmp2p4kqL5IIrhREufaa1xX+AtQZjISuUn60pOv/d6vil+KPWlJ5fS/Ecp5ukjNY9Du0b5W+fOUknSuH/2ntzYRSSW5MIau1bg5A/XG34XCmFsYay1jgCphissblFWFPMMmCtkK4KXmgwNQZT/N4Ka1b5xl6w6sYWp+or7r5uG1968RydKEUrwViLIBsstjDFQqMixW8KrSpZn9VcqPxzXZidQnITtBYR0KJQQnGdK0IAT4EPaKUQLI7SiFKF6xmwltjkZ2YsmbXE1hJnBk8rjIXU5p9b1q0jt2K7JsvG8OLcd/0OvnHkAnPLXZQIWeG7dkPQsJc4VO4SuQ+talUKIQthZf2UQkEKQSuFluK9SC68yq3AE8FVlqoIGIvnuKhuF18soiCoVEhdj05q6MYpnTQjMhbXWAItRGlGmBo8USSF8ILkbpQvb4UrFtfF2KW/EtjlTohSgjF2LX7bH7CerAaTVTNGCVoUulCaq3UhZPF5MctS3NVVgoOgVS68FnCVwhNLSQk1rZFOh5vuu5c9117NytnzBAbOv3iY6XPnaWaWrleiaYWVMKZtDFGa0YliSuUS3ShZs4rcfSxiKSxOSI3JgyeQGtDG2geNLRznEh9+o6VG1nIFJfmMuio3eN/RBI4D1lINfOq+i6eEQCt8pQi0ouIqyq5DyVN4ShALrrU4BkgtZBbfwIjrYs+epuwq/D172XXfe7jroz/P/ptvoqogOX8Gt9fDKwdYpUjihN1jg2wZHWB+uYPWGsHmChfB14okSTFpQrXkY9MMZfO4I4Hr2DBJ//GUpPiTa1IV5qvwtMaYlP1bR9m5aZRvv3iMWrmE7ygcayk7iiQzOEpIo5i4F6O1xgt8hoZqDNVL1Cs+npO7UeBo3DiFuSZmroHU6oT7LmfXW+5kZNt2Ltu/myAKefHhR3j605+knSZUhwYJrfCdi/O0LXQyQ5RlpFmGAFGScsWuLRzYNkGn1yOOU549eo7Fbg/xXcdGP0ABwsaVKx+gCGit1nw68BySKOL6K/ZS9h2OHz3D3pE6grBnrB/XWo5faJChGNsyyq03X841ByYYq1oqriJstkk7bbpRxLnpJvv3TxD0+eD2kfqDtBpNXn/tApOLGeHoAS6cOcaug4f48M98FHP2JEc+8TEmn32cmU7MsVbMdDdkPjW004wkM5hexFuv3sPQzk08/u1XaDdW2DE+zMhQP1968dU3tgB5g/RKFRFPicbRCgW4WuWnQH85IFDCSMllol5hsBpQNSlzkbD7wC7eee+NjI3VmDpzjBdfu8BzL53hzOwy52fbdMKEXpzRTQyX1TzqrjAx2s+1e0fYd/kWrjywieFAeOQ5i7vnZr786YfpORV+7Xd+m9tuuZETX/08z/7h79GYW+BYx3K+GzIfp7TihG1K8GsVHj87xV6/xJsP7OPlxhw763UOT80gnuPYOE1/SHopa4mDbFyytCpOTcVR9LmKiqO4Y+cgW+pllqOMgf1XcMe9N1Npz/PUky/wxLdf4+XTs8y2Ey7Ghhas5wEilF0HJzV4Imgr1LVQ01ArOVy3Z4wwy/De9AD3v/0OHvmLv+DV05O87d3v54EP/RjDOubL/+GXOffSqxyPYTmMaUYxlwc+J8Me4+Lwvit28/VWxJ+9+Arv3b6FFxoL6wrYOPOyMd8v0lJBcJQU16ARAkdRcjQVR1P3NWNll1s291HfupW7P/pTDJQMD//xn/O1b7yEl1rCxDAXJszHhqYxREBiLaE1eEqzqeKz1Iko0g4yBFGKQUdTE0UnjmmVSnz8kYfJps5w7uxpPvfN5xncupef/tmPcOuhq/n0L/0Cx558hmXXo9npMuIo+geGuGbXBA9/53UeW1giS2PuHBziifkFtBJ5ME981oNdroDVGVdrEd8pXn1HE2iFqwRfC2VHU3U1Ok3Zcehqbr3xGhajLv/1N/6A+WMX2O76XGyENDoRU6khNhmOyaN0gDBGnlt0k4wxUYzYPFkxAmItUWYIgeG+KsP1MmFqueya69nsGUbqZV56/TgvvXqSxVbE+379V5h87lnKczPUXIf+y3ZxtH+ch8/McXp5nrnlFW4b6Cc0Gad6PcRRyqbGfN/sbzR5paQQXvCUpuRqyo5DzXPQ1lDSCjdNuP8Db+fGHf381ce/xNzpGd61aYA4jPmr822WM8uwEs7ajCYWF8FFuE40IyhWFJzyNbclCs8Ic1gSsSTWEithEsOI9rnt0OUcbbZ5y8//W+zKIvsGKnTqdf74zx9mJinzix/9IO9/4B7+8t3vZsh2+ObgNh4/36LZbFHrzjDSbnGZdvnc8hKRzdAi8qBdtQABRK0JL0VaqgUcJbha42tFxc2VUHIUI2WPNIp470fu59pt/Tz00Bfwl5rc6Zd4rdHjEwtdlo0QiqWHZa9odqK4ySpuQ9iMUFPCbiN86MAOxnoplQwGXJeJwMNzHcY9zS40WzOozzQ4NttgwRhuuP+dfO6hjzOq4EfuPMgzL77Oa8cvMDIywg3veAdfev67PDkToi2kSqFnp7hHCd/sdFgyGZk1aCU8iF03exBEraeqjsqtwALWGPp8l5rvMOg5bKp4mF7Ine+5m+t2DPHQ//o8lV6PMc+n5zr8VaNJzxjKIgw4mmvRXI7lbUpxheuiHc2ocrkBxQqw+eY91BpttibClZUyO1yXcV8TlQIaI1Umqy59vsM2BeXFBfoOXo83McbTj3+Ld91zIzu3jfAPR05y7MRFfnl6hmaa0rLAi0LJwj7iMKs1zWvgUGV+0KYfJmBRoBA77B2vgOXxlpUXbdTg4VGFvNWCnhRIOc1WfRxUcFdiSWqaPn2Df3W9jenKKJMy44dAVPPb3T9P1yjzx1LP8x9/4OU6//hpHvvsC10RNjqaGWZOhrSUyltTm2+8H881NoQCVBz6tBEcrsDBar1Iv+bzj+n0cn15kc8lhHMvwtk18+P5b+fJnH2X2whw6MVxsRnx1uUPPGJJiRzlmhStx+DYZj5iUp0zCbJYPZMUYjtiMYaUJV7rcs7nOvkGfrzQ7PDK3hF/yuGxkgHrgkTZjdrs+J7RhyQsYO3qWoU3jjF53NUvTM4S2xAMP3MdjTx3mxPQ8zaUVfvIj7+ORh/6MYTFMJgZPIMoMqTVkuQXIg6vCQ75x0SrfriolOFrT6oVMLrV57/V7cHoh+zQ42uWuD9zNqSf/gZdfu0hVK16bXmEyTkmNpYkwUFiUQTgjGRfIt7QTSlFVwryxZCJkwEtpwuk0QbTizl1buGffJgaGa1y0sL+/TsUPGPdcpqaWqVXK3LFlBH9oCNkyTke7VPsr3HXPm9l785t44tFv0LBVjnz3CHe/+QY2j4xy+Mkn8X0f7LrwWNAKeXA1ERG1we+V4BSKcFZT3vOLDBnDqbkmE286xP5BxV/+zVPUB+s8cXyKs4lBZ5Y2woASrMCshY7kO8W6FfpFmBfwRXE7mreKy4edMvu1plxyeGBsiE1DNVLfZe/EINftGof+Et1eSCAJW4fKfObIeXqHDjKJYiBMcB2HbGiIrDHDVz/7Jfbv3MyLZ+fxSmXOnz5Py6mhkw7JxfO4WmNNXsvIjEUrpdYsQIolTytVWEFuFb6jqbkulcDjSLPLohvwsx+6nU8/9HkSJ+BLZ+Y4EyYMIKTFhqllLfPW4gFlEfpQfNDxWREYR/ETKG4UzQhwmXZ5xWb4gc+/uXw7khooe8RZShzGEPeQfRN8tlYj2DfMwakGf3L4eT4w3+LuqQanvv44Czu2M7F9K3tGKhyfafHkPzzL/OI8brnOdGOBoa0TVOMefpoQxTEVEd6ydTxfBtUG4fMCRV40rJc8tvZXSaMEx9GsGEPY7fHe99+B21zkW8+8zrEk5fhyF0cJjrW0xdIB2hY0EBQJzdXK4bSC0Bh+DoULLMtqVVCoew4HXR+thCHPJzYGq4E0Zt4RGn6Vrz9/kq+8epp/d+04J2dbTFiP3c023dQy7VjMnv242uXpU/PMNDs0zh9HB1VWVlqkSnF8qUno+dQGhpnYso3JThet4MHVoqGSPM11tEYp2DVQJUxTVqKE1FpKjkI7Lh985038/aceZ7LZ47mlLiov4tABQiApcnwNxMCoUpSUEFvLu0TltXgR+kUxiGJQFNc5PocGagyUfFIriLGY1NBUDlHqMHFhmRsy+JPXT9EoV8i0wzfbHTwlOMbwTNyjMbaHti1z7tQRTlycJWou0umFSFCh02oTdtpMNRaZS+BMZOgfGcbJa4O5vwo2V0JRpVnohKyECUqEPt8hCSMOHryMmVMXOHFmhrPisLqCmCKTrIliAjhliyIlULewNbPsQtBiSVFUEaoWylj6RLBpysryMpVaCbEgvQStoJYmjIQ9EEW9XOV3h8f51SdfZchxETJ+x9XszAze+E5uGqry4vl5zs21aC/OI45H//hWmivLOI5DmmYkJsHvNdncW+L41KkCGJEN+wBZL1FfWO6glNDnObhaaMcpB/Zv56Wjp+gBjdgUdV6oA02gZi1+EdkBtivFDeTltkTAFHdvWUOfdnCsJdXCsyqhdutVvHmpjd+LUYGDshY7s4TtWaQaEHVj7lMeDTfgr8MIURCbjE4cc9XO7WwarvC1506h+sfxndMYrw8qwySz0yRKGIg63B51OGSFs1nGC0NllJXvRV3WESHPySt9UZqx0O5RqpYYHapw/PgFOtohyXIxvQ0YTkngNWvWXGA7QmINulgFlIUMCEWYtYa6aJ6MIv591OP32h1Ojgzi9lKsNSxdbBBvGcS6GtMOSZpdTBizzw3YXyrxI0GZOLP0gNHxrTz+wgl6YY8wEUoDo3Rnz9CdOU1S7mdk7hy/vblC4GnOm4zRTf3ct2cLOQZkN9bMcxBC1krJeWAM44R6f5WlxSYX55ssF1PsCpSV0CyivRQuEQAewnZRREAmilgJFkPX5mcvzXgmSbj8vuvYVPE4cvgIRyfKqJ1DhJU+uOlySq9cxIYpcRgRt3u0w4RZC5fXqtxULTOQpcjgCNt3baFedVHaR+IeIg5YQ2l5El3yeJ8WrhkZozs2TOgaTjdbfOXwyyhr10EIKbalq9DSaolcgCQxlAKXEyenCI2lnWZrMEfLUhQihVJhEQZIJTf1rrVMCpwXSwOLrQQM16soLPsP7WK226M/zPjFWpXnn3qJI9fvRJsOc9MN2hWPYKWLiRMuLnVptxPaVoi0cDGNOeAF9I0McPjZ5zh3do5yySWOI7zqCE6pj9nFWbjwMnUM9SzDCXwW4oiZMCRTCoc1sCmv/4vk6W+amTxX3gB1jfSXaa40iTIIV7HCIp+2BdKaFcuesZBguSAwAHStoR9N21rOhwmBCD/6odv4n0fO8PR3jvE/XI837dvBC0MBf/2xr/H2kRpJL+ZPez3qZAQpXH5gKwOLTdpnGpxvxxwaHcBUKxyZa1KaXCSqb6HdnqcbJgwMDuKX64yuLLIUdpkylkEr+I6mayyOFTAGZw0HLnA+wSJWGNLgK1gwllKhJAdDtxuSCQWKvApfWcgMEZYZBLNa6rIwnRnGRVgCVkzK2JbNjI2XGRss80svnaF0apqP+h67EmgsrHDw1nHOnSzx2NkGuwcr7FDCf07aZHHG7cOXM/eWg7QXQ2pnLqCv3cPiNw+TnphkoRsyM3WEhXbE5QeuJktaDCrFLxvNf4pjrHYZFMtoOWDZWFKAzKAo4KM1sNMYFJZOZhnWmlGlsCa3hNQIvSSjCP5sRBKUQIpQF5UrQPKYUBLBs1B2HPbfdA3GVdgk5hPn53nm1Qu8XVyqqeHjwFebXZ5+dYrtowELriHqhuyMMjZlMKQVLz/xPKeWDc7u3YRXXMVDs12CSj+DtSpz8zNMn3mdH7/7IPvGSozbJnenIXtsSk2E0BjEmLxcT441bh2o5ZjEasU3R2AFY8Aoy0xmGXYU2BxgSFNohBmxXZ99VxR9WhMoRTdL6WUmBycRKkqYtnDrFdu4btMwy9u20T9b5lNPv0yrHfKJzUP8t/k2Z8TyAV9DSROeXcada2PDjKu15ltikMyAsdQCjy8+/En6x0fpppZSOaBU6+PW6w9SHxphZn6e0cF+/KDC7Mwke+MQRxReZvCUwmpFllm0hbrj0G8szipYaK3FCETGEGIIs3wlCGNDWQnaWlphghXBFYjtpWyDsAAhclwOUvLo2rOWFaBhMh777NdZTCxbsNzUX+L5KGSOjBtTw2DapXY2pr+vygrC9hgmifkLYlxrclA0jDDGsDTXQFyXaKGBiHD+4hR3vOXN/OQH3s/cfIPpC1NEpMwuLrBHORgyxtIUUfn4EguxMcwtt1BFzM8HXSCuanV2sThYdLGvX2l1+PG+Er/vGH53foXbDl7HxPgoTWMZnhgjNAZTFH5Whaeofl+yCqhi2fOUIkkzDt5wJYObRvmbLz7NYrsDgccN1rLPWL6SGHolh6Wl1hq34IcdWju5krIMZQxDpRJBpUIjSbjSDRjYvp3rr7+Koxem6EUhZQ0zK23SOMFYS5ImLDTbhFlK0osYVy5vUy4/qYWhis/v25RHVjrEUcrF6TkmxkaZWWkzXO+j5GjmWu28CcOu9x3wRkFQI1hjGR/uZ8++bXz8U4/SjmIcz+UG7TAYZ3y6F1Kplel0ozUy1Q9rkXIcB8f1UHGMrxROvR+jNB1H4xnF83FM++wUB6tloihiarlNliS4RQ9AFEWEUUzFGLYaww2ieJPjMOQqnlCGT4Y90iRln9Kcw7LcbpGcjhkZH+FsmPDAwctZeqrNZCdEr7b0vJECVv1frOW6K3bwladfpJ0ZJiolbiwFTLZ7fCEMqQUerSSlFaXoonlijYD/BgzzoNSH4zm4QZk4y3n9JU9DnDBYKTHd7uItzdM/GTC9vMKp6Wk67TZlpalZGMksO8Vyk2iu9F28wOUZY/ijMOZkklACqpJ3kYyL0LCW2GY0FpYZGR1iJjHsrvcx0+6x6quy3r2z7qaOgI/gi+LabYOcX24zGngEseWVVpe5JMVRQsXzaMdJvpSYPKqrImTLmkJyLZeCMtpxQQTXD0jiLr5fwmYZvW6bLDMEfkC1UuaK8SG6jQXaU7OMKsWoha0iDCL0C/SU5jlt+UKSkGaGgaKElxbPz4o+F0FIlRA7LqmrCZOEe7eM8PyFOabj5BIk8BILyIqCBTbj2LkGW2sBy62Q892YxFp8wFWaJEnRApk1ax0hq4mTxeb+VVBufNcjMhmB55LFXTzXQ7AkSZxjCdbgOYp2L2RheQlvZQklEFvLArAMdMTSMoZ2lhAleW2ir+hc6VmLI0ImEBeptMVSNsJYmpBicMs+y72QbpoVilprGbrUAmRDb41bBIq8l0eRYVEWAhESs9o/lBdLA6BUvFJcd4GSaGLtg6vzGpxSdE1GLHkFOlFgPR9XhIVmi23VErU05ZV2h9HCJb3VbLQYR9daWghxMYerBRtTlOTjohskK2SoFStaKzMkxXfmewjhOUKuFSYz+EoItM7zZBG8IidQAv0WSpa820IgkPwBI8CE0tSVMKAUg9oh1YoB32c5NCwnKY0sJRKhYw2x5CXlZaVYLAfMRDFdY7hx8whHpuYIeyGeCMnqWl1Q22ajmKCoWQoQWYv4PspzidOsKM7mSmFDe9xqfJOigLvW4KVyMojVRQC5ffcEbx8Z4ui5WbTSOFYIUFQx9FmhiuA7gisaVwuuoygL1ESouA595TL1Spl62aPSVyF1hHClS2elRa8bE6cZSZrlyJGxJBYiA12T0hZhEctSktDLMpomp9MnBb9gyA84Gfb42uQ0dZUzvGoTmxjbsoOV5hJJnJA3L+Y4hyB4Whele8isyusaSqGUQmuN1gpnta62Y6ife/fu5O+eeplOlPvKKv1F25zVHRQmqa3kOAG5NbgiuBYCa6kVZtcHRFhihFSEWHJqfCayxg1cTUkTa+kCbWvpCoRYIslnOMGSSg5kPrBnO81NE3xrZpa+SoXBzds5fvIEcRznGZ3J8iC4IS+xBVhrkQKy27A/sRYJyjVrgcDRxElKai2e1pe2yv1TGqg3dGbaDXyD1R60N2zI+N7e4PWm4aLmvx6pMmvJjKHqunSzDK0UWcFQUUrxQ1qNf3g/1Gr3+Goj5Crj65/XQ/7/7jDWrvEX/yUO59KeoHWz///1+JcUHuB/A+j61hZLy8IGAAAAAElFTkSuQmCC'''
png=base64.b64decode(png_b64)
header=struct.pack('<HHH',0,1,1)
entry=struct.pack('<BBBBHHII',64,64,0,0,1,32,len(png),6+16)
Path('BRZE-Trainer-V36.ico').write_bytes(header+entry+png)

proj=Path('MergedTrainerV35.csproj').read_text(encoding='utf-8')
proj=rep(proj,'<AssemblyName>BRZE-Trainer-FINAL-V35</AssemblyName>','<AssemblyName>BRZE-Trainer-FINAL-V36</AssemblyName>','V35 project AssemblyName missing')
proj=rep(proj,'    <PublishTrimmed>false</PublishTrimmed>','    <PublishTrimmed>false</PublishTrimmed>\n    <ApplicationIcon>BRZE-Trainer-V36.ico</ApplicationIcon>','V35 project PublishTrimmed marker missing')
proj=rep(proj,'    <Compile Include="OverlayHotkeyControllerV35.cs" />','    <Compile Include="OverlayHotkeyControllerV36.cs" />','V35 project overlay include missing')
Path('MergedTrainerV36.csproj').write_text(proj,encoding='utf-8')

print('V36 generated: delayed pre-launch safe start + true SelectionCore teardown/rearm + stage-safe Pause Peasant + adoptable/retrying Reveal guard + 500 Hero targets + fullscreen-safe button Unit Changer + premium icon')
