using System.Drawing;
using System.Windows.Forms;
using BRZEHeroEffectReplayV2;
using BRZEHeroEffectReplayDurationV10;

namespace BRZEHeroEffectReplayDurationMerged;

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
    readonly Label queue = new();
    readonly TextBox report = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 20 };

    public MainForm()
    {
        Text = "BRZE Hero Effect Replay + Duration — V2/V10 MERGED";
        ClientSize = new Size(1220, 760);
        MinimumSize = new Size(1050, 690);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10f);

        Controls.Add(new Label
        {
            Text = "HERO EFFECT REPLAY + DURATION — MERGED",
            Font = new Font("Segoe UI Semibold", 19f),
            AutoSize = true,
            Location = new Point(24, 18)
        });
        Controls.Add(new Label
        {
            Text = "Replay V2 native queue + V10 proven duration controller in ONE window",
            ForeColor = Color.FromArgb(150, 190, 220),
            AutoSize = true,
            Location = new Point(27, 58)
        });

        Button B(string text, int x, int y, int w)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 45, 63),
                ForeColor = Color.White
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(80, 105, 135);
            Controls.Add(b);
            return b;
        }

        var capture = B("1) CAPTURE ISSYL BASELINE", 24, 94, 250);
        var replay2x = B("2) REPLAY ISSYL 2X", 286, 94, 230);
        var restore = B("RESTORE 15000", 528, 94, 175);
        var reset = B("RESET", 715, 94, 120);
        var copy = B("COPY REPORT", 847, 94, 165);

        var gray = B("GRAYBACK NORMAL", 24, 150, 210);
        var issyl = B("ISSYL NORMAL", 246, 150, 210);
        var both = B("BOTH NORMAL", 468, 150, 210);

        capture.Click += (_, _) => phase.Text = ReplayDurationCore.ArmBaseline();
        replay2x.Click += (_, _) =>
        {
            string arm = ReplayDurationCore.ArmReplay2X();
            phase.Text = arm;
            if (!arm.Contains("ARMED", StringComparison.OrdinalIgnoreCase)) return;

            string q = ReplayCore.QueueSelected(ReplayCore.ISSYL_RUNTIME_ABILITY, uint.MaxValue, "ISSYL 2X");
            queue.Text = q;
            if (!q.Contains("queued", StringComparison.OrdinalIgnoreCase))
                phase.Text = ReplayDurationCore.RestoreNow() + " | V2 queue failed: " + q;
        };
        restore.Click += (_, _) => phase.Text = ReplayDurationCore.RestoreNow();
        reset.Click += (_, _) =>
        {
            ReplayCore.ResetRuntime();
            phase.Text = ReplayDurationCore.Reset();
            queue.Text = "QUEUE: idle";
            RefreshAll();
        };
        copy.Click += (_, _) => { try { Clipboard.SetText(report.Text); } catch { } };

        gray.Click += (_, _) => queue.Text = ReplayCore.QueueSelected(ReplayCore.GRAYBACK_RUNTIME_ABILITY, uint.MaxValue, "GRAYBACK 0xC0");
        issyl.Click += (_, _) => queue.Text = ReplayCore.QueueSelected(ReplayCore.ISSYL_RUNTIME_ABILITY, uint.MaxValue, "ISSYL 0xA5");
        both.Click += (_, _) => queue.Text = ReplayCore.QueueSelected(ReplayCore.GRAYBACK_RUNTIME_ABILITY, ReplayCore.ISSYL_RUNTIME_ABILITY, "BOTH 0xC0 + 0xA5");

        game.AutoSize = false;
        game.Location = new Point(24, 214);
        game.Size = new Size(1170, 28);
        game.ForeColor = Color.FromArgb(100, 225, 180);
        Controls.Add(game);

        phase.AutoSize = false;
        phase.Location = new Point(24, 246);
        phase.Size = new Size(1170, 52);
        phase.ForeColor = Color.FromArgb(235, 185, 95);
        Controls.Add(phase);

        queue.AutoSize = false;
        queue.Location = new Point(24, 300);
        queue.Size = new Size(1170, 45);
        queue.Font = new Font(FontFamily.GenericMonospace, 9.2f);
        Controls.Add(queue);

        Controls.Add(new Label
        {
            Text = "TEST FLOW: select ONE clean target → CAPTURE ISSYL BASELINE → cast ORIGINAL Issyl once → wait baseline complete → select same target again → REPLAY ISSYL 2X. The second button patches duration, queues V2 replay, and V10 restores automatically.",
            AutoSize = false,
            Location = new Point(24, 350),
            Size = new Size(1170, 60),
            ForeColor = Color.FromArgb(180, 195, 218)
        });

        report.Multiline = true;
        report.ReadOnly = true;
        report.ScrollBars = ScrollBars.Both;
        report.WordWrap = false;
        report.Font = new Font(FontFamily.GenericMonospace, 8.8f);
        report.Location = new Point(24, 415);
        report.Size = new Size(1170, 320);
        report.BackColor = Color.FromArgb(6, 9, 14);
        report.ForeColor = Color.Gainsboro;
        Controls.Add(report);

        timer.Tick += (_, _) =>
        {
            ReplayDurationCore.Tick();
            RefreshAll();
        };
        timer.Start();
        FormClosed += (_, _) =>
        {
            ReplayDurationCore.Shutdown();
            ReplayCore.ResetRuntime();
        };

        queue.Text = "QUEUE: idle";
        RefreshAll();
    }

    void RefreshAll()
    {
        var d = ReplayDurationCore.Snapshot();
        var r = ReplayCore.Snapshot();
        game.Text = d.Game + " | " + r.Game;
        phase.Text = d.Phase;
        queue.Text = r.Queue;
        report.Lines = d.Lines.ToArray();
    }
}
