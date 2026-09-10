using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEUnitChangerLab;

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
    readonly FlowLayoutPanel slotsPanel = new();
    readonly Label gameStatus = new();
    readonly Label trainingStatus = new();
    readonly Label rawStatus = new();
    readonly Label activeCount = new();
    readonly Button refreshButton = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 350 };
    readonly List<OutputSlotControl> slots = new();

    static readonly string[] StarterNames =
    {
        "OFF",
        "Peasant",
        "Spearman",
        "Archer",
        "Chemist",
        "Dragon Warrior",
        "Samurai",
        "Kabuki Warrior",
        "Sumo Cannoneer",
        "Swordsman",
        "Crossbowman",
        "Musketeer",
        "Ronin",
        "Brawler",
        "Ballistaman",
        "Sledger",
        "Berserker",
        "Werewolf",
        "Pack Master",
        "Blade Acolyte",
        "Leaf Disciple",
        "Staff Adept",
        "Diseased One",
        "Unclean One",
        "Infested One",
        "Warlock",
        "Master Warlock",
        "Channeler"
    };

    public MainForm()
    {
        Text = "BRZE Unit Changer Lab — Separate Prototype";
        ClientSize = new Size(940, 720);
        MinimumSize = new Size(900, 690);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 360));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(16, 22, 32) };
        var title = new Label
        {
            Text = "UNIT CHANGER // MULTI-OUTPUT LAB",
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 10)
        };
        var sub = new Label
        {
            Text = "Separate trainer • observer-only foundation • zero game writes",
            ForeColor = Color.FromArgb(157, 178, 202),
            AutoSize = true,
            Location = new Point(18, 43)
        };
        header.Controls.Add(title);
        header.Controls.Add(sub);
        root.Controls.Add(header, 0, 0);

        var info = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 29, 41), Padding = new Padding(14, 8, 14, 8) };
        gameStatus.Text = "GAME: waiting for Battle_Realms_F.exe";
        gameStatus.AutoSize = true;
        gameStatus.ForeColor = Color.FromArgb(97, 224, 179);
        gameStatus.Location = new Point(14, 8);
        activeCount.Text = "OUTPUTS ENABLED: 0 / 9";
        activeCount.AutoSize = true;
        activeCount.ForeColor = Color.FromArgb(198, 207, 220);
        activeCount.Location = new Point(610, 8);
        info.Controls.Add(gameStatus);
        info.Controls.Add(activeCount);
        root.Controls.Add(info, 0, 1);

        var slotsCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 24, 34), Padding = new Padding(12) };
        var slotsTitle = new Label
        {
            Text = "OUTPUT SLOTS 1–9",
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = Color.FromArgb(97, 224, 179),
            AutoSize = true,
            Location = new Point(14, 10)
        };
        var slotsHint = new Label
        {
            Text = "Enable only the outputs you want. Unit names are configuration-only until native unit IDs are mapped.",
            ForeColor = Color.FromArgb(145, 157, 176),
            AutoSize = true,
            Location = new Point(14, 34)
        };
        slotsCard.Controls.Add(slotsTitle);
        slotsCard.Controls.Add(slotsHint);

        slotsPanel.FlowDirection = FlowDirection.TopDown;
        slotsPanel.WrapContents = false;
        slotsPanel.AutoScroll = false;
        slotsPanel.Location = new Point(12, 62);
        slotsPanel.Size = new Size(880, 282);
        slotsPanel.BackColor = Color.Transparent;
        slotsCard.Controls.Add(slotsPanel);

        for (int i = 1; i <= 9; i++)
        {
            var slot = new OutputSlotControl(i, StarterNames);
            slot.Changed += (_, _) => UpdateSlotSummary();
            slots.Add(slot);
            slotsPanel.Controls.Add(slot);
        }
        root.Controls.Add(slotsCard, 0, 2);

        var monitor = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 24, 34), Padding = new Padding(14) };
        var monitorTitle = new Label
        {
            Text = "TRAINING OBSERVER",
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = Color.FromArgb(97, 224, 179),
            AutoSize = true,
            Location = new Point(14, 10)
        };
        trainingStatus.AutoSize = false;
        trainingStatus.Location = new Point(14, 36);
        trainingStatus.Size = new Size(860, 62);
        trainingStatus.Font = new Font(FontFamily.GenericMonospace, 9f);
        trainingStatus.ForeColor = Color.WhiteSmoke;
        trainingStatus.Text = "Selected building: none\r\nLocal active training buildings: 0";
        rawStatus.AutoSize = false;
        rawStatus.Location = new Point(14, 100);
        rawStatus.Size = new Size(860, 72);
        rawStatus.Font = new Font(FontFamily.GenericMonospace, 8.5f);
        rawStatus.ForeColor = Color.FromArgb(145, 157, 176);
        rawStatus.Text = "Read-only telemetry will appear here.";
        monitor.Controls.Add(monitorTitle);
        monitor.Controls.Add(trainingStatus);
        monitor.Controls.Add(rawStatus);
        root.Controls.Add(monitor, 0, 3);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(16, 22, 32) };
        refreshButton.Text = "REFRESH ATTACH";
        refreshButton.FlatStyle = FlatStyle.Flat;
        refreshButton.FlatAppearance.BorderColor = Color.FromArgb(70, 90, 115);
        refreshButton.BackColor = Color.FromArgb(42, 55, 72);
        refreshButton.ForeColor = Color.White;
        refreshButton.Size = new Size(150, 32);
        refreshButton.Location = new Point(12, 8);
        refreshButton.Click += (_, _) => { Observer.Reset(); Poll(); };
        var safety = new Label
        {
            Text = "LAB SAFETY: READ-ONLY • no hook • no patch • no spawn",
            AutoSize = true,
            ForeColor = Color.FromArgb(236, 183, 84),
            Location = new Point(520, 15)
        };
        footer.Controls.Add(refreshButton);
        footer.Controls.Add(safety);
        root.Controls.Add(footer, 0, 4);

        timer.Tick += (_, _) => Poll();
        timer.Start();
        FormClosed += (_, _) => Observer.Reset();
        UpdateSlotSummary();
        Poll();
    }

    void UpdateSlotSummary()
    {
        int n = slots.Count(x => x.EnabledOutput);
        activeCount.Text = $"OUTPUTS ENABLED: {n} / 9";
    }

    void Poll()
    {
        var s = Observer.Snapshot();
        gameStatus.Text = s.GameLine;
        trainingStatus.Text = s.TrainingLine;
        rawStatus.Text = s.RawLine;
    }
}

