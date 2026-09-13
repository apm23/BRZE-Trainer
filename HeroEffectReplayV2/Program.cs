using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectReplayV2;

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
    readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };

    public MainForm()
    {
        Text = "BRZE Hero Effect Replay V2 — Runtime IDs";
        ClientSize = new Size(860, 360);
        MinimumSize = new Size(860, 360);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10f);

        var title = new Label
        {
            Text = "HERO EFFECT REPLAY V2",
            Font = new Font("Segoe UI Semibold", 20f),
            AutoSize = true,
            Location = new Point(26, 22)
        };
        var sub = new Label
        {
            Text = "Runtime-sniffed native ability IDs: Grayback 0xC0 • Issyl 0xA5",
            ForeColor = Color.FromArgb(160, 185, 215),
            AutoSize = true,
            Location = new Point(29, 66)
        };
        Controls.Add(title); Controls.Add(sub);

        Button B(string text, int x, int w, Color back)
        {
            var b = new Button { Text = text, Location = new Point(x, 112), Size = new Size(w, 48), FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = Color.White };
            b.FlatAppearance.BorderColor = Color.FromArgb(72, 92, 116);
            Controls.Add(b); return b;
        }

        var gray = B("GRAYBACK  0xC0", 28, 235, Color.FromArgb(40, 120, 92));
        var issyl = B("ISSYL  0xA5", 276, 235, Color.FromArgb(56, 93, 145));
        var both = B("APPLY BOTH", 524, 290, Color.FromArgb(116, 77, 145));

        gray.Click += (_,_) => queue.Text = ReplayCore.QueueSelected(ReplayCore.GRAYBACK_RUNTIME_ABILITY, uint.MaxValue, "GRAYBACK 0xC0");
        issyl.Click += (_,_) => queue.Text = ReplayCore.QueueSelected(ReplayCore.ISSYL_RUNTIME_ABILITY, uint.MaxValue, "ISSYL 0xA5");
        both.Click += (_,_) => queue.Text = ReplayCore.QueueSelected(ReplayCore.GRAYBACK_RUNTIME_ABILITY, ReplayCore.ISSYL_RUNTIME_ABILITY, "BOTH 0xC0 + 0xA5");

        game.AutoSize = false; game.Location = new Point(28, 192); game.Size = new Size(786, 34); game.ForeColor = Color.FromArgb(97, 224, 179);
        queue.AutoSize = false; queue.Location = new Point(28, 232); queue.Size = new Size(786, 74); queue.Font = new Font(FontFamily.GenericMonospace, 9.4f);
        Controls.Add(game); Controls.Add(queue);

        var note = new Label
        {
            Text = "Close main trainer / Clone Lab / Sniffer before applying. Select target units, then press a button above.",
            ForeColor = Color.FromArgb(236, 183, 84),
            AutoSize = true,
            Location = new Point(28, 318)
        };
        Controls.Add(note);

        timer.Tick += (_,_) => { var s = ReplayCore.Snapshot(); game.Text = s.Game; if (string.IsNullOrWhiteSpace(queue.Text)) queue.Text = s.Queue; else if (s.Queue.StartsWith("QUEUE:")) queue.Text = s.Queue; };
        timer.Start();
        FormClosed += (_,_) => ReplayCore.ResetRuntime();
    }
}
