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
        public override string ToString()=>$"{Name}  ·  0x{Type:X2}";
    }

    sealed record BuildingProfile(string Clan,string Name,uint FingerIn,uint FingerOut);

    static readonly BuildingProfile[] Profiles=
    {
        new("DRAGON","Dojo",5,8),new("DRAGON","Target Range",5,0),new("DRAGON","Alchemist Hut",5,1),
        new("SERPENT","Tavern",24,29),new("SERPENT","Sharpshooter's Guild",24,21),new("SERPENT","Alchemist Hut",24,23),
        new("LOTUS","Forge",40,30),new("LOTUS","Blade Garden",40,38),new("LOTUS","Training Yard",40,41),
        new("WOLF","Combat Pit",51,46),new("WOLF","Ballistics Grounds",51,48),new("WOLF","Quarry",51,49)
    };

    static readonly UnitOption[] Units=
    {
        new(0,"Dragon Archer"),new(1,"Dragon Chemist"),new(2,"Dragon Dragon Warrior"),new(3,"Dragon Geisha"),new(4,"Dragon Kabuki Warrior"),new(5,"Dragon Peasant"),new(6,"Dragon Powder Keg Cannoneer"),new(7,"Dragon Samurai"),new(8,"Dragon Spearman"),
        new(19,"Serpent Bandit"),new(20,"Serpent Cannoneer"),new(21,"Serpent Crossbowman"),new(22,"Serpent Fan Geisha"),new(23,"Serpent Musketeer"),new(24,"Serpent Peasant"),new(25,"Serpent Raider"),new(26,"Serpent Ronin"),new(29,"Serpent Swordsman"),
        new(30,"Lotus Blade Acolyte"),new(34,"Lotus Channeler"),new(35,"Lotus Diseased One"),new(37,"Lotus Infested One"),new(38,"Lotus Leaf Disciple"),new(39,"Lotus Master Warlock"),new(40,"Lotus Peasant"),new(41,"Lotus Staff Adept"),new(42,"Lotus Unclean One"),new(43,"Lotus Warlock"),
        new(44,"Wolf Ballistaman"),new(45,"Wolf Berserker"),new(46,"Wolf Brawler"),new(47,"Wolf Druidess"),new(48,"Wolf Hurler"),new(49,"Wolf Mauler"),new(50,"Wolf Pack Master"),new(51,"Wolf Peasant"),new(52,"Wolf Pitch Slinger"),new(53,"Wolf Sledger"),new(54,"Wolf Werewolf")
    };

    readonly ToggleSwitch rice=new("Rice","F1"),water=new("Water","F2"),yinYang=new("Yin / Yang","F3"),population=new("Population","F4"),training=new("Training","F7"),peasant=new("Peasant 3s","F12"),pausePeasant=new("Pause Peasant","Pause");
    readonly ToggleSwitch hp=new("Health","F6"),stamina=new("Stamina","F5"),horses=new("Horses","F11"),wolves=new("Wolves","F10"),reveal=new("Reveal","Insert"),burst=new("Death Burst","End");

    readonly bool[,] profileOn=new bool[12,9];
    readonly uint[,] profileOut=new uint[12,9];
    readonly Button[] profileButtons=new Button[12];
    readonly CheckBox[] slotOn=Enumerable.Range(0,9).Select(_=>new CheckBox{Text="Use",AutoSize=true}).ToArray();
    readonly ComboBox[] slotOut=Enumerable.Range(0,9).Select(_=>new ComboBox()).ToArray();
    readonly Label editTitle=new(){AutoSize=false,TextAlign=ContentAlignment.MiddleLeft};
    readonly Label unitMode=new(){AutoSize=false,TextAlign=ContentAlignment.MiddleRight,Font=new Font("Segoe UI Semibold",9.1f)};
    readonly Label profileSummary=new(){AutoSize=false,ForeColor=Color.FromArgb(153,165,181),Font=new Font("Segoe UI",8.2f),TextAlign=ContentAlignment.MiddleLeft};
    readonly TextBox statusText=new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(24,29,37),ForeColor=Color.FromArgb(224,230,237),Font=new Font("Consolas",8.25f),TabStop=false};
    readonly System.Windows.Forms.Timer timer=new(){Interval=100};
    readonly bool[] held=new bool[48];

    int editProfile;
    bool loadingProfile;
    bool runtimeWasReady;
    bool unitChangerRunning;
    DateTime noticeUntilUtc=DateTime.MinValue,nextStatusUtc=DateTime.MinValue;
    string notice="",lastStatus="";

    public MainForm()
    {
#if RAW_STATUS
        Text="BRZE Trainer — V14 Diagnostics";
#else
        Text="BRZE Trainer — V14 Clean";
#endif
        ClientSize=new Size(1720,820);
        MinimumSize=MaximumSize=Size;
        StartPosition=FormStartPosition.CenterScreen;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;MinimizeBox=true;BackColor=Color.Black;
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);

        foreach(var t in MainToggles()){t.Width=188;t.Height=46;}
        InitProfileDefaults();

        var root=new BattleBackdrop{Dock=DockStyle.Fill,Padding=new Padding(18)};Controls.Add(root);
        var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute,66));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,64));root.Controls.Add(shell);

        var header=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        header.Controls.Add(new Label{Text="BATTLE REALMS  //  TRAINER",AutoSize=false,Location=new Point(3,0),Size=new Size(820,34),ForeColor=Color.White,BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",18f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft});
#if RAW_STATUS
        string mode="DIAGNOSTICS";
#else
        string mode="CLEAN";
#endif
        header.Controls.Add(new Label{Text=$"V14  ·  BUILDING PROFILES  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(880,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});
        shell.Controls.Add(header,0,0);

        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,720));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        content.Controls.Add(BuildLeft(),0,0);content.Controls.Add(BuildRight(),1,0);shell.Controls.Add(content,0,1);
        shell.Controls.Add(BuildActions(),0,2);

        Native.Start();timer.Tick+=(_,_)=>TickTrainer();timer.Start();FormClosed+=(_,_)=>StopAll();Shown+=(_,_)=>{GameGate.Probe(true);TickTrainer();};
    }

    IEnumerable<ToggleSwitch> MainToggles()
    {
        yield return rice;yield return water;yield return yinYang;yield return population;yield return training;yield return peasant;yield return pausePeasant;
        yield return hp;yield return stamina;yield return horses;yield return wolves;yield return reveal;yield return burst;
    }

    Control BuildLeft()
    {
        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Color.Transparent,Margin=new Padding(0,0,10,0),Padding=Padding.Empty};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,264));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var cheats=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,8),Padding=new Padding(12)};
        var cheatLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=5,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)cheatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));for(int r=0;r<5;r++)cheatLayout.RowStyles.Add(new RowStyle(SizeType.Percent,20f));
        Control[] cs={rice,water,yinYang,population,training,peasant,pausePeasant,hp,stamina,horses,wolves,reveal,burst};
        for(int i=0;i<cs.Length;i++){cs[i].Margin=new Padding(4,3,4,3);cheatLayout.Controls.Add(cs[i],i%3,i/3);}
        cheats.Controls.Add(cheatLayout);outer.Controls.Add(cheats,0,0);

        var stat=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,0),Padding=new Padding(14)};
        var statLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        statLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));statLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        statLayout.Controls.Add(new Label{Text="SYSTEM STATUS",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(109,214,167),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        statusText.Dock=DockStyle.Fill;statLayout.Controls.Add(statusText,0,1);stat.Controls.Add(statLayout);outer.Controls.Add(stat,0,1);
        return outer;
    }

    Control BuildRight()
    {
        var box=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(10,0,0,0),Padding=new Padding(14)};
        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,54));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,190));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        head.Controls.Add(new Label{Text="UNIT CHANGER  //  BUILDING SELECTOR",AutoSize=false,Location=new Point(0,0),Size=new Size(450,23),ForeColor=Color.FromArgb(109,214,167),Font=new Font("Segoe UI Semibold",10.4f),TextAlign=ContentAlignment.MiddleLeft});
        head.Controls.Add(new Label{Text="12 vanilla training profiles · each building keeps its own 1–9 output setup",AutoSize=false,Location=new Point(0,26),Size=new Size(620,20),ForeColor=Color.FromArgb(158,170,186),Font=new Font("Segoe UI",8.4f),TextAlign=ContentAlignment.MiddleLeft});
        unitMode.Location=new Point(610,0);unitMode.Size=new Size(325,23);head.Controls.Add(unitMode);outer.Controls.Add(head,0,0);

        var profiles=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=4,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(0,2,0,4)};
        profiles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,105));for(int c=1;c<4;c++)profiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));for(int r=0;r<4;r++)profiles.RowStyles.Add(new RowStyle(SizeType.Percent,25f));
        string[] clans={"DRAGON","SERPENT","LOTUS","WOLF"};
        for(int r=0;r<4;r++)profiles.Controls.Add(new Label{Text=clans[r],Dock=DockStyle.Fill,ForeColor=Color.FromArgb(188,198,210),Font=new Font("Segoe UI Semibold",8.8f),TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(2,3,5,3)},0,r);
        for(int p=0;p<Profiles.Length;p++)
        {
            int ix=p;var b=new Button{Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI Semibold",8.4f),Margin=new Padding(4,3,4,3),Cursor=Cursors.Hand,ForeColor=Color.WhiteSmoke,BackColor=Color.FromArgb(42,49,60)};
            b.FlatAppearance.BorderSize=1;b.Click+=(_,_)=>SelectProfile(ix);profileButtons[p]=b;profiles.Controls.Add(b,1+p%3,p/3);
        }
        outer.Controls.Add(profiles,0,1);

        var edit=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        editTitle.Location=new Point(0,0);editTitle.Size=new Size(640,24);editTitle.Font=new Font("Segoe UI Semibold",9.4f);editTitle.ForeColor=Color.WhiteSmoke;edit.Controls.Add(editTitle);
        profileSummary.Location=new Point(645,0);profileSummary.Size=new Size(290,24);profileSummary.TextAlign=ContentAlignment.MiddleRight;edit.Controls.Add(profileSummary);outer.Controls.Add(edit,0,2);

        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));for(int r=0;r<3;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,33.333f));
        for(int i=0;i<9;i++)grid.Controls.Add(BuildSlot(i),i%3,i/3);outer.Controls.Add(grid,0,3);
        box.Controls.Add(outer);
        UpdateProfileButtons();SelectProfile(0);return box;
    }

    Control BuildSlot(int i)
    {
        var p=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(31,37,46),Margin=new Padding(4)};
        var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,BackColor=Color.Transparent,Padding=new Padding(7,7,7,7),Margin=Padding.Empty};
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,38));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,55));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        row.Controls.Add(new Label{Text=$"S{i+1}",Dock=DockStyle.Fill,ForeColor=Color.WhiteSmoke,Font=new Font("Segoe UI Semibold",8.8f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        slotOn[i].Dock=DockStyle.Fill;slotOn[i].ForeColor=Color.FromArgb(220,226,233);slotOn[i].BackColor=Color.Transparent;slotOn[i].CheckedChanged+=(_,_)=>SlotChanged(i);row.Controls.Add(slotOn[i],1,0);
        slotOut[i].Dock=DockStyle.Fill;slotOut[i].DropDownStyle=ComboBoxStyle.DropDownList;slotOut[i].FlatStyle=FlatStyle.Flat;slotOut[i].BackColor=Color.FromArgb(44,51,62);slotOut[i].ForeColor=Color.WhiteSmoke;slotOut[i].Font=new Font("Segoe UI",8.3f);slotOut[i].Items.AddRange(Units.Cast<object>().ToArray());slotOut[i].SelectedIndexChanged+=(_,_)=>SlotChanged(i);row.Controls.Add(slotOut[i],2,0);
        p.Controls.Add(row);return p;
    }

    Control BuildActions()
    {
        var p=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,29,37),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(8,6,8,6),Margin=new Padding(0,4,0,0)};
        var single=new ActionButton("SINGLE KILL","PgDn"){Accent=true,Width=152};var build=new ActionButton("BUILD NOW","Del / F8"){Width=152};var refresh=new ActionButton("REFRESH TRAINER","Home"){Width=178};var allOn=new ActionButton("ALL ON","F9"){Accent=true,Width=142};var allOff=new ActionButton("ALL OFF","Shift + F9"){Danger=true,Width=142};
        var sig=new Label{Text="Create By PokakBg",AutoSize=false,Width=760,Height=48,TextAlign=ContentAlignment.MiddleRight,ForeColor=Color.FromArgb(219,188,121),BackColor=Color.Transparent,Font=new Font("Segoe Script",9.8f,FontStyle.Italic)};
        foreach(Control c in new Control[]{single,build,refresh,allOn,allOff})c.Margin=new Padding(4,0,4,0);sig.Margin=new Padding(8,0,0,0);
        p.Controls.Add(single);p.Controls.Add(build);p.Controls.Add(refresh);p.Controls.Add(allOn);p.Controls.Add(allOff);p.Controls.Add(sig);
        single.Click+=(_,_)=>SafeSingle();build.Click+=(_,_)=>SafeBuild();refresh.Click+=(_,_)=>RefreshTrainer();allOn.Click+=(_,_)=>AllOn();allOff.Click+=(_,_)=>AllOff();return p;
    }

    void InitProfileDefaults()
    {
        uint[] defs={0,39,54,23,7,43,50,29,2};
        for(int p=0;p<12;p++)for(int i=0;i<9;i++){profileOn[p,i]=false;profileOut[p,i]=defs[i];}
    }

    int ProfileSlots(int p){int n=0;for(int i=0;i<9;i++)if(profileOn[p,i])n++;return n;}
    int TotalSlots(){int n=0;for(int p=0;p<12;p++)n+=ProfileSlots(p);return n;}
    int ActiveProfiles(){int n=0;for(int p=0;p<12;p++)if(ProfileSlots(p)>0)n++;return n;}

    void SelectProfile(int p)
    {
        editProfile=Math.Clamp(p,0,Profiles.Length-1);loadingProfile=true;
        for(int i=0;i<9;i++)
        {
            slotOn[i].Checked=profileOn[editProfile,i];int ix=Array.FindIndex(Units,u=>u.Type==profileOut[editProfile,i]);slotOut[i].SelectedIndex=ix>=0?ix:0;
        }
        loadingProfile=false;UpdateProfileButtons();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void SlotChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;profileOn[editProfile,i]=slotOn[i].Checked;if(slotOut[i].SelectedItem is UnitOption u)profileOut[editProfile,i]=u.Type;UpdateProfileButtons();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void UpdateProfileButtons()
    {
        for(int p=0;p<Profiles.Length;p++)
        {
            var b=profileButtons[p];if(b==null)continue;int n=ProfileSlots(p);b.Text=n==0?Profiles[p].Name:$"{Profiles[p].Name}   [{n}]";
            bool selected=p==editProfile;b.BackColor=selected?Color.FromArgb(47,111,90):(n>0?Color.FromArgb(39,81,68):Color.FromArgb(42,49,60));b.FlatAppearance.BorderColor=selected?Color.FromArgb(105,216,168):Color.FromArgb(69,78,92);
        }
    }

    void UpdateUnitLabels()
    {
        var p=Profiles[editProfile];int n=ProfileSlots(editProfile);editTitle.Text=$"EDITING  {p.Clan}  /  {p.Name}";unitMode.Text=n==0?"OFF":n==1?"SINGLE OUTPUT · AUTO":$"MULTI OUTPUT · {n} SLOTS · AUTO";unitMode.ForeColor=n==0?Color.FromArgb(145,154,166):Color.FromArgb(97,224,179);
        profileSummary.Text=$"active buildings: {ActiveProfiles()} / 12   ·   total slots: {TotalSlots()}";
    }

    void PushProfiles()
    {
        if(TotalSlots()==0){if(unitChangerRunning){try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;}return;}
        UnitChangerCore.ConfigureProfiles(profileOn,profileOut);unitChangerRunning=true;
    }

    void SetNotice(string s){notice=s;noticeUntilUtc=DateTime.UtcNow.AddSeconds(3);nextStatusUtc=DateTime.MinValue;}
    void ToggleKey(int vk,int idx,Action action){bool now=(Native.GetAsyncKeyState(vk)&0x8000)!=0;if(now&&!held[idx])action();held[idx]=now;}

    void AllOn()
    {
        rice.Checked=true;water.Checked=true;yinYang.Checked=true;population.Checked=true;training.Checked=true;peasant.Checked=true;hp.Checked=true;stamina.Checked=true;reveal.Checked=true;
        SetNotice("ALL ON — Horses / Wolves / Death Burst / Pause / Building Profiles unchanged");
    }

    void AllOff()
    {
        rice.Checked=false;water.Checked=false;yinYang.Checked=false;population.Checked=false;training.Checked=false;peasant.Checked=false;pausePeasant.Checked=false;hp.Checked=false;stamina.Checked=false;horses.Checked=false;wolves.Checked=false;reveal.Checked=false;burst.Checked=false;
        for(int p=0;p<12;p++)for(int i=0;i<9;i++)profileOn[p,i]=false;SelectProfile(editProfile);try{PausePeasantCore.Stop();}catch{}try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;SetNotice("ALL OFF — every cheat and building profile disabled");
    }

    void SafeSingle(){if(!GameGate.Probe(true).Ready){SetNotice("SINGLE KILL ignored safely — waiting for battle");return;}burst.Checked=false;InstantDeathCore.TriggerSingle();SetNotice("SINGLE KILL fired at cursor target");}
    void SafeBuild(){if(!GameGate.Probe(true).Ready){SetNotice("BUILD NOW ignored safely — waiting for battle");return;}Native.InstantSelectedBuilding();SetNotice("BUILD NOW triggered for selected building");}

    void RefreshTrainer()
    {
        timer.Stop();try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;try{PausePeasantCore.Stop();}catch{}try{WolfCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}try{RevealMapCore.Stop(false);}catch{}try{Native.Detach();}catch{}runtimeWasReady=false;try{GameGate.Probe(true);}catch{}nextStatusUtc=DateTime.MinValue;timer.Start();SetNotice("REFRESH TRAINER — all profile/toggle selections preserved");
    }

    string ActiveList()
    {
        var a=new List<string>();void Add(ToggleSwitch t,string n){if(t.Checked)a.Add(n);}Add(rice,"Rice");Add(water,"Water");Add(yinYang,"Y/Y");Add(population,"Pop");Add(training,"Train");Add(peasant,"Peasant3s");Add(pausePeasant,"PausePeasant");Add(hp,"HP");Add(stamina,"Stamina");Add(horses,"Horses");Add(wolves,"Wolves");Add(reveal,"Reveal");Add(burst,"Burst");if(ActiveProfiles()>0)a.Add($"Profiles×{ActiveProfiles()}/{TotalSlots()}slots");return a.Count==0?"none":string.Join(" · ",a);
    }

    void HandleHotkeys()
    {
        ToggleKey(0x70,1,()=>rice.Checked=!rice.Checked);ToggleKey(0x71,2,()=>water.Checked=!water.Checked);ToggleKey(0x72,3,()=>yinYang.Checked=!yinYang.Checked);ToggleKey(0x73,4,()=>population.Checked=!population.Checked);ToggleKey(0x74,5,()=>stamina.Checked=!stamina.Checked);ToggleKey(0x75,6,()=>hp.Checked=!hp.Checked);ToggleKey(0x76,7,()=>training.Checked=!training.Checked);ToggleKey(0x77,8,()=>SafeBuild());ToggleKey(0x2E,11,()=>SafeBuild());ToggleKey(0x22,12,()=>SafeSingle());ToggleKey(0x21,13,()=>{bool n=!(hp.Checked&&stamina.Checked);hp.Checked=n;stamina.Checked=n;});ToggleKey(0x79,10,()=>wolves.Checked=!wolves.Checked);ToggleKey(0x7A,14,()=>horses.Checked=!horses.Checked);ToggleKey(0x7B,15,()=>peasant.Checked=!peasant.Checked);ToggleKey(0x2D,16,()=>reveal.Checked=!reveal.Checked);ToggleKey(0x23,17,()=>burst.Checked=!burst.Checked);ToggleKey(0x13,18,()=>pausePeasant.Checked=!pausePeasant.Checked);ToggleKey(0x24,19,()=>RefreshTrainer());
        bool f9=(Native.GetAsyncKeyState(0x78)&0x8000)!=0;if(f9&&!held[9]){bool shift=(Native.GetAsyncKeyState(0x10)&0x8000)!=0;if(shift)AllOff();else AllOn();}held[9]=f9;
    }

    static string S(bool on,bool ready)=>on?(ready?"ON":"ARMED"):"OFF";
    static string Line(string name,string state,string detail="")=>$"  {name,-15} {state,-7}{detail}";

    string BuildCheatStatus(bool ready,string mapStatus)
    {
        var x=new List<string>
        {
            Line("Rice",S(rice.Checked,ready)),Line("Water",S(water.Checked,ready)),Line("Yin / Yang",S(yinYang.Checked,ready)),Line("Population",S(population.Checked,ready),population.Checked&&ready?"Cap 9,999,999":""),Line("Training",S(training.Checked,ready)),Line("Peasant 3s",S(peasant.Checked,ready)),Line("Pause Peasant",S(pausePeasant.Checked,ready)),Line("Health",S(hp.Checked,ready)),Line("Stamina",S(stamina.Checked,ready)),Line("Horses",S(horses.Checked,ready),ready&&!horses.Checked?"Manual":""),Line("Wolves",S(wolves.Checked,ready),ready&&!wolves.Checked?"Manual":"")
        };
        string rd=reveal.Checked?(ready?(mapStatus.Contains("TRANSITION SAFE",StringComparison.OrdinalIgnoreCase)?"Journey guard active":"Transition-safe reveal"):"Waiting for battle"):"";x.Add(Line("Reveal",S(reveal.Checked,ready),rd));x.Add(Line("Death Burst",S(burst.Checked,ready),ready&&!burst.Checked?"Single Kill ready":""));int ap=ActiveProfiles(),ts=TotalSlots();x.Add(Line("Unit Changer",ts==0?"OFF":(ready?"ON":"ARMED"),ts==0?"":$"{ap} buildings · {ts} slots"));return string.Join("\r\n",x);
    }

    string BuildStatus(GameSnapshot gate,bool ready,string mapStatus,string wolf,string death,string horse,string selection,string staminaRaw,string hooks,string native,string pause,string ucGame,string ucContext,string ucMonitor,string ucRecipes)
    {
        string active=ActiveList();string text="GAME\r\n"+$"  Status          {gate.Label}\r\n"+$"  Safety          {(ready?"Runtime active · guarded hooks/writes":"SAFE STANDBY · no cheat hooks/writes")}\r\n\r\n"+"ACTIVE CHEATS\r\n"+$"  {(ready?active:(active=="none"?"none":"ARMED · "+active))}\r\n\r\n"+"CHEAT STATUS\r\n"+BuildCheatStatus(ready,mapStatus);
#if RAW_STATUS
        text+="\r\n\r\nRAW DIAGNOSTICS\r\n"+$"  [WOLVES]       {wolf}\r\n"+$"  [DEATH]        {death}\r\n"+$"  [HORSES]       {horse}\r\n"+$"  [PEASANT/SEL]  {selection}\r\n"+$"  [STAMINA]      {staminaRaw}\r\n"+$"  [HOOKS]        {hooks}\r\n"+$"  [NATIVE]       {native}\r\n"+$"  [PAUSE]        {pause}\r\n"+$"  [UC GAME]      {ucGame}\r\n"+$"  [UC CONTEXT]   {ucContext}\r\n"+$"  [UC MONITOR]   {ucMonitor}\r\n"+$"  [UC RECIPES]   {ucRecipes}";
#endif
        return text;
    }

    void UpdateStatus(string s){if(s==lastStatus)return;lastStatus=s;statusText.Text=s;}

    void SuspendRuntime()
    {
        try{UnitChangerCore.Reset();}catch{}unitChangerRunning=false;try{PausePeasantCore.Stop();}catch{}try{WolfCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}try{Native.Detach();}catch{}
    }

    void StopAll()
    {
        timer.Stop();bool revealSafe=false;try{revealSafe=GameGate.Probe(true).Ready;}catch{}try{UnitChangerCore.Reset();}catch{}try{PausePeasantCore.Stop();}catch{}try{WolfCore.Stop();}catch{}try{RevealMapCore.Stop(revealSafe);}catch{}try{InstantDeathCore.Stop();}catch{}try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}try{Native.Stop();}catch{}
    }

    void TickTrainer()
    {
        HandleHotkeys();UpdateUnitLabels();var gate=GameGate.Probe();
        if(!gate.Ready)
        {
            if(runtimeWasReady){SuspendRuntime();runtimeWasReady=false;}
            if(DateTime.UtcNow>=nextStatusUtc){nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);string n=DateTime.UtcNow<noticeUntilUtc?"\r\n\r\nNOTICE\r\n  "+notice:"";UpdateStatus(BuildStatus(gate,false,"","","","","","","","","","","","")+n);}return;
        }

        runtimeWasReady=true;string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);string staminaStatus=StaminaCore.Tick(stamina.Checked);Native.ApplyLegacyRuntime(false,false,false,false);string horseStatus=HorseCore.Tick(horses.Checked);string deathStatus=InstantDeathCore.Tick(burst.Checked);string mapStatus=RevealMapCore.Tick(reveal.Checked);string wolfStatus=WolfCore.Tick(wolves.Checked);string selectionStatus=SelectionCore.Tick(peasant.Checked);string pauseStatus=PausePeasantCore.Tick(pausePeasant.Checked);string nativeStatus=Native.Apply(rice.Checked,water.Checked,yinYang.Checked,population.Checked,training.Checked);

        PushProfiles();string ucGame="OFF",ucContext="No active building profile.",ucMonitor="OFF",ucRecipes="";
        if(TotalSlots()>0)
        {
            var snap=UnitChangerCore.Snapshot();ucGame=snap.Game;ucContext=snap.Context;ucMonitor=snap.Monitor;ucRecipes=snap.Recipes;
            profileSummary.Text=$"active buildings: {ActiveProfiles()} / 12   ·   total slots: {TotalSlots()}   ·   runtime: active";
        }

        if(DateTime.UtcNow>=nextStatusUtc){nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);string n=DateTime.UtcNow<noticeUntilUtc?"\r\n\r\nNOTICE\r\n  "+notice:"";UpdateStatus(BuildStatus(gate,true,mapStatus,wolfStatus,deathStatus,horseStatus,selectionStatus,staminaStatus,hookStatus,nativeStatus,pauseStatus,ucGame,ucContext,ucMonitor,ucRecipes)+n);}
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_ENTERSIZEMOVE=0x0231,WM_EXITSIZEMOVE=0x0232;if(m.Msg==WM_ENTERSIZEMOVE)timer.Stop();base.WndProc(ref m);if(m.Msg==WM_EXITSIZEMOVE){timer.Start();TickTrainer();}
    }
}
