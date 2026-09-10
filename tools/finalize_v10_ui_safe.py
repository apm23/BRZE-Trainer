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
    Bitmap? cache;
    Size cacheSize;

    public BattleBackdrop()
    {
        DoubleBuffered=true;
        ResizeRedraw=false;
        BackColor=Color.FromArgb(10,13,18);
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        RebuildCache();
        Invalidate();
    }

    void RebuildCache()
    {
        if(Width<=0||Height<=0)return;
        cache?.Dispose();
        cache=new Bitmap(Width,Height);
        cacheSize=ClientSize;
        using var g=Graphics.FromImage(cache);
        g.SmoothingMode=SmoothingMode.AntiAlias;
        var rect=new Rectangle(0,0,Width,Height);
        using(var bg=new LinearGradientBrush(rect,Color.FromArgb(10,15,25),Color.FromArgb(44,20,20),90f))g.FillRectangle(bg,rect);

        int w=Math.Max(1,Width),h=Math.Max(1,Height);
        var moon=new Rectangle(w-278,42,164,164);
        using(var glow=new SolidBrush(Color.FromArgb(28,235,190,112)))g.FillEllipse(glow,moon.X-28,moon.Y-28,moon.Width+56,moon.Height+56);
        using(var mb=new SolidBrush(Color.FromArgb(72,242,213,154)))g.FillEllipse(mb,moon);

        Point[] far={new(0,(int)(h*.52)),new((int)(w*.15),(int)(h*.28)),new((int)(w*.30),(int)(h*.50)),new((int)(w*.48),(int)(h*.25)),new((int)(w*.67),(int)(h*.50)),new((int)(w*.84),(int)(h*.31)),new(w,(int)(h*.53)),new(w,h),new(0,h)};
        using(var b=new SolidBrush(Color.FromArgb(70,14,22,28)))g.FillPolygon(b,far);
        Point[] near={new(0,(int)(h*.68)),new((int)(w*.18),(int)(h*.44)),new((int)(w*.37),(int)(h*.66)),new((int)(w*.57),(int)(h*.42)),new((int)(w*.79),(int)(h*.67)),new(w,(int)(h*.48)),new(w,h),new(0,h)};
        using(var b=new SolidBrush(Color.FromArgb(120,8,13,18)))g.FillPolygon(b,near);

        int tx=76,ty=h-218;
        using(var ink=new SolidBrush(Color.FromArgb(185,5,8,11)))
        {
            g.FillRectangle(ink,tx+62,ty+72,94,128);
            Point[] roof1={new(tx,ty+76),new(tx+110,ty+28),new(tx+220,ty+76),new(tx+178,ty+68),new(tx+110,ty+82),new(tx+42,ty+68)};
            g.FillPolygon(ink,roof1);
            Point[] roof2={new(tx+35,ty+28),new(tx+110,ty-8),new(tx+185,ty+28),new(tx+154,ty+23),new(tx+110,ty+36),new(tx+66,ty+23)};
            g.FillPolygon(ink,roof2);
        }
        using(var haze=new LinearGradientBrush(new Rectangle(0,h-235,w,235),Color.FromArgb(0,180,92,64),Color.FromArgb(70,4,6,9),90f))g.FillRectangle(haze,0,h-235,w,235);
        using(var vignette=new Pen(Color.FromArgb(55,0,0,0),34))g.DrawRectangle(vignette,0,0,w-1,h-1);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if(cache==null||cacheSize!=ClientSize)RebuildCache();
        if(cache!=null)e.Graphics.DrawImageUnscaled(cache,0,0);
        else base.OnPaintBackground(e);
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing){cache?.Dispose();cache=null;}
        base.Dispose(disposing);
    }
}

