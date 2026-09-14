using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectDurationWriteProbeV9;

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
        Text="BRZE Hero Effect Duration Write Probe V9 — GUARDED 2X TEST";
        ClientSize=new Size(1460,850);
        MinimumSize=new Size(1200,720);
        StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(10,14,22);
        ForeColor=Color.WhiteSmoke;
        Font=new Font("Segoe UI",10f);

        Controls.Add(new Label{Text="HERO EFFECT DURATION WRITE PROBE V9 — ISSYL 15000 → 30000 → AUTO RESTORE",Font=new Font("Segoe UI Semibold",18f),AutoSize=true,Location=new Point(24,18)});
        Controls.Add(new Label{Text="GUARDED WRITE • only A5 config+0x0E0 • baseline first • 2X test • automatic restore • no replay stacking",AutoSize=true,ForeColor=Color.FromArgb(125,205,170),Location=new Point(27,57)});

        Button B(string text,int x,int w)
        {
            var b=new Button{Text=text,Location=new Point(x,92),Size=new Size(w,44),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,45,63),ForeColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(80,105,135);Controls.Add(b);return b;
        }

        var arm=B("1) ARM BASELINE",24,180);
        var patch=B("2) PATCH 30000 + ARM TEST",216,270);
        var restore=B("RESTORE NOW",498,160);
        var reset=B("RESET",670,110);
        var copy=B("COPY REPORT",792,150);

        arm.Click+=(_,_)=>{phase.Text=ProbeCore.ArmBaseline();timer.Start();RefreshSnapshot();};
        patch.Click+=(_,_)=>{phase.Text=ProbeCore.PatchAndArmTest();timer.Start();RefreshSnapshot();};
        restore.Click+=(_,_)=>{phase.Text=ProbeCore.RestoreNow();RefreshSnapshot();};
        reset.Click+=(_,_)=>{timer.Stop();phase.Text=ProbeCore.Reset();RefreshSnapshot();};
        copy.Click+=(_,_)=>{try{Clipboard.SetText(output.Text);}catch{}};

        game.AutoSize=false;game.Location=new Point(24,154);game.Size=new Size(1410,27);game.ForeColor=Color.FromArgb(100,225,180);
        phase.AutoSize=false;phase.Location=new Point(24,184);phase.Size=new Size(1410,58);phase.ForeColor=Color.FromArgb(235,185,95);
        Controls.Add(game);Controls.Add(phase);

        Controls.Add(new Label{
            Text="FLOW: (A) select ONE clean target -> ARM BASELINE -> select Issyl -> cast ORIGINAL Haste once -> wait until baseline COMPLETE/ready. (B) select ONE clean target -> click PATCH 30000 + ARM TEST -> select Issyl -> cast ORIGINAL Haste once -> wait natural expiry. V9 restores 15000 automatically after the test. If anything looks wrong, click RESTORE NOW.",
            AutoSize=false,Location=new Point(24,246),Size=new Size(1410,82),ForeColor=Color.FromArgb(180,195,218)});

        output.Multiline=true;output.ReadOnly=true;output.ScrollBars=ScrollBars.Both;output.WordWrap=false;output.Font=new Font(FontFamily.GenericMonospace,8.8f);
        output.Location=new Point(24,336);output.Size=new Size(1410,486);output.BackColor=Color.FromArgb(6,9,14);output.ForeColor=Color.Gainsboro;Controls.Add(output);

        timer.Tick+=(_,_)=>{ProbeCore.Tick();var s=ProbeCore.Snapshot();Apply(s);if(s.Done)timer.Stop();};
        FormClosed+=(_,_)=>ProbeCore.Shutdown();
        RefreshSnapshot();
    }

    void Apply(ProbeSnapshot s){game.Text="GAME: "+s.Game;phase.Text=s.Phase;output.Lines=s.Lines.ToArray();}
    void RefreshSnapshot()=>Apply(ProbeCore.Snapshot());
}