internal sealed class OutputSlotControl : Panel
{
    readonly CheckBox enabled = new();
    readonly ComboBox unit = new();
    public event EventHandler? Changed;
    public bool EnabledOutput => enabled.Checked && unit.Text.Length > 0 && !unit.Text.Equals("OFF", StringComparison.OrdinalIgnoreCase);

    public OutputSlotControl(int index, string[] names)
    {
        Width = 860;
        Height = 30;
        Margin = new Padding(0, 0, 0, 1);
        BackColor = index % 2 == 0 ? Color.FromArgb(22, 29, 41) : Color.FromArgb(25, 33, 46);

        var number = new Label
        {
            Text = $"SLOT {index}",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.WhiteSmoke,
            Location = new Point(10, 3),
            Size = new Size(70, 24)
        };
        enabled.Text = "ON";
        enabled.ForeColor = Color.FromArgb(168, 181, 198);
        enabled.Location = new Point(88, 5);
        enabled.Size = new Size(52, 22);
        enabled.CheckedChanged += (_, _) => { unit.Enabled = enabled.Checked; Changed?.Invoke(this, EventArgs.Empty); };

        unit.DropDownStyle = ComboBoxStyle.DropDown;
        unit.FlatStyle = FlatStyle.Flat;
        unit.Location = new Point(150, 3);
        unit.Size = new Size(690, 24);
        unit.BackColor = Color.FromArgb(35, 45, 60);
        unit.ForeColor = Color.White;
        unit.Items.AddRange(names.Cast<object>().ToArray());
        unit.SelectedIndex = 0;
        unit.Enabled = false;
        unit.TextChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);

        Controls.Add(number);
        Controls.Add(enabled);
        Controls.Add(unit);
    }
}

internal readonly record struct ObserverSnapshot(string GameLine, string TrainingLine, string RawLine);

