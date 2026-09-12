from pathlib import Path

p=Path('FinalV18MainForm.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('V18.2 missing fragment:\n'+old[:500])
    s=s.replace(old,new,1)

rep('        Text="BRZE Trainer — V18 Diagnostics";','        Text="BRZE Trainer — V18.2 Diagnostics";')
rep('        Text="BRZE Trainer — V18 Clean";','        Text="BRZE Trainer — V18.2 Clean";')
rep('        header.Controls.Add(new Label{Text=$"V18  ·  QUICK UNIT TABS  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});',
    '        header.Controls.Add(new Label{Text=$"V18.2  ·  QUICK UNIT TABS  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});')

old='''        var quick=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        quick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));quick.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));
        var tabs=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        SetupUnitTab(tabAll,"ALL",-1,54);SetupUnitTab(tabDragon,"DRAGON",0,72);SetupUnitTab(tabSerpent,"SERPENT",1,78);SetupUnitTab(tabLotus,"LOTUS",2,66);SetupUnitTab(tabWolf,"WOLF",3,58);SetupUnitTab(tabHeroes,"HEROES",4,72);
        foreach(var b in new[]{tabAll,tabDragon,tabSerpent,tabLotus,tabWolf,tabHeroes})tabs.Controls.Add(b);
        quick.Controls.Add(tabs,0,0);
        allSlotsOn.Text="ALL 1–9 ON";allSlotsOn.Dock=DockStyle.Fill;allSlotsOn.Margin=new Padding(4,1,0,2);allSlotsOn.FlatStyle=FlatStyle.Flat;allSlotsOn.FlatAppearance.BorderColor=Color.FromArgb(66,121,102);allSlotsOn.BackColor=Color.FromArgb(39,112,88);allSlotsOn.ForeColor=Color.White;allSlotsOn.Font=new Font("Segoe UI Semibold",8.0f);allSlotsOn.Click+=(_,_)=>ActivateAllSlots();quick.Controls.Add(allSlotsOn,1,0);
        outer.Controls.Add(quick,0,2);'''
new='''        var quick=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        SetupUnitTab(tabAll,"ALL",-1,54);SetupUnitTab(tabDragon,"DRAGON",0,72);SetupUnitTab(tabSerpent,"SERPENT",1,78);SetupUnitTab(tabLotus,"LOTUS",2,66);SetupUnitTab(tabWolf,"WOLF",3,58);SetupUnitTab(tabHeroes,"HEROES",4,72);
        foreach(var b in new[]{tabAll,tabDragon,tabSerpent,tabLotus,tabWolf,tabHeroes})quick.Controls.Add(b);
        allSlotsOn.Text="1-9 ON";allSlotsOn.Size=new Size(70,25);allSlotsOn.Margin=new Padding(7,0,0,0);allSlotsOn.FlatStyle=FlatStyle.Flat;allSlotsOn.FlatAppearance.BorderSize=1;allSlotsOn.FlatAppearance.BorderColor=Color.FromArgb(70,132,110);allSlotsOn.UseVisualStyleBackColor=false;allSlotsOn.BackColor=Color.FromArgb(36,78,65);allSlotsOn.ForeColor=Color.White;allSlotsOn.Font=new Font("Segoe UI Semibold",8.0f);allSlotsOn.TextAlign=ContentAlignment.MiddleCenter;allSlotsOn.Cursor=Cursors.Hand;allSlotsOn.Click+=(_,_)=>ActivateAllSlots();quick.Controls.Add(allSlotsOn);
        outer.Controls.Add(quick,0,2);'''
rep(old,new)

p.write_text(s,encoding='utf-8')
print('V18.2 generated: 1-9 ON is a normal explicit-size Button in the same working FlowLayout as quick tabs')
