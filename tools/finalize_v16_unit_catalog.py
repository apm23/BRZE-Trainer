from pathlib import Path

p=Path('FinalV15MainForm.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new):
    global s
    if old not in s:
        raise SystemExit('V16 missing fragment:\n'+old[:320])
    s=s.replace(old,new,1)

def between(start,end,new):
    global s
    a=s.find(start)
    if a<0: raise SystemExit('V16 missing start: '+start)
    b=s.find(end,a)
    if b<0: raise SystemExit('V16 missing end: '+end)
    s=s[:a]+new+s[b:]

unit_model=r'''    sealed class UnitOption
    {
        public uint Type { get; }
        public string Name { get; }
        public bool Risky { get; }
        public bool Header { get; }
        public UnitOption(uint type,string name,bool risky=false,bool header=false){Type=type;Name=name;Risky=risky;Header=header;}
        public override string ToString()=>Header?Name:(Risky?$"⚠ {Name}":Name);
    }

    static UnitOption H(string name,bool risky=false)=>new(uint.MaxValue,name,risky,true);
    static UnitOption U(uint type,string name,bool risky=false)=>new(type,name,risky,false);

'''
between('    sealed class UnitOption','    sealed record BuildingProfile',unit_model)

units=r'''    static readonly UnitOption[] Units=
    {
        H("DRAGON — REGULAR"),
        U(0,"Dragon Archer"),U(1,"Dragon Chemist"),U(2,"Dragon Dragon Warrior"),U(3,"Dragon Geisha"),U(4,"Dragon Kabuki Warrior"),U(5,"Dragon Peasant"),U(6,"Dragon Powder Keg Cannoneer"),U(7,"Dragon Samurai"),U(8,"Dragon Spearman"),

        H("SERPENT — REGULAR"),
        U(19,"Serpent Bandit"),U(20,"Serpent Cannoneer"),U(21,"Serpent Crossbowman"),U(22,"Serpent Fan Geisha"),U(23,"Serpent Musketeer"),U(24,"Serpent Peasant"),U(25,"Serpent Raider"),U(26,"Serpent Ronin"),U(29,"Serpent Swordsman"),

        H("LOTUS — REGULAR"),
        U(30,"Lotus Blade Acolyte"),U(34,"Lotus Channeler"),U(35,"Lotus Diseased One"),U(37,"Lotus Infested One"),U(38,"Lotus Leaf Disciple"),U(39,"Lotus Master Warlock"),U(40,"Lotus Peasant"),U(41,"Lotus Staff Adept"),U(42,"Lotus Unclean One"),U(43,"Lotus Warlock"),

        H("WOLF — REGULAR"),
        U(44,"Wolf Ballistaman"),U(45,"Wolf Berserker"),U(46,"Wolf Brawler"),U(47,"Wolf Druidess"),U(48,"Wolf Hurler"),U(49,"Wolf Mauler"),U(50,"Wolf Pack Master"),U(51,"Wolf Peasant"),U(52,"Wolf Pitch Slinger"),U(53,"Wolf Sledger"),U(54,"Wolf Werewolf"),

        H("⚠ SPECIAL / UNIQUE — STORY RISK",true),
        U(27,"Serpent Spirit Warrior",true),U(28,"Spirit Warrior 2",true),
        U(31,"Lotus Brother One",true),U(32,"Lotus Brother Two",true),U(33,"Lotus Brother Three",true),U(36,"Lotus Golem",true),
        U(104,"Serpent Necromancer",true),

        H("⚠ CLASSIC HEROES / STORY — RISK",true),
        U(85,"Hero Arah",true),U(86,"Hero Budo",true),U(87,"Hero Gaihla",true),U(88,"Hero Garrin",true),U(89,"Hero Grayback",true),U(90,"Hero Issyl",true),U(91,"Hero Kazan",true),
        U(92,"Hero Kenji",true),U(93,"Hero Kenji No Sword",true),U(94,"Hero Kenji (Serpent)",true),U(95,"Hero Kenji Young",true),U(96,"Hero Kenji 2",true),U(97,"Hero Kenji 2 No Sword",true),U(98,"Hero Kenji 2 (Serpent)",true),U(99,"Hero Kenji 3",true),U(100,"Hero Kenji (One with the Dragon)",true),U(101,"Hero Kenji 3 Serpent",true),
        U(102,"Hero Koril",true),U(103,"Hero Longtooth",true),U(105,"Hero Otomo",true),U(106,"Hero Otomo No Sword",true),U(107,"Hero Shinja",true),U(108,"Hero Shinja No Sword",true),U(109,"Hero Soban",true),U(110,"Hero Tao",true),U(111,"Hero The Shale Lord",true),U(112,"Hero Utara",true),U(113,"Hero Vetkin",true),U(114,"Hero Zymeth",true),U(115,"Hero Zymeth No Corruption",true),

        H("⚠ WOTW UNITS — RISK",true),
        U(116,"Dragon Chakram Maiden",true),U(117,"Dragon Chakram Maiden Doppleganger",true),U(118,"Dragon Guardian",true),U(119,"Dragon Guardian (Dying)",true),
        U(120,"Serpent Enforcer",true),U(121,"Serpent Witch",true),U(122,"Serpent Witch (Demon Form)",true),U(123,"Serpent Witch (Demon Form 2)",true),
        U(124,"Lotus Overseer",true),U(125,"Lotus Reaper",true),U(126,"Wolf Digger",true),U(127,"Wolf Dryad",true),

        H("⚠ WOTW HEROES / STORY — RISK",true),
        U(136,"Hero Grayback (Endgame)",true),U(137,"Hero Grayback (Middle)",true),U(138,"Hero Grayback (Slave)",true),U(139,"Hero Longtooth (Slave)",true),
        U(140,"Hero Taro",true),U(141,"Hero Teppo",true),U(142,"Hero Wildeye",true),U(143,"Hero Yvaine",true),U(144,"Hero Yvaine (Ungodly Power)",true)
    };

'''
between('    static readonly UnitOption[] Units=','    readonly ToggleSwitch rice=',units+'    readonly ToggleSwitch rice=')

rep('        Text="BRZE Trainer — V15 Diagnostics";','        Text="BRZE Trainer — V16 Diagnostics";')
rep('        Text="BRZE Trainer — V15 Clean";','        Text="BRZE Trainer — V16 Clean";')
rep('        header.Controls.Add(new Label{Text=$"V15  ·  COMPACT BUILDING PROFILES  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(900,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});',
    '        header.Controls.Add(new Label{Text=$"V16  ·  CATEGORIZED UNIT CATALOG  ·  {mode}  ·  BRZE 1.60",AutoSize=false,Location=new Point(7,37),Size=new Size(940,20),ForeColor=Color.FromArgb(177,188,202),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.8f),TextAlign=ContentAlignment.MiddleLeft});')

old_slot='''        slotOn[i].Dock=DockStyle.Fill;slotOn[i].ForeColor=Color.FromArgb(220,226,233);slotOn[i].BackColor=Color.Transparent;slotOn[i].Text="";slotOn[i].CheckAlign=ContentAlignment.MiddleCenter;slotOn[i].CheckedChanged+=(_,_)=>SlotChanged(i);row.Controls.Add(slotOn[i],1,0);
        slotOut[i].Dock=DockStyle.Fill;slotOut[i].DropDownStyle=ComboBoxStyle.DropDownList;slotOut[i].FlatStyle=FlatStyle.Flat;slotOut[i].BackColor=Color.FromArgb(44,51,62);slotOut[i].ForeColor=Color.WhiteSmoke;slotOut[i].Font=new Font("Segoe UI",8.2f);slotOut[i].Items.AddRange(Units.Cast<object>().ToArray());slotOut[i].SelectedIndexChanged+=(_,_)=>SlotChanged(i);row.Controls.Add(slotOut[i],2,0);'''
new_slot='''        slotOn[i].Dock=DockStyle.Fill;slotOn[i].ForeColor=Color.FromArgb(220,226,233);slotOn[i].BackColor=Color.Transparent;slotOn[i].Text="";slotOn[i].CheckAlign=ContentAlignment.MiddleCenter;slotOn[i].CheckedChanged+=(_,_)=>SlotToggleChanged(i);row.Controls.Add(slotOn[i],1,0);
        slotOut[i].Dock=DockStyle.Fill;slotOut[i].DropDownStyle=ComboBoxStyle.DropDownList;slotOut[i].FlatStyle=FlatStyle.Flat;slotOut[i].BackColor=Color.FromArgb(44,51,62);slotOut[i].ForeColor=Color.WhiteSmoke;slotOut[i].Font=new Font("Segoe UI",8.2f);slotOut[i].DrawMode=DrawMode.OwnerDrawFixed;slotOut[i].ItemHeight=22;slotOut[i].IntegralHeight=false;slotOut[i].DropDownHeight=374;slotOut[i].MaxDropDownItems=18;slotOut[i].Items.AddRange(Units.Cast<object>().ToArray());slotOut[i].DrawItem+=DrawUnitItem;slotOut[i].SelectedIndexChanged+=(_,_)=>UnitSelectionChanged(i);row.Controls.Add(slotOut[i],2,0);'''
rep(old_slot,new_slot)

select=r'''    int FindUnitIndex(uint type)
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
        loadingProfile=false;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
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

    void UnitSelectionChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;
        if(slotOut[i].SelectedItem is not UnitOption u)return;
        if(u.Header)
        {
            loadingProfile=true;slotOut[i].SelectedIndex=FindUnitIndex(profileOut[editProfile,i]);loadingProfile=false;UpdateSlotVisual(i);return;
        }

        profileOut[editProfile,i]=u.Type;
        if(u.Risky&&slotOn[i].Checked)
        {
            loadingProfile=true;slotOn[i].Checked=false;loadingProfile=false;profileOn[editProfile,i]=false;
            SetNotice($"S{i+1} disabled — risky Hero/WotW selection requires manual confirmation before spawning");
        }
        UpdateSlotVisual(i);UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

    void SlotToggleChanged(int i)
    {
        if(loadingProfile||i<0||i>=9)return;
        if(slotOut[i].SelectedItem is not UnitOption u||u.Header)
        {
            loadingProfile=true;slotOn[i].Checked=false;loadingProfile=false;profileOn[editProfile,i]=false;return;
        }

        if(slotOn[i].Checked&&u.Risky)
        {
            var answer=MessageBox.Show(this,
                $"RISKY STORY / WOTW UNIT\n\n{u.Name}\n\nSpawning heroes, story variants, or WotW-only units in Kenji Journey can interfere with map scripts and may crash or break progression.\n\nEnable this output anyway?",
                "BRZE Trainer — Risky Unit Confirmation",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2);
            if(answer!=DialogResult.Yes)
            {
                loadingProfile=true;slotOn[i].Checked=false;loadingProfile=false;profileOn[editProfile,i]=false;UpdateSlotVisual(i);UpdateUnitLabels();return;
            }
        }

        profileOn[editProfile,i]=slotOn[i].Checked;profileOut[editProfile,i]=u.Type;
        UpdateSlotVisual(i);UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;
    }

'''
between('    void SelectProfile(int p)','    void UpdateUnitLabels()',select)

# A compact legend explains the red catalog without making the panel taller.
rep('        head.Controls.Add(new Label{Text="UNIT CHANGER  //  BUILDING",AutoSize=false,Location=new Point(0,0),Size=new Size(360,23),ForeColor=Color.FromArgb(109,214,167),Font=new Font("Segoe UI Semibold",10.2f),TextAlign=ContentAlignment.MiddleLeft});',
'''        head.Controls.Add(new Label{Text="UNIT CHANGER  //  BUILDING",AutoSize=false,Location=new Point(0,0),Size=new Size(360,23),ForeColor=Color.FromArgb(109,214,167),Font=new Font("Segoe UI Semibold",10.2f),TextAlign=ContentAlignment.MiddleLeft});
        head.Controls.Add(new Label{Text="RED = HERO / STORY / WOTW RISK",AutoSize=false,Location=new Point(350,0),Size=new Size(235,23),ForeColor=Color.FromArgb(255,145,145),Font=new Font("Segoe UI Semibold",8.0f),TextAlign=ContentAlignment.MiddleRight});''')

Path('FinalV16MainForm.cs').write_text(s,encoding='utf-8')
print('V16 categorized catalog generated: regular groups + red risky heroes/story/WotW + explicit enable confirmation')
