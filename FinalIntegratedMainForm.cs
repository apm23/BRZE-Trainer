using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BRZETrainer;

internal sealed class MainForm : Form
{
    sealed class UnitOption
    {
        public uint Type { get; }
        public string Name { get; }
        public UnitOption(uint type,string name){Type=type;Name=name;}
        public override string ToString()=>$"{Name}   ·   0x{Type:X2}";
    }

    readonly ToggleSwitch rice=new("Rice","F1"),water=new("Water","F2"),yinYang=new("Yin / Yang","F3"),population=new("Population","F4"),training=new("Training","F7"),peasant=new("Peasant 3s","F12"),pausePeasant=new("Pause Peasant","Pause");
    readonly ToggleSwitch hp=new("Health","F6"),stamina=new("Stamina","F5"),horses=new("Horses","F11"),wolves=new("Wolves","F10"),reveal=new("Reveal","Insert"),burst=new("Death Burst","End");

    readonly CheckBox[] unitOn=Enumerable.Range(0,9).Select(_=>new CheckBox{Text="Use",AutoSize=true}).ToArray();
    readonly ComboBox[] unitOut=Enumerable.Range(0,9).Select(_=>new ComboBox()).ToArray();
    readonly Label unitMode=new(){AutoSize=false,TextAlign=ContentAlignment.MiddleRight,Font=new Font("Segoe UI Semibold",9.2f)};
    readonly Label unitRuntime=new(){AutoSize=false,ForeColor=Color.FromArgb(155,166,181),Font=new Font("Segoe UI",8.1f)};
    readonly Label statusText=new(){Dock=DockStyle.Fill,ForeColor=Color.FromArgb(222,228,236),BackColor=Color.Transparent,Font=new Font("Consolas",8.35f),AutoEllipsis=false,Padding=new Padding(0,2,0,0)};
    readonly System.Windows.Forms.Timer timer=new(){Interval=100};
    readonly bool[] held=new bool[48];

    bool runtimeWasReady;
    bool unitChangerRunning;
    DateTime noticeUntilUtc=DateTime.MinValue;
    DateTime nextStatusUtc=DateTime.MinValue;
    string notice="";
    string lastStatus="";

    static readonly UnitOption[] Units=
    {
        new(0,"Dragon Archer"),new(1,"Dragon Chemist"),new(2,"Dragon Dragon Warrior"),new(3,"Dragon Geisha"),new(4,"Dragon Kabuki Warrior"),new(5,"Dragon Peasant"),new(6,"Dragon Powder Keg Cannoneer"),new(7,"Dragon Samurai"),new(8,"Dragon Spearman"),
        new(19,"Serpent Bandit"),new(20,"Serpent Cannoneer"),new(21,"Serpent Crossbowman"),new(22,"Serpent Fan Geisha"),new(23,"Serpent Musketeer"),new(24,"Serpent Peasant"),new(25,"Serpent Raider"),new(26,"Serpent Ronin"),new(29,"Serpent Swordsman"),
        new(30,"Lotus Blade Acolyte"),new(34,"Lotus Channeler"),new(35,"Lotus Diseased One"),new(37,"Lotus Infested One"),new(38,"Lotus Leaf Disciple"),new(39,"Lotus Master Warlock"),new(40,"Lotus Peasant"),new(41,"Lotus Staff Adept"),new(42,"Lotus Unclean One"),new(43,"Lotus Warlock"),
        new(44,"Wolf Ballistaman"),new(45,"Wolf Berserker"),new(46,"Wolf Brawler"),new(47,"Wolf Druidess"),new(48,"Wolf Hurler"),new(49,"Wolf Mauler"),new(50,"Wolf Pack Master"),new(51,"Wolf Peasant"),new(52,"Wolf Pitch Slinger"),new(53,"Wolf Sledger"),new(54,"Wolf Werewolf")
    };

    public MainForm()
    {
        Text="BRZE Trainer — Final Integrated";
        ClientSize=new Size(1380,1000);
        MinimumSize=MaximumSize=Size;
        StartPosition=FormStartPosition.CenterScreen;
        FormBorderStyle=FormBorderStyle.FixedSingle;
        MaximizeBox=false;MinimizeBox=true;BackColor=Color.Black;
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);

