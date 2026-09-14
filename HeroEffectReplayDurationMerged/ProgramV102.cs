using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BRZEHeroEffectReplayV2;
using BRZEHeroEffectReplayDurationV10;

namespace BRZEHeroEffectReplayDurationMerged;

internal static class ProgramV102
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainFormV102());
    }
}

internal sealed class MainFormV102 : Form
{
    readonly Label game = new();
    readonly Label phase = new();
    readonly Label queue = new();
    readonly TextBox report = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 20 };
    readonly ReplayLifetimeWatcher watcher = new();

    bool hold30000;
    double baselineMs;
    double replayMs;
    string mergedResult = "V10.2: not tested";

    public MainFormV102()
    {
        Text = "BRZE Hero Effect Replay + Duration — V10.2 MERGED";
        ClientSize = new Size(1180, 735);
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10f);

        Controls.Add(new Label { Text = "HERO EFFECT REPLAY + DURATION — V10.2", Font = new Font("Segoe UI Semibold", 19f), AutoSize = true, Location = new Point(24, 18) });
        Controls.Add(new Label { Text = "One EXE. Replay V2 + V9-proven full-lifetime duration hold.", ForeColor = Color.FromArgb(150, 190, 220), AutoSize = true, Location = new Point(27, 58) });

        Button B(string text, int x, int y, int w)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 44), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(32, 45, 63), ForeColor = Color.White };
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

        capture.Click += (_, _) =>
        {
            hold30000 = false;
            watcher.Reset();
            baselineMs = replayMs = 0;
            mergedResult = "V10.2: baseline armed";
            phase.Text = ReplayDurationCore.ArmBaseline();
        };

        replay2x.Click += (_, _) =>
        {
            var before = ReplayDurationCore.Snapshot();
            baselineMs = ParseMs(before.Lines, "Baseline ORIGINAL Issyl wall lifetime:");

            string arm = ReplayDurationCore.ArmReplay2X();
            phase.Text = arm;
            if (!arm.Contains("ARMED", StringComparison.OrdinalIgnoreCase)) return;

            var armed = ReplayDurationCore.Snapshot();
            if (!TryPinnedUnit(armed.Lines, out uint unit))
            {
                phase.Text = ReplayDurationCore.RestoreNow() + " | V10.2 could not parse pinned unit";
                return;
            }

            if (!watcher.Arm(unit, out string watchError))
            {
                phase.Text = ReplayDurationCore.RestoreNow() + " | V10.2 watcher: " + watchError;
                return;
            }

            string q = ReplayCore.QueueSelected(ReplayCore.ISSYL_RUNTIME_ABILITY, uint.MaxValue, "ISSYL 2X");
            queue.Text = q;
            if (!q.Contains("queued", StringComparison.OrdinalIgnoreCase))
            {
                phase.Text = ReplayDurationCore.RestoreNow() + " | V2 queue failed: " + q;
                watcher.Reset();
                return;
            }

            hold30000 = true;
            replayMs = 0;
            mergedResult = "V10.2: HOLDING 30000 through full replay lifetime";
            phase.Text = "ISSYL 2X QUEUED — keeping A5 duration=30000 until natural expiry.";
        };

        restore.Click += (_, _) =>
        {
            hold30000 = false;
            watcher.Reset();
            phase.Text = ReplayDurationCore.RestoreNow();
        };

        reset.Click += (_, _) =>
        {
            hold30000 = false;
            watcher.Reset();
            baselineMs = replayMs = 0;
            mergedResult = "V10.2: reset";
            ReplayCore.ResetRuntime();
            phase.Text = ReplayDurationCore.Reset();
            RefreshAll();
        };

        copy.Click += (_, _) => { try { Clipboard.SetText(report.Text); } catch { } };
        gray.Click += (_, _) => queue.Text = ReplayCore.QueueSelected(ReplayCore.GRAYBACK_RUNTIME_ABILITY, uint.MaxValue, "GRAYBACK 0xC0");
        issyl.Click += (_, _) => queue.Text = ReplayCore.QueueSelected(ReplayCore.ISSYL_RUNTIME_ABILITY, uint.MaxValue, "ISSYL 0xA5");
        both.Click += (_, _) => queue.Text = ReplayCore.QueueSelected(ReplayCore.GRAYBACK_RUNTIME_ABILITY, ReplayCore.ISSYL_RUNTIME_ABILITY, "BOTH 0xC0 + 0xA5");

        game.AutoSize = false; game.Location = new Point(24, 214); game.Size = new Size(1125, 28); game.ForeColor = Color.FromArgb(100, 225, 180); Controls.Add(game);
        phase.AutoSize = false; phase.Location = new Point(24, 246); phase.Size = new Size(1125, 52); phase.ForeColor = Color.FromArgb(235, 185, 95); Controls.Add(phase);
        queue.AutoSize = false; queue.Location = new Point(24, 300); queue.Size = new Size(1125, 45); queue.Font = new Font(FontFamily.GenericMonospace, 9.2f); Controls.Add(queue);

        Controls.Add(new Label
        {
            Text = "FLOW: one clean target → CAPTURE BASELINE → cast original Issyl once → wait baseline complete → select same target → REPLAY ISSYL 2X once. V10.2 holds 30000 for the whole replay and restores only after the effect naturally expires.",
            AutoSize = false,
            Location = new Point(24, 350),
            Size = new Size(1125, 55),
            ForeColor = Color.FromArgb(180, 195, 218)
        });

        report.Multiline = true; report.ReadOnly = true; report.ScrollBars = ScrollBars.Both; report.WordWrap = false;
        report.Font = new Font(FontFamily.GenericMonospace, 8.8f); report.Location = new Point(24, 410); report.Size = new Size(1125, 295);
        report.BackColor = Color.FromArgb(6, 9, 14); report.ForeColor = Color.Gainsboro; Controls.Add(report);

        timer.Tick += (_, _) =>
        {
            var replay = ReplayCore.Snapshot();
            if (hold30000)
            {
                var w = watcher.Tick();
                if (w.State == WatchState.Active)
                {
                    phase.Text = $"REPLAY ACTIVE — HOLDING 30000 | elapsed {w.ElapsedMs / 1000.0:0.0}s";
                }
                else if (w.State == WatchState.Complete)
                {
                    replayMs = w.ElapsedMs;
                    hold30000 = false;
                    string restored = ReplayDurationCore.RestoreNow();
                    double ratio = baselineMs > 0 ? replayMs / baselineMs : 0;
                    mergedResult = $"V10.2 COMPLETE | baseline={baselineMs:0.0} ms | replay={replayMs:0.0} ms | ratio={ratio:0.000000}x | {restored}";
                    phase.Text = mergedResult;
                    watcher.Reset();
                }
                else if (w.State == WatchState.Error)
                {
                    hold30000 = false;
                    string restored = ReplayDurationCore.RestoreNow();
                    mergedResult = "V10.2 ERROR: " + w.Message + " | " + restored;
                    phase.Text = mergedResult;
                    watcher.Reset();
                }
                else
                {
                    phase.Text = "ISSYL 2X QUEUED — HOLDING 30000; waiting replay lifecycle...";
                }
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
            hold30000 = false;
            watcher.Reset();
            ReplayDurationCore.Shutdown();
            ReplayCore.ResetRuntime();
        };

        queue.Text = "QUEUE: idle";
        RefreshAll();
    }

    static bool TryPinnedUnit(IReadOnlyList<string> lines, out uint unit)
    {
        unit = 0;
        const string key = "Pinned Unit*=0x";
        foreach (string line in lines)
        {
            int p = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (p < 0) continue;
            p += key.Length;
            int e = p;
            while (e < line.Length && Uri.IsHexDigit(line[e])) e++;
            if (e > p && uint.TryParse(line[p..e], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out unit)) return unit != 0;
        }
        return false;
    }

    static double ParseMs(IReadOnlyList<string> lines, string key)
    {
        foreach (string line in lines)
        {
            if (!line.StartsWith(key, StringComparison.OrdinalIgnoreCase)) continue;
            string tail = line[key.Length..].Trim();
            int sp = tail.IndexOf(' ');
            if (sp > 0) tail = tail[..sp];
            if (double.TryParse(tail, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return v;
        }
        return 0;
    }

    void RefreshAll(RuntimeSnapshot? replaySnapshot = null)
    {
        var d = ReplayDurationCore.Snapshot();
        var r = replaySnapshot ?? ReplayCore.Snapshot();
        game.Text = d.Game + " | " + r.Game;
        if (!hold30000 && !mergedResult.StartsWith("V10.2 COMPLETE", StringComparison.Ordinal)) phase.Text = d.Phase;
        queue.Text = r.Queue;

        var lines = d.Lines.ToList();
        lines.Add("");
        lines.Add("=== MERGED V10.2 FULL-LIFETIME HOLD ===");
        lines.Add(mergedResult);
        lines.Add($"Hold 30000 active now: {hold30000}");
        if (baselineMs > 0) lines.Add($"Baseline used by merged proof: {baselineMs:0.0} ms");
        if (replayMs > 0)
        {
            double ratio = baselineMs > 0 ? replayMs / baselineMs : 0;
            lines.Add($"Merged replay lifetime: {replayMs:0.0} ms");
            lines.Add($"Merged replay/baseline ratio: {ratio:0.000000}x");
        }
        lines.Add("Policy: config+0x0E0 stays 30000 for the ENTIRE replay lifetime and returns to 15000 only after natural expiry. This intentionally mirrors the V9 timing that produced ~1.976x.");
        report.Lines = lines.ToArray();
    }
}

internal enum WatchState { Waiting, Active, Complete, Error }
internal readonly record struct WatchResult(WatchState State, double ElapsedMs, string Message);

internal sealed class ReplayLifetimeWatcher
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_READ = 0x0010;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    static readonly int[] RootOffsets = { 0x1E4, 0x1E8, 0x20C, 0x210, 0x214 };
    static readonly int[] TransientIndexes = { 0, 1, 2, 3 };

    IntPtr h = IntPtr.Zero;
    Process? process;
    uint unit;
    readonly uint[] baseline = new uint[RootOffsets.Length];
    bool armed, active;
    int stable;
    long startStamp;

    static IntPtr A(long x) => new(unchecked((int)(uint)x));

    bool Attach()
    {
        try { if (process != null && !process.HasExited && h != IntPtr.Zero) return true; } catch { }
        Detach();
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;
        process = ps[0];
        h = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, process.Id);
        return h != IntPtr.Zero;
    }

    uint R32(long address)
    {
        if (h == IntPtr.Zero) return 0;
        var b = new byte[4];
        return ReadProcessMemory(h, A(address), b, 4, out var n) && n.ToInt64() == 4 ? BitConverter.ToUInt32(b, 0) : 0;
    }

    uint[] Roots()
    {
        var r = new uint[RootOffsets.Length];
        for (int i = 0; i < RootOffsets.Length; i++) r[i] = R32((long)unit + RootOffsets[i]);
        return r;
    }

    static bool Same(uint[] a, uint[] b)
    {
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    bool EffectPattern(uint[] now)
    {
        int changed = 0;
        for (int i = 0; i < now.Length; i++) if (now[i] != baseline[i]) changed++;
        bool transientChanged = TransientIndexes.Any(i => now[i] != baseline[i] && now[i] >= 0x00010000 && now[i] < 0x7FFF0000);
        return changed >= 2 && transientChanged;
    }

    public bool Arm(uint targetUnit, out string error)
    {
        error = "";
        Reset();
        if (targetUnit == 0) { error = "unit pointer is zero"; return false; }
        if (!Attach()) { error = "cannot attach read-only watcher"; return false; }
        unit = targetUnit;
        var r = Roots();
        for (int i = 0; i < r.Length; i++) baseline[i] = r[i];
        if (TransientIndexes.Any(i => baseline[i] != 0))
        {
            error = "target transient roots were not clean at replay arm";
            Reset();
            return false;
        }
        armed = true;
        active = false;
        stable = 0;
        startStamp = 0;
        return true;
    }

    public WatchResult Tick()
    {
        if (!armed) return new(WatchState.Error, 0, "watcher not armed");
        if (!Attach()) return new(WatchState.Error, 0, "game process unavailable");
        var now = Roots();

        if (!active)
        {
            if (EffectPattern(now))
            {
                active = true;
                stable = 0;
                startStamp = Stopwatch.GetTimestamp();
                return new(WatchState.Active, 0, "replay lifecycle detected");
            }
            return new(WatchState.Waiting, 0, "waiting lifecycle");
        }

        double ms = (Stopwatch.GetTimestamp() - startStamp) * 1000.0 / Stopwatch.Frequency;
        if (Same(now, baseline)) stable++; else stable = 0;
        if (stable >= 4)
        {
            armed = false;
            return new(WatchState.Complete, ms, "natural expiry observed");
        }
        return new(WatchState.Active, ms, "active");
    }

    public void Reset()
    {
        armed = active = false;
        stable = 0;
        startStamp = 0;
        unit = 0;
        Array.Clear(baseline, 0, baseline.Length);
        Detach();
    }

    void Detach()
    {
        try { if (h != IntPtr.Zero) CloseHandle(h); } catch { }
        h = IntPtr.Zero;
        process = null;
    }
}
