using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZETrainer;

/// <summary>
/// V33 overlay-only layer on top of V30 gameplay.
/// - global Alt+W via HWND RegisterHotKey + GetAsyncKeyState edge fallback
/// - separate no-activate premium deck at 91% opacity
/// - explicit Hero duration and COPY UNIT base offset readouts
/// - death target policy switch only (tap=single / hold=burst remains game-side)
/// - compact full-function Unit Changer bridge to the existing V18.3 controls
/// Gameplay cores are not reimplemented or modified here.
/// </summary>
internal static class OverlayHotkeyControllerV33
{
    const uint MOD_ALT=0x0001,MOD_NOREPEAT=0x4000;
    const uint VK_W=0x57,VK_MENU=0x12;
    const int HOTKEY_ID=0x3377;
    const int WM_HOTKEY=0x0312;
    const int SW_SHOWNOACTIVATE=4;
    const uint SWP_NOSIZE=0x0001,SWP_NOACTIVATE=0x0010,SWP_SHOWWINDOW=0x0040;
    static readonly IntPtr HWND_TOPMOST=new(-1);

    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr hWnd,int id,uint fsModifiers,uint vk);
    [DllImport("user32.dll",SetLastError=true)] static extern bool UnregisterHotKey(IntPtr hWnd,int id);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd,int nCmdShow);
    [DllImport("user32.dll",SetLastError=true)] static extern bool SetWindowPos(IntPtr hWnd,IntPtr hWndInsertAfter,int x,int y,int cx,int cy,uint flags);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd,out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left,Top,Right,Bottom; }

    sealed class HotkeySink : NativeWindow,IDisposable
    {
        readonly Action action;
        public bool Registered { get; }
        public HotkeySink(Action action)
        {
            this.action=action;
            var cp=new CreateParams{Caption="BRZE.V33.AltW.HotkeySink",Parent=new IntPtr(-3)};
            CreateHandle(cp);
            Registered=RegisterHotKey(Handle,HOTKEY_ID,MOD_ALT|MOD_NOREPEAT,VK_W);
        }
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==WM_HOTKEY&&m.WParam.ToInt32()==HOTKEY_ID){action();return;}
            base.WndProc(ref m);
        }
        public void Dispose()
        {
            try{if(Handle!=IntPtr.Zero)UnregisterHotKey(Handle,HOTKEY_ID);}catch{}
            try{DestroyHandle();}catch{}
        }
    }

    static Form? main;
    static PremiumOverlayFormV33? overlay;
    static HotkeySink? sink;
    static System.Windows.Forms.Timer? poll;
    static bool comboWasDown;
    static long lastToggleStamp;
    static bool installed;

    public static void Attach(Form target)
    {
        if(installed)return;
        installed=true;main=target;
        sink=new HotkeySink(TryToggle);
        poll=new System.Windows.Forms.Timer{Interval=45};
        poll.Tick+=(_,_)=>PollFallback();
        poll.Start();
        target.FormClosed+=(_,_)=>Shutdown();
    }

    static void PollFallback()
    {
        bool alt=(GetAsyncKeyState((int)VK_MENU)&0x8000)!=0;
        bool w=(GetAsyncKeyState((int)VK_W)&0x8000)!=0;
        bool down=alt&&w;
        if(down&&!comboWasDown)TryToggle();
        comboWasDown=down;
    }

    static void TryToggle()
    {
        long now=Stopwatch.GetTimestamp();
        if(lastToggleStamp!=0&&(now-lastToggleStamp)/(double)Stopwatch.Frequency<0.32)return;
        lastToggleStamp=now;
        Toggle();
    }

    static IntPtr GameWindow()
    {
        try{return Process.GetProcessesByName("Battle_Realms_F").Select(p=>p.MainWindowHandle).FirstOrDefault(h=>h!=IntPtr.Zero);}
        catch{return IntPtr.Zero;}
    }

    internal static void ReturnGameFocus()
    {
        var game=GameWindow();
        if(game!=IntPtr.Zero)SetForegroundWindow(game);
    }

    static Rectangle OverlayBounds(Form f,IntPtr game)
    {
        Rectangle area;
        if(game!=IntPtr.Zero&&GetWindowRect(game,out var r)&&r.Right>r.Left&&r.Bottom>r.Top)area=Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom);
        else area=Screen.PrimaryScreen?.WorkingArea??new Rectangle(0,0,1920,1080);
        var wa=Screen.FromRectangle(area).WorkingArea;
        int x=area.Right-f.Width-24;
        int y=area.Top+24;
        x=Math.Max(wa.Left+8,Math.Min(x,wa.Right-f.Width-8));
        y=Math.Max(wa.Top+8,Math.Min(y,wa.Bottom-f.Height-8));
        return new Rectangle(x,y,f.Width,f.Height);
    }

    static void Toggle()
    {
        if(main==null||main.IsDisposed)return;
        if(overlay==null||overlay.IsDisposed)overlay=new PremiumOverlayFormV33(main,Hide);
        if(overlay.Visible)Hide();else Show();
    }

    static void Show()
    {
        if(main==null)return;
        if(overlay==null||overlay.IsDisposed)overlay=new PremiumOverlayFormV33(main,Hide);
        var game=GameWindow();
        var b=OverlayBounds(overlay,game);
        overlay.Location=b.Location;
        overlay.SyncNow();
        if(!overlay.Visible)overlay.Show();
        ShowWindow(overlay.Handle,SW_SHOWNOACTIVATE);
        SetWindowPos(overlay.Handle,HWND_TOPMOST,b.X,b.Y,0,0,SWP_NOSIZE|SWP_NOACTIVATE|SWP_SHOWWINDOW);
        ReturnGameFocus();
    }

    static void Hide()
    {
        if(overlay!=null&&!overlay.IsDisposed)overlay.Hide();
        ReturnGameFocus();
    }

    static void Shutdown()
    {
        if(!installed)return;
        try{poll?.Stop();poll?.Dispose();}catch{}
        try{sink?.Dispose();}catch{}
        try{overlay?.Close();overlay?.Dispose();}catch{}
        poll=null;sink=null;overlay=null;main=null;installed=false;comboWasDown=false;lastToggleStamp=0;
    }
}

