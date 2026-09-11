from pathlib import Path

src=Path('FinalV14MainForm.cs').read_text(encoding='utf-8')
s=src

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('V15 missing fragment:\n'+old[:300])
    s=s.replace(old,new,1)

def between(start,end,new):
    global s
    a=s.find(start)
    if a<0: raise SystemExit('V15 missing start: '+start)
    b=s.find(end,a)
    if b<0: raise SystemExit('V15 missing end: '+end)
    s=s[:a]+new+s[b:]

# Display names are more useful than hex IDs in the compact UI; Diagnostics still exposes raw IDs.
rep('        public override string ToString()=>$"{Name}  ·  0x{Type:X2}";','        public override string ToString()=>Name;')

rep('    readonly Button[] profileButtons=new Button[12];\n    readonly CheckBox[] slotOn=Enumerable.Range(0,9).Select(_=>new CheckBox{Text="Use",AutoSize=true}).ToArray();',
'''    readonly ComboBox profileSelect=new(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    readonly CheckBox[] slotOn=Enumerable.Range(0,9).Select(_=>new CheckBox{Text="",AutoSize=false,Width=24,Height=24}).ToArray();''')

rep('        Text="BRZE Trainer — V14 Diagnostics";','        Text="BRZE Trainer — V15 Diagnostics";')
rep('        Text="BRZE Trainer — V14 Clean";','        Text="BRZE Trainer — V15 Clean";')
rep('        ClientSize=new Size(1720,820);','        ClientSize=new Size(1640,720);')
rep('        header.Controls.Add(new Label{Text=$"V14  ·  BUILDING PROFILES  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(880,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});',
    '        header.Controls.Add(new Label{Text=$"V15  ·  COMPACT BUILDING PROFILES  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(900,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});')

old_layout='''        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,720));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        content.Controls.Add(BuildLeft(),0,0);content.Controls.Add(BuildRight(),1,0);shell.Controls.Add(content,0,1);'''
new_layout='''        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,700));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute,258));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        content.Controls.Add(BuildCheats(),0,0);content.Controls.Add(BuildRight(),1,0);
        var statusPanel=BuildStatusPanel();content.Controls.Add(statusPanel,0,1);content.SetColumnSpan(statusPanel,2);
        shell.Controls.Add(content,0,1);'''
rep(old_layout,new_layout)

# Replace old left stacked column with two independent panels. Status now spans full width below top row.
left_block=r'''    Control BuildCheats()
    {
        var cheats=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,0,8,8),Padding=new Padding(12)};
        var cheatLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=5,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)cheatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int r=0;r<5;r++)cheatLayout.RowStyles.Add(new RowStyle(SizeType.Percent,20f));
        Control[] cs={rice,water,yinYang,population,training,peasant,pausePeasant,hp,stamina,horses,wolves,reveal,burst};
        for(int i=0;i<cs.Length;i++){cs[i].Margin=new Padding(4,3,4,3);cheatLayout.Controls.Add(cs[i],i%3,i/3);}
        cheats.Controls.Add(cheatLayout);return cheats;
    }

    Control BuildStatusPanel()
    {
        var stat=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,0,0,0),Padding=new Padding(14)};
        var statLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        statLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));statLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        statLayout.Controls.Add(new Label{Text="SYSTEM STATUS",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(109,214,167),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        statusText.Dock=DockStyle.Fill;statLayout.Controls.Add(statusText,0,1);stat.Controls.Add(statLayout);return stat;
    }

'''
between('    Control BuildLeft()','    Control BuildRight()',left_block)

