using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectReplayDurationV10;

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
        Text="BRZE Hero Effect Replay Duration V10";
        ClientSize=new Size(1320,780);
        StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(10,14,22);
        ForeColor=Color.WhiteSmoke;
        Font=new Font("Segoe UI",10f);

        Controls.Add(new Label{Text="HERO EFFECT REPLAY DURATION V10",Font=new Font("Segoe UI Semibold",18f),AutoSize=true,Location=new Point(24,18)});
        Controls.Add(new Label{Text="Duration-field integration proof companion",AutoSize=true,ForeColor=Color.FromArgb(125,205,170),Location=new Point(27,57)});

        Button MakeButton(string text,int x,int w)
        {
            var b=new Button{Text=text,Location=new Point(x,92),Size=new Size(w,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,45,63),ForeColor=Color.White};
            Controls.Add(b);return b;
        }

        var baseline=MakeButton("1) ARM BASELINE / CAPTURE",24,250);
        var replay=MakeButton("2) ARM REPLAY 2X",286,220);
        var restore=MakeButton("RESTORE NOW",518,150);
        var reset=MakeButton("RESET",680,110);
        var copy=MakeButton("COPY REPORT",802,150);

        baseline.Click+=(_,_)=>{phase.Text=ReplayDurationCore.ArmBaseline();timer.Start();RefreshSnapshot();};
        replay.Click+=(_,_)=>{phase.Text=ReplayDurationCore.ArmReplay2X();timer.Start();RefreshSnapshot();};
        restore.Click+=(_,_)=>{phase.Text=ReplayDurationCore.RestoreNow();RefreshSnapshot();};
        reset.Click+=(_,_)=>{phase.Text=ReplayDurationCore.Reset();timer.Start();RefreshSnapshot();};
        copy.Click+=(_,_)=>{try{Clipboard.SetText(output.Text);}catch{}};

        game.AutoSize=false;game.Location=new Point(24,154);game.Size=new Size(1260,27);game.ForeColor=Color.FromArgb(100,225,180);
        phase.AutoSize=false;phase.Location=new Point(24,184);phase.Size=new Size(1260,62);phase.ForeColor=Color.FromArgb(235,185,95);
        Controls.Add(game);Controls.Add(phase);

        output.Multiline=true;output.ReadOnly=true;output.ScrollBars=ScrollBars.Both;output.WordWrap=false;output.Font=new Font(FontFamily.GenericMonospace,8.5f);
        output.Location=new Point(24,258);output.Size=new Size(1260,490);output.BackColor=Color.FromArgb(6,9,14);output.ForeColor=Color.Gainsboro;Controls.Add(output);

        timer.Tick+=(_,_)=>{ReplayDurationCore.Tick();var s=ReplayDurationCore.Snapshot();Apply(s);if(s.Done)timer.Stop();};
        FormClosed+=(_,_)=>ReplayDurationCore.Shutdown();
        timer.Start();RefreshSnapshot();
    }

    void Apply(LabSnapshot s){game.Text=s.Game;phase.Text=s.Phase;output.Lines=s.Lines.ToArray();}
    void RefreshSnapshot()=>Apply(ReplayDurationCore.Snapshot());
}