internal sealed class PremiumOverlayFormV33 : Form
{
    const int WS_EX_TOOLWINDOW=0x00000080,WS_EX_NOACTIVATE=0x08000000;
    const int WM_MOUSEACTIVATE=0x0021,MA_NOACTIVATE=3;

    sealed class UnitEntry
    {
        public int MainIndex { get; }
        public uint Type { get; }
        public string Name { get; }
        public string Group { get; }
        public UnitEntry(int index,uint type,string name,string group){MainIndex=index;Type=type;Name=name;Group=group;}
        public override string ToString()=>Name;
    }

    readonly Form main;
    readonly Action hideAction;
    readonly Dictionary<string,(string Label,Button Button)> quick=new();
    readonly Label status=new();
    readonly Label durationValue=new();
    readonly Label offsetValue=new();
    readonly Button deathPolicy=new();
    readonly Button pageMainButton=new();
    readonly Button pageUnitButton=new();
    readonly Panel pageHost=new(){Dock=DockStyle.Fill,BackColor=Color.Transparent};
    Panel? mainPage,unitPage;
    readonly System.Windows.Forms.Timer refresh=new(){Interval=180};
    decimal heroSeconds=30m;
    float copyOffset=8f;

    readonly ComboBox ucProfile=new(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    readonly ComboBox[] ucUnits=Enumerable.Range(0,9).Select(_=>new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat}).ToArray();
    readonly Button[] ucOn=Enumerable.Range(0,9).Select(_=>new Button()).ToArray();
    readonly Label ucInfo=new();
    readonly List<UnitEntry> unitEntries=new();
    readonly Dictionary<string,Button> filterButtons=new(StringComparer.OrdinalIgnoreCase);
    string? unitFilter;
    bool ucBridgeReady,ucSyncing;

    static readonly Color Bg=Color.FromArgb(13,17,24);
    static readonly Color Card=Color.FromArgb(23,29,39);
    static readonly Color Accent=Color.FromArgb(94,224,177);
    static readonly Color Accent2=Color.FromArgb(110,184,255);
    static readonly Color TextColor=Color.FromArgb(239,244,249);
    static readonly Color Dim=Color.FromArgb(153,166,183);
    static readonly Color Off=Color.FromArgb(42,50,64);
    static readonly Color On=Color.FromArgb(29,115,88);
    static readonly Color Red=Color.FromArgb(105,48,52);

