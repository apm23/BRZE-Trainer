from pathlib import Path

p=Path('FinalV17MainForm.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('V18 missing fragment:\n'+old[:420])
    s=s.replace(old,new,1)

def between(start,end,new):
    global s
    a=s.find(start)
    if a<0: raise SystemExit('V18 missing start: '+start)
    b=s.find(end,a)
    if b<0: raise SystemExit('V18 missing end: '+end)
    s=s[:a]+new+s[b:]

# No unit is blocked or risk-colored in V18.  Quick tabs are only a compact
# catalog filter; game-side UnitChangerCore remains byte-identical.
unit_model=r'''    enum UnitGroup { Dragon, Serpent, Lotus, Wolf, Heroes }

    sealed class UnitOption
    {
        public uint Type { get; }
        public string Name { get; }
        public UnitGroup Group { get; }
        public bool Header { get; }
        public UnitOption(uint type,string name,UnitGroup group,bool header=false){Type=type;Name=name;Group=group;Header=header;}
        public override string ToString()=>Name;
    }

    static UnitOption H(UnitGroup group,string name)=>new(uint.MaxValue,name,group,true);
    static UnitOption U(UnitGroup group,uint type,string name)=>new(type,name,group,false);

'''
between('    sealed class UnitOption','    sealed record BuildingProfile',unit_model)

units=r'''    static readonly UnitOption[] Units=
    {
        H(UnitGroup.Dragon,"DRAGON"),
        U(UnitGroup.Dragon,0,"Dragon Archer"),U(UnitGroup.Dragon,1,"Dragon Chemist"),U(UnitGroup.Dragon,2,"Dragon Dragon Warrior"),U(UnitGroup.Dragon,3,"Dragon Geisha"),U(UnitGroup.Dragon,4,"Dragon Kabuki Warrior"),U(UnitGroup.Dragon,5,"Dragon Peasant"),U(UnitGroup.Dragon,6,"Dragon Powder Keg Cannoneer"),U(UnitGroup.Dragon,7,"Dragon Samurai"),U(UnitGroup.Dragon,8,"Dragon Spearman"),
        U(UnitGroup.Dragon,116,"Dragon Chakram Maiden"),U(UnitGroup.Dragon,117,"Dragon Chakram Maiden Doppleganger"),U(UnitGroup.Dragon,118,"Dragon Guardian"),U(UnitGroup.Dragon,119,"Dragon Guardian (Dying)"),

        H(UnitGroup.Serpent,"SERPENT"),
        U(UnitGroup.Serpent,19,"Serpent Bandit"),U(UnitGroup.Serpent,20,"Serpent Cannoneer"),U(UnitGroup.Serpent,21,"Serpent Crossbowman"),U(UnitGroup.Serpent,22,"Serpent Fan Geisha"),U(UnitGroup.Serpent,23,"Serpent Musketeer"),U(UnitGroup.Serpent,24,"Serpent Peasant"),U(UnitGroup.Serpent,25,"Serpent Raider"),U(UnitGroup.Serpent,26,"Serpent Ronin"),U(UnitGroup.Serpent,27,"Serpent Spirit Warrior"),U(UnitGroup.Serpent,28,"Spirit Warrior 2"),U(UnitGroup.Serpent,29,"Serpent Swordsman"),U(UnitGroup.Serpent,104,"Serpent Necromancer"),
        U(UnitGroup.Serpent,120,"Serpent Enforcer"),U(UnitGroup.Serpent,121,"Serpent Witch"),U(UnitGroup.Serpent,122,"Serpent Witch (Demon Form)"),U(UnitGroup.Serpent,123,"Serpent Witch (Demon Form 2)"),

        H(UnitGroup.Lotus,"LOTUS"),
        U(UnitGroup.Lotus,30,"Lotus Blade Acolyte"),U(UnitGroup.Lotus,31,"Lotus Brother One"),U(UnitGroup.Lotus,32,"Lotus Brother Two"),U(UnitGroup.Lotus,33,"Lotus Brother Three"),U(UnitGroup.Lotus,34,"Lotus Channeler"),U(UnitGroup.Lotus,35,"Lotus Diseased One"),U(UnitGroup.Lotus,36,"Lotus Golem"),U(UnitGroup.Lotus,37,"Lotus Infested One"),U(UnitGroup.Lotus,38,"Lotus Leaf Disciple"),U(UnitGroup.Lotus,39,"Lotus Master Warlock"),U(UnitGroup.Lotus,40,"Lotus Peasant"),U(UnitGroup.Lotus,41,"Lotus Staff Adept"),U(UnitGroup.Lotus,42,"Lotus Unclean One"),U(UnitGroup.Lotus,43,"Lotus Warlock"),
        U(UnitGroup.Lotus,124,"Lotus Overseer"),U(UnitGroup.Lotus,125,"Lotus Reaper"),

        H(UnitGroup.Wolf,"WOLF"),
        U(UnitGroup.Wolf,44,"Wolf Ballistaman"),U(UnitGroup.Wolf,45,"Wolf Berserker"),U(UnitGroup.Wolf,46,"Wolf Brawler"),U(UnitGroup.Wolf,47,"Wolf Druidess"),U(UnitGroup.Wolf,48,"Wolf Hurler"),U(UnitGroup.Wolf,49,"Wolf Mauler"),U(UnitGroup.Wolf,50,"Wolf Pack Master"),U(UnitGroup.Wolf,51,"Wolf Peasant"),U(UnitGroup.Wolf,52,"Wolf Pitch Slinger"),U(UnitGroup.Wolf,53,"Wolf Sledger"),U(UnitGroup.Wolf,54,"Wolf Werewolf"),U(UnitGroup.Wolf,126,"Wolf Digger"),U(UnitGroup.Wolf,127,"Wolf Dryad"),

        H(UnitGroup.Heroes,"HEROES"),
        U(UnitGroup.Heroes,85,"Hero Arah"),U(UnitGroup.Heroes,86,"Hero Budo"),U(UnitGroup.Heroes,87,"Hero Gaihla"),U(UnitGroup.Heroes,88,"Hero Garrin"),U(UnitGroup.Heroes,89,"Hero Grayback"),U(UnitGroup.Heroes,90,"Hero Issyl"),U(UnitGroup.Heroes,91,"Hero Kazan"),
        U(UnitGroup.Heroes,92,"Hero Kenji"),U(UnitGroup.Heroes,93,"Hero Kenji No Sword"),U(UnitGroup.Heroes,94,"Hero Kenji (Serpent)"),U(UnitGroup.Heroes,95,"Hero Kenji Young"),U(UnitGroup.Heroes,96,"Hero Kenji 2"),U(UnitGroup.Heroes,97,"Hero Kenji 2 No Sword"),U(UnitGroup.Heroes,98,"Hero Kenji 2 (Serpent)"),U(UnitGroup.Heroes,99,"Hero Kenji 3"),U(UnitGroup.Heroes,100,"Hero Kenji (One with the Dragon)"),U(UnitGroup.Heroes,101,"Hero Kenji 3 Serpent"),
        U(UnitGroup.Heroes,102,"Hero Koril"),U(UnitGroup.Heroes,103,"Hero Longtooth"),U(UnitGroup.Heroes,105,"Hero Otomo"),U(UnitGroup.Heroes,106,"Hero Otomo No Sword"),U(UnitGroup.Heroes,107,"Hero Shinja"),U(UnitGroup.Heroes,108,"Hero Shinja No Sword"),U(UnitGroup.Heroes,109,"Hero Soban"),U(UnitGroup.Heroes,110,"Hero Tao"),U(UnitGroup.Heroes,111,"Hero The Shale Lord"),U(UnitGroup.Heroes,112,"Hero Utara"),U(UnitGroup.Heroes,113,"Hero Vetkin"),U(UnitGroup.Heroes,114,"Hero Zymeth"),U(UnitGroup.Heroes,115,"Hero Zymeth No Corruption"),
        U(UnitGroup.Heroes,136,"Hero Grayback (Endgame)"),U(UnitGroup.Heroes,137,"Hero Grayback (Middle)"),U(UnitGroup.Heroes,138,"Hero Grayback (Slave)"),U(UnitGroup.Heroes,139,"Hero Longtooth (Slave)"),U(UnitGroup.Heroes,140,"Hero Taro"),U(UnitGroup.Heroes,141,"Hero Teppo"),U(UnitGroup.Heroes,142,"Hero Wildeye"),U(UnitGroup.Heroes,143,"Hero Yvaine"),U(UnitGroup.Heroes,144,"Hero Yvaine (Ungodly Power)")
    };

'''
between('    static readonly UnitOption[] Units=','    readonly ToggleSwitch rice=',units)

rep('    readonly Label unitRiskWarning=new(){Text="",AutoSize=false,ForeColor=Color.FromArgb(255,145,145),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",8.0f),TextAlign=ContentAlignment.MiddleLeft};\n    readonly CheckBox[] slotOn=',
'''    readonly Button tabAll=new(),tabDragon=new(),tabSerpent=new(),tabLotus=new(),tabWolf=new(),tabHeroes=new(),allSlotsOn=new();
    int unitFilter=-1;
    readonly CheckBox[] slotOn=''')

rep('        Text="BRZE Trainer — V17 Diagnostics";','        Text="BRZE Trainer — V18 Diagnostics";')
rep('        Text="BRZE Trainer — V17 Clean";','        Text="BRZE Trainer — V18 Clean";')
rep('        header.Controls.Add(new Label{Text=$"V17  ·  WOTW-ONLY RISK MARKING  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});',
    '        header.Controls.Add(new Label{Text=$"V18  ·  QUICK UNIT TABS  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});')

right_block=r'''    Control BuildRight()
    {
        var box=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(8,0,0,8),Padding=new Padding(12)};
        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        head.Controls.Add(new Label{Text="UNIT CHANGER  //  BUILDING",AutoSize=false,Location=new Point(0,0),Size=new Size(360,23),ForeColor=Color.FromArgb(109,214,167),Font=new Font("Segoe UI Semibold",10.2f),TextAlign=ContentAlignment.MiddleLeft});
        unitMode.Anchor=AnchorStyles.Top|AnchorStyles.Right;unitMode.Location=new Point(570,0);unitMode.Size=new Size(330,23);head.Controls.Add(unitMode);
        outer.Controls.Add(head,0,0);

        var selectRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(0,1,0,3)};
        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,72));
        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,340));
        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,12));
        selectRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        selectRow.Controls.Add(new Label{Text="BUILDING",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(188,198,210),Font=new Font("Segoe UI Semibold",8.5f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        profileSelect.Dock=DockStyle.Fill;profileSelect.BackColor=Color.FromArgb(44,51,62);profileSelect.ForeColor=Color.WhiteSmoke;profileSelect.Font=new Font("Segoe UI",8.8f);
        for(int p=0;p<Profiles.Length;p++)profileSelect.Items.Add($"{Profiles[p].Clan}  ·  {Profiles[p].Name}");
        profileSelect.SelectedIndexChanged+=(_,_)=>{if(!loadingProfile&&profileSelect.SelectedIndex>=0)SelectProfile(profileSelect.SelectedIndex);};
        selectRow.Controls.Add(profileSelect,1,0);
        profileSummary.Dock=DockStyle.Fill;profileSummary.TextAlign=ContentAlignment.MiddleRight;selectRow.Controls.Add(profileSummary,3,0);
        outer.Controls.Add(selectRow,0,1);

        var quick=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        quick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));quick.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));
        var tabs=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        SetupUnitTab(tabAll,"ALL",-1,54);SetupUnitTab(tabDragon,"DRAGON",0,72);SetupUnitTab(tabSerpent,"SERPENT",1,78);SetupUnitTab(tabLotus,"LOTUS",2,66);SetupUnitTab(tabWolf,"WOLF",3,58);SetupUnitTab(tabHeroes,"HEROES",4,72);
        foreach(var b in new[]{tabAll,tabDragon,tabSerpent,tabLotus,tabWolf,tabHeroes})tabs.Controls.Add(b);
        quick.Controls.Add(tabs,0,0);
        allSlotsOn.Text="ALL 1–9 ON";allSlotsOn.Dock=DockStyle.Fill;allSlotsOn.Margin=new Padding(4,1,0,2);allSlotsOn.FlatStyle=FlatStyle.Flat;allSlotsOn.FlatAppearance.BorderColor=Color.FromArgb(66,121,102);allSlotsOn.BackColor=Color.FromArgb(39,112,88);allSlotsOn.ForeColor=Color.White;allSlotsOn.Font=new Font("Segoe UI Semibold",8.0f);allSlotsOn.Click+=(_,_)=>ActivateAllSlots();quick.Controls.Add(allSlotsOn,1,0);
        outer.Controls.Add(quick,0,2);

        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int r=0;r<3;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,33.333f));
        for(int i=0;i<9;i++)grid.Controls.Add(BuildSlot(i),i%3,i/3);
        outer.Controls.Add(grid,0,3);box.Controls.Add(outer);
        SelectProfile(0);SetUnitFilter(-1);return box;
    }

    Control BuildSlot(int i)
    {
        var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,BackColor=Color.FromArgb(31,37,46),Padding=new Padding(6,4,6,4),Margin=new Padding(3)};
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        row.Controls.Add(new Label{Text=$"S{i+1}",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(210,218,228),Font=new Font("Segoe UI Semibold",8.4f),TextAlign=ContentAlignment.MiddleCenter},0,0);
        slotOn[i].Dock=DockStyle.Fill;slotOn[i].ForeColor=Color.FromArgb(220,226,233);slotOn[i].BackColor=Color.Transparent;slotOn[i].Text="";slotOn[i].CheckAlign=ContentAlignment.MiddleCenter;slotOn[i].CheckedChanged+=(_,_)=>SlotToggleChanged(i);row.Controls.Add(slotOn[i],1,0);
        slotOut[i].Dock=DockStyle.Fill;slotOut[i].DropDownStyle=ComboBoxStyle.DropDownList;slotOut[i].FlatStyle=FlatStyle.Flat;slotOut[i].BackColor=Color.FromArgb(44,51,62);slotOut[i].ForeColor=Color.WhiteSmoke;slotOut[i].Font=new Font("Segoe UI",8.2f);slotOut[i].DrawMode=DrawMode.OwnerDrawFixed;slotOut[i].ItemHeight=22;slotOut[i].IntegralHeight=false;slotOut[i].DropDownHeight=374;slotOut[i].MaxDropDownItems=18;slotOut[i].Items.AddRange(Units.Cast<object>().ToArray());slotOut[i].DrawItem+=DrawUnitItem;slotOut[i].SelectedIndexChanged+=(_,_)=>UnitSelectionChanged(i);row.Controls.Add(slotOut[i],2,0);
        return row;
    }

'''
between('    Control BuildRight()','    Control BuildActions()',right_block)

methods=r'''    int FindUnitIndex(uint type)
    {
        int ix=Array.FindIndex(Units,u=>!u.Header&&u.Type==type);
        if(ix>=0)return ix;
        return Array.FindIndex(Units,u=>!u.Header);
    }

    void SetupUnitTab(Button b,string text,int filter,int width)
    {
        b.Text=text;b.Width=width;b.Height=25;b.Margin=new Padding(0,1,4,2);b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=1;b.Font=new Font("Segoe UI Semibold",7.8f);b.Click+=(_,_)=>SetUnitFilter(filter);
    }

    void SetUnitFilter(int filter)
    {
        unitFilter=filter;
        foreach(var x in new[]{(tabAll,-1),(tabDragon,0),(tabSerpent,1),(tabLotus,2),(tabWolf,3),(tabHeroes,4)})
        {
            bool on=x.Item2==filter;x.Item1.BackColor=on?Color.FromArgb(42,116,91):Color.FromArgb(39,46,56);x.Item1.ForeColor=on?Color.White:Color.FromArgb(195,204,215);x.Item1.FlatAppearance.BorderColor=on?Color.FromArgb(89,210,166):Color.FromArgb(72,82,96);
        }
        for(int i=0;i<9;i++)RebuildUnitCombo(i);
    }

    void RebuildUnitCombo(int i)
    {
        if(i<0||i>=9)return;uint current=profileOut[editProfile,i];bool old=loadingProfile;loadingProfile=true;
        var cb=slotOut[i];cb.BeginUpdate();cb.Items.Clear();
        if(unitFilter<0)
        {
            cb.Items.AddRange(Units.Cast<object>().ToArray());
        }
        else
        {
            var group=(UnitGroup)unitFilter;var currentUnit=Units.FirstOrDefault(u=>!u.Header&&u.Type==current);
            if(currentUnit!=null&&currentUnit.Group!=group)cb.Items.Add(currentUnit);
            foreach(var u in Units.Where(u=>!u.Header&&u.Group==group))cb.Items.Add(u);
        }
        int selected=-1;for(int x=0;x<cb.Items.Count;x++)if(cb.Items[x] is UnitOption u&&!u.Header&&u.Type==current){selected=x;break;}
        if(selected<0)for(int x=0;x<cb.Items.Count;x++)if(cb.Items[x] is UnitOption u&&!u.Header){selected=x;profileOut[editProfile,i]=u.Type;break;}
        cb.SelectedIndex=selected;cb.EndUpdate();loadingProfile=old;cb.Invalidate();
    }

    void SelectProfile(int p)
    {
        editProfile=Math.Clamp(p,0,Profiles.Length-1);loadingProfile=true;
        if(profileSelect.Items.Count==Profiles.Length)profileSelect.SelectedIndex=editProfile;
        for(int i=0;i<9;i++)slotOn[i].Checked=profileOn[editProfile,i];
        loadingProfile=false;for(int i=0;i<9;i++)RebuildUnitCombo(i);UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void DrawUnitItem(object? sender,DrawItemEventArgs e)
    {
        if(sender is not ComboBox cb||e.Index<0||e.Index>=cb.Items.Count)return;
        if(cb.Items[e.Index] is not UnitOption u)return;bool selected=(e.State&DrawItemState.Selected)!=0;
        Color bg=u.Header?Color.FromArgb(35,43,52):(selected?Color.FromArgb(43,92,76):Color.FromArgb(44,51,62));
        Color fg=u.Header?Color.FromArgb(109,214,167):Color.WhiteSmoke;
        using(var bb=new SolidBrush(bg))e.Graphics.FillRectangle(bb,e.Bounds);
        Font? custom=null;Font font=e.Font??this.Font;if(u.Header){custom=new Font(font,FontStyle.Bold);font=custom;}
        using(var fb=new SolidBrush(fg))e.Graphics.DrawString(u.Name,font,fb,new RectangleF(e.Bounds.X+6,e.Bounds.Y+2,e.Bounds.Width-10,e.Bounds.Height-3));custom?.Dispose();
        if((e.State&DrawItemState.Focus)!=0&&!u.Header)e.DrawFocusRectangle();
    }

    void UnitSelectionChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;if(slotOut[i].SelectedItem is not UnitOption u)return;
        if(u.Header){RebuildUnitCombo(i);return;}
        profileOut[editProfile,i]=u.Type;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void SlotToggleChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;if(slotOut[i].SelectedItem is not UnitOption u||u.Header)return;
        profileOn[editProfile,i]=slotOn[i].Checked;profileOut[editProfile,i]=u.Type;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void ActivateAllSlots()
    {
        loadingProfile=true;for(int i=0;i<9;i++){profileOn[editProfile,i]=true;slotOn[i].Checked=true;}loadingProfile=false;
        UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 ON");
    }

'''
between('    int FindUnitIndex(uint type)','    void UpdateUnitLabels()',methods)

Path('FinalV18MainForm.cs').write_text(s,encoding='utf-8')
print('V18 generated: no risk blocking/colors; compact clan/hero quick tabs; all 1-9 ON button')
