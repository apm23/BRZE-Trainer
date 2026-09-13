using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectReplayV3;

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
    readonly Label queue = new();
    readonly Label hold = new();
    readonly ComboBox duration = new();
    readonly ComboBox refresh = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };

    bool holdActive;
    bool holdInfinite;
    DateTime holdUntil;
    DateTime nextRefresh;
    TimeSpan refreshEvery=TimeSpan.FromSeconds(5);
    uint holdA1,holdA2;
    string holdLabel="";

    public MainForm()
    {
        Text = "BRZE Hero Effect Replay V3 — Duration Hold";
        ClientSize = new Size(920, 500);
        MinimumSize = new Size(920, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10f);

        var title = new Label { Text = "HERO EFFECT REPLAY V3 — DURATION", Font = new Font("Segoe UI Semibold", 20f), AutoSize = true, Location = new Point(26, 20) };
        var sub = new Label { Text = "Locked native IDs: Grayback 0xC0 • Issyl 0xA5 • V2 replay path preserved", ForeColor = Color.FromArgb(160,185,215), AutoSize = true, Location = new Point(29,64) };
        Controls.Add(title); Controls.Add(sub);

        Controls.Add(new Label { Text="Hold duration", AutoSize=true, Location=new Point(29,104), ForeColor=Color.FromArgb(190,205,225) });
        duration.DropDownStyle=ComboBoxStyle.DropDownList; duration.Location=new Point(29,128); duration.Size=new Size(180,30);
        duration.Items.AddRange(new object[]{"30 seconds","60 seconds","5 minutes","INFINITE"}); duration.SelectedIndex=2; Controls.Add(duration);

        Controls.Add(new Label { Text="Refresh interval", AutoSize=true, Location=new Point(229,104), ForeColor=Color.FromArgb(190,205,225) });
        refresh.DropDownStyle=ComboBoxStyle.DropDownList; refresh.Location=new Point(229,128); refresh.Size=new Size(160,30);
        refresh.Items.AddRange(new object[]{"2 seconds","5 seconds","10 seconds"}); refresh.SelectedIndex=1; Controls.Add(refresh);

        Button B(string text,int x,int w,Color back)
        {
            var b=new Button{Text=text,Location=new Point(x,186),Size=new Size(w,48),FlatStyle=FlatStyle.Flat,BackColor=back,ForeColor=Color.White};
            b.FlatAppearance.BorderColor=Color.FromArgb(72,92,116); Controls.Add(b); return b;
        }

        var gray=B("GRAYBACK  0xC0",28,210,Color.FromArgb(40,120,92));
        var issyl=B("ISSYL  0xA5",250,210,Color.FromArgb(56,93,145));
        var both=B("APPLY BOTH",472,210,Color.FromArgb(116,77,145));
        var stop=B("STOP HOLD",694,190,Color.FromArgb(130,58,58));

        gray.Click += (_,_) => StartHold(ReplayCore.GRAYBACK_RUNTIME_ABILITY,uint.MaxValue,"GRAYBACK 0xC0");
        issyl.Click += (_,_) => StartHold(ReplayCore.ISSYL_RUNTIME_ABILITY,uint.MaxValue,"ISSYL 0xA5");
        both.Click += (_,_) => StartHold(ReplayCore.GRAYBACK_RUNTIME_ABILITY,ReplayCore.ISSYL_RUNTIME_ABILITY,"BOTH 0xC0 + 0xA5");
        stop.Click += (_,_) => StopHold("manual stop");

        game.AutoSize=false; game.Location=new Point(28,270); game.Size=new Size(840,30); game.ForeColor=Color.FromArgb(97,224,179);
        hold.AutoSize=false; hold.Location=new Point(28,306); hold.Size=new Size(840,30); hold.ForeColor=Color.FromArgb(236,183,84);
        queue.AutoSize=false; queue.Location=new Point(28,344); queue.Size=new Size(840,70); queue.Font=new Font(FontFamily.GenericMonospace,9.4f);
        Controls.Add(game); Controls.Add(hold); Controls.Add(queue);

        var note=new Label
        {
            Text="V3 captures the units once, then replays the proven native effect on those same live units. STOP only stops refresh; it does not forcibly strip an already-active buff.",
            ForeColor=Color.FromArgb(160,185,215), AutoSize=false, Location=new Point(28,424), Size=new Size(850,48)
        };
        Controls.Add(note);

        timer.Tick += (_,_) => TickRuntime(); timer.Start();
        FormClosed += (_,_) => { holdActive=false; ReplayCore.ResetRuntime(); };
    }

    void StartHold(uint a1,uint a2,string label)
    {
        if(ReplayCore.IsQueueActive()){hold.Text="HOLD: wait for current queue to finish.";return;}
        int n=ReplayCore.CaptureSelectedTargets(); if(n==0){hold.Text="HOLD: no units selected.";return;}

        holdA1=a1;holdA2=a2;holdLabel=label;
        refreshEvery=refresh.SelectedIndex switch {0=>TimeSpan.FromSeconds(2),2=>TimeSpan.FromSeconds(10),_=>TimeSpan.FromSeconds(5)};
        holdInfinite=duration.SelectedIndex==3;
        TimeSpan d=duration.SelectedIndex switch {0=>TimeSpan.FromSeconds(30),1=>TimeSpan.FromSeconds(60),2=>TimeSpan.FromMinutes(5),_=>TimeSpan.MaxValue};
        holdUntil=holdInfinite?DateTime.MaxValue:DateTime.UtcNow+d;
        holdActive=true;
        queue.Text=ReplayCore.QueueHeld(holdA1,holdA2,$"{holdLabel} initial");
        nextRefresh=DateTime.UtcNow+refreshEvery;
        hold.Text=$"HOLD: ACTIVE • {n} captured • duration {(holdInfinite?"∞":d.TotalSeconds+"s")} • refresh {refreshEvery.TotalSeconds}s";
    }

    void StopHold(string why)
    {
        holdActive=false; ReplayCore.ClearHeld(); hold.Text=$"HOLD: stopped ({why}).";
    }

    void TickRuntime()
    {
        var s=ReplayCore.Snapshot(); game.Text=s.Game; queue.Text=s.Queue;
        if(!holdActive)return;
        var now=DateTime.UtcNow;
        if(!holdInfinite && now>=holdUntil){StopHold("duration complete");return;}
        if(now<nextRefresh || ReplayCore.IsQueueActive())return;
        string result=ReplayCore.QueueHeld(holdA1,holdA2,$"{holdLabel} refresh");
        queue.Text=result;
        nextRefresh=now+refreshEvery;
        string left=holdInfinite?"∞":Math.Max(0,(holdUntil-now).TotalSeconds).ToString("0")+"s";
        hold.Text=$"HOLD: ACTIVE • {ReplayCore.HeldCount} captured • remaining {left} • refresh every {refreshEvery.TotalSeconds:0}s";
    }
}