    public PremiumOverlayFormV33(Form main,Action hideAction)
    {
        this.main=main;this.hideAction=hideAction;
        Text="BRZE Live Control Deck";
        ClientSize=new Size(860,540);
        FormBorderStyle=FormBorderStyle.None;
        StartPosition=FormStartPosition.Manual;
        ShowInTaskbar=false;TopMost=true;Opacity=0.91;BackColor=Bg;
        MaximizeBox=false;MinimizeBox=false;DoubleBuffered=true;
        Font=new Font("Segoe UI",9f);
        BuildUi();
        InitUnitChangerBridge();
        refresh.Tick+=(_,_)=>SyncNow();refresh.Start();
        FormClosed+=(_,_)=>refresh.Dispose();
        Resize+=(_,_)=>UpdateRoundedRegion();
        UpdateRoundedRegion();
    }

    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams
    {
        get{var cp=base.CreateParams;cp.ExStyle|=WS_EX_TOOLWINDOW|WS_EX_NOACTIVATE;return cp;}
    }
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==WM_MOUSEACTIVATE){m.Result=(IntPtr)MA_NOACTIVATE;return;}
        base.WndProc(ref m);
    }

    void UpdateRoundedRegion()
    {
        if(Width<40||Height<40)return;
        using var p=new GraphicsPath();int r=20,d=r*2;
        p.AddArc(0,0,d,d,180,90);p.AddArc(Width-d,0,d,d,270,90);p.AddArc(Width-d,Height-d,d,d,0,90);p.AddArc(0,Height-d,d,d,90,90);p.CloseFigure();
        Region?.Dispose();Region=new Region(p);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var br=new LinearGradientBrush(ClientRectangle,Color.FromArgb(16,21,30),Color.FromArgb(9,12,18),LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(br,ClientRectangle);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var p=new Pen(Color.FromArgb(92,Accent),1.4f);e.Graphics.DrawRectangle(p,1,1,Width-3,Height-3);
        using var top=new SolidBrush(Accent);e.Graphics.FillRectangle(top,22,0,165,3);
    }

    void BuildUi()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(20,16,20,16),BackColor=Color.Transparent};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,64));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
        Controls.Add(root);

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        head.Controls.Add(new Label{Text="BATTLE REALMS  //  LIVE CONTROL DECK",Location=new Point(0,2),Size=new Size(470,29),ForeColor=TextColor,Font=new Font("Segoe UI Semibold",15.2f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft});
#if RAW_STATUS
        string mode="DIAGNOSTICS";
#else
        string mode="CLEAN";
#endif
        head.Controls.Add(new Label{Text=$"V33  ·  {mode}  ·  91% GLASS  ·  GAME FOCUS LOCKED",Location=new Point(2,34),Size=new Size(490,20),ForeColor=Dim,Font=new Font("Segoe UI",8.4f),TextAlign=ContentAlignment.MiddleLeft});
        StyleNav(pageMainButton,"MAIN",82);pageMainButton.Location=new Point(505,8);pageMainButton.Click+=(_,_)=>ShowPage(false);head.Controls.Add(pageMainButton);
        StyleNav(pageUnitButton,"UNIT CHANGER",118);pageUnitButton.Location=new Point(593,8);pageUnitButton.Click+=(_,_)=>ShowPage(true);head.Controls.Add(pageUnitButton);
        var hide=FlatButton("ALT+W  HIDE",108,Accent2);hide.Location=new Point(718,8);hide.Height=38;hide.Click+=(_,_)=>hideAction();head.Controls.Add(hide);
        root.Controls.Add(head,0,0);

        mainPage=BuildMainPage();unitPage=BuildUnitChangerPage();
        pageHost.Controls.Add(mainPage);pageHost.Controls.Add(unitPage);root.Controls.Add(pageHost,0,1);ShowPage(false);

        var foot=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(18,23,32),Margin=new Padding(0,10,0,0)};
        status.Dock=DockStyle.Fill;status.Padding=new Padding(14,0,12,0);status.ForeColor=Color.FromArgb(196,207,220);status.Font=new Font("Segoe UI",8.4f);status.TextAlign=ContentAlignment.MiddleLeft;status.AutoEllipsis=true;foot.Controls.Add(status);root.Controls.Add(foot,0,2);
    }

    void StyleNav(Button b,string text,int width)
    {
        b.Text=text;b.Size=new Size(width,38);b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(62,76,94);b.FlatAppearance.BorderSize=1;b.BackColor=Off;b.ForeColor=TextColor;b.Font=new Font("Segoe UI Semibold",8.0f);b.TabStop=false;b.Cursor=Cursors.Hand;
    }

    void ShowPage(bool unit)
    {
        if(mainPage==null||unitPage==null)return;
        mainPage.Visible=!unit;unitPage.Visible=unit;
        pageMainButton.BackColor=unit?Off:Color.FromArgb(38,83,70);
        pageUnitButton.BackColor=unit?Color.FromArgb(41,76,105):Off;
        if(unit){InitUnitChangerBridge();SyncUnitChanger();}
        OverlayHotkeyControllerV33.ReturnGameFocus();
    }

    Panel BuildMainPage()
    {
        var p=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty};
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,53));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,47));
        body.Controls.Add(BuildQuickCard(),0,0);body.Controls.Add(BuildActionCard(),1,0);p.Controls.Add(body);return p;
    }

    Control BuildQuickCard()
    {
        var card=CardPanel("QUICK CHEATS",Accent);
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=4,Padding=new Padding(12,42,12,12),BackColor=Color.Transparent};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int r=0;r<4;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,25));
        var defs=new[]{
            ("rice","RICE"),("water","WATER"),("yinYang","YIN / YANG"),
            ("population","POPULATION"),("training","TRAINING"),("hp","HEALTH"),
            ("stamina","STAMINA"),("horses","HORSES"),("wolves","WOLVES"),
            ("reveal","REVEAL MAP"),("pausePeasant","PAUSE PEASANT"),("peasant","PEASANT 3S")};
        for(int i=0;i<defs.Length;i++)
        {
            var d=defs[i];var b=FlatButton(d.Item2,116,Off);b.Dock=DockStyle.Fill;b.Margin=new Padding(4);string field=d.Item1;
            b.Click+=(_,_)=>ToggleMain(field);quick[field]=(d.Item2,b);grid.Controls.Add(b,i%3,i/3);
        }
        card.Controls.Add(grid);return card;
    }

    Control BuildActionCard()
    {
        var card=CardPanel("HERO + UNIT ACTIONS",Accent2);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,Padding=new Padding(14,43,14,12),BackColor=Color.Transparent};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,42));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,18));root.RowStyles.Add(new RowStyle(SizeType.Absolute,42));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,46));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var dur=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,BackColor=Color.Transparent};
        dur.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,52));dur.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));dur.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,52));dur.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,54));
        var minus=FlatButton("−5",46,Off);var plus=FlatButton("+5",46,Off);var reset=FlatButton("30",48,Off);
        durationValue.Dock=DockStyle.Fill;durationValue.BackColor=Color.FromArgb(34,43,56);durationValue.ForeColor=Accent;durationValue.Font=new Font("Segoe UI Semibold",11.5f);durationValue.TextAlign=ContentAlignment.MiddleCenter;
        minus.Click+=(_,_)=>{heroSeconds=Math.Max(1m,heroSeconds-5m);UpdateDuration();};plus.Click+=(_,_)=>{heroSeconds=Math.Min(420m,heroSeconds+5m);UpdateDuration();};reset.Click+=(_,_)=>{heroSeconds=30m;UpdateDuration();};
        dur.Controls.Add(minus,0,0);dur.Controls.Add(durationValue,1,0);dur.Controls.Add(plus,2,0);dur.Controls.Add(reset,3,0);root.Controls.Add(dur,0,0);UpdateDuration();

        var hero=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,BackColor=Color.Transparent};for(int c=0;c<3;c++)hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        var issyl=FlatButton("ISSYL",100,On);var gray=FlatButton("GRAYBACK",100,On);var both=FlatButton("BOTH",100,Color.FromArgb(45,92,128));
        issyl.Dock=gray.Dock=both.Dock=DockStyle.Fill;issyl.Margin=gray.Margin=both.Margin=new Padding(3);
        issyl.Click+=(_,_)=>Run(()=>HeroEffectDirectCoreV30.Start(DirectHeroMode.Issyl,heroSeconds));
        gray.Click+=(_,_)=>Run(()=>HeroEffectDirectCoreV30.Start(DirectHeroMode.Grayback,heroSeconds));
        both.Click+=(_,_)=>Run(()=>HeroEffectDirectCoreV30.Start(DirectHeroMode.Both,heroSeconds));
        hero.Controls.Add(issyl,0,0);hero.Controls.Add(gray,1,0);hero.Controls.Add(both,2,0);root.Controls.Add(hero,0,1);

        root.Controls.Add(new Label{Text="COPY UNIT  //  BASE OFFSET",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI Semibold",8.1f),TextAlign=ContentAlignment.BottomLeft},0,2);
        var off=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,BackColor=Color.Transparent};
        off.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,52));off.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));off.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,52));off.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,54));
        var om=FlatButton("−4",46,Off);var op=FlatButton("+4",46,Off);var orst=FlatButton("8",48,Off);
        offsetValue.Dock=DockStyle.Fill;offsetValue.BackColor=Color.FromArgb(34,43,56);offsetValue.ForeColor=Accent2;offsetValue.Font=new Font("Segoe UI Semibold",10.6f);offsetValue.TextAlign=ContentAlignment.MiddleCenter;
        om.Click+=(_,_)=>{copyOffset=Math.Max(4f,copyOffset-4f);UpdateOffset();};op.Click+=(_,_)=>{copyOffset=Math.Min(64f,copyOffset+4f);UpdateOffset();};orst.Click+=(_,_)=>{copyOffset=8f;UpdateOffset();};
        off.Controls.Add(om,0,0);off.Controls.Add(offsetValue,1,0);off.Controls.Add(op,2,0);off.Controls.Add(orst,3,0);root.Controls.Add(off,0,3);UpdateOffset();

        var copy=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,BackColor=Color.Transparent};copy.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));copy.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        var cp=FlatButton("COPY SELECTED",120,Off);var paste=FlatButton("PASTE BESIDE",120,Off);
        cp.Dock=paste.Dock=DockStyle.Fill;cp.Margin=paste.Margin=new Padding(3);
        cp.Click+=(_,_)=>Run(()=>IntegratedFrameDispatcherCore.CopySelection().Message);paste.Click+=(_,_)=>Run(()=>IntegratedFrameDispatcherCore.PasteCopied(copyOffset));copy.Controls.Add(cp,0,0);copy.Controls.Add(paste,1,0);root.Controls.Add(copy,0,4);

        deathPolicy.Dock=DockStyle.Fill;deathPolicy.Margin=new Padding(3);deathPolicy.Click+=(_,_)=>ToggleMain("killAll");root.Controls.Add(deathPolicy,0,5);

        var bottom=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,BackColor=Color.Transparent};bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,55));bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,45));
        var uc=FlatButton("UNIT CHANGER  ›",170,Color.FromArgb(45,71,96));uc.Dock=DockStyle.Fill;uc.Margin=new Padding(3);uc.Click+=(_,_)=>ShowPage(true);bottom.Controls.Add(uc,0,0);
        bottom.Controls.Add(new Label{Text="tap = single  ·  hold = burst",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI",7.9f),TextAlign=ContentAlignment.MiddleCenter},1,0);root.Controls.Add(bottom,0,6);
        card.Controls.Add(root);return card;
    }

    Panel BuildUnitChangerPage()
    {
        var page=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        var card=CardPanel("UNIT CHANGER  //  FULL MINI CONTROL",Accent);
        card.Margin=Padding.Empty;page.Controls.Add(card);
        var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(14,43,14,12),BackColor=Color.Transparent};
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,36));outer.RowStyles.Add(new RowStyle(SizeType.Percent,100));outer.RowStyles.Add(new RowStyle(SizeType.Absolute,42));

        var profileRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,BackColor=Color.Transparent};profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,76));profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));profileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,180));
        profileRow.Controls.Add(new Label{Text="BUILDING",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI Semibold",8.3f),TextAlign=ContentAlignment.MiddleLeft},0,0);
        ucProfile.Dock=DockStyle.Fill;ucProfile.BackColor=Color.FromArgb(44,51,62);ucProfile.ForeColor=TextColor;ucProfile.Font=new Font("Segoe UI",8.5f);ucProfile.SelectedIndexChanged+=(_,_)=>UnitProfileChanged();profileRow.Controls.Add(ucProfile,1,0);
        ucInfo.Dock=DockStyle.Fill;ucInfo.ForeColor=Dim;ucInfo.Font=new Font("Segoe UI",7.8f);ucInfo.TextAlign=ContentAlignment.MiddleRight;ucInfo.AutoEllipsis=true;profileRow.Controls.Add(ucInfo,2,0);outer.Controls.Add(profileRow,0,0);

        var filters=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent,Padding=Padding.Empty,Margin=Padding.Empty};
        AddFilter(filters,"ALL",null,54);AddFilter(filters,"DRAGON","Dragon",70);AddFilter(filters,"SERPENT","Serpent",72);AddFilter(filters,"LOTUS","Lotus",62);AddFilter(filters,"WOLF","Wolf",58);AddFilter(filters,"HEROES","Heroes",68);outer.Controls.Add(filters,0,1);

        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));for(int r=0;r<3;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,33.333f));
        for(int i=0;i<9;i++)grid.Controls.Add(BuildUnitSlot(i),i%3,i/3);outer.Controls.Add(grid,0,2);

        var bulk=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,BackColor=Color.Transparent};bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));bulk.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));
        var allOn=FlatButton("1–9 ON",92,On);var allOff=FlatButton("ALL OFF",92,Red);allOn.Dock=allOff.Dock=DockStyle.Fill;allOn.Margin=allOff.Margin=new Padding(3);allOn.Click+=(_,_)=>SetAllUnitSlots(true);allOff.Click+=(_,_)=>SetAllUnitSlots(false);bulk.Controls.Add(allOn,0,0);bulk.Controls.Add(allOff,1,0);
        bulk.Controls.Add(new Label{Text="12 BUILDINGS · 9 OUTPUTS · FULL CATALOG",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI",7.8f),TextAlign=ContentAlignment.MiddleCenter},2,0);
        var back=FlatButton("‹  BACK TO MAIN",150,Off);back.Dock=DockStyle.Fill;back.Margin=new Padding(3);back.Click+=(_,_)=>ShowPage(false);bulk.Controls.Add(back,3,0);outer.Controls.Add(bulk,0,3);
        card.Controls.Add(outer);return page;
    }

    void AddFilter(FlowLayoutPanel host,string text,string? group,int width)
    {
        var b=FlatButton(text,width,Off);b.Height=28;b.Margin=new Padding(0,2,5,2);b.Font=new Font("Segoe UI Semibold",7.7f);b.Click+=(_,_)=>SetUnitFilter(group);host.Controls.Add(b);filterButtons[text]=b;
    }

    Control BuildUnitSlot(int i)
    {
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,BackColor=Color.FromArgb(31,37,46),Padding=new Padding(6),Margin=new Padding(3)};
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,44));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        panel.Controls.Add(new Label{Text=$"S{i+1}",Dock=DockStyle.Fill,ForeColor=TextColor,Font=new Font("Segoe UI Semibold",8.1f),TextAlign=ContentAlignment.MiddleCenter},0,0);
        var on=ucOn[i];on.Text="OFF";on.Dock=DockStyle.Fill;on.Margin=new Padding(2);on.FlatStyle=FlatStyle.Flat;on.FlatAppearance.BorderSize=1;on.FlatAppearance.BorderColor=Color.FromArgb(70,82,101);on.BackColor=Off;on.ForeColor=TextColor;on.Font=new Font("Segoe UI Semibold",7.5f);on.TabStop=false;int slot=i;on.Click+=(_,_)=>ToggleUnitSlot(slot);panel.Controls.Add(on,1,0);
        var cb=ucUnits[i];cb.Dock=DockStyle.Fill;cb.BackColor=Color.FromArgb(44,51,62);cb.ForeColor=TextColor;cb.Font=new Font("Segoe UI",7.8f);cb.IntegralHeight=false;cb.DropDownHeight=300;cb.MaxDropDownItems=16;cb.SelectedIndexChanged+=(_,_)=>UnitOutputChanged(slot);panel.Controls.Add(cb,2,0);return panel;
    }

    T? MainField<T>(string field) where T:class
    {
        try{return main.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)?.GetValue(main) as T;}catch{return null;}
    }

    bool InitUnitChangerBridge()
    {
        if(ucBridgeReady)return true;
        var profile=MainField<ComboBox>("profileSelect");var outs=MainField<ComboBox[]>("slotOut");var ons=MainField<CheckBox[]>("slotOn");
        if(profile==null||outs==null||ons==null||outs.Length<9||ons.Length<9||outs[0].Items.Count==0)return false;
        ucSyncing=true;
        ucProfile.Items.Clear();foreach(var x in profile.Items)ucProfile.Items.Add(x?.ToString()??"");
        unitEntries.Clear();
        for(int i=0;i<outs[0].Items.Count;i++)
        {
            var item=outs[0].Items[i];if(item==null)continue;var t=item.GetType();
            bool header=false;uint type=0;string name=item.ToString()??$"Unit {i}";string group="ALL";
            try{if(t.GetProperty("Header")?.GetValue(item) is bool h)header=h;}catch{}
            if(header)continue;
            try{if(t.GetProperty("Type")?.GetValue(item) is uint u)type=u;}catch{}
            try{group=t.GetProperty("Group")?.GetValue(item)?.ToString()??"ALL";}catch{}
            unitEntries.Add(new UnitEntry(i,type,name,group));
        }
        ucSyncing=false;ucBridgeReady=ucProfile.Items.Count>0&&unitEntries.Count>0;
        if(ucBridgeReady){SetUnitFilter(null);SyncUnitChanger();}
        return ucBridgeReady;
    }

    void SetUnitFilter(string? group)
    {
        unitFilter=group;
        foreach(var kv in filterButtons)kv.Value.BackColor=(group==null&&kv.Key=="ALL")||string.Equals(kv.Key,group,StringComparison.OrdinalIgnoreCase)?Color.FromArgb(38,83,70):Off;
        PopulateUnitCombos();
        OverlayHotkeyControllerV33.ReturnGameFocus();
    }

    void PopulateUnitCombos()
    {
        if(!ucBridgeReady)return;var mainOut=MainField<ComboBox[]>("slotOut");if(mainOut==null)return;
        ucSyncing=true;
        for(int i=0;i<9;i++)PopulateUnitCombo(i,mainOut[i].SelectedIndex);
        ucSyncing=false;
    }

    void PopulateUnitCombo(int slot,int mainIndex)
    {
        var cb=ucUnits[slot];var list=unitEntries.Where(e=>unitFilter==null||string.Equals(e.Group,unitFilter,StringComparison.OrdinalIgnoreCase)).ToList();
        var current=unitEntries.FirstOrDefault(e=>e.MainIndex==mainIndex);if(current!=null&&!list.Any(e=>e.MainIndex==current.MainIndex))list.Insert(0,current);
        cb.BeginUpdate();cb.Items.Clear();foreach(var e in list)cb.Items.Add(e);int ix=list.FindIndex(e=>e.MainIndex==mainIndex);cb.SelectedIndex=ix>=0?ix:(list.Count>0?0:-1);cb.EndUpdate();
    }

    void UnitProfileChanged()
    {
        if(ucSyncing)return;var profile=MainField<ComboBox>("profileSelect");if(profile==null||ucProfile.SelectedIndex<0)return;
        profile.SelectedIndex=ucProfile.SelectedIndex;SyncUnitChanger();OverlayHotkeyControllerV33.ReturnGameFocus();
    }

    void ToggleUnitSlot(int i)
    {
        var ons=MainField<CheckBox[]>("slotOn");if(ons==null||i<0||i>=ons.Length)return;ons[i].Checked=!ons[i].Checked;SyncUnitChanger();OverlayHotkeyControllerV33.ReturnGameFocus();
    }

    void SetAllUnitSlots(bool value)
    {
        var ons=MainField<CheckBox[]>("slotOn");if(ons==null)return;for(int i=0;i<Math.Min(9,ons.Length);i++)ons[i].Checked=value;SyncUnitChanger();SetStatus(value?"UNIT CHANGER: S1–S9 ON":"UNIT CHANGER: S1–S9 OFF");OverlayHotkeyControllerV33.ReturnGameFocus();
    }

    void UnitOutputChanged(int i)
    {
        if(ucSyncing||ucUnits[i].SelectedItem is not UnitEntry e)return;var outs=MainField<ComboBox[]>("slotOut");if(outs==null||i>=outs.Length)return;
        outs[i].SelectedIndex=e.MainIndex;SyncUnitChanger();OverlayHotkeyControllerV33.ReturnGameFocus();
    }

    void SyncUnitChanger()
    {
        if(!InitUnitChangerBridge())return;
        var profile=MainField<ComboBox>("profileSelect");var ons=MainField<CheckBox[]>("slotOn");var outs=MainField<ComboBox[]>("slotOut");if(profile==null||ons==null||outs==null)return;
        ucSyncing=true;
        if(ucProfile.SelectedIndex!=profile.SelectedIndex&&profile.SelectedIndex>=0)ucProfile.SelectedIndex=profile.SelectedIndex;
        for(int i=0;i<9;i++)
        {
            bool on=ons[i].Checked;ucOn[i].Text=on?"ON":"OFF";ucOn[i].BackColor=on?On:Off;
            int mainIndex=outs[i].SelectedIndex;var cur=ucUnits[i].SelectedItem as UnitEntry;if(cur==null||cur.MainIndex!=mainIndex)PopulateUnitCombo(i,mainIndex);
        }
        ucSyncing=false;
        string mode=MainField<Label>("unitMode")?.Text??"";string summary=MainField<Label>("profileSummary")?.Text??"";ucInfo.Text=string.IsNullOrWhiteSpace(mode)?summary:$"{mode} · {summary}";
    }

    Panel CardPanel(string title,Color accent)
    {
        var p=new Panel{Dock=DockStyle.Fill,BackColor=Card,Margin=new Padding(0,0,10,0)};
        var label=new Label{Text=title,Location=new Point(14,10),Size=new Size(390,24),ForeColor=accent,Font=new Font("Segoe UI Semibold",9.5f),TextAlign=ContentAlignment.MiddleLeft};p.Controls.Add(label);return p;
    }

    Button FlatButton(string text,int width,Color back)
    {
        var b=new Button{Text=text,Width=width,Height=36,FlatStyle=FlatStyle.Flat,BackColor=back,ForeColor=TextColor,Font=new Font("Segoe UI Semibold",8.4f),Cursor=Cursors.Hand,UseMnemonic=false,TabStop=false};b.FlatAppearance.BorderColor=Color.FromArgb(70,82,101);b.FlatAppearance.BorderSize=1;return b;
    }

    void UpdateDuration()=>durationValue.Text=$"{heroSeconds:0.#} SEC";
    void UpdateOffset()=>offsetValue.Text=$"+X  {copyOffset:0.0}";

    bool? MainToggleState(string field)
    {
        try
        {
            var fi=main.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);var obj=fi?.GetValue(main);if(obj==null)return null;
            var pi=obj.GetType().GetProperty("Checked",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);return pi?.PropertyType==typeof(bool)?(bool?)pi.GetValue(obj):null;
        }
        catch{return null;}
    }

    void ToggleMain(string field)
    {
        try
        {
            var fi=main.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);var obj=fi?.GetValue(main);if(obj==null){SetStatus($"{field}: control unavailable");return;}
            var pi=obj.GetType().GetProperty("Checked",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(pi==null||!pi.CanWrite||pi.PropertyType!=typeof(bool)){SetStatus($"{field}: toggle property unavailable");return;}
            bool old=(bool)(pi.GetValue(obj)??false);pi.SetValue(obj,!old);SetStatus($"{field}: {(!old?"ON":"OFF")}");
        }
        catch(Exception ex){SetStatus("Toggle error: "+ex.Message);}
        SyncNow();OverlayHotkeyControllerV33.ReturnGameFocus();
    }

    void Run(Func<string> action)
    {
        try{SetStatus(action());}catch(Exception ex){SetStatus("Action error: "+ex.Message);}finally{OverlayHotkeyControllerV33.ReturnGameFocus();}
    }

    void SetStatus(string text){status.Text=text;}

    public void SyncNow()
    {
        foreach(var kv in quick)
        {
            bool? on=MainToggleState(kv.Key);var b=kv.Value.Button;b.BackColor=on==true?On:Off;b.Text=kv.Value.Label+(on==true?"  ON":on==false?"  OFF":"  —");
        }
        bool? all=MainToggleState("killAll");deathPolicy.BackColor=all==true?Red:Color.FromArgb(37,77,65);deathPolicy.Text=all==true?"DEATH TARGET  //  ALL UNITS":"DEATH TARGET  //  ENEMY ONLY";
        SyncUnitChanger();
        if(string.IsNullOrWhiteSpace(status.Text))status.Text="LIVE · Alt+W ready · V30 gameplay core locked · overlay opacity 91%";
    }
}
