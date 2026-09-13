using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectDurationProbeV2;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    readonly Label game=new();
    readonly Label phase=new();
    readonly TextBox output=new();
    readonly System.Windows.Forms.Timer watchTimer=new(){Interval=500};
    bool watching;

    public MainForm()
    {
        Text="BRZE Hero Effect Duration Probe V2 — READ ONLY";
        ClientSize=new Size(1280,760);
        MinimumSize=new Size(1080,650);
        StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(10,14,22);
        ForeColor=Color.WhiteSmoke;
        Font=new Font("Segoe UI",10f);

        Controls.Add(new Label{Text="HERO EFFECT DURATION PROBE V2 — RECURSIVE / READ ONLY",Font=new Font("Segoe UI Semibold",18f),AutoSize=true,Location=new Point(24,18)});
        Controls.Add(new Label{Text="Recursive pointer graph depth 3 • no game writes • no hooks • no ability calls",AutoSize=true,ForeColor=Color.FromArgb(125,205,170),Location=new Point(27,57)});

        Button B(string text,int x,int w)
        {
            var b=new Button{Text=text,Location=new Point(x,92),Size=new Size(w,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,45,63),ForeColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(80,105,135);Controls.Add(b);return b;
        }

        var baseline=B("1  CAPTURE BASELINE",24,200);
        var effect=B("2  CAPTURE EFFECT DIFF",236,220);
        var watch=B("3  START WATCH",468,170);
        var expired=B("4  MARK EFFECT EXPIRED",650,220);
        var reset=B("RESET",882,105);
        var copy=B("COPY REPORT",999,150);

        baseline.Click+=(_,_)=>{StopWatch();phase.Text=ProbeCore.CaptureBaseline();RefreshSnapshot();};
        effect.Click+=(_,_)=>{StopWatch();phase.Text=ProbeCore.CaptureEffectDiff();RefreshSnapshot();};
        watch.Click+=(_,_)=>
        {
            watching=!watching;
            watch.Text=watching?"STOP WATCH":"3  START WATCH";
            if(watching)watchTimer.Start();else watchTimer.Stop();
            RefreshSnapshot();
        };
        expired.Click+=(_,_)=>
        {
            StopWatch();watch.Text="3  START WATCH";
            var s=ProbeCore.MarkExpired();
            ApplySnapshot(s);
        };
        reset.Click+=(_,_)=>{StopWatch();watch.Text="3  START WATCH";phase.Text=ProbeCore.Reset();RefreshSnapshot();};
        copy.Click+=(_,_)=>{try{Clipboard.SetText(output.Text);}catch{}};

        game.AutoSize=false;game.Location=new Point(24,154);game.Size=new Size(1228,27);game.ForeColor=Color.FromArgb(100,225,180);
        phase.AutoSize=false;phase.Location=new Point(24,184);phase.Size=new Size(1228,48);phase.ForeColor=Color.FromArgb(235,185,95);
        Controls.Add(game);Controls.Add(phase);

        Controls.Add(new Label
        {
            Text="FLOW: fresh/reloaded game -> select exactly ONE clean normal unit -> BASELINE -> cast ONE ORIGINAL Grayback buff ONCE -> EFFECT DIFF -> START WATCH -> when the visible buff disappears, immediately MARK EFFECT EXPIRED -> COPY REPORT. Do not recast and do not use V3.",
            AutoSize=false,Location=new Point(24,237),Size=new Size(1228,50),ForeColor=Color.FromArgb(180,195,218)
        });

        output.Multiline=true;output.ReadOnly=true;output.ScrollBars=ScrollBars.Both;output.WordWrap=false;
        output.Font=new Font(FontFamily.GenericMonospace,8.8f);output.Location=new Point(24,292);output.Size=new Size(1228,438);
        output.BackColor=Color.FromArgb(6,9,14);output.ForeColor=Color.Gainsboro;Controls.Add(output);

        watchTimer.Tick+=(_,_)=>RefreshSnapshot();
        FormClosed+=(_,_)=>ProbeCore.Shutdown();
        RefreshSnapshot();
    }

    void StopWatch(){watching=false;watchTimer.Stop();}
    void ApplySnapshot(ProbeSnapshot s){game.Text="GAME: "+s.Game;phase.Text=s.Phase;output.Lines=s.Lines.ToArray();}
    void RefreshSnapshot()=>ApplySnapshot(ProbeCore.Sample());
}
