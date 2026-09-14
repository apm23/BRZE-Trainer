using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectChildProbeV7;

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
    readonly System.Windows.Forms.Timer timer=new(){Interval=20};

    public MainForm()
    {
        Text="BRZE Hero Effect Child Probe V7 — AUTO / READ ONLY";
        ClientSize=new Size(1440,820);
        MinimumSize=new Size(1180,700);
        StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(10,14,22);
        ForeColor=Color.WhiteSmoke;
        Font=new Font("Segoe UI",10f);

        Controls.Add(new Label{Text="HERO EFFECT CHILD PROBE V7 — A5+TARGET CHILD TIMER HUNT",Font=new Font("Segoe UI Semibold",18f),AutoSize=true,Location=new Point(24,18)});
        Controls.Add(new Label{Text="READ ONLY • signature-lock parent • follow/deduplicate child pointers • 20 ms sampling • automatic expiry",AutoSize=true,ForeColor=Color.FromArgb(125,205,170),Location=new Point(27,57)});

        Button B(string text,int x,int w)
        {
            var b=new Button{Text=text,Location=new Point(x,92),Size=new Size(w,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,45,63),ForeColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(80,105,135);Controls.Add(b);return b;
        }
        var arm=B("ARM TARGET + AUTO WATCH",24,250);
        var reset=B("RESET",286,120);
        var copy=B("COPY REPORT",418,160);

        arm.Click+=(_,_)=>{phase.Text=ProbeCore.Arm();timer.Start();RefreshSnapshot();};
        reset.Click+=(_,_)=>{timer.Stop();phase.Text=ProbeCore.Reset();RefreshSnapshot();};
        copy.Click+=(_,_)=>{try{Clipboard.SetText(output.Text);}catch{}};

        game.AutoSize=false;game.Location=new Point(24,154);game.Size=new Size(1388,27);game.ForeColor=Color.FromArgb(100,225,180);
        phase.AutoSize=false;phase.Location=new Point(24,184);phase.Size=new Size(1388,50);phase.ForeColor=Color.FromArgb(235,185,95);
        Controls.Add(game);Controls.Add(phase);
        Controls.Add(new Label{Text="FLOW: select ONE clean target -> ARM TARGET + AUTO WATCH -> select Issyl -> cast ORIGINAL Haste ONCE on the armed target -> do nothing until COMPLETE -> COPY REPORT. Human click speed does not matter. V7 identifies the A5+target parent by content, then follows its readable child pointers automatically.",AutoSize=false,Location=new Point(24,238),Size=new Size(1388,72),ForeColor=Color.FromArgb(180,195,218)});

        output.Multiline=true;output.ReadOnly=true;output.ScrollBars=ScrollBars.Both;output.WordWrap=false;output.Font=new Font(FontFamily.GenericMonospace,8.3f);
        output.Location=new Point(24,318);output.Size=new Size(1388,474);output.BackColor=Color.FromArgb(6,9,14);output.ForeColor=Color.Gainsboro;Controls.Add(output);

        timer.Tick+=(_,_)=>{ProbeCore.Tick();var s=ProbeCore.Snapshot();Apply(s);if(s.Done)timer.Stop();};
        FormClosed+=(_,_)=>ProbeCore.Shutdown();
        RefreshSnapshot();
    }

    void Apply(ProbeSnapshot s){game.Text="GAME: "+s.Game;phase.Text=s.Phase;output.Lines=s.Lines.ToArray();}
    void RefreshSnapshot()=>Apply(ProbeCore.Snapshot());
}