        foreach(var t in AllMainToggles())t.Width=182;

        var root=new BattleBackdrop{Dock=DockStyle.Fill,Padding=new Padding(20)};
        Controls.Add(root);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,56));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,56));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,280));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,68));
        root.Controls.Add(layout);

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        head.Controls.Add(new Label{Text="BATTLE REALMS  //  TRAINER",AutoSize=false,Location=new Point(4,2),Size=new Size(850,34),ForeColor=Color.White,BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",18f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft});
        head.Controls.Add(new Label{Text="FINAL INTEGRATED  ·  BRZE 1.60  ·  UNIT CHANGER V5 CORE",AutoSize=false,Location=new Point(7,39),Size=new Size(900,22),ForeColor=Color.FromArgb(174,185,199),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.9f),TextAlign=ContentAlignment.MiddleLeft});
        layout.Controls.Add(head,0,0);

        layout.Controls.Add(MakeToggleRow(new Control[]{rice,water,yinYang,population,training,peasant,pausePeasant}),0,1);
        layout.Controls.Add(MakeToggleRow(new Control[]{hp,stamina,horses,wolves,reveal,burst}),0,2);

        var uc=BuildUnitChangerPanel();
        layout.Controls.Add(uc,0,3);

        var statusBox=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,8,0,8)};
        var statusLayout=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.Transparent,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
        statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));
        statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        statusLayout.Controls.Add(new Label{Text="SYSTEM STATUS",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(109,214,167),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        statusLayout.Controls.Add(statusText,0,1);
        statusBox.Controls.Add(statusLayout);
        layout.Controls.Add(statusBox,0,4);

        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,29,37),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(8,7,8,7),Margin=new Padding(0,2,0,0)};
        var single=new ActionButton("SINGLE KILL","PgDn"){Accent=true,Width=155};
        var build=new ActionButton("BUILD NOW","Del / F8"){Width=155};
        var refresh=new ActionButton("REFRESH TRAINER","Home"){Width=175};
        var allOn=new ActionButton("ALL ON","F9"){Accent=true,Width=145};
        var allOff=new ActionButton("ALL OFF","Shift + F9"){Danger=true,Width=145};
        var signature=new Label{Text="Create By PokakBg",AutoSize=false,Width=465,Height=50,TextAlign=ContentAlignment.MiddleRight,ForeColor=Color.FromArgb(219,188,121),BackColor=Color.Transparent,Font=new Font("Segoe Script",10f,FontStyle.Italic)};
        foreach(Control c in new Control[]{single,build,refresh,allOn,allOff})c.Margin=new Padding(4,0,4,0);
        signature.Margin=new Padding(10,0,2,0);
        actions.Controls.Add(single);actions.Controls.Add(build);actions.Controls.Add(refresh);actions.Controls.Add(allOn);actions.Controls.Add(allOff);actions.Controls.Add(signature);
        layout.Controls.Add(actions,0,5);

        single.Click+=(_,_)=>SafeSingle();
        build.Click+=(_,_)=>SafeBuild();
        refresh.Click+=(_,_)=>RefreshTrainer();
        allOn.Click+=(_,_)=>AllOn();
        allOff.Click+=(_,_)=>AllOff();

        for(int i=0;i<9;i++)
        {
            unitOn[i].CheckedChanged+=(_,_)=>SyncUnitMode();
            unitOut[i].SelectedIndexChanged+=(_,_)=>nextStatusUtc=DateTime.MinValue;
        }
        SetUnitDefaults();
        SyncUnitMode();

        Native.Start();
        timer.Tick+=(_,_)=>TickTrainer();
        timer.Start();
        FormClosed+=(_,_)=>StopAll();
        Shown+=(_,_)=>{GameGate.Probe(true);TickTrainer();};
    }

    IEnumerable<ToggleSwitch> AllMainToggles()
    {
        yield return rice;yield return water;yield return yinYang;yield return population;yield return training;yield return peasant;yield return pausePeasant;
        yield return hp;yield return stamina;yield return horses;yield return wolves;yield return reveal;yield return burst;
    }

    static Control MakeToggleRow(Control[] controls)
    {
        var p=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,29,37),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(5,4,5,4),Margin=new Padding(0,2,0,2)};
        foreach(var c in controls){c.Margin=new Padding(4,0,4,0);p.Controls.Add(c);}return p;
    }

    Control BuildUnitChangerPanel()
    {
        var shell=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,8,0,6),Padding=new Padding(14)};
        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.Transparent,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,48));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var top=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        top.Controls.Add(new Label{Text="UNIT CHANGER",AutoSize=false,Location=new Point(0,0),Size=new Size(180,22),ForeColor=Color.FromArgb(109,214,167),Font=new Font("Segoe UI Semibold",10.4f),TextAlign=ContentAlignment.MiddleLeft});
        top.Controls.Add(new Label{Text="Centang slot saja — mode single / multi aktif otomatis",AutoSize=false,Location=new Point(185,1),Size=new Size(465,21),ForeColor=Color.FromArgb(158,169,184),Font=new Font("Segoe UI",8.5f),TextAlign=ContentAlignment.MiddleLeft});
        unitMode.Location=new Point(930,0);unitMode.Size=new Size(365,22);top.Controls.Add(unitMode);
        unitRuntime.Location=new Point(0,25);unitRuntime.Size=new Size(1290,20);top.Controls.Add(unitRuntime);
        outer.Controls.Add(top,0,0);

        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int r=0;r<3;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,33.333f));
        for(int i=0;i<9;i++)grid.Controls.Add(BuildSlot(i),i%3,i/3);
        outer.Controls.Add(grid,0,1);shell.Controls.Add(outer);return shell;
    }

    Control BuildSlot(int i)
    {
        var p=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(31,37,46),Margin=new Padding(4)};
        var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,BackColor=Color.Transparent,Padding=new Padding(8,7,8,7),Margin=Padding.Empty};
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,50));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,58));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        row.Controls.Add(new Label{Text=$"S{i+1}",Dock=DockStyle.Fill,ForeColor=Color.WhiteSmoke,Font=new Font("Segoe UI Semibold",9.2f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        unitOn[i].Dock=DockStyle.Fill;unitOn[i].ForeColor=Color.FromArgb(216,222,230);unitOn[i].BackColor=Color.Transparent;row.Controls.Add(unitOn[i],1,0);
        unitOut[i].Dock=DockStyle.Fill;unitOut[i].DropDownStyle=ComboBoxStyle.DropDownList;unitOut[i].FlatStyle=FlatStyle.Flat;unitOut[i].BackColor=Color.FromArgb(43,50,61);unitOut[i].ForeColor=Color.WhiteSmoke;unitOut[i].Font=new Font("Segoe UI",8.7f);unitOut[i].Items.AddRange(Units.Cast<object>().ToArray());unitOut[i].SelectedIndex=0;row.Controls.Add(unitOut[i],2,0);
        p.Controls.Add(row);return p;
    }

    void SetUnitDefaults()
    {
        uint[] defs={0,39,54,23,7,43,50,29,2};
        for(int i=0;i<9;i++){int ix=Array.FindIndex(Units,u=>u.Type==defs[i]);if(ix>=0)unitOut[i].SelectedIndex=ix;}
    }

    int ActiveUnitSlots()=>unitOn.Count(x=>x.Checked);

    void SyncUnitMode()
    {
        int n=ActiveUnitSlots();
        unitMode.Text=n==0?"OFF":n==1?"SINGLE OUTPUT · AUTO":$"MULTI OUTPUT · {n} SLOTS · AUTO";
        unitMode.ForeColor=n==0?Color.FromArgb(145,153,165):Color.FromArgb(97,224,179);
        nextStatusUtc=DateTime.MinValue;
        if(n==0&&unitChangerRunning){try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;}
    }

    void SetNotice(string text){notice=text;noticeUntilUtc=DateTime.UtcNow.AddSeconds(3);nextStatusUtc=DateTime.MinValue;}
    void ToggleKey(int vk,int idx,Action action){bool now=(Native.GetAsyncKeyState(vk)&0x8000)!=0;if(now&&!held[idx])action();held[idx]=now;}

    void AllOn()
    {
        // Preserve V11 semantics: manual/advanced features are not changed by ALL ON.
        rice.Checked=true;water.Checked=true;yinYang.Checked=true;population.Checked=true;training.Checked=true;peasant.Checked=true;hp.Checked=true;stamina.Checked=true;reveal.Checked=true;
        SetNotice("ALL ON — Horses / Wolves / Death Burst / Pause / Unit Changer unchanged");
    }

    void AllOff()
    {
        rice.Checked=false;water.Checked=false;yinYang.Checked=false;population.Checked=false;training.Checked=false;peasant.Checked=false;pausePeasant.Checked=false;
        hp.Checked=false;stamina.Checked=false;horses.Checked=false;wolves.Checked=false;reveal.Checked=false;burst.Checked=false;
        foreach(var x in unitOn)x.Checked=false;
        try{PausePeasantCore.Stop();}catch{}try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;
        SetNotice("ALL OFF — every cheat disabled");
    }

    void SafeSingle()
    {
        if(!GameGate.Probe(true).Ready){SetNotice("SINGLE KILL ignored safely — waiting for battle");return;}
        burst.Checked=false;InstantDeathCore.TriggerSingle();SetNotice("SINGLE KILL fired at cursor target");
    }

    void SafeBuild()
    {
        if(!GameGate.Probe(true).Ready){SetNotice("BUILD NOW ignored safely — waiting for battle");return;}
        Native.InstantSelectedBuilding();SetNotice("BUILD NOW triggered for selected building");
    }

    void RefreshTrainer()
    {
        timer.Stop();
        try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;
        try{PausePeasantCore.Stop();}catch{}
        try{WolfCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}
        // Refresh intentionally drops the Reveal hook without issuing a heavy native FOW restore.
        // If Reveal remains checked it is cleanly rebound/reapplied on the next ready tick.
        try{RevealMapCore.Stop(false);}catch{}
        try{Native.Detach();}catch{}
        runtimeWasReady=false;
        try{GameGate.Probe(true);}catch{}
        nextStatusUtc=DateTime.MinValue;
        timer.Start();
        SetNotice("REFRESH TRAINER — runtime cores re-resolved; toggle states preserved");
        TickTrainer();
    }

    string ActiveList()
    {
        var a=new List<string>();
        void Add(ToggleSwitch t,string n){if(t.Checked)a.Add(n);}
        Add(rice,"Rice");Add(water,"Water");Add(yinYang,"Y/Y");Add(population,"Pop");Add(training,"Train");Add(peasant,"Peasant3s");Add(pausePeasant,"PausePeasant");Add(hp,"HP");Add(stamina,"Stamina");Add(horses,"Horses");Add(wolves,"Wolves");Add(reveal,"Reveal");Add(burst,"Burst");
        int uc=ActiveUnitSlots();if(uc>0)a.Add($"UnitChanger×{uc}");
        return a.Count==0?"none":string.Join("  ·  ",a);
    }

    void HandleHotkeys()
    {
        ToggleKey(0x70,1,()=>rice.Checked=!rice.Checked);ToggleKey(0x71,2,()=>water.Checked=!water.Checked);ToggleKey(0x72,3,()=>yinYang.Checked=!yinYang.Checked);
        ToggleKey(0x73,4,()=>population.Checked=!population.Checked);ToggleKey(0x74,5,()=>stamina.Checked=!stamina.Checked);ToggleKey(0x75,6,()=>hp.Checked=!hp.Checked);ToggleKey(0x76,7,()=>training.Checked=!training.Checked);
        ToggleKey(0x77,8,()=>SafeBuild());ToggleKey(0x2E,11,()=>SafeBuild());ToggleKey(0x22,12,()=>SafeSingle());
        ToggleKey(0x21,13,()=>{bool n=!(hp.Checked&&stamina.Checked);hp.Checked=n;stamina.Checked=n;});
        ToggleKey(0x79,10,()=>wolves.Checked=!wolves.Checked);ToggleKey(0x7A,14,()=>horses.Checked=!horses.Checked);ToggleKey(0x7B,15,()=>peasant.Checked=!peasant.Checked);
        ToggleKey(0x2D,16,()=>reveal.Checked=!reveal.Checked);ToggleKey(0x23,17,()=>burst.Checked=!burst.Checked);
        ToggleKey(0x13,18,()=>pausePeasant.Checked=!pausePeasant.Checked); // Pause/Break
        ToggleKey(0x24,19,()=>RefreshTrainer()); // Home

        bool f9=(Native.GetAsyncKeyState(0x78)&0x8000)!=0;
        if(f9&&!held[9]){bool shift=(Native.GetAsyncKeyState(0x10)&0x8000)!=0;if(shift)AllOff();else AllOn();}
        held[9]=f9;
    }

    static string S(bool on,bool ready)=>on?(ready?"ON":"ARMED"):"OFF";
    static string Line(string name,string state,string detail="")=>$"  {name,-15} {state,-7}{detail}";

    string BuildCheatStatus(bool ready,string mapStatus,string pauseStatus,string ucStatus)
    {
        var x=new List<string>
        {
            Line("Rice",S(rice.Checked,ready),rice.Checked?(ready?"Infinite resource":"Waiting for battle"):""),
            Line("Water",S(water.Checked,ready),water.Checked?(ready?"Infinite resource":"Waiting for battle"):""),
            Line("Yin / Yang",S(yinYang.Checked,ready),yinYang.Checked?(ready?"Infinite Yin + Yang":"Waiting for battle"):""),
            Line("Population",S(population.Checked,ready),population.Checked?(ready?"Cap 9,999,999":"Waiting for battle"):""),
            Line("Training",S(training.Checked,ready),training.Checked?(ready?"Instant unit training":"Waiting for battle"):""),
            Line("Peasant 3s",S(peasant.Checked,ready),peasant.Checked?(ready?"Fixed 3.0 s target":"Waiting for battle"):""),
            Line("Pause Peasant",S(pausePeasant.Checked,ready),pausePeasant.Checked?(ready?"Local creation paused":"Waiting for battle"):""),
            Line("Health",S(hp.Checked,ready),hp.Checked?(ready?"Selected-local lock":"Waiting for battle"):""),
            Line("Stamina",S(stamina.Checked,ready),stamina.Checked?(ready?"Selected-local unlimited":"Waiting for battle"):""),
            Line("Horses",S(horses.Checked,ready),horses.Checked?(ready?"Local Stable port":"Waiting for battle"):(ready?"Manual toggle":"")),
            Line("Wolves",S(wolves.Checked,ready),wolves.Checked?(ready?"Wolves Den stock 250":"Waiting for battle"):(ready?"Manual toggle":""))
        };
        string rd=reveal.Checked?(ready?(mapStatus.Contains("TRANSITION SAFE",StringComparison.OrdinalIgnoreCase)?"Journey guard active":"Transition-safe reveal"):"Waiting for battle"):"";
        x.Add(Line("Reveal",S(reveal.Checked,ready),rd));
        x.Add(Line("Death Burst",S(burst.Checked,ready),burst.Checked?(ready?"Hover eraser active":"Waiting for battle"):(ready?"Single Kill ready":"")));
        int n=ActiveUnitSlots();string mode=n==0?"OFF":n==1?"SINGLE":$"MULTI×{n}";
        x.Add(Line("Unit Changer",n==0?"OFF":(ready?"ON":"ARMED"),n==0?"":$"{mode} · {(ready?ucStatus:"Waiting for battle")}"));
        return string.Join("\r\n",x);
    }

    string BuildStatus(GameSnapshot gate,bool ready,string mapStatus,string pauseStatus,string ucStatus)
    {
        string safety=ready?"Runtime active · guarded hooks/writes":"SAFE STANDBY · no cheat hooks/writes";
        return "GAME\r\n"+
               $"  Status          {gate.Label}\r\n"+
               $"  Safety          {safety}\r\n\r\n"+
               "ACTIVE CHEATS\r\n"+
               $"  {(ready?ActiveList():(ActiveList()=="none"?"none":"ARMED · "+ActiveList()))}\r\n\r\n"+
               "CHEAT STATUS\r\n"+
               BuildCheatStatus(ready,mapStatus,pauseStatus,ucStatus);
    }

    static string FirstLine(string s)
    {
        if(string.IsNullOrWhiteSpace(s))return "ready";
        int i=s.IndexOf("\r\n",StringComparison.Ordinal);return i<0?s:s[..i];
    }

    void UpdateStatus(string text){if(text==lastStatus)return;lastStatus=text;statusText.Text=text;}

    void SuspendRuntime()
    {
        try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;
        try{PausePeasantCore.Stop();}catch{}
        try{WolfCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}try{Native.Detach();}catch{}
        // Preserve V11 behavior: Reveal stays attached across Journey/menu transitions so its transition guard can fire.
    }

    void StopAll()
    {
        timer.Stop();
        try{UnitChangerCore.Reset();}catch{}try{PausePeasantCore.Stop();}catch{}
        bool revealSafe=false;try{revealSafe=GameGate.Probe(true).Ready;}catch{}
        try{WolfCore.Stop();}catch{}try{RevealMapCore.Stop(revealSafe);}catch{}try{InstantDeathCore.Stop();}catch{}try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}try{Native.Stop();}catch{}
    }

    void TickTrainer()
    {
        HandleHotkeys();
        SyncUnitMode();
        var gate=GameGate.Probe();
        int ucCount=ActiveUnitSlots();
        if(!gate.Ready)
        {
            if(runtimeWasReady){SuspendRuntime();runtimeWasReady=false;}
            unitRuntime.Text=ucCount==0?"Runtime: off":$"Runtime: ARMED · {ucCount} configured slot{(ucCount==1?"":"s")} · waiting for battle";
            if(DateTime.UtcNow>=nextStatusUtc)
            {
                nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);
                string text=BuildStatus(gate,false,"","",ucCount==0?"off":"armed");
                if(DateTime.UtcNow<noticeUntilUtc)text+="\r\n\r\nNOTICE  "+notice;
                UpdateStatus(text);
            }
            return;
        }

        runtimeWasReady=true;
        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);
        string staminaStatus=StaminaCore.Tick(stamina.Checked);
        string horseStatus=HorseCore.Tick(horses.Checked);
        string deathStatus=InstantDeathCore.Tick(burst.Checked);
        string mapStatus=RevealMapCore.Tick(reveal.Checked);
        string wolfStatus=WolfCore.Tick(wolves.Checked);
        string selectionStatus=SelectionCore.Tick(peasant.Checked);
        string pauseStatus=PausePeasantCore.Tick(pausePeasant.Checked);
        string nativeStatus=Native.Apply(rice.Checked,water.Checked,yinYang.Checked,population.Checked,training.Checked);

        string ucStatus="off";
        if(ucCount>0)
        {
            var en=new bool[9];var outs=new uint[9];
            for(int i=0;i<9;i++){en[i]=unitOn[i].Checked;outs[i]=unitOut[i].SelectedItem is UnitOption u?u.Type:0;}
            UnitChangerCore.Configure(true,en,outs);unitChangerRunning=true;
            ucStatus=FirstLine(UnitChangerCore.Snapshot().Monitor);
            unitRuntime.Text=$"Runtime: {ucStatus}   ·   {ucCount} active slot{(ucCount==1?"":"s")}";
        }
        else
        {
            if(unitChangerRunning){try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;}
            unitRuntime.Text="Runtime: off";
        }

        if(DateTime.UtcNow>=nextStatusUtc)
        {
            nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);
            string text=BuildStatus(gate,true,mapStatus,pauseStatus,ucStatus);
            if(DateTime.UtcNow<noticeUntilUtc)text+="\r\n\r\nNOTICE  "+notice;
            UpdateStatus(text);
        }

        _=hookStatus;_=staminaStatus;_=horseStatus;_=deathStatus;_=wolfStatus;_=selectionStatus;_=nativeStatus;
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_ENTERSIZEMOVE=0x0231,WM_EXITSIZEMOVE=0x0232;
        if(m.Msg==WM_ENTERSIZEMOVE)timer.Stop();
        base.WndProc(ref m);
        if(m.Msg==WM_EXITSIZEMOVE){timer.Start();TickTrainer();}
    }
}