internal sealed class ToggleSwitch : CheckBox
{
    readonly string feature;
    readonly string hotkey;
    bool hover;
    public ToggleSwitch(string feature,string hotkey)
    {
        this.feature=feature;this.hotkey=hotkey;
        Text=feature;
        AccessibleName=feature+" "+hotkey;
        AutoSize=false;Width=190;Height=48;
        Cursor=Cursors.Hand;
        Font=new Font("Segoe UI Semibold",9.4f,FontStyle.Regular);
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        CheckedChanged+=(_,_)=>Invalidate();
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        var card=new Rectangle(0,0,Width-1,Height-1);
        using(var gp=Ui.Round(card,11))using(var cb=new SolidBrush(hover?Color.FromArgb(49,54,65):Color.FromArgb(37,42,51)))g.FillPath(cb,gp);
        using(var gp=Ui.Round(card,11))using(var pen=new Pen(Checked?Color.FromArgb(92,196,150):Color.FromArgb(72,78,90),1f))g.DrawPath(pen,gp);

        var mainRect=new Rectangle(14,5,112,21);
        TextRenderer.DrawText(g,feature,Font,mainRect,Color.FromArgb(238,242,246),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
        using(var hf=new Font("Segoe UI",7.4f))
            TextRenderer.DrawText(g,hotkey,hf,new Rectangle(14,27,112,15),Color.FromArgb(150,160,174),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);

        var track=new Rectangle(132,12,46,24);
        using(var gp=Ui.Round(track,12))using(var tb=new SolidBrush(Checked?Color.FromArgb(52,174,122):Color.FromArgb(82,88,99)))g.FillPath(tb,gp);
        int cx=Checked?165:145;
        using(var thumb=new SolidBrush(Color.White))g.FillEllipse(thumb,cx-9,15,18,18);
    }
}

internal sealed class ActionButton : Button
{
    readonly string feature;
    readonly string hotkey;
    public bool Accent {get;set;}
    public bool Danger {get;set;}
    bool hover;
    public ActionButton(string feature,string hotkey)
    {
        this.feature=feature;this.hotkey=hotkey;Text=feature;
        AccessibleName=feature+" "+hotkey;
        FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;
        Width=168;Height=50;Cursor=Cursors.Hand;
        Font=new Font("Segoe UI Semibold",9.1f);
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
    }
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        Color c=Danger?Color.FromArgb(143,58,58):Accent?Color.FromArgb(40,133,102):Color.FromArgb(54,61,73);
        if(hover)c=ControlPaint.Light(c,0.12f);
        var r=new Rectangle(0,0,Width-1,Height-1);
        using(var gp=Ui.Round(r,10))using(var b=new SolidBrush(c))g.FillPath(b,gp);
        TextRenderer.DrawText(g,feature,Font,new Rectangle(12,6,Width-24,21),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
        using(var hf=new Font("Segoe UI",7.4f))
            TextRenderer.DrawText(g,hotkey,hf,new Rectangle(12,28,Width-24,15),Color.FromArgb(205,218,228),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
    }
}

internal sealed class StatusBox : Panel
{
    public StatusBox()
    {
        DoubleBuffered=true;
        BackColor=Color.FromArgb(24,29,37);
        Padding=new Padding(20);
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        var r=new Rectangle(0,0,Width-1,Height-1);
        using(var gp=Ui.Round(r,14))using(var pen=new Pen(Color.FromArgb(82,91,105),1f))g.DrawPath(pen,gp);
    }
}

internal readonly struct GameSnapshot
{
    public readonly int Stage;
    public readonly string Label;
    public bool Ready=>Stage==3;
    public GameSnapshot(int stage,string label){Stage=stage;Label=label;}
}

// Read-only, low-frequency readiness probe. It never opens BRZE with write rights and never patches it.
// This lets the trainer be launched first and lets toggles be armed safely at menu/loading screens.
internal static class GameGate
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    const uint READ_ONLY=0x400|0x10;
    const int RVA_PLAYER_PTR=0x4416A0,RVA_LOCAL_ID=0x4416D0,PLAYER_STRIDE=0x5E8;
    static DateTime nextProbeUtc=DateTime.MinValue;
    static GameSnapshot cached=new(0,"○ BRZE NOT RUNNING");

    static bool R32(IntPtr h,long addr,out uint value)
    {
        value=0;var b=new byte[4];
        if(!ReadProcessMemory(h,new IntPtr(unchecked((int)(uint)addr)),b,4,out var n)||n.ToInt64()!=4)return false;
        value=BitConverter.ToUInt32(b,0);return true;
    }

