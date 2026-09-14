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
/// V32 overlay-only layer. Gameplay remains V30.
/// Uses BOTH a real HWND-bound RegisterHotKey sink and an edge-triggered
/// GetAsyncKeyState fallback, so Alt+W works while BRZE owns foreground focus.
/// The overlay is a separate no-activate control deck; the normal trainer form
/// is never converted into an overlay and its layout/state remains intact.
/// </summary>
internal static class OverlayHotkeyControllerV32
{
    const uint MOD_ALT=0x0001,MOD_NOREPEAT=0x4000;
    const uint VK_W=0x57,VK_MENU=0x12;
    const int HOTKEY_ID=0x3277;
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
            var cp=new CreateParams{Caption="BRZE.V32.AltW.HotkeySink",Parent=new IntPtr(-3)}; // HWND_MESSAGE
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
    static PremiumOverlayForm? overlay;
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
        if(overlay==null||overlay.IsDisposed)overlay=new PremiumOverlayForm(main,()=>{Hide();});
        if(overlay.Visible)Hide();else Show();
    }

    static void Show()
    {
        if(main==null)return;
        if(overlay==null||overlay.IsDisposed)overlay=new PremiumOverlayForm(main,()=>{Hide();});
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

internal sealed class PremiumOverlayForm : Form
{
    const int WS_EX_TOOLWINDOW=0x00000080,WS_EX_NOACTIVATE=0x08000000;
    const int WM_MOUSEACTIVATE=0x0021,MA_NOACTIVATE=3;

    readonly Form main;
    readonly Action hideAction;
    readonly Dictionary<string,(string Label,Button Button)> quick=new();
    readonly Label status=new();
    readonly Label durationLabel=new();
    readonly System.Windows.Forms.Timer refresh=new(){Interval=180};
    decimal heroSeconds=30m;

    static readonly Color Bg=Color.FromArgb(13,17,24);
    static readonly Color Card=Color.FromArgb(23,29,39);
    static readonly Color Card2=Color.FromArgb(29,36,48);
    static readonly Color Accent=Color.FromArgb(94,224,177);
    static readonly Color Accent2=Color.FromArgb(110,184,255);
    static readonly Color Text=Color.FromArgb(239,244,249);
    static readonly Color Dim=Color.FromArgb(153,166,183);
    static readonly Color Off=Color.FromArgb(42,50,64);
    static readonly Color On=Color.FromArgb(29,115,88);

    public PremiumOverlayForm(Form main,Action hideAction)
    {
        this.main=main;this.hideAction=hideAction;
        Text="BRZE Live Control Deck";
        ClientSize=new Size(820,510);
        FormBorderStyle=FormBorderStyle.None;
        StartPosition=FormStartPosition.Manual;
        ShowInTaskbar=false;TopMost=true;Opacity=0.87;BackColor=Bg;
        MaximizeBox=false;MinimizeBox=false;DoubleBuffered=true;
        Font=new Font("Segoe UI",9f);
        BuildUi();
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
        using var p=new Pen(Color.FromArgb(80,Accent),1.4f);e.Graphics.DrawRectangle(p,1,1,Width-3,Height-3);
        using var top=new SolidBrush(Accent);e.Graphics.FillRectangle(top,22,0,150,3);
    }

    void BuildUi()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(20,16,20,16),BackColor=Color.Transparent};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,64));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
        Controls.Add(root);

        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        head.Controls.Add(new Label{Text="BATTLE REALMS  //  LIVE CONTROL DECK",Location=new Point(0,2),Size=new Size(530,29),ForeColor=Text,Font=new Font("Segoe UI Semibold",15.2f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft});
#if RAW_STATUS
        string mode="DIAGNOSTICS";
#else
        string mode="CLEAN";
#endif
        head.Controls.Add(new Label{Text=$"V32  ·  {mode}  ·  87% GLASS  ·  GAME FOCUS LOCKED",Location=new Point(2,34),Size=new Size(560,20),ForeColor=Dim,Font=new Font("Segoe UI",8.4f),TextAlign=ContentAlignment.MiddleLeft});
        var hide=FlatButton("ALT+W  HIDE",132,Accent2);hide.Location=new Point(640,8);hide.Height=38;hide.Click+=(_,_)=>hideAction();head.Controls.Add(hide);
        root.Controls.Add(head,0,0);

        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Color.Transparent,Margin=Padding.Empty};
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,53));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,47));
        body.Controls.Add(BuildQuickCard(),0,0);body.Controls.Add(BuildActionCard(),1,0);root.Controls.Add(body,0,1);

        var foot=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(18,23,32),Margin=new Padding(0,10,0,0)};
        status.Dock=DockStyle.Fill;status.Padding=new Padding(14,0,12,0);status.ForeColor=Color.FromArgb(196,207,220);status.Font=new Font("Segoe UI",8.4f);status.TextAlign=ContentAlignment.MiddleLeft;status.AutoEllipsis=true;foot.Controls.Add(status);root.Controls.Add(foot,0,2);
    }

    Control BuildQuickCard()
    {
        var card=CardPanel("QUICK CHEATS",Accent);
        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=5,Padding=new Padding(12,42,12,12),BackColor=Color.Transparent};
        for(int c=0;c<3;c++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int r=0;r<5;r++)grid.RowStyles.Add(new RowStyle(SizeType.Percent,20));
        var defs=new[]{
            ("rice","RICE"),("water","WATER"),("yinYang","YIN / YANG"),
            ("population","POPULATION"),("training","TRAINING"),("hp","HEALTH"),
            ("stamina","STAMINA"),("horses","HORSES"),("wolves","WOLVES"),
            ("reveal","REVEAL MAP"),("pausePeasant","PAUSE PEASANT"),("peasant","PEASANT 3S"),
            ("burst","DEATH BURST")};
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
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Padding=new Padding(14,43,14,12),BackColor=Color.Transparent};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,42));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,18));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        var dur=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,BackColor=Color.Transparent};
        dur.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,56));dur.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));dur.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,56));dur.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,58));
        var minus=FlatButton("−5",48,Off),plus=FlatButton("+5",48,Off),reset=FlatButton("30",50,Off);
        durationLabel.Dock=DockStyle.Fill;durationLabel.ForeColor=Text;durationLabel.Font=new Font("Segoe UI Semibold",12f);durationLabel.TextAlign=ContentAlignment.MiddleCenter;
        minus.Click+=(_,_)=>{heroSeconds=Math.Max(1m,heroSeconds-5m);UpdateDuration();};plus.Click+=(_,_)=>{heroSeconds=Math.Min(420m,heroSeconds+5m);UpdateDuration();};reset.Click+=(_,_)=>{heroSeconds=30m;UpdateDuration();};
        dur.Controls.Add(minus,0,0);dur.Controls.Add(durationLabel,1,0);dur.Controls.Add(plus,2,0);dur.Controls.Add(reset,3,0);root.Controls.Add(dur,0,0);UpdateDuration();

        var hero=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,BackColor=Color.Transparent};for(int c=0;c<3;c++)hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        var issyl=FlatButton("ISSYL",100,On),gray=FlatButton("GRAYBACK",100,On),both=FlatButton("BOTH",100,Color.FromArgb(45,92,128));
        issyl.Dock=gray.Dock=both.Dock=DockStyle.Fill;issyl.Margin=gray.Margin=both.Margin=new Padding(3);
        issyl.Click+=(_,_)=>Run(()=>HeroEffectDirectCoreV30.Start(DirectHeroMode.Issyl,heroSeconds));
        gray.Click+=(_,_)=>Run(()=>HeroEffectDirectCoreV30.Start(DirectHeroMode.Grayback,heroSeconds));
        both.Click+=(_,_)=>Run(()=>HeroEffectDirectCoreV30.Start(DirectHeroMode.Both,heroSeconds));
        hero.Controls.Add(issyl,0,0);hero.Controls.Add(gray,1,0);hero.Controls.Add(both,2,0);root.Controls.Add(hero,0,1);
        root.Controls.Add(new Label{Text="COPY UNIT",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI Semibold",8.1f),TextAlign=ContentAlignment.BottomLeft},0,2);

        var copy=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,BackColor=Color.Transparent};copy.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));copy.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        var cp=FlatButton("COPY SELECTED",120,Off),paste=FlatButton("PASTE BESIDE",120,Off);cp.Dock=paste.Dock=DockStyle.Fill;cp.Margin=paste.Margin=new Padding(3);
        cp.Click+=(_,_)=>Run(()=>IntegratedFrameDispatcherCore.CopySelection().Message);paste.Click+=(_,_)=>Run(()=>IntegratedFrameDispatcherCore.PasteCopied(8f));copy.Controls.Add(cp,0,0);copy.Controls.Add(paste,1,0);root.Controls.Add(copy,0,3);

        var kill=FlatButton("SINGLE KILL  //  ENEMY",250,Color.FromArgb(105,48,52));kill.Dock=DockStyle.Fill;kill.Margin=new Padding(3);kill.Click+=(_,_)=>Run(()=>IntegratedFrameDispatcherCore.DeathTriggerSingle(false)?"Single-kill queued on hovered enemy.":"Single-kill could not be queued.");root.Controls.Add(kill,0,4);

        var hint=new Label{Text="Alt+W toggles this deck globally. Mouse actions do not activate the overlay, so BRZE keeps foreground focus.",Dock=DockStyle.Fill,ForeColor=Dim,Font=new Font("Segoe UI",8.1f),TextAlign=ContentAlignment.MiddleLeft};root.Controls.Add(hint,0,5);
        card.Controls.Add(root);return card;
    }

    Panel CardPanel(string title,Color accent)
    {
        var p=new Panel{Dock=DockStyle.Fill,BackColor=Card,Margin=new Padding(0,0,10,0)};
        var label=new Label{Text=title,Location=new Point(14,10),Size=new Size(300,24),ForeColor=accent,Font=new Font("Segoe UI Semibold",9.5f),TextAlign=ContentAlignment.MiddleLeft};p.Controls.Add(label);return p;
    }

    Button FlatButton(string text,int width,Color back)
    {
        var b=new Button{Text=text,Width=width,Height=36,FlatStyle=FlatStyle.Flat,BackColor=back,ForeColor=Text,Font=new Font("Segoe UI Semibold",8.4f),Cursor=Cursors.Hand,UseMnemonic=false,TabStop=false};b.FlatAppearance.BorderColor=Color.FromArgb(70,82,101);b.FlatAppearance.BorderSize=1;return b;
    }

    void UpdateDuration()=>durationLabel.Text=$"DURATION  {heroSeconds:0.#}s";

    bool? MainToggleState(string field)
    {
        try
        {
            var fi=main.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);
            var obj=fi?.GetValue(main);if(obj==null)return null;
            var pi=obj.GetType().GetProperty("Checked",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            return pi?.PropertyType==typeof(bool)?(bool?)pi.GetValue(obj):null;
        }
        catch{return null;}
    }

    void ToggleMain(string field)
    {
        try
        {
            var fi=main.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);
            var obj=fi?.GetValue(main);if(obj==null){SetStatus($"{field}: control unavailable");return;}
            var pi=obj.GetType().GetProperty("Checked",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if(pi==null||!pi.CanWrite||pi.PropertyType!=typeof(bool)){SetStatus($"{field}: toggle property unavailable");return;}
            bool old=(bool)(pi.GetValue(obj)??false);pi.SetValue(obj,!old);SetStatus($"{field}: {(!old?"ON":"OFF")}");
        }
        catch(Exception ex){SetStatus("Toggle error: "+ex.Message);}
        SyncNow();OverlayHotkeyControllerV32.ReturnGameFocus();
    }

    void Run(Func<string> action)
    {
        try{SetStatus(action());}catch(Exception ex){SetStatus("Action error: "+ex.Message);}finally{OverlayHotkeyControllerV32.ReturnGameFocus();}
    }

    void SetStatus(string text){status.Text=text;}

    public void SyncNow()
    {
        foreach(var kv in quick)
        {
            bool? on=MainToggleState(kv.Key);var b=kv.Value.Button;
            b.BackColor=on==true?On:Off;b.Text=kv.Value.Label+(on==true?"  ON":on==false?"  OFF":"  —");
        }
        if(string.IsNullOrWhiteSpace(status.Text))status.Text="LIVE · Alt+W ready · V30 gameplay core locked · overlay opacity 87%";
    }
}
