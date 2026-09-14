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
    bool holdDurationTickUntilNativeCall;

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
            {
                holdDurationTickUntilNativeCall = false;
                phase.Text = ReplayDurationCore.RestoreNow() + " | V2 queue failed: " + q;
                return;
            }

            // CRITICAL V10.1 timing fix:
            // Do not let V10 observe lifecycle / restore 30000->15000 until V2's
            // native-call counter proves the apply helper has RETURNED.
            // Previous merged build restored on first lifecycle visibility, which
            // happened before the helper finished consuming the duration config.
            holdDurationTickUntilNativeCall = true;
            phase.Text = "ISSYL 2X QUEUED — holding duration at 30000 until V2 native call returns...";
        };
        restore.Click += (_, _) =>
        {
            holdDurationTickUntilNativeCall = false;
            phase.Text = ReplayDurationCore.RestoreNow();
        };
        reset.Click += (_, _) =>
        {
            holdDurationTickUntilNativeCall = false;
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
            Text = "TEST FLOW: select ONE clean target → CAPTURE ISSYL BASELINE → cast ORIGINAL Issyl once → wait baseline complete → select same target again → REPLAY ISSYL 2X. The second button holds 30000 through the native call, then V10 restores automatically after the call returns.",
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
            var replay = ReplayCore.Snapshot();

            if (holdDurationTickUntilNativeCall)
            {
                if (TryNativeCalls(replay.Queue, out uint calls) && calls > 0)
                {
                    // V2 increments CALLS only after the native apply helper returns.
                    // It is now safe for V10 Tick() to see the lifecycle and restore.
                    holdDurationTickUntilNativeCall = false;
                    ReplayDurationCore.Tick();
                }
                else if (replay.Queue.StartsWith("QUEUE: DONE", StringComparison.OrdinalIgnoreCase))
                {
                    // Defensive fail-safe: never leave 30000 resident if V2 ends
                    // without proving a native apply call.
                    holdDurationTickUntilNativeCall = false;
                    phase.Text = ReplayDurationCore.RestoreNow() + " | V2 ended with zero native calls";
                }
                // While native calls == 0, intentionally DO NOT Tick V10.
            }
            else
            {
                ReplayDurationCore.Tick();
            }

            RefreshAll(replay);
        };
        timer.Start();
        FormClosed += (_, _) =>
        {
            holdDurationTickUntilNativeCall = false;
            ReplayDurationCore.Shutdown();
            ReplayCore.ResetRuntime();
        };

        queue.Text = "QUEUE: idle";
        RefreshAll();
    }

    static bool TryNativeCalls(string text, out uint calls)
    {
        calls = 0;
        const string key = "native calls:";
        int p = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (p < 0) return false;
        p += key.Length;
        while (p < text.Length && char.IsWhiteSpace(text[p])) p++;
        int e = p;
        while (e < text.Length && char.IsDigit(text[e])) e++;
        return e > p && uint.TryParse(text[p..e], out calls);
    }

    void RefreshAll(RuntimeSnapshot? replaySnapshot = null)
    {
        var d = ReplayDurationCore.Snapshot();
        var r = replaySnapshot ?? ReplayCore.Snapshot();
        game.Text = d.Game + " | " + r.Game;
        phase.Text = d.Phase;
        if (holdDurationTickUntilNativeCall)
            phase.Text += " | HOLD 30000 until native calls > 0";
        queue.Text = r.Queue;
        report.Lines = d.Lines.ToArray();
    }
}
