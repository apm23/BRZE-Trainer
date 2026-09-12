from pathlib import Path

p=Path('FinalV18MainForm.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('V18.1 missing fragment:\n'+old[:420])
    s=s.replace(old,new,1)

rep('    readonly Button tabAll=new(),tabDragon=new(),tabSerpent=new(),tabLotus=new(),tabWolf=new(),tabHeroes=new(),allSlotsOn=new();',
'''    readonly Button tabAll=new(),tabDragon=new(),tabSerpent=new(),tabLotus=new(),tabWolf=new(),tabHeroes=new();
    readonly Label allSlotsOn=new();''')

rep('        Text="BRZE Trainer — V18 Diagnostics";','        Text="BRZE Trainer — V18.1 Diagnostics";')
rep('        Text="BRZE Trainer — V18 Clean";','        Text="BRZE Trainer — V18.1 Clean";')
rep('        header.Controls.Add(new Label{Text=$"V18  ·  QUICK UNIT TABS  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});',
    '        header.Controls.Add(new Label{Text=$"V18.1  ·  QUICK UNIT TABS  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});')

rep('        quick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));quick.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,112));',
    '        quick.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));quick.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,88));')

rep('        allSlotsOn.Text="ALL 1–9 ON";allSlotsOn.Dock=DockStyle.Fill;allSlotsOn.Margin=new Padding(4,1,0,2);allSlotsOn.FlatStyle=FlatStyle.Flat;allSlotsOn.FlatAppearance.BorderColor=Color.FromArgb(66,121,102);allSlotsOn.BackColor=Color.FromArgb(39,112,88);allSlotsOn.ForeColor=Color.White;allSlotsOn.Font=new Font("Segoe UI Semibold",8.0f);allSlotsOn.Click+=(_,_)=>ActivateAllSlots();quick.Controls.Add(allSlotsOn,1,0);',
'''        allSlotsOn.Text="1–9 ON";allSlotsOn.Dock=DockStyle.Fill;allSlotsOn.Margin=new Padding(5,2,0,2);allSlotsOn.AutoSize=false;allSlotsOn.TextAlign=ContentAlignment.MiddleCenter;allSlotsOn.BorderStyle=BorderStyle.FixedSingle;allSlotsOn.BackColor=Color.FromArgb(35,79,66);allSlotsOn.ForeColor=Color.FromArgb(205,245,228);allSlotsOn.Font=new Font("Segoe UI Semibold",7.8f);allSlotsOn.Cursor=Cursors.Hand;allSlotsOn.Click+=(_,_)=>ActivateAllSlots();allSlotsOn.MouseEnter+=(_,_)=>{if(!AllSlotsActive())allSlotsOn.BackColor=Color.FromArgb(45,103,85);};allSlotsOn.MouseLeave+=(_,_)=>UpdateAllSlotsAction();quick.Controls.Add(allSlotsOn,1,0);''')

rep('        loadingProfile=false;for(int i=0;i<9;i++)RebuildUnitCombo(i);UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;',
    '        loadingProfile=false;for(int i=0;i<9;i++)RebuildUnitCombo(i);UpdateAllSlotsAction();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;')

rep('        profileOn[editProfile,i]=slotOn[i].Checked;profileOut[editProfile,i]=u.Type;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;',
    '        profileOn[editProfile,i]=slotOn[i].Checked;profileOut[editProfile,i]=u.Type;UpdateAllSlotsAction();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;')

rep('    void ActivateAllSlots()\n    {\n        loadingProfile=true;for(int i=0;i<9;i++){profileOn[editProfile,i]=true;slotOn[i].Checked=true;}loadingProfile=false;\n        UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 ON");\n    }',
'''    bool AllSlotsActive()=>Enumerable.Range(0,9).All(i=>profileOn[editProfile,i]);

    void UpdateAllSlotsAction()
    {
        bool all=AllSlotsActive();
        allSlotsOn.Text=all?"✓ 1–9 ON":"1–9 ON";
        allSlotsOn.BackColor=all?Color.FromArgb(39,112,88):Color.FromArgb(35,79,66);
        allSlotsOn.ForeColor=all?Color.White:Color.FromArgb(205,245,228);
    }

    void ActivateAllSlots()
    {
        loadingProfile=true;for(int i=0;i<9;i++){profileOn[editProfile,i]=true;slotOn[i].Checked=true;}loadingProfile=false;
        UpdateAllSlotsAction();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 ON");
    }''')

p.write_text(s,encoding='utf-8')
print('V18.1 generated: compact reliable 1–9 action label with active-state feedback')
