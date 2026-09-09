using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BRZETelemetryObserver;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ObserverForm());
    }
}

internal sealed class ObserverForm : Form
{
    readonly Label headline = new()
    {
        AutoSize = true,
        Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
        Text = "READ-ONLY — no BRZE memory is modified"
    };

    readonly Label current = new()
    {
        AutoSize = false,
        Height = 112,
        Dock = DockStyle.Top,
        Font = new Font(FontFamily.GenericMonospace, 10f)
    };

    readonly TextBox history = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        Font = new Font(FontFamily.GenericMonospace, 9f)
    };

    readonly Label footer = new()
    {
        AutoSize = false,
        Height = 42,
        Dock = DockStyle.Bottom
    };

    readonly System.Windows.Forms.Timer uiTimer = new() { Interval = 50 };
    readonly TelemetryReader reader = new();

    public ObserverForm()
    {
        Text = "BRZE 1.60 — Bulk Selection Telemetry Observer";
        ClientSize = new Size(920, 560);
        StartPosition = FormStartPosition.CenterScreen;

        var top = new Panel { Dock = DockStyle.Top, Height = 145, Padding = new Padding(12) };
        headline.Location = new Point(12, 8);
        current.Location = new Point(12, 32);
        current.Width = 890;
        top.Controls.Add(headline);
        top.Controls.Add(current);

        Controls.Add(history);
        Controls.Add(footer);
        Controls.Add(top);

        uiTimer.Tick += (_, _) => RefreshUi();
        uiTimer.Start();
        reader.Start();

        FormClosed += (_, _) => reader.Stop();
    }

    void RefreshUi()
    {
        var snap = reader.GetSnapshot();
        current.Text = snap.Current;
        history.Text = snap.History;
        history.SelectionStart = history.TextLength;
        history.ScrollToCaret();
        footer.Text = $"Log: {reader.LogPath}   |   Jalankan bersama Sim-Pipeline-120, reproduce satu drag besar, lalu kirim screenshot bagian ini setelah freeze.";
    }
}