    public static GameSnapshot Probe(bool force=false)
    {
        var now=DateTime.UtcNow;
        if(!force&&now<nextProbeUtc)return cached;
        nextProbeUtc=now.AddMilliseconds(400);
        Process[] ps=Array.Empty<Process>();
        try
        {
            ps=Process.GetProcessesByName("Battle_Realms_F");
            if(ps.Length==0)return cached=new GameSnapshot(0,"○ BRZE NOT RUNNING");
            var gp=ps[0];
            if(gp.HasExited)return cached=new GameSnapshot(0,"○ BRZE NOT RUNNING");
            long baseAddr;
            try{baseAddr=gp.MainModule?.BaseAddress.ToInt64()??0;}catch{return cached=new GameSnapshot(1,"◐ BRZE STARTING");}
            if(baseAddr==0)return cached=new GameSnapshot(1,"◐ BRZE STARTING");

            IntPtr h=OpenProcess(READ_ONLY,false,gp.Id);
            if(h==IntPtr.Zero)return cached=new GameSnapshot(1,"◐ BRZE STARTING");
            try
            {
                if(!R32(h,baseAddr+RVA_PLAYER_PTR,out uint playerPtr))return cached=new GameSnapshot(1,"◐ BRZE STARTING");
                if(playerPtr==0)return cached=new GameSnapshot(2,"○ MENU / WAITING FOR BATTLE");
                if(!R32(h,baseAddr+RVA_LOCAL_ID,out uint localId)||localId>=10)return cached=new GameSnapshot(2,"○ LOADING BATTLE DATA");
                long player=(long)playerPtr+(long)localId*PLAYER_STRIDE;
                if(player<0x10000||!R32(h,player,out _))return cached=new GameSnapshot(2,"○ LOADING BATTLE DATA");
                return cached=new GameSnapshot(3,"● BATTLE READY");
            }
            finally{CloseHandle(h);}
        }
        catch{return cached=new GameSnapshot(ps.Length==0?0:1,ps.Length==0?"○ BRZE NOT RUNNING":"◐ BRZE STARTING");}
        finally{foreach(var q in ps)try{q.Dispose();}catch{}}
    }
}

internal sealed class MainForm : Form
{
    readonly ToggleSwitch rice=new("Rice","F1"),water=new("Water","F2"),yinYang=new("Yin / Yang","F3"),population=new("Population","F4"),training=new("Training","F7"),peasant=new("Peasant 3s","F12");
    readonly ToggleSwitch hp=new("Health","F6"),stamina=new("Stamina","F5"),horses=new("Horses","F11"),wolves=new("Wolves","F10"),reveal=new("Reveal","Insert"),burst=new("Death Burst","End");
    readonly Label statusText=new(){Dock=DockStyle.Fill,ForeColor=Color.FromArgb(222,228,236),BackColor=Color.Transparent,Font=new Font("Consolas",9.4f),AutoEllipsis=false,Padding=new Padding(0,4,0,0)};
    readonly System.Windows.Forms.Timer timer=new(){Interval=100};
    readonly bool[] held=new bool[32];
    bool runtimeWasReady;
    DateTime noticeUntilUtc=DateTime.MinValue;
    string notice="";
    string lastStatus="";
    DateTime nextStatusUtc=DateTime.MinValue;

    public MainForm()
    {
        Text="BRZE Trainer — Final";
        ClientSize=new Size(1270,770);
        MinimumSize=MaximumSize=Size;
        StartPosition=FormStartPosition.CenterScreen;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;MinimizeBox=true;BackColor=Color.Black;
        SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);

