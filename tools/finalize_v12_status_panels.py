from pathlib import Path

p=Path('MergedProgram.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('MISSING V11 FRAGMENT:\n'+old)
    s=s.replace(old,new,1)

# Diagnostics build gets extra vertical room. Clean keeps the proven V10/V11 dimensions.
rep('''        Text="BRZE Trainer — Final";
        ClientSize=new Size(1270,770);
        MinimumSize=MaximumSize=Size;''',
'''#if RAW_STATUS
        Text="BRZE Trainer — Diagnostics";
        ClientSize=new Size(1270,920);
        statusText.Font=new Font("Consolas",8.65f);
#else
        Text="BRZE Trainer — Final";
        ClientSize=new Size(1270,770);
#endif
        MinimumSize=MaximumSize=Size;''')

marker='''    void TickTrainer()
    {
        HandleHotkeys();'''
if marker not in s:
    raise SystemExit('MISSING TickTrainer marker')

helpers=r'''    static string S(bool on,bool ready)=>on?(ready?"ON":"ARMED"):"OFF";
    static string Line(string name,string state,string detail="")
        => $"  {name,-14} {state,-7}{detail}";

    string BuildCheatStatus(bool ready,string mapStatus="")
    {
        var x=new List<string>();
        x.Add(Line("Rice",S(rice.Checked,ready),rice.Checked?(ready?"Infinite resource":"Waiting for battle"):""));
        x.Add(Line("Water",S(water.Checked,ready),water.Checked?(ready?"Infinite resource":"Waiting for battle"):""));
        x.Add(Line("Yin / Yang",S(yinYang.Checked,ready),yinYang.Checked?(ready?"Infinite Yin + Yang":"Waiting for battle"):""));
        x.Add(Line("Population",S(population.Checked,ready),population.Checked?(ready?"Cap 9,999,999":"Waiting for battle"):""));
        x.Add(Line("Training",S(training.Checked,ready),training.Checked?(ready?"Instant unit training":"Waiting for battle"):""));
        x.Add(Line("Peasant 3s",S(peasant.Checked,ready),peasant.Checked?(ready?"Fixed 3.0 s target":"Waiting for battle"):""));
        x.Add(Line("Health",S(hp.Checked,ready),hp.Checked?(ready?"Selected-local hard lock":"Waiting for battle"):""));
        x.Add(Line("Stamina",S(stamina.Checked,ready),stamina.Checked?(ready?"Selected-local unlimited":"Waiting for battle"):""));
        x.Add(Line("Horses",S(horses.Checked,ready),horses.Checked?(ready?"Local stable port armed":"Waiting for battle"):(ready?"Manual toggle":"")));
        x.Add(Line("Wolves",S(wolves.Checked,ready),wolves.Checked?(ready?"Wolves Den stock 250":"Waiting for battle"):(ready?"Manual toggle":"")));

        string revealDetail="";
        if(reveal.Checked)
        {
            if(!ready)revealDetail="Waiting for battle";
            else if(mapStatus.Contains("TRANSITION SAFE",StringComparison.OrdinalIgnoreCase))revealDetail="Journey transition guard active";
            else revealDetail="Transition-safe reveal";
        }
        x.Add(Line("Reveal",S(reveal.Checked,ready),revealDetail));
        x.Add(Line("Death Burst",S(burst.Checked,ready),burst.Checked?(ready?"Hover eraser active":"Waiting for battle"):(ready?"Single Kill ready · manual Burst":"")));
        return string.Join("\r\n",x);
    }

    string BaseStatus(GameSnapshot gate,bool ready,string mapStatus="")
    {
        string safety=ready?"Runtime active · guarded writes/hooks":"SAFE STANDBY · no cheat writes/hooks";
        string active=ActiveList();
        return
            "GAME\r\n"+
            $"  Status         {gate.Label}\r\n"+
            $"  Safety         {safety}\r\n\r\n"+
            "ACTIVE CHEATS\r\n"+
            $"  {(ready?active:(active=="none"?"none":"ARMED · "+active))}\r\n\r\n"+
            "CHEAT STATUS\r\n"+
            BuildCheatStatus(ready,mapStatus);
    }

#if RAW_STATUS
    static string RawLine(string name,string raw)=>$"  [{name,-12}] {raw}";
    string RawDiagnostics(string wolf,string death,string horse,string selection,string staminaRaw,string hooks,string native)
    {
        return
            "\r\n\r\nRAW DIAGNOSTICS\r\n"+
            "  Exact subsystem messages for troubleshooting; one source per line.\r\n"+
            RawLine("WOLVES",wolf)+"\r\n"+
            RawLine("DEATH",death)+"\r\n"+
            RawLine("HORSES",horse)+"\r\n"+
            RawLine("PEASANT/SEL",selection)+"\r\n"+
            RawLine("STAMINA",staminaRaw)+"\r\n"+
            RawLine("HOOKS",hooks)+"\r\n"+
            RawLine("NATIVE",native);
    }
#endif

    void TickTrainer()
    {
        HandleHotkeys();'''
s=s.replace(marker,helpers,1)

# Replace safe-standby presentation. Keep runtime gating exactly as V11.
old=r'''                UpdateStatus(
                    $"GAME       {gate.Label}\r\n"+
                    $"ARMED      {ActiveList()}\r\n\r\n"+
                    "SAFE STANDBY\r\n"+
                    "No cheat writes or game hooks are active yet.\r\n"+
                    "Your toggles stay armed and will apply automatically when battle data is ready."+n);'''
new=r'''                string text=BaseStatus(gate,false)+
                    "\r\n\r\nRUNTIME\r\n  Toggles stay armed and apply automatically when battle data becomes ready.";
#if RAW_STATUS
                text+="\r\n\r\nRAW DIAGNOSTICS\r\n  Runtime cores intentionally not attached in SAFE STANDBY.";
#endif
                UpdateStatus(text+n);'''
rep(old,new)

# Replace the in-battle dump with human-readable blocks + optional exact raw subsystem section.
old=r'''            UpdateStatus(
                $"GAME       {gate.Label}\r\n"+
                $"ACTIVE     {ActiveList()}\r\n\r\n"+
                $"DEATH      {(burst.Checked?"BURST / ERASER":"IDLE — SINGLE READY")}\r\n"+
                $"MAP        {OnOff(reveal.Checked),-8}   WOLVES  {OnOff(wolves.Checked),-8}   HORSES  {OnOff(horses.Checked)}\r\n"+
                $"HP         {OnOff(hp.Checked),-8}   STAMINA {OnOff(stamina.Checked),-8}   TRAIN   {OnOff(training.Checked)}\r\n\r\n"+
                wolfStatus+"\r\n"+deathStatus+"\r\n"+horseStatus+"\r\n"+selectionStatus+"\r\n"+staminaStatus+"\r\n"+hookStatus+"\r\n"+nativeStatus+n);'''
new=r'''            string text=BaseStatus(gate,true,mapStatus);
#if RAW_STATUS
            text+=RawDiagnostics(wolfStatus,deathStatus,horseStatus,selectionStatus,staminaStatus,hookStatus,nativeStatus);
#endif
            UpdateStatus(text+n);'''
rep(old,new)

p.write_text(s,encoding='utf-8')
print('V12 clean + diagnostics SYSTEM STATUS layouts applied OK')