internal sealed class TelemetryReader
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_READ = 0x0010;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;

    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_ACTIVE = 0x441708;
    const int RVA_SIM_LISTS_PTR = 0x441730;
    const int RVA_EVENT_USED = 0x441C94;
    const int RVA_EVENT_REMAIN = 0x441C98;
    const int RVA_EVENT_PTR = 0x441C9C;
    const int RVA_CANDIDATE = 0x479748;

    const int OFF_COUNT = 0x18;
    const int OFF_BLOCKS = 0x1C;
    const int OFF_FIRST = 0x20;
    const int OFF_GROWTH = 0x24;
    const int SIM_STRIDE = 0x28;

    readonly object gate = new();
    readonly Queue<string> changes = new();
    Thread? worker;
    volatile bool stop;

    IntPtr h = IntPtr.Zero;
    Process? process;
    long moduleBase;
    int pid;
    long attachEpoch;

    Sample last;
    bool hasLast;
    string currentText = "Waiting for Battle_Realms_F.exe...";

    uint maxCandidate;
    uint maxActive;
    uint maxSim;

    public string LogPath { get; } = Path.Combine(Path.GetTempPath(), "BRZE-Selection-Telemetry.log");

    public void Start()
    {
        stop = false;
        worker = new Thread(Loop) { IsBackground = true, Name = "BRZE telemetry reader" };
        worker.Start();
    }

    public void Stop()
    {
        stop = true;
        try { worker?.Join(500); } catch { }
        Detach();
    }

    public (string Current, string History) GetSnapshot()
    {
        lock (gate)
        {
            return (currentText, string.Join(Environment.NewLine, changes));
        }
    }

    void Loop()
    {
        while (!stop)
        {
            try
            {
                if (!Attach())
                {
                    SetWaiting();
                    Thread.Sleep(200);
                    continue;
                }

                Sample s = ReadSample();
                maxCandidate = Math.Max(maxCandidate, s.CandidateCount);
                maxActive = Math.Max(maxActive, s.ActiveCount);
                maxSim = Math.Max(maxSim, s.SimCount);

                string cur = FormatCurrent(s);
                lock (gate) currentText = cur;

                if (!hasLast || !s.Equals(last))
                {
                    string line = FormatChange(s);
                    AddChange(line);
                    last = s;
                    hasLast = true;
                }
            }
            catch (Exception ex)
            {
                AddChange($"observer error: {ex.GetType().Name}: {ex.Message}");
                Detach();
            }

            Thread.Sleep(5);
        }
    }

    bool Attach()
    {
        try
        {
            if (process != null && !process.HasExited && h != IntPtr.Zero) return true;
        }
        catch { }

        Detach();
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;

        process = ps[0];
        pid = process.Id;
        try { moduleBase = process.MainModule!.BaseAddress.ToInt64(); }
        catch { process = null; return false; }

        h = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, pid);
        if (h == IntPtr.Zero)
        {
            process = null;
            return false;
        }

        attachEpoch = Environment.TickCount64;
        hasLast = false;
        maxCandidate = maxActive = maxSim = 0;
        lock (gate) changes.Clear();
        AddChange($"ATTACH pid:{pid} base:0x{moduleBase:X8}");
        return true;
    }

    void Detach()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero;
        process = null;
        moduleBase = 0;
        pid = 0;
        hasLast = false;
    }

    void SetWaiting()
    {
        lock (gate) currentText = "Waiting for Battle_Realms_F.exe...";
    }

    uint R32(long addr)
    {
        if (h == IntPtr.Zero) return 0;
        byte[] b = new byte[4];
        bool ok = ReadProcessMemory(h, new IntPtr(unchecked((int)(uint)addr)), b, 4, out var n) && n.ToInt64() == 4;
        return ok ? BitConverter.ToUInt32(b, 0) : 0u;
    }

    Sample ReadSample()
    {
        uint lid = R32(moduleBase + RVA_LOCAL_ID);
        long active = moduleBase + RVA_ACTIVE;
        long candidate = moduleBase + RVA_CANDIDATE;
        uint simBase = R32(moduleBase + RVA_SIM_LISTS_PTR);
        long sim = simBase == 0 ? 0 : (long)simBase + lid * SIM_STRIDE;

        return new Sample
        {
            Ms = Environment.TickCount64 - attachEpoch,
            LocalId = lid,
            CandidateCount = R32(candidate + OFF_COUNT),
            CandidateBlocks = R32(candidate + OFF_BLOCKS),
            CandidateFirst = R32(candidate + OFF_FIRST),
            CandidateGrowth = R32(candidate + OFF_GROWTH),
            ActiveCount = R32(active + OFF_COUNT),
            ActiveBlocks = R32(active + OFF_BLOCKS),
            ActiveFirst = R32(active + OFF_FIRST),
            ActiveGrowth = R32(active + OFF_GROWTH),
            SimCount = sim == 0 ? 0 : R32(sim + OFF_COUNT),
            SimBlocks = sim == 0 ? 0 : R32(sim + OFF_BLOCKS),
            SimFirst = sim == 0 ? 0 : R32(sim + OFF_FIRST),
            SimGrowth = sim == 0 ? 0 : R32(sim + OFF_GROWTH),
            EventUsed = R32(moduleBase + RVA_EVENT_USED),
            EventRemain = R32(moduleBase + RVA_EVENT_REMAIN),
            EventPtr = R32(moduleBase + RVA_EVENT_PTR)
        };
    }

    string FormatCurrent(Sample s)
    {
        return $"pid:{pid} player:{s.LocalId} t:{s.Ms}ms   MAX cand:{maxCandidate} active:{maxActive} sim:{maxSim}\r\n" +
               $"CAND n:{s.CandidateCount} b:{s.CandidateBlocks} first:{s.CandidateFirst} grow:{s.CandidateGrowth}\r\n" +
               $"ACTIVE n:{s.ActiveCount} b:{s.ActiveBlocks} first:{s.ActiveFirst} grow:{s.ActiveGrowth}   |   SIM n:{s.SimCount} b:{s.SimBlocks} first:{s.SimFirst} grow:{s.SimGrowth}\r\n" +
               $"EVENT used:{s.EventUsed} remain:{s.EventRemain} ptr:0x{s.EventPtr:X8}";
    }

    string FormatChange(Sample s)
    {
        return $"{s.Ms,7}ms | C:{s.CandidateCount,3} A:{s.ActiveCount,3} S:{s.SimCount,3} | E:{s.EventUsed,4}/{s.EventRemain,4} | Cb:{s.CandidateBlocks} Ab:{s.ActiveBlocks} Sb:{s.SimBlocks}";
    }

    void AddChange(string line)
    {
        lock (gate)
        {
            changes.Enqueue(line);
            while (changes.Count > 80) changes.Dequeue();
        }
        try { File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss.fff ") + line + Environment.NewLine, Encoding.UTF8); } catch { }
    }

    struct Sample : IEquatable<Sample>
    {
        public long Ms;
        public uint LocalId;
        public uint CandidateCount, CandidateBlocks, CandidateFirst, CandidateGrowth;
        public uint ActiveCount, ActiveBlocks, ActiveFirst, ActiveGrowth;
        public uint SimCount, SimBlocks, SimFirst, SimGrowth;
        public uint EventUsed, EventRemain, EventPtr;

        public bool Equals(Sample o) =>
            LocalId == o.LocalId && CandidateCount == o.CandidateCount && CandidateBlocks == o.CandidateBlocks &&
            CandidateFirst == o.CandidateFirst && CandidateGrowth == o.CandidateGrowth &&
            ActiveCount == o.ActiveCount && ActiveBlocks == o.ActiveBlocks && ActiveFirst == o.ActiveFirst && ActiveGrowth == o.ActiveGrowth &&
            SimCount == o.SimCount && SimBlocks == o.SimBlocks && SimFirst == o.SimFirst && SimGrowth == o.SimGrowth &&
            EventUsed == o.EventUsed && EventRemain == o.EventRemain && EventPtr == o.EventPtr;
    }
}
