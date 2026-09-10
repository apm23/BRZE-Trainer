from pathlib import Path

p = Path('MergedProgram.cs')
src = p.read_text(encoding='utf-8')
if 'using System.Drawing.Drawing2D;' not in src:
    src = src.replace('using System.Drawing;\n', 'using System.Drawing;\nusing System.Drawing.Drawing2D;\n', 1)

start = src.index('internal sealed class MainForm : Form')
end = src.index('internal static class Native')

ui = r'''internal static class Ui
{
    public static GraphicsPath Round(Rectangle r,int radius)
    {
        int d=radius*2;
        var gp=new GraphicsPath();
        gp.AddArc(r.X,r.Y,d,d,180,90);
        gp.AddArc(r.Right-d,r.Y,d,d,270,90);
        gp.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);
        gp.AddArc(r.X,r.Bottom-d,d,d,90,90);
        gp.CloseFigure();
        return gp;
    }
}

internal sealed class BattleBackdrop : Panel
{
    public BattleBackdrop()
    {
        DoubleBuffered=true;
        ResizeRedraw=true;
        BackColor=Color.FromArgb(10,13,18);
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g=e.Graphics;
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var bg=new LinearGradientBrush(ClientRectangle,Color.FromArgb(10,15,25),Color.FromArgb(44,20,20),90f))g.FillRectangle(bg,ClientRectangle);

        int w=Math.Max(1,Width),h=Math.Max(1,Height);
        var moon=new Rectangle(w-250,38,150,150);
        using(var glow=new SolidBrush(Color.FromArgb(28,235,190,112)))g.FillEllipse(glow,moon.X-25,moon.Y-25,moon.Width+50,moon.Height+50);
        using(var mb=new SolidBrush(Color.FromArgb(72,242,213,154)))g.FillEllipse(mb,moon);

        Point[] far={new(0,(int)(h*.52)),new((int)(w*.15),(int)(h*.28)),new((int)(w*.30),(int)(h*.50)),new((int)(w*.48),(int)(h*.25)),new((int)(w*.67),(int)(h*.50)),new((int)(w*.84),(int)(h*.31)),new(w,(int)(h*.53)),new(w,h),new(0,h)};
        using(var b=new SolidBrush(Color.FromArgb(70,14,22,28)))g.FillPolygon(b,far);
        Point[] near={new(0,(int)(h*.68)),new((int)(w*.18),(int)(h*.44)),new((int)(w*.37),(int)(h*.66)),new((int)(w*.57),(int)(h*.42)),new((int)(w*.79),(int)(h*.67)),new(w,(int)(h*.48)),new(w,h),new(0,h)};
        using(var b=new SolidBrush(Color.FromArgb(120,8,13,18)))g.FillPolygon(b,near);

        // Minimal temple silhouette: enough atmosphere without embedding a bitmap asset.
        int tx=70,ty=h-205;
        using(var ink=new SolidBrush(Color.FromArgb(185,5,8,11)))
        {
            g.FillRectangle(ink,tx+62,ty+72,94,118);
            Point[] roof1={new(tx,ty+76),new(tx+110,ty+28),new(tx+220,ty+76),new(tx+178,ty+68),new(tx+110,ty+82),new(tx+42,ty+68)};
            g.FillPolygon(ink,roof1);
            Point[] roof2={new(tx+35,ty+28),new(tx+110,ty-8),new(tx+185,ty+28),new(tx+154,ty+23),new(tx+110,ty+36),new(tx+66,ty+23)};
            g.FillPolygon(ink,roof2);
        }

        using(var haze=new LinearGradientBrush(new Rectangle(0,h-220,w,220),Color.FromArgb(0,180,92,64),Color.FromArgb(70,4,6,9),90f))g.FillRectangle(haze,0,h-220,w,220);
        using(var vignette=new Pen(Color.FromArgb(55,0,0,0),34))g.DrawRectangle(vignette,0,0,w-1,h-1);
    }
}

internal sealed class ToggleSwitch : CheckBox
{
    readonly string feature;
    readonly string hotkey;
    bool hover;
    public ToggleSwitch(string feature,string hotkey="")
    {
        this.feature=feature;this.hotkey=hotkey;
        AutoSize=false;Width=164;Height=46;
        Cursor=Cursors.Hand;
        Font=new Font("Segoe UI Semibold",9.2f,FontStyle.Regular);
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        CheckedChanged+=(_,_)=>Invalidate();
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        var card=new Rectangle(0,0,Width-1,Height-1);
        using(var gp=Ui.Round(card,11))
        using(var cb=new SolidBrush(hover?Color.FromArgb(49,54,65):Color.FromArgb(37,42,51)))g.FillPath(cb,gp);
        using(var gp=Ui.Round(card,11))
        using(var pen=new Pen(Checked?Color.FromArgb(92,196,150):Color.FromArgb(72,78,90),1f))g.DrawPath(pen,gp);

        using(var fb=new SolidBrush(Color.FromArgb(238,242,246)))g.DrawString(feature,Font,fb,new RectangleF(12,7,92,18));
        if(!string.IsNullOrWhiteSpace(hotkey))using(var hb=new SolidBrush(Color.FromArgb(145,155,169)))g.DrawString(hotkey,new Font("Segoe UI",7.3f),hb,new RectangleF(12,26,68,14));

        var track=new Rectangle(108,11,44,24);
        using(var gp=Ui.Round(track,12))using(var tb=new SolidBrush(Checked?Color.FromArgb(52,174,122):Color.FromArgb(82,88,99)))g.FillPath(tb,gp);
        int cx=Checked?139:121;
        using(var thumb=new SolidBrush(Color.White))g.FillEllipse(thumb,cx-9,14,18,18);
    }
}

internal sealed class ActionButton : Button
{
    public bool Accent {get;set;}
    public bool Danger {get;set;}
    bool hover;
    public ActionButton(string text){Text=text;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Width=146;Height=40;Cursor=Cursors.Hand;Font=new Font("Segoe UI Semibold",9.2f);SetStyle(ControlStyles.UserPaint,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        Color c=Danger?Color.FromArgb(143,58,58):Accent?Color.FromArgb(40,133,102):Color.FromArgb(54,61,73);
        if(hover)c=ControlPaint.Light(c,0.12f);
        var r=new Rectangle(0,0,Width-1,Height-1);
        using(var gp=Ui.Round(r,10))using(var b=new SolidBrush(c))g.FillPath(b,gp);
        TextRenderer.DrawText(g,Text,Font,r,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
    }
}

internal sealed class StatusBox : Panel
{
    public StatusBox(){DoubleBuffered=true;BackColor=Color.FromArgb(24,29,37);Padding=new Padding(20);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        var r=new Rectangle(0,0,Width-1,Height-1);
        using(var gp=Ui.Round(r,14))using(var pen=new Pen(Color.FromArgb(82,91,105),1f))g.DrawPath(pen,gp);
    }
}

internal sealed class MainForm : Form
{
    readonly ToggleSwitch rice=new("Rice","F1"),water=new("Water","F2"),yinYang=new("Yin / Yang","F3"),population=new("Population","F4"),training=new("Training","F7"),peasant=new("Peasant 3s","");
    readonly ToggleSwitch hp=new("Health","F6"),stamina=new("Stamina","F5"),horses=new("Horses","") ,wolves=new("Wolves","F10"),reveal=new("Reveal","") ,burst=new("Death Burst","");
    readonly Label statusText=new(){Dock=DockStyle.Fill,ForeColor=Color.FromArgb(222,228,236),BackColor=Color.Transparent,Font=new Font("Consolas",9.3f),AutoEllipsis=false};
    readonly Label title=new(){Text="BATTLE REALMS  //  TRAINER",AutoSize=true,ForeColor=Color.White,BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",19f,FontStyle.Bold)};
    readonly Label subtitle=new(){Text="ZEN EDITION · BRZE 1.60  —  FINAL PACK",AutoSize=true,ForeColor=Color.FromArgb(181,191,204),BackColor=Color.Transparent,Font=new Font("Segoe UI",9f)};
    readonly System.Windows.Forms.Timer timer=new(){Interval=16};
    readonly bool[] held=new bool[24];

    public MainForm()
    {
        Text="BRZE Trainer — Final";ClientSize=new Size(1130,670);StartPosition=FormStartPosition.CenterScreen;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;MinimizeBox=true;BackColor=Color.Black;

        var root=new BattleBackdrop{Dock=DockStyle.Fill,Padding=new Padding(24)};Controls.Add(root);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,72));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
        root.Controls.Add(layout);

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};head.Controls.Add(title);head.Controls.Add(subtitle);title.Location=new Point(4,4);subtitle.Location=new Point(7,43);layout.Controls.Add(head,0,0);
        layout.Controls.Add(MakeRow(new Control[]{rice,water,yinYang,population,training,peasant}),0,1);
        layout.Controls.Add(MakeRow(new Control[]{hp,stamina,horses,wolves,reveal,burst}),0,2);

        var sb=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,10,0,10)};
        var sh=new Label{Text="SYSTEM STATUS",Dock=DockStyle.Top,Height=30,ForeColor=Color.FromArgb(109,214,167),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10.2f)};
        sb.Controls.Add(statusText);sb.Controls.Add(sh);statusText.Padding=new Padding(0,34,0,0);layout.Controls.Add(sb,0,3);

        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,29,37),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(12,8,12,6)};
        var single=new ActionButton("SINGLE KILL"){Accent=true};
        var build=new ActionButton("BUILD NOW");
        var allOn=new ActionButton("ALL ON"){Accent=true};
        var allOff=new ActionButton("ALL OFF"){Danger=true};
        var hint=new Label{Text="PgDn Single  ·  Del/F8 Build  ·  PgUp HP+Stamina  ·  F9 Master",AutoSize=false,Width=470,Height=40,TextAlign=ContentAlignment.MiddleRight,ForeColor=Color.FromArgb(156,166,179),BackColor=Color.Transparent,Font=new Font("Segoe UI",8.2f)};
        actions.Controls.Add(single);actions.Controls.Add(build);actions.Controls.Add(allOn);actions.Controls.Add(allOff);actions.Controls.Add(hint);layout.Controls.Add(actions,0,4);

        single.Click+=(_,_)=>FireSingleDeath();build.Click+=(_,_)=>Native.InstantSelectedBuilding();allOn.Click+=(_,_)=>SetAll(true);allOff.Click+=(_,_)=>SetAll(false);
        Native.Start();timer.Tick+=(_,_)=>TickTrainer();timer.Start();
        FormClosed+=(_,_)=>{WolfCore.Stop();RevealMapCore.Stop();InstantDeathCore.Stop();StaminaCore.Stop();HorseCore.Stop();HookCore.Stop();SelectionCore.Stop();Native.Stop();};
    }

    static Control MakeRow(Control[] controls)
    {
        var p=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,29,37),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(12,6,12,6),Margin=new Padding(0,2,0,2)};
        foreach(var c in controls){c.Margin=new Padding(5,0,5,0);p.Controls.Add(c);}return p;
    }

    void Toggle(int vk,int idx,Action action){bool now=(Native.GetAsyncKeyState(vk)&0x8000)!=0;if(now&&!held[idx])action();held[idx]=now;}
    void SetAll(bool e){rice.Checked=e;water.Checked=e;yinYang.Checked=e;population.Checked=e;training.Checked=e;peasant.Checked=e;hp.Checked=e;stamina.Checked=e;horses.Checked=e;wolves.Checked=e;reveal.Checked=e;burst.Checked=e;}
    void FireSingleDeath(){burst.Checked=false;InstantDeathCore.TriggerSingle();}

    string ActiveList()
    {
        var a=new List<string>();
        void Add(ToggleSwitch t,string n){if(t.Checked)a.Add(n);}
        Add(rice,"Rice");Add(water,"Water");Add(yinYang,"Y/Y");Add(population,"Pop");Add(training,"Train");Add(peasant,"Peasant");Add(hp,"HP");Add(stamina,"Stamina");Add(horses,"Horses");Add(wolves,"Wolves");Add(reveal,"Reveal");Add(burst,"Burst");
        return a.Count==0?"none":string.Join("  ·  ",a);
    }

    void TickTrainer()
    {
        Toggle(0x70,1,()=>rice.Checked=!rice.Checked);Toggle(0x71,2,()=>water.Checked=!water.Checked);Toggle(0x72,3,()=>yinYang.Checked=!yinYang.Checked);
        Toggle(0x73,4,()=>population.Checked=!population.Checked);Toggle(0x74,5,()=>stamina.Checked=!stamina.Checked);Toggle(0x75,6,()=>hp.Checked=!hp.Checked);
        Toggle(0x76,7,()=>training.Checked=!training.Checked);Toggle(0x77,8,()=>Native.InstantSelectedBuilding());Toggle(0x2E,11,()=>Native.InstantSelectedBuilding());
        Toggle(0x22,12,()=>FireSingleDeath());Toggle(0x21,13,()=>{bool n=!(hp.Checked&&stamina.Checked);hp.Checked=n;stamina.Checked=n;});
        Toggle(0x78,9,()=>{bool all=rice.Checked&&water.Checked&&yinYang.Checked&&population.Checked&&training.Checked&&peasant.Checked&&hp.Checked&&stamina.Checked&&horses.Checked&&wolves.Checked&&reveal.Checked&&burst.Checked;SetAll(!all);});
        Toggle(0x79,10,()=>wolves.Checked=!wolves.Checked);

        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);
        string staminaStatus=StaminaCore.Tick(stamina.Checked);
        Native.ApplyLegacyRuntime(false,false,false,false);
        string horseStatus=HorseCore.Tick(horses.Checked);
        string deathStatus=InstantDeathCore.Tick(burst.Checked);
        string mapStatus=RevealMapCore.Tick(reveal.Checked);
        string wolfStatus=WolfCore.Tick(wolves.Checked);
        string selectionStatus=SelectionCore.Tick(peasant.Checked);
        string nativeStatus=Native.Apply(rice.Checked,water.Checked,yinYang.Checked,population.Checked,training.Checked);

        bool attached=!(nativeStatus.Contains("not found",StringComparison.OrdinalIgnoreCase)||nativeStatus.Contains("waiting",StringComparison.OrdinalIgnoreCase));
        statusText.Text=
            $"GAME       {(attached?"● CONNECTED":"○ WAITING FOR BRZE")}\r\n"+
            $"ACTIVE     {ActiveList()}\r\n\r\n"+
            $"DEATH      {(burst.Checked?"BURST / ERASER ON":"IDLE — SINGLE READY")}\r\n"+
            $"MAP        {(reveal.Checked?"REVEALED":"NORMAL")}        WOLVES  {(wolves.Checked?"ON":"OFF")}        HORSES  {(horses.Checked?"ON":"OFF")}\r\n"+
            $"HP         {(hp.Checked?"ON":"OFF")}        STAMINA {(stamina.Checked?"ON":"OFF")}        TRAIN {(training.Checked?"ON":"OFF")}\r\n\r\n"+
            $"{wolfStatus}\r\n{deathStatus}\r\n{horseStatus}\r\n{selectionStatus}\r\n{staminaStatus}\r\n{hookStatus}";
    }
}

'''

src = src[:start] + ui + src[end:]
p.write_text(src, encoding='utf-8')
print('V9 final themed two-row UI generated OK')