        var root=new BattleBackdrop{Dock=DockStyle.Fill,Padding=new Padding(24)};Controls.Add(root);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,88));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,68));
        root.Controls.Add(layout);

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        var title=new Label{Text="BATTLE REALMS  //  TRAINER",AutoSize=false,Location=new Point(6,6),Size=new Size(900,38),ForeColor=Color.White,BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",19f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};
        var subtitle=new Label{Text="ZEN EDITION  ·  BRZE 1.60  ·  FINAL PACK",AutoSize=false,Location=new Point(9,48),Size=new Size(900,24),ForeColor=Color.FromArgb(181,191,204),BackColor=Color.Transparent,Font=new Font("Segoe UI",9.2f),TextAlign=ContentAlignment.MiddleLeft};
        head.Controls.Add(title);head.Controls.Add(subtitle);layout.Controls.Add(head,0,0);

        layout.Controls.Add(MakeRow(new Control[]{rice,water,yinYang,population,training,peasant}),0,1);
        layout.Controls.Add(MakeRow(new Control[]{hp,stamina,horses,wolves,reveal,burst}),0,2);

        var sb=new StatusBox{Dock=DockStyle.Fill,Margin=new Padding(0,10,0,10)};
        var statusLayout=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.Transparent,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
        statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,32));statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var sh=new Label{Text="SYSTEM STATUS",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(109,214,167),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10.2f),TextAlign=ContentAlignment.MiddleLeft};
        statusLayout.Controls.Add(sh,0,0);statusLayout.Controls.Add(statusText,0,1);sb.Controls.Add(statusLayout);layout.Controls.Add(sb,0,3);

        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,29,37),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(10,7,10,7),Margin=new Padding(0,2,0,0)};
        var single=new ActionButton("SINGLE KILL","PgDn"){Accent=true};
        var build=new ActionButton("BUILD NOW","Del / F8");
        var allOn=new ActionButton("ALL ON","F9"){Accent=true};
        var allOff=new ActionButton("ALL OFF","Shift + F9"){Danger=true};
        var signature=new Label{Text="Create By PokakBg",AutoSize=false,Width=475,Height=50,TextAlign=ContentAlignment.MiddleRight,ForeColor=Color.FromArgb(219,188,121),BackColor=Color.Transparent,Font=new Font("Segoe Script",10.2f,FontStyle.Italic)};
        foreach(Control c in new Control[]{single,build,allOn,allOff})c.Margin=new Padding(5,0,5,0);
        signature.Margin=new Padding(10,0,4,0);
        actions.Controls.Add(single);actions.Controls.Add(build);actions.Controls.Add(allOn);actions.Controls.Add(allOff);actions.Controls.Add(signature);layout.Controls.Add(actions,0,4);

        single.Click+=(_,_)=>SafeSingle();
        build.Click+=(_,_)=>SafeBuild();
        allOn.Click+=(_,_)=>SetAll(true);
        allOff.Click+=(_,_)=>SetAll(false);

        Native.Start();
        timer.Tick+=(_,_)=>TickTrainer();timer.Start();
        FormClosed+=(_,_)=>StopAll();
        Shown+=(_,_)=>{GameGate.Probe(true);TickTrainer();};
    }

    static Control MakeRow(Control[] controls)
    {
        var p=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(24,29,37),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Padding=new Padding(8,6,8,6),Margin=new Padding(0,2,0,2)};
        foreach(var c in controls){c.Margin=new Padding(5,0,5,0);p.Controls.Add(c);}return p;
    }

    void SetNotice(string s){notice=s;noticeUntilUtc=DateTime.UtcNow.AddSeconds(2.5);nextStatusUtc=DateTime.MinValue;}
    void ToggleKey(int vk,int idx,Action action){bool now=(Native.GetAsyncKeyState(vk)&0x8000)!=0;if(now&&!held[idx])action();held[idx]=now;}
    void SetAll(bool e){rice.Checked=e;water.Checked=e;yinYang.Checked=e;population.Checked=e;training.Checked=e;peasant.Checked=e;hp.Checked=e;stamina.Checked=e;horses.Checked=e;wolves.Checked=e;reveal.Checked=e;burst.Checked=e;}

    void SafeSingle()
    {
        if(!GameGate.Probe(true).Ready){SetNotice("SINGLE KILL armed action ignored safely — waiting for battle.");return;}
        burst.Checked=false;InstantDeathCore.TriggerSingle();SetNotice("SINGLE KILL fired at current cursor target.");
    }
    void SafeBuild()
    {
        if(!GameGate.Probe(true).Ready){SetNotice("BUILD NOW ignored safely — waiting for battle.");return;}
        Native.InstantSelectedBuilding();SetNotice("BUILD NOW triggered for selected building.");
    }

    string ActiveList()
    {
        var a=new List<string>();
        void Add(ToggleSwitch t,string n){if(t.Checked)a.Add(n);}
        Add(rice,"Rice");Add(water,"Water");Add(yinYang,"Y/Y");Add(population,"Pop");Add(training,"Train");Add(peasant,"Peasant");Add(hp,"HP");Add(stamina,"Stamina");Add(horses,"Horses");Add(wolves,"Wolves");Add(reveal,"Reveal");Add(burst,"Burst");
        return a.Count==0?"none":string.Join("  ·  ",a);
    }

    void HandleHotkeys()
    {
        ToggleKey(0x70,1,()=>rice.Checked=!rice.Checked);ToggleKey(0x71,2,()=>water.Checked=!water.Checked);ToggleKey(0x72,3,()=>yinYang.Checked=!yinYang.Checked);
        ToggleKey(0x73,4,()=>population.Checked=!population.Checked);ToggleKey(0x74,5,()=>stamina.Checked=!stamina.Checked);ToggleKey(0x75,6,()=>hp.Checked=!hp.Checked);
        ToggleKey(0x76,7,()=>training.Checked=!training.Checked);
        ToggleKey(0x77,8,()=>SafeBuild());ToggleKey(0x2E,11,()=>SafeBuild());
        ToggleKey(0x22,12,()=>SafeSingle());
        ToggleKey(0x21,13,()=>{bool n=!(hp.Checked&&stamina.Checked);hp.Checked=n;stamina.Checked=n;});
        ToggleKey(0x79,10,()=>wolves.Checked=!wolves.Checked);
        ToggleKey(0x7A,14,()=>horses.Checked=!horses.Checked);
        ToggleKey(0x7B,15,()=>peasant.Checked=!peasant.Checked);
        ToggleKey(0x2D,16,()=>reveal.Checked=!reveal.Checked);
        ToggleKey(0x23,17,()=>burst.Checked=!burst.Checked);

        bool f9=(Native.GetAsyncKeyState(0x78)&0x8000)!=0;
        if(f9&&!held[9])
        {
            bool shift=(Native.GetAsyncKeyState(0x10)&0x8000)!=0;
            SetAll(!shift);
            SetNotice(shift?"ALL OFF":"ALL ON");
        }
        held[9]=f9;
    }

    void SuspendRuntime()
    {
        try{WolfCore.Stop();}catch{}try{RevealMapCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}
        try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}try{Native.Detach();}catch{}
    }

    void StopAll()
    {
        timer.Stop();
        try{WolfCore.Stop();}catch{}try{RevealMapCore.Stop();}catch{}try{InstantDeathCore.Stop();}catch{}
        try{StaminaCore.Stop();}catch{}try{HorseCore.Stop();}catch{}try{HookCore.Stop();}catch{}try{SelectionCore.Stop();}catch{}try{Native.Stop();}catch{}
    }

    static string OnOff(bool x)=>x?"ON":"OFF";
    void UpdateStatus(string text)
    {
        if(text==lastStatus)return;
        lastStatus=text;statusText.Text=text;
    }

    void TickTrainer()
    {
        HandleHotkeys();
        var gate=GameGate.Probe();
        if(!gate.Ready)
        {
            if(runtimeWasReady){SuspendRuntime();runtimeWasReady=false;}
            if(DateTime.UtcNow>=nextStatusUtc)
            {
                nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);
                string n=DateTime.UtcNow<noticeUntilUtc?"\r\nNOTICE     "+notice:"";
                UpdateStatus(
                    $"GAME       {gate.Label}\r\n"+
                    $"ARMED      {ActiveList()}\r\n\r\n"+
                    "SAFE STANDBY\r\n"+
                    "No cheat writes or game hooks are active yet.\r\n"+
                    "Your toggles stay armed and will apply automatically when battle data is ready."+n);
            }
            return;
        }

        runtimeWasReady=true;
        string hookStatus=HookCore.Tick(false,hp.Checked,training.Checked);
        string staminaStatus=StaminaCore.Tick(stamina.Checked);
        Native.ApplyLegacyRuntime(false,false,false,false);
        string horseStatus=HorseCore.Tick(horses.Checked);
        string deathStatus=InstantDeathCore.Tick(burst.Checked);
        string mapStatus=RevealMapCore.Tick(reveal.Checked);
        string wolfStatus=WolfCore.Tick(wolves.Checked);
        string selectionStatus=SelectionCore.Tick(peasant.Checked);
        string nativeStatus=Native.Apply(rice.Checked,water.Checked,yinYang.Checked,population.Checked,training.Checked);

        if(DateTime.UtcNow>=nextStatusUtc)
        {
            nextStatusUtc=DateTime.UtcNow.AddMilliseconds(250);
            string n=DateTime.UtcNow<noticeUntilUtc?"\r\nNOTICE     "+notice:"";
            UpdateStatus(
                $"GAME       {gate.Label}\r\n"+
                $"ACTIVE     {ActiveList()}\r\n\r\n"+
                $"DEATH      {(burst.Checked?"BURST / ERASER":"IDLE — SINGLE READY")}\r\n"+
                $"MAP        {OnOff(reveal.Checked),-8}   WOLVES  {OnOff(wolves.Checked),-8}   HORSES  {OnOff(horses.Checked)}\r\n"+
                $"HP         {OnOff(hp.Checked),-8}   STAMINA {OnOff(stamina.Checked),-8}   TRAIN   {OnOff(training.Checked)}\r\n\r\n"+
                wolfStatus+"\r\n"+deathStatus+"\r\n"+horseStatus+"\r\n"+selectionStatus+"\r\n"+staminaStatus+"\r\n"+hookStatus+"\r\n"+nativeStatus+n);
        }
    }

    // Do not run the 10 Hz memory/status polling loop while Windows is interactively moving the form.
    // Game-side hooks keep doing their job; pausing polling here removes the visible drag stutter.
    protected override void WndProc(ref Message m)
    {
        const int WM_ENTERSIZEMOVE=0x0231,WM_EXITSIZEMOVE=0x0232;
        if(m.Msg==WM_ENTERSIZEMOVE)timer.Stop();
        base.WndProc(ref m);
        if(m.Msg==WM_EXITSIZEMOVE){timer.Start();TickTrainer();}
    }
}

'''

src = src[:start] + ui + src[end:]
p.write_text(src, encoding='utf-8')
print('V10 polished dark UI + safe pre-game gate generated OK')
