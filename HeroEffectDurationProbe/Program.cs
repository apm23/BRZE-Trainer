using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectDurationProbe;

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
    readonly Label game = new();
    readonly Label phase = new();
    readonly TextBox output = new();
    readonly System.Windows.Forms.Timer watchTimer = new(){Interval=500};
    bool watching;

    public MainForm()
    {
        Text="BRZE Hero Effect Duration Probe — READ ONLY";
        ClientSize=new Size(1180,720);
        MinimumSize=new Size(980,620);
        StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(10,14,22);
        ForeColor=Color.WhiteSmoke;
        Font=new Font("Segoe UI",10f);

        var title=new Label{Text="HERO EFFECT DURATION PROBE — READ ONLY",Font=new Font("Segoe UI Semibold",19f),AutoSize=true,Location=new Point(24,18)};
        var sub=new Label{Text="No WriteProcessMemory • no hooks • no native ability calls • observes one selected Unit* only",AutoSize=true,ForeColor=Color.FromArgb(125,205,170),Location=new Point(27,57)};
        Controls.Add(title);Controls.Add(sub);

        Button B(string text,int x,int w)
        {
            var b=new Button{Text=text,Location=new Point(x,92),Size=new Size(w,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,45,63),ForeColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(80,105,135);Controls.Add(b);return b;
        }

        var baseline=B("1  CAPTURE BASELINE",24,205);
        var effect=B("2  CAPTURE EFFECT DIFF",241,230);
        var watch=B("3  START WATCH",483,185);
        var reset=B("RESET",680,120);
        var copy=B("COPY REPORT",812,160);

        baseline.Click+=(_,_)=>{watching=false;watchTimer.Stop();phase.Text=ProbeCore.CaptureBaseline();RefreshSnapshot();};
        effect.Click+=(_,_)=>{watching=false;watchTimer.Stop();phase.Text=ProbeCore.CaptureEffectDiff();RefreshSnapshot();};
        watch.Click+=(_,_)=>
        {
            watching=!watching;
            watch.Text=watching?"STOP WATCH":"3  START WATCH";
            if(watching)watchTimer.Start();else watchTimer.Stop();
            RefreshSnapshot();
        };
        reset.Click+=(_,_)=>{watching=false;watchTimer.Stop();watch.Text="3  START WATCH";phase.Text=ProbeCore.Reset();RefreshSnapshot();};
        copy.Click+=(_,_)=>{try{Clipboard.SetText(output.Text);}catch{}};

        game.AutoSize=false;game.Location=new Point(24,155);game.Size=new Size(1128,27);game.ForeColor=Color.FromArgb(100,225,180);
        phase.AutoSize=false;phase.Location=new Point(24,185);phase.Size=new Size(1128,50);phase.ForeColor=Color.FromArgb(235,185,95);
        Controls.Add(game);Controls.Add(phase);

        var instructions=new Label
        {
            Text="TEST FLOW: fresh/reloaded game → select exactly ONE clean normal unit → CAPTURE BASELINE → use proven V2 to apply Grayback OR Issyl ONCE → return here → CAPTURE EFFECT DIFF → START WATCH. Do not use V3.",
            AutoSize=false,Location=new Point(24,237),Size=new Size(1128,48),ForeColor=Color.FromArgb(180,195,218)
        };
        Controls.Add(instructions);

        output.Multiline=true;output.ReadOnly=true;output.ScrollBars=ScrollBars.Both;output.WordWrap=false;
        output.Font=new Font(FontFamily.GenericMonospace,9.2f);output.Location=new Point(24,292);output.Size=new Size(1128,398);
        output.BackColor=Color.FromArgb(6,9,14);output.ForeColor=Color.Gainsboro;Controls.Add(output);

        watchTimer.Tick+=(_,_)=>RefreshSnapshot();
        FormClosed+=(_,_)=>ProbeCore.Shutdown();
        RefreshSnapshot();
    }

    void RefreshSnapshot()
    {
        var s=ProbeCore.Sample(100);
        game.Text="GAME: "+s.Game;
        phase.Text=s.Phase;
        output.Lines=s.Lines.ToArray();
    }
}