right_block=r'''    Control BuildRight()
    {
        var box=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(8,0,0,8),Padding=new Padding(12)};
        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        head.Controls.Add(new Label{Text="UNIT CHANGER  //  BUILDING",AutoSize=false,Location=new Point(0,0),Size=new Size(360,23),ForeColor=Color.FromArgb(109,214,167),Font=new Font("Segoe UI Semibold",10.2f),TextAlign=ContentAlignment.MiddleLeft});
        unitMode.Anchor=AnchorStyles.Top|AnchorStyles.Right;unitMode.Location=new Point(570,0);unitMode.Size=new Size(330,23);head.Controls.Add(unitMode);
        outer.Controls.Add(head,0,0);

        var selectRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(0,2,0,4)};
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

        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int r=0;r<3;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,33.333f));
        for(int i=0;i<9;i++)grid.Controls.Add(BuildSlot(i),i%3,i/3);
        outer.Controls.Add(grid,0,2);box.Controls.Add(outer);
        SelectProfile(0);return box;
    }

    Control BuildSlot(int i)
    {
        var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,BackColor=Color.FromArgb(31,37,46),Padding=new Padding(6,5,6,5),Margin=new Padding(3)};
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        row.Controls.Add(new Label{Text=$"S{i+1}",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(210,218,228),Font=new Font("Segoe UI Semibold",8.4f),TextAlign=ContentAlignment.MiddleCenter},0,0);
        slotOn[i].Dock=DockStyle.Fill;slotOn[i].ForeColor=Color.FromArgb(220,226,233);slotOn[i].BackColor=Color.Transparent;slotOn[i].Text="";slotOn[i].CheckAlign=ContentAlignment.MiddleCenter;slotOn[i].CheckedChanged+=(_,_)=>SlotChanged(i);row.Controls.Add(slotOn[i],1,0);
        slotOut[i].Dock=DockStyle.Fill;slotOut[i].DropDownStyle=ComboBoxStyle.DropDownList;slotOut[i].FlatStyle=FlatStyle.Flat;slotOut[i].BackColor=Color.FromArgb(44,51,62);slotOut[i].ForeColor=Color.WhiteSmoke;slotOut[i].Font=new Font("Segoe UI",8.2f);slotOut[i].Items.AddRange(Units.Cast<object>().ToArray());slotOut[i].SelectedIndexChanged+=(_,_)=>SlotChanged(i);row.Controls.Add(slotOut[i],2,0);
        return row;
    }

'''
between('    Control BuildRight()','    Control BuildActions()',right_block)

# Profile selector replaces the 12 oversized buttons. Preserve all per-building settings.
select_block=r'''    void SelectProfile(int p)
    {
        editProfile=Math.Clamp(p,0,Profiles.Length-1);loadingProfile=true;
        if(profileSelect.Items.Count==Profiles.Length)profileSelect.SelectedIndex=editProfile;
        for(int i=0;i<9;i++)
        {
            slotOn[i].Checked=profileOn[editProfile,i];
            int ix=Array.FindIndex(Units,u=>u.Type==profileOut[editProfile,i]);slotOut[i].SelectedIndex=ix>=0?ix:0;
        }
        loadingProfile=false;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void SlotChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;
        profileOn[editProfile,i]=slotOn[i].Checked;
        if(slotOut[i].SelectedItem is UnitOption u)profileOut[editProfile,i]=u.Type;
        UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

'''
between('    void SelectProfile(int p)','    void UpdateUnitLabels()',select_block)

rep('        var p=Profiles[editProfile];int n=ProfileSlots(editProfile);editTitle.Text=$"EDITING  {p.Clan}  /  {p.Name}";unitMode.Text=n==0?"OFF":n==1?"SINGLE OUTPUT · AUTO":$"MULTI OUTPUT · {n} SLOTS · AUTO";unitMode.ForeColor=n==0?Color.FromArgb(145,154,166):Color.FromArgb(97,224,179);\n        profileSummary.Text=$"active buildings: {ActiveProfiles()} / 12   ·   total slots: {TotalSlots()}";',
'''        var p=Profiles[editProfile];int n=ProfileSlots(editProfile);
        editTitle.Text=$"{p.Clan} / {p.Name}";
        unitMode.Text=n==0?"OFF":n==1?"SINGLE · AUTO":$"MULTI · {n} OUTPUTS · AUTO";
        unitMode.ForeColor=n==0?Color.FromArgb(145,154,166):Color.FromArgb(97,224,179);
        profileSummary.Text=$"{ActiveProfiles()} buildings active  ·  {TotalSlots()} total outputs";''')

# Slightly shorter chrome; top panels remain exactly same height.
rep('        shell.RowStyles.Add(new RowStyle(SizeType.Absolute,66));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,64));root.Controls.Add(shell);',
    '        shell.RowStyles.Add(new RowStyle(SizeType.Absolute,58));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.Absolute,58));root.Controls.Add(shell);')

# editTitle is no longer a visible control; harmless field retained for behavior/status text.
Path('FinalV15MainForm.cs').write_text(s,encoding='utf-8')
print('V15 compact UI generated: equal-height cheat/unit panels, dropdown building selector, compact 3x3 slots, full-width status')
