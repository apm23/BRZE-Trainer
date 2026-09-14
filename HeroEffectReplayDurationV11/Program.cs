using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BRZEHeroEffectReplayV2;

namespace BRZEHeroEffectReplayDurationV11;

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
    readonly NumericUpDown customX = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 20 };
    readonly ReplayLifetimeWatcher watcher = new();

    readonly List<Button> replayButtons = new();
    bool holdActive;
    uint activeDuration = ConfigurableDurationCore.ORIGINAL_DURATION;
    string activeLabel = "none";
    double activeMultiplier = 1.0;

    public MainForm()
    {
        Text = "BRZE Hero Effect Replay + Duration — V11 CONFIGURABLE";
        ClientSize = new Size(1240, 790);
        MinimumSize = new Size(1080, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10f);

        Controls.Add(new Label
        {
            Text = "HERO EFFECT REPLAY + DURATION — V11 CONFIGURABLE",
            Font = new Font("Segoe UI Semibold", 19f),
            AutoSize = true,
            Location = new Point(24, 18)
        });
        Controls.Add(new Label
        {
            Text = "Runtime-proven V10.2 full-lifetime hold + known-good Replay V2. Baseline once, then replay 1X / 2X / 3X / custom.",
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
        var oneX = B("REPLAY 1X", 286, 94, 150);
        var twoX = B("REPLAY 2X", 448, 94, 150);
        var threeX = B("REPLAY 3X", 610, 94, 150);
        replayButtons.AddRange(new[] { oneX, twoX, threeX });

        Controls.Add(new Label { Text = "CUSTOM", AutoSize = true, Location = new Point(780, 83), ForeColor = Color.FromArgb(180, 195, 218) });
        customX.Location = new Point(780, 105);
        customX.Size = new Size(95, 30);
        customX.DecimalPlaces = 2;
        customX.Increment = 0.25m;
        customX.Minimum = 0.25m;
        customX.Maximum = 20.00m;
        customX.Value = 4.00m;
        customX.BackColor = Color.FromArgb(20, 27, 38);
        customX.ForeColor = Color.White;
        Controls.Add(customX);
        Controls.Add(new Label { Text = "x", AutoSize = true, Location = new Point(879, 109), Font = new Font("Segoe UI Semibold", 11f) });
        var custom = B("REPLAY CUSTOM", 905, 94, 175);
        replayButtons.Add(custom);

        var restore = B("RESTORE 15000", 1092, 94, 125);

        var reset = B("RESET", 24, 150, 120);
        var copy = B("COPY REPORT", 156, 150, 165);
        var gray = B("GRAYBACK NORMAL", 345, 150, 195);
        var issyl = B("ISSYL NORMAL", 552, 150, 195);
        var both = B("BOTH NORMAL", 759, 150, 195);

        capture.Click += (_, _) =>
        {
            if (holdActive) { phase.Text = "BLOCKED — active replay must finish or be restored first."; return; }
            watcher.Reset();
            phase.Text = ConfigurableDurationCore.ArmBaseline();
        };

        oneX.Click += (_, _) => StartReplay(1.0);
        twoX.Click += (_, _) => StartReplay(2.0);
        threeX.Click += (_, _) => StartReplay(3.0);
        custom.Click += (_, _) => StartReplay((double)customX.Value);

        restore.Click += (_, _) =>
        {
            holdActive = false;
            watcher.Reset();
            SetReplayButtons(true);
            phase.Text = ConfigurableDurationCore.RestoreNow();
        };

        reset.Click += (_, _) =>
        {
            holdActive = false;
            watcher.Reset();
            SetReplayButtons(true);
            ReplayCore.ResetRuntime();
            phase.Text = ConfigurableDurationCore.Reset();
            queue.Text = "QUEUE: idle";
            RefreshAll();
        };

        copy.Click += (_, _) => { try { Clipboard.SetText(report.Text); } catch { } };
        gray.Click += (_, _) => QueueNormal(ReplayCore.GRAYBACK_RUNTIME_ABILITY, uint.MaxValue, "GRAYBACK 0xC0");
        issyl.Click += (_, _) => QueueNormal(ReplayCore.ISSYL_RUNTIME_ABILITY, uint.MaxValue, "ISSYL 0xA5");
        both.Click += (_, _) => QueueNormal(ReplayCore.GRAYBACK_RUNTIME_ABILITY, ReplayCore.ISSYL_RUNTIME_ABILITY, "BOTH 0xC0 + 0xA5");

        game.AutoSize = false;
        game.Location = new Point(24, 214);
        game.Size = new Size(1190, 28);
        game.ForeColor = Color.FromArgb(100, 225, 180);
        Controls.Add(game);

        phase.AutoSize = false;
        phase.Location = new Point(24, 246);
        phase.Size = new Size(1190, 55);
        phase.ForeColor = Color.FromArgb(235, 185, 95);
        Controls.Add(phase);

        queue.AutoSize = false;
        queue.Location = new Point(24, 305);
        queue.Size = new Size(1190, 45);
        queue.Font = new Font(FontFamily.GenericMonospace, 9.2f);
        Controls.Add(queue);

        Controls.Add(new Label
        {
            Text = "FLOW: select ONE clean target → CAPTURE BASELINE → cast original Issyl once → wait until READY. After that you can select one clean target and press 1X / 2X / 3X / CUSTOM repeatedly without recapturing baseline. Any non-1X config remains active for the FULL effect lifetime and auto-restores only after natural expiry.",
            AutoSize = false,
            Location = new Point(24, 355),
            Size = new Size(1190, 62),
            ForeColor = Color.FromArgb(180, 195, 218)
        });

        Controls.Add(new Label
        {
            Text = "Safety: while an extended Issyl replay is active, do not manually cast Issyl or start another replay; the A5 duration config is globally held until that effect expires.",
            AutoSize = false,
            Location = new Point(24, 414),
            Size = new Size(1190, 36),
            ForeColor = Color.FromArgb(225, 145, 105)
        });

        report.Multiline = true;
        report.ReadOnly = true;
        report.ScrollBars = ScrollBars.Both;
        report.WordWrap = false;
        report.Font = new Font(FontFamily.GenericMonospace, 8.8f);
        report.Location = new Point(24, 455);
        report.Size = new Size(1190, 305);
        report.BackColor = Color.FromArgb(6, 9, 14);
        report.ForeColor = Color.Gainsboro;
        Controls.Add(report);

        timer.Tick += (_, _) =>
        {
            var replay = ReplayCore.Snapshot();
            if (holdActive)
            {
                var w = watcher.Tick();
                if (w.State == WatchState.Active)
                {
                    phase.Text = $"{activeLabel} ACTIVE — holding nominal {activeDuration} ({activeMultiplier:0.##}X) | elapsed {w.ElapsedMs / 1000.0:0.0}s";
                }
                else if (w.State == WatchState.Complete)
                {
                    holdActive = false;
                    string complete = ConfigurableDurationCore.CompleteReplay(w.ElapsedMs);
                    phase.Text = complete;
                    watcher.Reset();
                    SetReplayButtons(true);
                }
                else if (w.State == WatchState.Error)
                {
                    holdActive = false;
                    phase.Text = ConfigurableDurationCore.AbortAndRestore(w.Message);
                    watcher.Reset();
                    SetReplayButtons(true);
                }
                else
                {
                    phase.Text = $"{activeLabel} QUEUED — holding nominal {activeDuration}; waiting replay lifecycle...";
                }
            }
            else
            {
                ConfigurableDurationCore.Tick();
            }
            RefreshAll(replay);
        };

        timer.Start();
        FormClosed += (_, _) =>
        {
            holdActive = false;
            watcher.Reset();
            ConfigurableDurationCore.Shutdown();
            ReplayCore.ResetRuntime();
        };

        queue.Text = "QUEUE: idle";
        RefreshAll();
    }

    void StartReplay(double multiplier)
    {
        if (holdActive)
        {
            phase.Text = "BLOCKED — wait for the current replay to expire or press RESTORE 15000.";
            return;
        }

        uint desired = checked((uint)Math.Round(ConfigurableDurationCore.ORIGINAL_DURATION * multiplier, MidpointRounding.AwayFromZero));
        string label = multiplier == 1.0 ? "ISSYL 1X" : multiplier == 2.0 ? "ISSYL 2X" : multiplier == 3.0 ? "ISSYL 3X" : $"ISSYL CUSTOM {multiplier:0.##}X";
        string arm = ConfigurableDurationCore.ArmReplay(desired,label);
        phase.Text = arm;

        var s = ConfigurableDurationCore.Snapshot();
        if (s.Stage != DurationStage.ReplayArmed) return;

        if (!watcher.Arm(s.Unit,out string watchError))
        {
            phase.Text = ConfigurableDurationCore.AbortAndRestore("watcher arm failed: "+watchError);
            return;
        }

        string q = ReplayCore.QueueSelected(ReplayCore.ISSYL_RUNTIME_ABILITY,uint.MaxValue,label);
        queue.Text = q;
        if (!q.Contains("queued",StringComparison.OrdinalIgnoreCase))
        {
            watcher.Reset();
            phase.Text = ConfigurableDurationCore.AbortAndRestore("V2 queue failed: "+q);
            return;
        }

        activeDuration = desired;
        activeLabel = label;
        activeMultiplier = multiplier;
        holdActive = true;
        SetReplayButtons(false);
        phase.Text = $"{label} QUEUED — nominal duration {desired}; hold remains until natural expiry.";
    }

    void QueueNormal(uint a1,uint a2,string label)
    {
        if (holdActive)
        {
            phase.Text = "BLOCKED — normal replay disabled while configurable Issyl hold is active.";
            return;
        }
        queue.Text = ReplayCore.QueueSelected(a1,a2,label);
    }

    void SetReplayButtons(bool enabled)
    {
        foreach (var b in replayButtons) b.Enabled = enabled;
        customX.Enabled = enabled;
    }

    void RefreshAll(RuntimeSnapshot? replaySnapshot = null)
    {
        var d = ConfigurableDurationCore.Snapshot();
        var r = replaySnapshot ?? ReplayCore.Snapshot();
        game.Text = d.Game + " | " + r.Game;
        if (!holdActive) phase.Text = d.Phase;
        queue.Text = r.Queue;

        var lines = d.Lines.ToList();
        lines.Add("");
        lines.Add("=== V11 ACTIVE HOLD ===");
        lines.Add($"Hold active now: {holdActive}");
        lines.Add($"Active label: {activeLabel}");
        lines.Add($"Active multiplier: {activeMultiplier:0.##}x");
        lines.Add($"Active nominal duration: {activeDuration}");
        lines.Add("Presets: 1X=15000, 2X=30000, 3X=45000. Custom = 15000 × chosen multiplier.");
        lines.Add("Restore rule: non-1X config stays resident through the entire active effect lifetime; restore only after natural expiry.");
        report.Lines = lines.ToArray();
    }
}

internal enum WatchState { Waiting, Active, Complete, Error }
internal readonly record struct WatchResult(WatchState State,double ElapsedMs,string Message);

internal sealed class ReplayLifetimeWatcher
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_READ=0x0010;
    const uint PROCESS_QUERY_INFORMATION=0x0400;
    static readonly int[] RootOffsets={0x1E4,0x1E8,0x20C,0x210,0x214};
    static readonly int[] TransientIndexes={0,1,2,3};

    IntPtr h=IntPtr.Zero;
    Process? process;
    uint unit;
    readonly uint[] baseline=new uint[RootOffsets.Length];
    bool armed,active;
    int stable;
    long startStamp;

    static IntPtr A(long x)=>new(unchecked((int)(uint)x));

    bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach();
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0)return false;
        process=ps[0];
        h=OpenProcess(PROCESS_VM_READ|PROCESS_QUERY_INFORMATION,false,process.Id);
        return h!=IntPtr.Zero;
    }

    uint R32(long address)
    {
        if(h==IntPtr.Zero)return 0;
        var b=new byte[4];
        return ReadProcessMemory(h,A(address),b,4,out var n)&&n.ToInt64()==4?BitConverter.ToUInt32(b,0):0;
    }

    uint[] Roots()
    {
        var r=new uint[RootOffsets.Length];
        for(int i=0;i<RootOffsets.Length;i++)r[i]=R32((long)unit+RootOffsets[i]);
        return r;
    }

    static bool Same(uint[] a,uint[] b)
    {
        for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;
        return true;
    }

    bool EffectPattern(uint[] now)
    {
        int changed=0;
        for(int i=0;i<now.Length;i++)if(now[i]!=baseline[i])changed++;
        bool transientChanged=TransientIndexes.Any(i=>now[i]!=baseline[i]&&now[i]>=0x00010000&&now[i]<0x7FFF0000);
        return changed>=2&&transientChanged;
    }

    public bool Arm(uint targetUnit,out string error)
    {
        error="";
        Reset();
        if(targetUnit==0){error="unit pointer is zero";return false;}
        if(!Attach()){error="cannot attach read-only watcher";return false;}
        unit=targetUnit;
        var r=Roots();
        for(int i=0;i<r.Length;i++)baseline[i]=r[i];
        if(TransientIndexes.Any(i=>baseline[i]!=0))
        {
            error="target transient roots were not clean at replay arm";
            Reset();
            return false;
        }
        armed=true;active=false;stable=0;startStamp=0;
        return true;
    }

    public WatchResult Tick()
    {
        if(!armed)return new(WatchState.Error,0,"watcher not armed");
        if(!Attach())return new(WatchState.Error,0,"game process unavailable");
        var now=Roots();
        if(!active)
        {
            if(EffectPattern(now))
            {
                active=true;stable=0;startStamp=Stopwatch.GetTimestamp();
                return new(WatchState.Active,0,"replay lifecycle detected");
            }
            return new(WatchState.Waiting,0,"waiting lifecycle");
        }

        double ms=(Stopwatch.GetTimestamp()-startStamp)*1000.0/Stopwatch.Frequency;
        if(Same(now,baseline))stable++;else stable=0;
        if(stable>=4)
        {
            armed=false;
            return new(WatchState.Complete,ms,"natural expiry observed");
        }
        return new(WatchState.Active,ms,"active");
    }

    public void Reset()
    {
        armed=active=false;stable=0;startStamp=0;unit=0;
        Array.Clear(baseline,0,baseline.Length);
        Detach();
    }

    void Detach()
    {
        try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}
        h=IntPtr.Zero;process=null;
    }
}
