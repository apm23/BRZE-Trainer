using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEUnitChangerLabV2;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class UnitChoice
{
    public uint Type { get; }
    public string Name { get; }
    public UnitChoice(uint type, string name) { Type = type; Name = name; }
    public override string ToString() => $"0x{Type:X2}  {Name}";
}

internal sealed class MainForm : Form
{
    readonly CheckBox master = new() { Text = "OUTPUT OVERRIDE" };
    readonly CheckBox slot1 = new() { Text = "ON" };
    readonly ComboBox output1 = new();
    readonly Label game = new();
    readonly Label context = new();
    readonly Label monitor = new();
    readonly Label nativeRecipes = new();
    readonly Button refresh = new();
    readonly Button resetCount = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 250 };

    static readonly UnitChoice[] ProofUnits =
    {
        new(0, "Dragon Archer"),
        new(1, "Dragon Chemist"),
        new(2, "Dragon Dragon Warrior"),
        new(3, "Dragon Geisha"),
        new(4, "Dragon Kabuki Warrior"),
        new(5, "Dragon Peasant"),
        new(6, "Dragon Powder Keg Cannoneer"),
        new(7, "Dragon Samurai"),
        new(8, "Dragon Spearman"),

        new(19, "Serpent Bandit"),
        new(20, "Serpent Cannoneer"),
        new(21, "Serpent Crossbowman"),
        new(22, "Serpent Fan Geisha"),
        new(23, "Serpent Musketeer"),
        new(24, "Serpent Peasant"),
        new(25, "Serpent Raider"),
        new(26, "Serpent Ronin"),
        new(29, "Serpent Swordsman"),

        new(30, "Lotus Blade Acolyte"),
        new(34, "Lotus Channeler"),
        new(35, "Lotus Diseased One"),
        new(37, "Lotus Infested One"),
        new(38, "Lotus Leaf Disciple"),
        new(39, "Lotus Master Warlock"),
        new(40, "Lotus Peasant"),
        new(41, "Lotus Staff Adept"),
        new(42, "Lotus Unclean One"),
        new(43, "Lotus Warlock"),

        new(44, "Wolf Ballistaman"),
        new(45, "Wolf Berserker"),
        new(46, "Wolf Brawler"),
        new(47, "Wolf Druidess"),
        new(48, "Wolf Hurler"),
        new(49, "Wolf Mauler"),
        new(50, "Wolf Pack Master"),
        new(51, "Wolf Peasant"),
        new(52, "Wolf Pitch Slinger"),
        new(53, "Wolf Sledger"),
        new(54, "Wolf Werewolf")
    };

    public MainForm()
    {
        Text = "BRZE Unit Changer Lab v2 — Single Output Proof";
        ClientSize = new Size(1080, 820);
        MinimumSize = new Size(1030, 780);
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
            RowCount = 7,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 165));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        Controls.Add(root);

        var header = Card();
        header.Controls.Add(new Label
        {
            Text = "UNIT CHANGER // SINGLE OUTPUT PROOF",
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 10)
        });
        header.Controls.Add(new Label
        {
            Text = "Native-valid input only • 1 training → 1 overridden output • heroes/unique units locked for proof",
            ForeColor = Color.FromArgb(157, 178, 202),
            AutoSize = true,
            Location = new Point(18, 44)
        });
        root.Controls.Add(header, 0, 0);

        var top = Card();
        game.AutoSize = true;
        game.ForeColor = Color.FromArgb(97, 224, 179);
        game.Location = new Point(14, 16);
        top.Controls.Add(game);
        root.Controls.Add(top, 0, 1);

        var outputs = Card();
        outputs.Controls.Add(Title("OUTPUT SLOTS 1–9", 12));
        outputs.Controls.Add(new Label
        {
            Text = "V2 unlocks Slot 1 only. Slots 2–9 stay hard-locked until 1→1 is runtime-proven.",
            AutoSize = true,
            ForeColor = Color.FromArgb(157, 178, 202),
            Location = new Point(14, 38)
        });

        master.AutoSize = true;
        master.Location = new Point(780, 12);
        master.ForeColor = Color.FromArgb(97, 224, 179);
        master.CheckedChanged += (_, _) => PushConfiguration();
        outputs.Controls.Add(master);

        AddSlotRow(outputs, 1, 68, true);
        for (int i = 2; i <= 9; i++) AddSlotRow(outputs, i, 68 + (i - 1) * 24, false);
        root.Controls.Add(outputs, 0, 2);

        var ctx = Card();
        ctx.Controls.Add(Title("CURRENT TRAINING CONTEXT", 10));
        context.AutoSize = false;
        context.Font = new Font(FontFamily.GenericMonospace, 9.2f);
        context.Location = new Point(14, 36);
        context.Size = new Size(1010, 70);
        context.ForeColor = Color.WhiteSmoke;
        ctx.Controls.Add(context);
        root.Controls.Add(ctx, 0, 3);

        var mon = Card();
        mon.Controls.Add(Title("OVERRIDE MONITOR", 10));
        monitor.AutoSize = false;
        monitor.Font = new Font(FontFamily.GenericMonospace, 9.2f);
        monitor.Location = new Point(14, 38);
        monitor.Size = new Size(1010, 120);
        monitor.ForeColor = Color.WhiteSmoke;
        mon.Controls.Add(monitor);
        root.Controls.Add(mon, 0, 4);

        var recipes = Card();
        recipes.Controls.Add(Title("SELECTED BUILDING — NATIVE RECIPES", 10));
        nativeRecipes.AutoSize = false;
        nativeRecipes.Font = new Font(FontFamily.GenericMonospace, 8.7f);
        nativeRecipes.Location = new Point(14, 36);
        nativeRecipes.Size = new Size(1010, 100);
        nativeRecipes.ForeColor = Color.FromArgb(170, 181, 197);
        recipes.Controls.Add(nativeRecipes);
        root.Controls.Add(recipes, 0, 5);

        var footer = Card();
        refresh.Text = "REFRESH ATTACH";
        refresh.Size = new Size(150, 32);
        refresh.Location = new Point(12, 10);
        StyleButton(refresh);
        refresh.Click += (_, _) => { master.Checked = false; OverrideCore.Reset(); Poll(); };

        resetCount.Text = "RESET COUNT";
        resetCount.Size = new Size(140, 32);
        resetCount.Location = new Point(170, 10);
        StyleButton(resetCount);
        resetCount.Click += (_, _) => OverrideCore.ResetCounter();

        var safety = new Label
        {
            Text = "V2 SAFETY • invalid inputs stay native • local buildings only • stock hook restored on close",
            AutoSize = true,
            ForeColor = Color.FromArgb(236, 183, 84),
            Location = new Point(405, 18)
        };
        footer.Controls.Add(refresh);
        footer.Controls.Add(resetCount);
        footer.Controls.Add(safety);
        root.Controls.Add(footer, 0, 6);

        output1.DropDownStyle = ComboBoxStyle.DropDownList;
        output1.Items.AddRange(ProofUnits.Cast<object>().ToArray());
        output1.SelectedIndex = 0;
        output1.SelectedIndexChanged += (_, _) => PushConfiguration();
        slot1.CheckedChanged += (_, _) => PushConfiguration();

        timer.Tick += (_, _) => Poll();
        timer.Start();
        FormClosed += (_, _) => OverrideCore.Reset();
        Poll();
    }

    void AddSlotRow(Panel parent, int index, int y, bool unlocked)
    {
        var n = new Label
        {
            Text = $"SLOT {index}",
            AutoSize = false,
            Size = new Size(70, 22),
            Location = new Point(18, y),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.WhiteSmoke
        };
        parent.Controls.Add(n);

        if (unlocked)
        {
            slot1.Location = new Point(95, y + 1);
            slot1.Size = new Size(52, 22);
            slot1.ForeColor = Color.WhiteSmoke;
            output1.Location = new Point(155, y - 1);
            output1.Size = new Size(835, 24);
            output1.BackColor = Color.FromArgb(35, 45, 60);
            output1.ForeColor = Color.White;
            parent.Controls.Add(slot1);
            parent.Controls.Add(output1);
        }
        else
        {
            var off = new Label
            {
                Text = "LOCKED",
                AutoSize = false,
                Size = new Size(64, 22),
                Location = new Point(95, y),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(116, 127, 143)
            };
            var locked = new TextBox
            {
                Text = "Unlock after V2 1→1 runtime PASS",
                ReadOnly = true,
                Enabled = false,
                Location = new Point(155, y - 1),
                Size = new Size(835, 24)
            };
            parent.Controls.Add(off);
            parent.Controls.Add(locked);
        }
    }

    void PushConfiguration()
    {
        if (output1.SelectedItem is not UnitChoice c) return;
        bool enabled = master.Checked && slot1.Checked;
        OverrideCore.Configure(enabled, c.Type);
    }

    void Poll()
    {
        PushConfiguration();
        var s = OverrideCore.Snapshot();
        game.Text = s.Game;
        context.Text = s.Context;
        monitor.Text = s.Monitor;
        nativeRecipes.Text = s.Recipes;
    }

    static Panel Card() => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 24, 34), Padding = new Padding(10) };
    static Label Title(string text, int y) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 11f),
        ForeColor = Color.FromArgb(97, 224, 179),
        AutoSize = true,
        Location = new Point(14, y)
    };
    static void StyleButton(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(70, 90, 115);
        b.BackColor = Color.FromArgb(42, 55, 72);
        b.ForeColor = Color.White;
    }
}

