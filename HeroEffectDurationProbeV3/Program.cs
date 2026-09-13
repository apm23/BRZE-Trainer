using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectDurationProbeV3;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm:Form
{
    readonly Label game=new();
    readonly Label phase=new();
    readonly TextBox output=new();
    readonly System.Windows.Forms.Timer timer=new(){Interval=250};
    enum Mode{None,Control,Effect}
    Mode mode;
    int controlTicks;

    public MainForm()
    {
        Text="BRZE Hero Effect Duration Probe V3 — CONTROL DIFF / READ ONLY";
        ClientSize=new Size(1380,790);
        MinimumSize=new Size(1180,680);
        StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(10,14,22);
        ForeColor=Color.WhiteSmoke;
        Font=new Font("Segoe UI",10f);

        Controls.Add(new Label{Text="HERO EFFECT DURATION PROBE V3 — CONTROL DIFFERENTIAL",Font=new Font("Segoe UI Semibold",18f),AutoSize=true,Location=new Point(24,18)});
        Controls.Add(new Label{Text="READ ONLY • direct Unit* fields • compares 15s no-buff control against Grayback-active behavior",AutoSize=true,ForeColor=Color.FromArgb(125,205,170),Location=new Point(27,57)});

        Button B(string text,int x,int w)
        {
            var b=new Button{Text=text,Location=new Point(x,92),Size=new Size(w,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,45,63),ForeColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(80,105,135);Controls.Add(b);return b;
        }

        var control=B("1  START 15s CONTROL",24,205);
        var pre=B("2  CAPTURE PRE-EFFECT",241,215);
        var effect=B("3  START EFFECT WATCH",468,220);
        var expired=B("4  MARK EXPIRED",700,180);
        var reset=B("RESET",892,105);
        var copy=B("COPY REPORT",1009,150);

        control.Click+=(_,_)=>
        {
            StopTimer();
            phase.Text=ProbeCore.StartControl();
            controlTicks=0;mode=Mode.Control;timer.Start();
            RefreshSnapshot();
        };
        pre.Click+=(_,_)=>{StopTimer();phase.Text=ProbeCore.CapturePreEffect();RefreshSnapshot();};
        effect.Click+=(_,_)=>
        {
            StopTimer();
            phase.Text=ProbeCore.StartEffect();
            mode=Mode.Effect;timer.Start();
            RefreshSnapshot();
        };
        expired.Click+=(_,_)=>
        {
            StopTimer();
            ApplySnapshot(ProbeCore.MarkExpired());
        };
        reset.Click+=(_,_)=>{StopTimer();phase.Text=ProbeCore.Reset();RefreshSnapshot();};
        copy.Click+=(_,_)=>{try{Clipboard.SetText(output.Text);}catch{}};

        game.AutoSize=false;game.Location=new Point(24,154);game.Size=new Size(1328,27);game.ForeColor=Color.FromArgb(100,225,180);
        phase.AutoSize=false;phase.Location=new Point(24,184);phase.Size=new Size(1328,50);phase.ForeColor=Color.FromArgb(235,185,95);
        Controls.Add(game);Controls.Add(phase);

        Controls.Add(new Label
        {
            Text="FLOW: select ONE clean normal unit and leave it totally idle -> START 15s CONTROL (auto-stops) -> CAPTURE PRE-EFFECT -> cast ORIGINAL Grayback exactly ONCE -> immediately START EFFECT WATCH -> do not move/attack/recast -> when visible Grayback ends, MARK EXPIRED -> COPY REPORT. Unit+0x460 is always pinned at the top.",
            AutoSize=false,Location=new Point(24,238),Size=new Size(1328,52),ForeColor=Color.FromArgb(180,195,218)
        });

        output.Multiline=true;output.ReadOnly=true;output.ScrollBars=ScrollBars.Both;output.WordWrap=false;
        output.Font=new Font(FontFamily.GenericMonospace,8.4f);output.Location=new Point(24,298);output.Size=new Size(1328,462);
        output.BackColor=Color.FromArgb(6,9,14);output.ForeColor=Color.Gainsboro;Controls.Add(output);

        timer.Tick+=(_,_)=>
        {
            if(mode==Mode.Control)
            {
                ProbeCore.SampleControl();
                controlTicks++;
                if(controlTicks>=60)
                {
                    timer.Stop();mode=Mode.None;
                    phase.Text=ProbeCore.FinishControl();
                }
            }
            else if(mode==Mode.Effect)ProbeCore.SampleEffect();
            RefreshSnapshot();
        };
        FormClosed+=(_,_)=>ProbeCore.Shutdown();
        RefreshSnapshot();
    }

    void StopTimer(){timer.Stop();mode=Mode.None;}
    void ApplySnapshot(ProbeSnapshot s){game.Text="GAME: "+s.Game;phase.Text=s.Phase;output.Lines=s.Lines.ToArray();}
    void RefreshSnapshot()=>ApplySnapshot(ProbeCore.Snapshot());
}