internal static class Observer
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);

    const uint PROCESS_VM_READ = 0x0010;
    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const uint ACCESS = PROCESS_VM_READ | PROCESS_QUERY_INFORMATION;

    // Current BRZE 1.60 target mappings copied only as READ-ONLY telemetry inputs.
    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_SELECTED_BUILDING_A = 0x4417D4;
    const int RVA_SELECTED_BUILDING_B = 0x4417D8;
    const int RVA_BUILDING_POOL = 0x4814E0;
    const int BUILDING_STRIDE = 0x6A4;
    const int BUILDING_COUNT = 500;
    const int OFF_BUILD_OWNER = 0x84;
    const int OFF_TRAIN_TYPE = 0x488;
    const int OFF_TRAIN_PROGRESS = 0x490;
    const int OFF_TRAIN_GATE = 0x4B8;
    const uint TRAIN_COMPLETE_FIXED = 0x00640000;

    static IntPtr h;
    static Process? process;
    static long moduleBase;
    static DateTime lastAttachTry;
    static readonly byte[] buildingBuffer = new byte[BUILDING_COUNT * BUILDING_STRIDE];

    public static void Reset()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero;
        process = null;
        moduleBase = 0;
        lastAttachTry = DateTime.MinValue;
    }

    static bool EnsureAttached()
    {
        try
        {
            if (process != null && !process.HasExited && h != IntPtr.Zero) return true;
        }
        catch { }

        Reset();
        if ((DateTime.UtcNow - lastAttachTry).TotalMilliseconds < 450) return false;
        lastAttachTry = DateTime.UtcNow;

        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;
        process = ps[0];
        try { moduleBase = process.MainModule!.BaseAddress.ToInt64(); }
        catch { Reset(); return false; }
        h = OpenProcess(ACCESS, false, process.Id);
        return h != IntPtr.Zero;
    }

    static bool ReadExact(long address, byte[] buffer)
    {
        return h != IntPtr.Zero &&
               ReadProcessMemory(h, new IntPtr(unchecked((int)(uint)address)), buffer, buffer.Length, out var n) &&
               n.ToInt64() == buffer.Length;
    }

    static uint R32(long address)
    {
        var b = new byte[4];
        return ReadExact(address, b) ? BitConverter.ToUInt32(b, 0) : 0;
    }

    static string BuildingSummary(uint ptr, uint localId)
    {
        if (ptr == 0) return "none";
        uint owner = R32((long)ptr + OFF_BUILD_OWNER);
        uint trainType = R32((long)ptr + OFF_TRAIN_TYPE);
        uint progress = R32((long)ptr + OFF_TRAIN_PROGRESS);
        uint gate = R32((long)ptr + OFF_TRAIN_GATE);
        double pct = TRAIN_COMPLETE_FIXED == 0 ? 0 : Math.Clamp(progress * 100.0 / TRAIN_COMPLETE_FIXED, 0, 999);
        string side = owner == localId ? "LOCAL" : $"owner:{owner}";
        return $"0x{ptr:X8} {side} trainType:0x{trainType:X} progress:{progress:X8} ({pct:0.0}%) gate:0x{gate:X}";
    }

    static long ResolveBuildingPool(out string mode)
    {
        uint candidate = R32(moduleBase + RVA_BUILDING_POOL);
        if (candidate >= 0x10000)
        {
            var test = new byte[16];
            if (ReadExact(candidate, test))
            {
                mode = "pointer";
                return candidate;
            }
        }

        long direct = moduleBase + RVA_BUILDING_POOL;
        var directTest = new byte[16];
        if (ReadExact(direct, directTest))
        {
            mode = "direct";
            return direct;
        }
        mode = "unresolved";
        return 0;
    }

    static int CountLocalActiveTraining(long pool, uint localId)
    {
        if (pool == 0 || !ReadExact(pool, buildingBuffer)) return -1;
        int count = 0;
        for (int i = 0; i < BUILDING_COUNT; i++)
        {
            int o = i * BUILDING_STRIDE;
            uint owner = BitConverter.ToUInt32(buildingBuffer, o + OFF_BUILD_OWNER);
            uint progress = BitConverter.ToUInt32(buildingBuffer, o + OFF_TRAIN_PROGRESS);
            if (owner == localId && progress > 0 && progress <= TRAIN_COMPLETE_FIXED * 4u) count++;
        }
        return count;
    }

    public static ObserverSnapshot Snapshot()
    {
        if (!EnsureAttached())
            return new("GAME: waiting for Battle_Realms_F.exe", "Selected building: none\r\nLocal active training buildings: n/a", "READ-ONLY | no process handle with write access");

        uint localId = R32(moduleBase + RVA_LOCAL_ID);
        uint a = R32(moduleBase + RVA_SELECTED_BUILDING_A);
        uint b = R32(moduleBase + RVA_SELECTED_BUILDING_B);
        long pool = ResolveBuildingPool(out string poolMode);
        int active = CountLocalActiveTraining(pool, localId);

        string game = $"GAME: attached • PID {process!.Id} • local player {localId} • READ-ONLY";
        var sb = new StringBuilder();
        sb.Append("Selected A: ").Append(BuildingSummary(a, localId)).Append("\r\n");
        if (b != 0 && b != a) sb.Append("Selected B: ").Append(BuildingSummary(b, localId)).Append("\r\n");
        sb.Append("Local active training buildings: ").Append(active < 0 ? "unresolved" : active.ToString());

        string raw = $"base:0x{moduleBase:X8} | pool:{poolMode} 0x{pool:X8} | selA:0x{a:X8} selB:0x{b:X8}\r\n" +
                     "Known observer fields: owner +0x84 | trainType +0x488 | progress +0x490 | gate +0x4B8 | complete 0x640000";
        return new(game, sb.ToString(), raw);
    }
}