internal readonly record struct CoreSnapshot(string Game, string Context, string Monitor, string Recipes);

internal static class OverrideCore
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualFreeEx(IntPtr h, IntPtr addr, UIntPtr size, uint freeType);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x0010 | 0x0020 | 0x0008 | 0x0400;
    const uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, MEM_RELEASE = 0x8000, PAGE_EXECUTE_READWRITE = 0x40;

    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_SELECTION_LIST = 0x441708;
    const int RVA_SELECTED_BUILDING_A = 0x4417D4;
    const int RVA_SELECTED_BUILDING_B = 0x4417D8;
    const int RVA_TRAIN_MAP = 0x0D69F6;

    const int OFF_UNIT_DEF = 0x74;
    const int OFF_UNIT_OWNER = 0x240;
    const int OFF_BUILD_OWNER = 0x84;
    const int OFF_BUILD_TYPE = 0x78;
    const int OFF_BUILD_DEF = 0x258;
    const int OFF_TRAIN_TYPE = 0x488;
    const int OFF_TRAIN_PROGRESS = 0x490;

    // 004D69F6: 55 8B EC 8B 81 58 02 00 00
    // ... epilogue at 004D6A59: 5D C2 04 00 (pop ebp; ret 4)
    static readonly byte[] TrainMapOriginal = { 0x55, 0x8B, 0xEC, 0x8B, 0x81, 0x58, 0x02, 0x00, 0x00 };
    static readonly int[] UnitInOffsets = { 0x68, 0x78, 0x88, 0x98, 0xA8, 0xB8 };
    static readonly int[] UnitOutOffsets = { 0x6C, 0x7C, 0x8C, 0x9C, 0xAC, 0xBC };

    const int CFG_ENABLED = 0x00;
    const int CFG_OUTPUT = 0x04;
    const int T_OVERRIDE_COUNT = 0x08;
    const int T_LAST_BUILDING = 0x0C;
    const int T_LAST_INPUT = 0x10;
    const int T_LAST_DEF = 0x14;
    const int STUB_OFFSET = 0x80;

    static readonly object sync = new();
    static Process? process;
    static IntPtr h;
    static long moduleBase;
    static IntPtr cave;
    static bool hookInstalled;
    static byte[]? installedPatch;
    static DateTime lastTry;
    static string hookStatus = "not attached";
    static bool wantedEnabled;
    static uint wantedOutput;

    public static void Configure(bool enabled, uint output)
    {
        lock (sync)
        {
            wantedEnabled = enabled;
            wantedOutput = output;
            if (!EnsureAttached()) return;
            if (!hookInstalled || cave == IntPtr.Zero) return;
            W32(cave.ToInt64() + CFG_OUTPUT, output);
            W32(cave.ToInt64() + CFG_ENABLED, enabled ? 1u : 0u);
        }
    }

    public static void ResetCounter()
    {
        lock (sync)
        {
            if (!EnsureAttached() || cave == IntPtr.Zero) return;
            W32(cave.ToInt64() + T_OVERRIDE_COUNT, 0);
            W32(cave.ToInt64() + T_LAST_BUILDING, 0);
            W32(cave.ToInt64() + T_LAST_INPUT, 0xFFFFFFFF);
            W32(cave.ToInt64() + T_LAST_DEF, 0);
        }
    }

    public static void Reset()
    {
        lock (sync) ResetUnlocked();
    }

    static void ResetUnlocked()
    {
        try
        {
            if (h != IntPtr.Zero && hookInstalled && installedPatch != null)
            {
                long target = moduleBase + RVA_TRAIN_MAP;
                var now = new byte[installedPatch.Length];
                if (ReadExact(target, now) && now.SequenceEqual(installedPatch))
                    WriteCode(target, TrainMapOriginal);
            }
            if (h != IntPtr.Zero && cave != IntPtr.Zero)
                VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE);
            if (h != IntPtr.Zero) CloseHandle(h);
        }
        catch { }
        process = null;
        h = IntPtr.Zero;
        moduleBase = 0;
        cave = IntPtr.Zero;
        hookInstalled = false;
        installedPatch = null;
        hookStatus = "not attached";
        lastTry = DateTime.MinValue;
    }

    static bool EnsureAttached()
    {
        try
        {
            if (process != null && !process.HasExited && h != IntPtr.Zero)
            {
                if (!hookInstalled) InstallHook();
                return true;
            }
        }
        catch { }

        ResetUnlocked();
        if ((DateTime.UtcNow - lastTry).TotalMilliseconds < 400) return false;
        lastTry = DateTime.UtcNow;
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;
        process = ps[0];
        try { moduleBase = process.MainModule!.BaseAddress.ToInt64(); }
        catch { process = null; return false; }
        h = OpenProcess(ACCESS, false, process.Id);
        if (h == IntPtr.Zero) return false;
        InstallHook();
        return true;
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

    static bool WriteBytes(long address, byte[] data)
    {
        return h != IntPtr.Zero &&
               WriteProcessMemory(h, new IntPtr(unchecked((int)(uint)address)), data, data.Length, out var n) &&
               n.ToInt64() == data.Length;
    }
    static bool W32(long address, uint value) => WriteBytes(address, BitConverter.GetBytes(value));

    static bool WriteCode(long address, byte[] data)
    {
        if (h == IntPtr.Zero) return false;
        if (!VirtualProtectEx(h, new IntPtr(address), (UIntPtr)data.Length, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteBytes(address, data);
        FlushInstructionCache(h, new IntPtr(address), (UIntPtr)data.Length);
        VirtualProtectEx(h, new IntPtr(address), (UIntPtr)data.Length, old, out _);
        return ok;
    }

    static void U32(List<byte> b, uint v) => b.AddRange(BitConverter.GetBytes(v));
    static void I32(List<byte> b, int v) => b.AddRange(BitConverter.GetBytes(v));
    static void PatchRel(List<byte> b, int at, long fromNext, long to)
    {
        var x = BitConverter.GetBytes(unchecked((int)(to - fromNext)));
        for (int i = 0; i < 4; i++) b[at + i] = x[i];
    }

    static byte[] BuildOverrideStub(long stub, long cfg, long target)
    {
        long enabled = cfg + CFG_ENABLED;
        long output = cfg + CFG_OUTPUT;
        long count = cfg + T_OVERRIDE_COUNT;
        long lastBuilding = cfg + T_LAST_BUILDING;
        long lastInput = cfg + T_LAST_INPUT;
        long lastDef = cfg + T_LAST_DEF;

        var b = new List<byte>();

        // Disabled => preserve stock path exactly.
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)enabled); b.Add(0x00);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jDisabled = b.Count; I32(b, 0);

        // Local-player buildings only: eax = local player ID; cmp [ecx+84], eax.
        b.Add(0xA1); U32(b, (uint)(moduleBase + RVA_LOCAL_ID));
        b.AddRange(new byte[] { 0x39, 0x81, 0x84, 0x00, 0x00, 0x00 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jOwner = b.Count; I32(b, 0);

        // edx = BuildingDef*; reject null.
        b.AddRange(new byte[] { 0x8B, 0x91, 0x58, 0x02, 0x00, 0x00 });
        b.AddRange(new byte[] { 0x85, 0xD2 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jNull = b.Count; I32(b, 0);

        // eax = original input unit type from [esp+4].
        b.AddRange(new byte[] { 0x8B, 0x44, 0x24, 0x04 });

        // Match only an existing native UnitIn1..6. Invalid/red-X inputs fall back untouched.
        var matchJumps = new List<int>();
        foreach (int off in UnitInOffsets)
        {
            if (off <= 0x7F)
                b.AddRange(new byte[] { 0x3B, 0x42, (byte)off }); // cmp eax,[edx+off]
            else
            {
                b.AddRange(new byte[] { 0x3B, 0x82 }); I32(b, off); // cmp eax,[edx+off32]
            }
            b.AddRange(new byte[] { 0x0F, 0x84 }); int jm = b.Count; I32(b, 0); matchJumps.Add(jm);
        }
        b.Add(0xE9); int jNoMatch = b.Count; I32(b, 0);

        int matched = b.Count;
        // Telemetry. EAX currently input; EDX BuildingDef; ECX Building.
        b.AddRange(new byte[] { 0x89, 0x0D }); U32(b, (uint)lastBuilding);
        b.Add(0xA3); U32(b, (uint)lastInput);
        b.AddRange(new byte[] { 0x89, 0x15 }); U32(b, (uint)lastDef);
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)count);
        b.Add(0xA1); U32(b, (uint)output); // eax = configured output type
        b.AddRange(new byte[] { 0xC2, 0x04, 0x00 }); // exact native convention: ret 4

        int fallback = b.Count;
        b.AddRange(TrainMapOriginal);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);

        PatchRel(b, jDisabled, stub + jDisabled + 4, stub + fallback);
        PatchRel(b, jOwner, stub + jOwner + 4, stub + fallback);
        PatchRel(b, jNull, stub + jNull + 4, stub + fallback);
        foreach (int jm in matchJumps) PatchRel(b, jm, stub + jm + 4, stub + matched);
        PatchRel(b, jNoMatch, stub + jNoMatch + 4, stub + fallback);
        PatchRel(b, jBack, stub + jBack + 4, target + TrainMapOriginal.Length);
        return b.ToArray();
    }

    static byte[] JmpPatch(long target, long stub, int len)
    {
        var p = new byte[len];
        p[0] = 0xE9;
        Array.Copy(BitConverter.GetBytes(unchecked((int)(stub - (target + 5)))), 0, p, 1, 4);
        for (int i = 5; i < len; i++) p[i] = 0x90;
        return p;
    }

    static void InstallHook()
    {
        if (hookInstalled || h == IntPtr.Zero) return;
        long target = moduleBase + RVA_TRAIN_MAP;
        var stock = new byte[TrainMapOriginal.Length];
        if (!ReadExact(target, stock) || !stock.SequenceEqual(TrainMapOriginal))
        {
            hookStatus = "HOOK BLOCKED — 0x4D69F6 stock bytes mismatch (close other Unit Changer lab builds)";
            return;
        }

        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)1024, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (cave == IntPtr.Zero)
        {
            hookStatus = "HOOK BLOCKED — cave allocation failed";
            return;
        }

        long cfg = cave.ToInt64();
        long stub = cfg + STUB_OFFSET;
        WriteBytes(cfg, new byte[64]);
        W32(cfg + CFG_OUTPUT, wantedOutput);
        W32(cfg + CFG_ENABLED, wantedEnabled ? 1u : 0u);
        W32(cfg + T_LAST_INPUT, 0xFFFFFFFF);

        var code = BuildOverrideStub(stub, cfg, target);
        installedPatch = JmpPatch(target, stub, TrainMapOriginal.Length);
        if (!WriteBytes(stub, code) || !WriteCode(target, installedPatch))
        {
            hookStatus = "HOOK BLOCKED — cave/patch write failed";
            VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE);
            cave = IntPtr.Zero;
            installedPatch = null;
            return;
        }

        hookInstalled = true;
        hookStatus = "V2 HOOK ACTIVE — invalid inputs remain native";
    }

    static uint FirstSelectedUnit()
    {
        long list = moduleBase + RVA_SELECTION_LIST;
        uint count = R32(list + 0x18);
        uint node = R32(list);
        if (count == 0 || node == 0) return 0;
        return R32((long)node + 8);
    }

    static uint SelectedBuilding()
    {
        uint a = R32(moduleBase + RVA_SELECTED_BUILDING_A);
        if (a != 0) return a;
        return R32(moduleBase + RVA_SELECTED_BUILDING_B);
    }

    static uint UnitType(uint unit)
    {
        if (unit == 0) return 0xFFFFFFFF;
        uint def = R32((long)unit + OFF_UNIT_DEF);
        return def == 0 ? 0xFFFFFFFF : R32(def);
    }

    static uint NativeOutput(uint building, uint input)
    {
        if (building == 0 || input == 0xFFFFFFFF) return 0xFFFFFFFF;
        uint def = R32((long)building + OFF_BUILD_DEF);
        if (def == 0) return 0xFFFFFFFF;
        for (int i = 0; i < UnitInOffsets.Length; i++)
            if (R32((long)def + UnitInOffsets[i]) == input)
                return R32((long)def + UnitOutOffsets[i]);
        return 0xFFFFFFFF;
    }

    static string ContextText(uint localId)
    {
        uint unit = FirstSelectedUnit();
        uint building = SelectedBuilding();
        uint unitOwner = unit == 0 ? 0xFFFFFFFF : R32((long)unit + OFF_UNIT_OWNER);
        uint input = UnitType(unit);
        if (building == 0)
            return $"Selected unit: {(unit == 0 ? "none" : $"0x{unit:X8} type:0x{input:X} owner:{unitOwner}")}\r\nSelected building: none";

        uint owner = R32((long)building + OFF_BUILD_OWNER);
        uint type = R32((long)building + OFF_BUILD_TYPE);
        uint train = R32((long)building + OFF_TRAIN_TYPE);
        uint progress = R32((long)building + OFF_TRAIN_PROGRESS);
        uint normal = NativeOutput(building, input);
        return $"Selected unit: {(unit == 0 ? "none" : $"0x{unit:X8} type:0x{input:X} owner:{unitOwner}{(unitOwner == localId ? " LOCAL" : "")}")}\r\n" +
               $"Selected building: 0x{building:X8} type:0x{type:X} owner:{owner}{(owner == localId ? " LOCAL" : "")} | trainType:0x{train:X8} progress:0x{progress:X8} | selected-input nativeOut:0x{normal:X8}";
    }

    static string RecipesText(uint building)
    {
        if (building == 0) return "Select a training building to inspect its six native UnitIn → UnitOut recipes.";
        uint def = R32((long)building + OFF_BUILD_DEF);
        if (def == 0) return "BuildingDef unresolved.";
        var sb = new StringBuilder();
        for (int i = 0; i < 6; i++)
        {
            uint input = R32((long)def + UnitInOffsets[i]);
            uint output = R32((long)def + UnitOutOffsets[i]);
            sb.Append($"#{i + 1} 0x{input:X8}→0x{output:X8}");
            if (i != 5) sb.Append("    ");
            if (i == 2) sb.AppendLine();
        }
        return sb.ToString();
    }

    static string MonitorText()
    {
        if (!hookInstalled || cave == IntPtr.Zero)
            return hookStatus + "\r\nOverride is NOT active.";

        long cfg = cave.ToInt64();
        uint remoteEnabled = R32(cfg + CFG_ENABLED);
        uint output = R32(cfg + CFG_OUTPUT);
        uint count = R32(cfg + T_OVERRIDE_COUNT);
        uint building = R32(cfg + T_LAST_BUILDING);
        uint input = R32(cfg + T_LAST_INPUT);
        uint def = R32(cfg + T_LAST_DEF);
        uint native = NativeOutput(building, input);
        return $"{hookStatus}\r\n" +
               $"override: {(remoteEnabled != 0 ? "ON" : "OFF")} | configured output:0x{output:X8} | intercept count:{count}\r\n" +
               $"last valid local training map: building:0x{building:X8} def:0x{def:X8} input:0x{input:X8} nativeOut:0x{native:X8} → override:0x{output:X8}\r\n" +
               "Guard: only existing native UnitIn1..6 matches are overridden; red-X/invalid mappings are untouched.";
    }

    public static CoreSnapshot Snapshot()
    {
        lock (sync)
        {
            if (!EnsureAttached())
                return new("GAME: waiting for Battle_Realms_F.exe", "Selected unit: none\r\nSelected building: none", hookStatus, "No building selected.");

            if (hookInstalled && cave != IntPtr.Zero)
            {
                W32(cave.ToInt64() + CFG_OUTPUT, wantedOutput);
                W32(cave.ToInt64() + CFG_ENABLED, wantedEnabled ? 1u : 0u);
            }

            uint localId = R32(moduleBase + RVA_LOCAL_ID);
            uint building = SelectedBuilding();
            string g = $"GAME: attached • PID {process!.Id} • base 0x{moduleBase:X8} • local player {localId}";
            return new(g, ContextText(localId), MonitorText(), RecipesText(building));
        }
    }
}
