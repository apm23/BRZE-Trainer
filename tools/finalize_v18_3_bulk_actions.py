from pathlib import Path

p=Path('FinalV18MainForm.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('V18.3 missing fragment:\n'+old[:600])
    s=s.replace(old,new,1)

rep('    readonly Button tabAll=new(),tabDragon=new(),tabSerpent=new(),tabLotus=new(),tabWolf=new(),tabHeroes=new(),allSlotsOn=new();',
    '    readonly Button tabAll=new(),tabDragon=new(),tabSerpent=new(),tabLotus=new(),tabWolf=new(),tabHeroes=new(),allSlotsOn=new(),allSlotsOff=new();')

rep('        Text="BRZE Trainer — V18.2 Diagnostics";','        Text="BRZE Trainer — V18.3 Diagnostics";')
rep('        Text="BRZE Trainer — V18.2 Clean";','        Text="BRZE Trainer — V18.3 Clean";')
rep('        header.Controls.Add(new Label{Text=$"V18.2  ·  QUICK UNIT TABS  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});',
    '        header.Controls.Add(new Label{Text=$"V18.3  ·  QUICK UNIT TABS  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});')

old='''        var quick=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        SetupUnitTab(tabAll,"ALL",-1,54);SetupUnitTab(tabDragon,"DRAGON",0,72);SetupUnitTab(tabSerpent,"SERPENT",1,78);SetupUnitTab(tabLotus,"LOTUS",2,66);SetupUnitTab(tabWolf,"WOLF",3,58);SetupUnitTab(tabHeroes,"HEROES",4,72);
        foreach(var b in new[]{tabAll,tabDragon,tabSerpent,tabLotus,tabWolf,tabHeroes})quick.Controls.Add(b);
        allSlotsOn.Text="1-9 ON";allSlotsOn.Size=new Size(70,25);allSlotsOn.Margin=new Padding(7,0,0,0);allSlotsOn.FlatStyle=FlatStyle.Flat;allSlotsOn.FlatAppearance.BorderSize=1;allSlotsOn.FlatAppearance.BorderColor=Color.FromArgb(70,132,110);allSlotsOn.UseVisualStyleBackColor=false;allSlotsOn.BackColor=Color.FromArgb(36,78,65);allSlotsOn.ForeColor=Color.White;allSlotsOn.Font=new Font("Segoe UI Semibold",8.0f);allSlotsOn.TextAlign=ContentAlignment.MiddleCenter;allSlotsOn.Cursor=Cursors.Hand;allSlotsOn.Click+=(_,_)=>ActivateAllSlots();quick.Controls.Add(allSlotsOn);
        outer.Controls.Add(quick,0,2);'''
new='''        var quick=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        quick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        quick.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,166));

        var tabs=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        SetupUnitTab(tabAll,"ALL",-1,54);SetupUnitTab(tabDragon,"DRAGON",0,72);SetupUnitTab(tabSerpent,"SERPENT",1,78);SetupUnitTab(tabLotus,"LOTUS",2,66);SetupUnitTab(tabWolf,"WOLF",3,58);SetupUnitTab(tabHeroes,"HEROES",4,72);
        foreach(var b in new[]{tabAll,tabDragon,tabSerpent,tabLotus,tabWolf,tabHeroes})tabs.Controls.Add(b);
        quick.Controls.Add(tabs,0,0);

        var bulk=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Margin=new Padding(14,0,0,0),Padding=Padding.Empty};
        allSlotsOn.Text="1-9 ON";allSlotsOn.Size=new Size(72,25);allSlotsOn.Margin=new Padding(0);allSlotsOn.FlatStyle=FlatStyle.Flat;allSlotsOn.FlatAppearance.BorderSize=1;allSlotsOn.FlatAppearance.BorderColor=Color.FromArgb(70,132,110);allSlotsOn.UseVisualStyleBackColor=false;allSlotsOn.BackColor=Color.FromArgb(36,78,65);allSlotsOn.ForeColor=Color.White;allSlotsOn.Font=new Font("Segoe UI Semibold",8.0f);allSlotsOn.TextAlign=ContentAlignment.MiddleCenter;allSlotsOn.Cursor=Cursors.Hand;allSlotsOn.Click+=(_,_)=>ActivateAllSlots();
        allSlotsOff.Text="ALL OFF";allSlotsOff.Size=new Size(74,25);allSlotsOff.Margin=new Padding(8,0,0,0);allSlotsOff.FlatStyle=FlatStyle.Flat;allSlotsOff.FlatAppearance.BorderSize=1;allSlotsOff.FlatAppearance.BorderColor=Color.FromArgb(132,76,78);allSlotsOff.UseVisualStyleBackColor=false;allSlotsOff.BackColor=Color.FromArgb(78,42,45);allSlotsOff.ForeColor=Color.FromArgb(255,226,226);allSlotsOff.Font=new Font("Segoe UI Semibold",8.0f);allSlotsOff.TextAlign=ContentAlignment.MiddleCenter;allSlotsOff.Cursor=Cursors.Hand;allSlotsOff.Click+=(_,_)=>DeactivateAllSlots();
        bulk.Controls.Add(allSlotsOn);bulk.Controls.Add(allSlotsOff);quick.Controls.Add(bulk,1,0);
        outer.Controls.Add(quick,0,2);'''
rep(old,new)

old_method='''    void ActivateAllSlots()
    {
        loadingProfile=true;for(int i=0;i<9;i++){profileOn[editProfile,i]=true;slotOn[i].Checked=true;}loadingProfile=false;
        UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 ON");
    }'''
new_method='''    void ActivateAllSlots()
    {
        loadingProfile=true;for(int i=0;i<9;i++){profileOn[editProfile,i]=true;slotOn[i].Checked=true;}loadingProfile=false;
        UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 ON");
    }

    void DeactivateAllSlots()
    {
        loadingProfile=true;for(int i=0;i<9;i++){profileOn[editProfile,i]=false;slotOn[i].Checked=false;}loadingProfile=false;
        UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 OFF");
    }'''
rep(old_method,new_method)

p.write_text(s,encoding='utf-8')
print('V18.3 generated: clan filters left; separated right-side 1-9 ON + ALL OFF bulk actions')
