from pathlib import Path

p=Path('FinalV16MainForm.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('V17 missing fragment:\n'+old[:360])
    s=s.replace(old,new,1)

def between(start,end,new):
    global s
    a=s.find(start)
    if a<0: raise SystemExit('V17 missing start: '+start)
    b=s.find(end,a)
    if b<0: raise SystemExit('V17 missing end: '+end)
    s=s[:a]+new+s[b:]

# V17 policy: ONLY WotW-release-exclusive units/heroes are red/warned.
# Classic heroes and old special/unique units remain ordinary selectable entries.
rep('        H("⚠ SPECIAL / UNIQUE — STORY RISK",true),\n        U(27,"Serpent Spirit Warrior",true),U(28,"Spirit Warrior 2",true),\n        U(31,"Lotus Brother One",true),U(32,"Lotus Brother Two",true),U(33,"Lotus Brother Three",true),U(36,"Lotus Golem",true),\n        U(104,"Serpent Necromancer",true),',
'''        H("SPECIAL / UNIQUE"),
        U(27,"Serpent Spirit Warrior"),U(28,"Spirit Warrior 2"),
        U(31,"Lotus Brother One"),U(32,"Lotus Brother Two"),U(33,"Lotus Brother Three"),U(36,"Lotus Golem"),
        U(104,"Serpent Necromancer"),''')

rep('        H("⚠ CLASSIC HEROES / STORY — RISK",true),\n        U(85,"Hero Arah",true),U(86,"Hero Budo",true),U(87,"Hero Gaihla",true),U(88,"Hero Garrin",true),U(89,"Hero Grayback",true),U(90,"Hero Issyl",true),U(91,"Hero Kazan",true),\n        U(92,"Hero Kenji",true),U(93,"Hero Kenji No Sword",true),U(94,"Hero Kenji (Serpent)",true),U(95,"Hero Kenji Young",true),U(96,"Hero Kenji 2",true),U(97,"Hero Kenji 2 No Sword",true),U(98,"Hero Kenji 2 (Serpent)",true),U(99,"Hero Kenji 3",true),U(100,"Hero Kenji (One with the Dragon)",true),U(101,"Hero Kenji 3 Serpent",true),\n        U(102,"Hero Koril",true),U(103,"Hero Longtooth",true),U(105,"Hero Otomo",true),U(106,"Hero Otomo No Sword",true),U(107,"Hero Shinja",true),U(108,"Hero Shinja No Sword",true),U(109,"Hero Soban",true),U(110,"Hero Tao",true),U(111,"Hero The Shale Lord",true),U(112,"Hero Utara",true),U(113,"Hero Vetkin",true),U(114,"Hero Zymeth",true),U(115,"Hero Zymeth No Corruption",true),',
'''        H("CLASSIC HEROES / STORY"),
        U(85,"Hero Arah"),U(86,"Hero Budo"),U(87,"Hero Gaihla"),U(88,"Hero Garrin"),U(89,"Hero Grayback"),U(90,"Hero Issyl"),U(91,"Hero Kazan"),
        U(92,"Hero Kenji"),U(93,"Hero Kenji No Sword"),U(94,"Hero Kenji (Serpent)"),U(95,"Hero Kenji Young"),U(96,"Hero Kenji 2"),U(97,"Hero Kenji 2 No Sword"),U(98,"Hero Kenji 2 (Serpent)"),U(99,"Hero Kenji 3"),U(100,"Hero Kenji (One with the Dragon)"),U(101,"Hero Kenji 3 Serpent"),
        U(102,"Hero Koril"),U(103,"Hero Longtooth"),U(105,"Hero Otomo"),U(106,"Hero Otomo No Sword"),U(107,"Hero Shinja"),U(108,"Hero Shinja No Sword"),U(109,"Hero Soban"),U(110,"Hero Tao"),U(111,"Hero The Shale Lord"),U(112,"Hero Utara"),U(113,"Hero Vetkin"),U(114,"Hero Zymeth"),U(115,"Hero Zymeth No Corruption"),''')

rep('        Text="BRZE Trainer — V16 Diagnostics";','        Text="BRZE Trainer — V17 Diagnostics";')
rep('        Text="BRZE Trainer — V16 Clean";','        Text="BRZE Trainer — V17 Clean";')
rep('        header.Controls.Add(new Label{Text=$"V16  ·  CATEGORIZED UNIT CATALOG  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});',
    '        header.Controls.Add(new Label{Text=$"V17  ·  WOTW-ONLY RISK MARKING  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});')

# Small persistent warning line below the 3x3 slots; no modal dialog.
rep('    readonly ComboBox profileSelect=new(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};\n    readonly CheckBox[] slotOn=',
'''    readonly ComboBox profileSelect=new(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    readonly Label unitRiskWarning=new(){Text="",AutoSize=false,ForeColor=Color.FromArgb(255,145,145),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",8.0f),TextAlign=ContentAlignment.MiddleLeft};
    readonly CheckBox[] slotOn=''')

rep('        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};\n        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,36));\n        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));\n        outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));',
'''        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,22));''')

rep('        head.Controls.Add(new Label{Text="RED = HERO / STORY / WOTW RISK",AutoSize=false,Location=new Point(350,0),Size=new Size(235,23),ForeColor=Color.FromArgb(255,145,145),Font=new Font("Segoe UI Semibold",8.0f),TextAlign=ContentAlignment.MiddleRight});',
    '        head.Controls.Add(new Label{Text="RED = WOTW RELEASE ONLY",AutoSize=false,Location=new Point(350,0),Size=new Size(235,23),ForeColor=Color.FromArgb(255,145,145),Font=new Font("Segoe UI Semibold",8.0f),TextAlign=ContentAlignment.MiddleRight});')

rep('        outer.Controls.Add(grid,0,2);box.Controls.Add(outer);\n        SelectProfile(0);return box;',
'''        outer.Controls.Add(grid,0,2);
        unitRiskWarning.Dock=DockStyle.Fill;unitRiskWarning.Padding=new Padding(5,1,0,0);outer.Controls.Add(unitRiskWarning,0,3);
        box.Controls.Add(outer);
        SelectProfile(0);return box;''')

methods=r'''    int FindUnitIndex(uint type)
    {
        int ix=Array.FindIndex(Units,u=>!u.Header&&u.Type==type);
        if(ix>=0)return ix;
        return Array.FindIndex(Units,u=>!u.Header&&!u.Risky);
    }

    void SelectProfile(int p)
    {
        editProfile=Math.Clamp(p,0,Profiles.Length-1);loadingProfile=true;
        if(profileSelect.Items.Count==Profiles.Length)profileSelect.SelectedIndex=editProfile;
        for(int i=0;i<9;i++)
        {
            slotOn[i].Checked=profileOn[editProfile,i];
            slotOut[i].SelectedIndex=FindUnitIndex(profileOut[editProfile,i]);
            UpdateSlotVisual(i);
        }
        loadingProfile=false;UpdateRiskWarning();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void DrawUnitItem(object? sender,DrawItemEventArgs e)
    {
        if(sender is not ComboBox cb||e.Index<0||e.Index>=cb.Items.Count)return;
        if(cb.Items[e.Index] is not UnitOption u)return;
        bool selected=(e.State&DrawItemState.Selected)!=0;
        Color bg=u.Header?(u.Risky?Color.FromArgb(92,34,38):Color.FromArgb(35,43,52)):(u.Risky?Color.FromArgb(112,38,43):(selected?Color.FromArgb(43,92,76):Color.FromArgb(44,51,62)));
        Color fg=u.Header?(u.Risky?Color.FromArgb(255,196,196):Color.FromArgb(109,214,167)):Color.WhiteSmoke;
        using(var bb=new SolidBrush(bg))e.Graphics.FillRectangle(bb,e.Bounds);
        string text=u.Header?u.Name:(u.Risky?$"⚠  {u.Name}":u.Name);
        Font? custom=null;Font font=e.Font??this.Font;
        if(u.Header){custom=new Font(font,FontStyle.Bold);font=custom;}
        using(var fb=new SolidBrush(fg))e.Graphics.DrawString(text,font,fb,new RectangleF(e.Bounds.X+6,e.Bounds.Y+2,e.Bounds.Width-10,e.Bounds.Height-3));
        custom?.Dispose();
        if((e.State&DrawItemState.Focus)!=0&&!u.Header)e.DrawFocusRectangle();
    }

    void UpdateSlotVisual(int i)
    {
        if(i<0||i>=9)return;
        if(slotOut[i].SelectedItem is UnitOption u&&u.Risky&&!u.Header)
        {
            slotOut[i].BackColor=Color.FromArgb(112,38,43);
            slotOut[i].ForeColor=Color.WhiteSmoke;
        }
        else
        {
            slotOut[i].BackColor=Color.FromArgb(44,51,62);
            slotOut[i].ForeColor=Color.WhiteSmoke;
        }
        slotOut[i].Invalidate();
    }

    void UpdateRiskWarning()
    {
        UnitOption? first=null;int count=0;
        for(int i=0;i<9;i++)
        {
            if(slotOut[i].SelectedItem is UnitOption u&&u.Risky&&!u.Header)
            {
                first??=u;count++;
            }
        }
        unitRiskWarning.Text=count==0?"":count==1?$"⚠ WotW-only: {first!.Name} — hindari di Kenji Journey/story map.":$"⚠ {count} WotW-only outputs dipilih — hindari di Kenji Journey/story map.";
    }

    void UnitSelectionChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;
        if(slotOut[i].SelectedItem is not UnitOption u)return;
        if(u.Header)
        {
            loadingProfile=true;slotOut[i].SelectedIndex=FindUnitIndex(profileOut[editProfile,i]);loadingProfile=false;UpdateSlotVisual(i);UpdateRiskWarning();return;
        }
        profileOut[editProfile,i]=u.Type;
        UpdateSlotVisual(i);UpdateRiskWarning();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void SlotToggleChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;
        if(slotOut[i].SelectedItem is not UnitOption u||u.Header)
        {
            loadingProfile=true;slotOn[i].Checked=false;loadingProfile=false;profileOn[editProfile,i]=false;return;
        }
        profileOn[editProfile,i]=slotOn[i].Checked;profileOut[editProfile,i]=u.Type;
        UpdateSlotVisual(i);UpdateRiskWarning();UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

'''
between('    int FindUnitIndex(uint type)','    void UpdateUnitLabels()',methods)

Path('FinalV17MainForm.cs').write_text(s,encoding='utf-8')
print('V17 generated: only WotW-exclusive entries red; no modal/auto-off; compact bottom warning')
