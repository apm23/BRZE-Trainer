using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEUnitChangerLabV1;

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
    readonly Label selection = new();
    readonly Label recipe = new();
    readonly Label trace = new();
    readonly Label hint = new();
    readonly Button resetTrace = new();
    readonly Button refresh = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 250 };

    public MainForm()
    {
        Text = "BRZE Unit Changer Lab v1 — Eligibility Trace";
        ClientSize = new Size(1030, 760);
        MinimumSize = new Size(980, 720);
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
            RowCount = 6
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        Controls.Add(root);

        var header = Card();
        header.Controls.Add(new Label
        {
            Text = "UNIT CHANGER // ELIGIBILITY TRACE",
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 10)
        });
        header.Controls.Add(new Label
        {
            Text = "Separate lab • passive native-map tracer • no eligibility override yet",
            ForeColor = Color.FromArgb(157, 178, 202),
            AutoSize = true,
            Location = new Point(18, 43)
        });
        root.Controls.Add(header, 0, 0);

        var top = Card();
        game.AutoSize = true;
        game.ForeColor = Color.FromArgb(97, 224, 179);
        game.Location = new Point(14, 13);
        top.Controls.Add(game);
        root.Controls.Add(top, 0, 1);

        var selCard = Card();
        selCard.Controls.Add(Title("CURRENT CONTEXT", 12));
        selection.AutoSize = false;
        selection.Font = new Font(FontFamily.GenericMonospace, 9.3f);
        selection.Location = new Point(14, 40);
        selection.Size = new Size(970, 68);
        selCard.Controls.Add(selection);
        root.Controls.Add(selCard, 0, 2);

        var recipeCard = Card();
        recipeCard.Controls.Add(Title("SELECTED BUILDING — NATIVE TRAINING RECIPES", 12));
        recipe.AutoSize = false;
        recipe.Font = new Font(FontFamily.GenericMonospace, 9.2f);
        recipe.Location = new Point(14, 40);
        recipe.Size = new Size(970, 210);
        recipe.ForeColor = Color.WhiteSmoke;
        recipeCard.Controls.Add(recipe);
        root.Controls.Add(recipeCard, 0, 3);

        var traceCard = Card();
        traceCard.Controls.Add(Title("0x4D69F6 PASSIVE TRACE", 12));
        trace.AutoSize = false;
        trace.Font = new Font(FontFamily.GenericMonospace, 9.2f);
        trace.Location = new Point(14, 40);
        trace.Size = new Size(970, 120);
        trace.ForeColor = Color.WhiteSmoke;
        hint.AutoSize = false;
        hint.Location = new Point(14, 162);
        hint.Size = new Size(970, 54);
        hint.ForeColor = Color.FromArgb(236, 183, 84);
        hint.Text = "TEST: click RESET TRACE → order a normal trainable unit into the building → note counters.\r\nThen RESET TRACE → try the already-trained unit that gives the red X → screenshot this panel.";
        traceCard.Controls.Add(trace);
        traceCard.Controls.Add(hint);
        root.Controls.Add(traceCard, 0, 4);

        var footer = Card();
        refresh.Text = "REFRESH ATTACH";
        refresh.Size = new Size(150, 32);
        refresh.Location = new Point(12, 10);
        StyleButton(refresh);
        refresh.Click += (_, _) => { NativeTrace.Reset(); Poll(); };

        resetTrace.Text = "RESET TRACE";
        resetTrace.Size = new Size(140, 32);
        resetTrace.Location = new Point(170, 10);
        StyleButton(resetTrace);
        resetTrace.Click += (_, _) => NativeTrace.ResetCounters();

        var safety = new Label
        {
            Text = "TRACE ONLY • stock result preserved • hook restored on close",
            AutoSize = true,
            ForeColor = Color.FromArgb(236, 183, 84),
            Location = new Point(570, 18)
        };
        footer.Controls.Add(refresh);
        footer.Controls.Add(resetTrace);
        footer.Controls.Add(safety);
        root.Controls.Add(footer, 0, 5);

        timer.Tick += (_, _) => Poll();
        timer.Start();
        FormClosed += (_, _) => NativeTrace.Reset();
        Poll();
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

    void Poll()
    {
        var s = NativeTrace.Snapshot();
        game.Text = s.Game;
        selection.Text = s.Selection;
        recipe.Text = s.Recipes;
        trace.Text = s.Trace;
    }
}

internal readonly record struct Snapshot(string Game, string Selection, string Recipes, string Trace);

internal static class NativeTrace
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
    const int RVA_CALLER_A = 0x131A55; // return from call at preferred 0x531A50
    const int RVA_CALLER_B = 0x15545E; // return from call at preferred 0x555459

    const int OFF_UNIT_DEF = 0x74;
    const int OFF_UNIT_OWNER = 0x240;
    const int OFF_BUILD_OWNER = 0x84;
    const int OFF_BUILD_TYPE = 0x78;
    const int OFF_BUILD_DEF = 0x258;
    const int OFF_TRAIN_TYPE = 0x488;
    const int OFF_TRAIN_PROGRESS = 0x490;

    static readonly byte[] TrainMapOriginal = { 0x55, 0x8B, 0xEC, 0x8B, 0x81, 0x58, 0x02, 0x00, 0x00 };

    // cave telemetry layout
    const int T_TOTAL = 0;
    const int T_LAST_CALLER = 4;
    const int T_LAST_BUILDING = 8;
    const int T_LAST_INPUT = 12;
    const int T_LAST_DEF = 16;
    const int T_CALLER_A = 20;
    const int T_CALLER_B = 24;
    const int T_OTHER = 28;
    const int STUB_OFFSET = 0x80;

    static readonly object gate = new();
    static Process? process;
    static IntPtr h;
    static long moduleBase;
    static IntPtr cave;
    static bool hookInstalled;
    static string hookStatus = "not attached";
    static DateTime lastTry;

    public static void Reset()
    {
        lock (gate) ResetUnlocked();
    }

    static void ResetUnlocked()
    {
        try
        {
            if (h != IntPtr.Zero && hookInstalled)
            {
                long target = moduleBase + RVA_TRAIN_MAP;
                var now = new byte[TrainMapOriginal.Length];
                if (ReadExact(target, now) && now.Length >= 5 && now[0] == 0xE9)
                    WriteCode(target, TrainMapOriginal);
            }
            if (h != IntPtr.Zero && cave != IntPtr.Zero)
                VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE);
            if (h != IntPtr.Zero) CloseHandle(h);
        }
        catch { }
        h = IntPtr.Zero;
        cave = IntPtr.Zero;
        process = null;
        moduleBase = 0;
        hookInstalled = false;
        hookStatus = "not attached";
        lastTry = DateTime.MinValue;
    }

    static bool EnsureAttached()
    {
        lock (gate)
        {
            try
            {
                if (process != null && !process.HasExited && h != IntPtr.Zero)
                {
                    if (!hookInstalled) InstallTraceHook();
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
            InstallTraceHook();
            return true;
        }
    }

    static bool ReadExact(long address, byte[] buffer)
    {
        return h != IntPtr.Zero && ReadProcessMemory(h, new IntPtr(unchecked((int)(uint)address)), buffer, buffer.Length, out var n) && n.ToInt64() == buffer.Length;
    }
    static uint R32(long address)
    {
        var b = new byte[4];
        return ReadExact(address, b) ? BitConverter.ToUInt32(b, 0) : 0;
    }
    static bool WriteBytes(long address, byte[] data)
    {
        return h != IntPtr.Zero && WriteProcessMemory(h, new IntPtr(unchecked((int)(uint)address)), data, data.Length, out var n) && n.ToInt64() == data.Length;
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

    static byte[] BuildTraceStub(long stub, long telemetry, long target)
    {
        long total = telemetry + T_TOTAL;
        long lastCaller = telemetry + T_LAST_CALLER;
        long lastBuilding = telemetry + T_LAST_BUILDING;
        long lastInput = telemetry + T_LAST_INPUT;
        long lastDef = telemetry + T_LAST_DEF;
        long ca = telemetry + T_CALLER_A;
        long cb = telemetry + T_CALLER_B;
        long other = telemetry + T_OTHER;
        uint callerA = (uint)(moduleBase + RVA_CALLER_A);
        uint callerB = (uint)(moduleBase + RVA_CALLER_B);

        var b = new List<byte>();
        b.Add(0x50); // push eax
        b.Add(0x52); // push edx
        b.AddRange(new byte[] { 0x8B, 0x44, 0x24, 0x08 }); // mov eax,[esp+8] return address
        b.Add(0xA3); U32(b, (uint)lastCaller);               // mov [lastCaller],eax
        b.AddRange(new byte[] { 0x89, 0x0D }); U32(b, (uint)lastBuilding); // mov [lastBuilding],ecx
        b.AddRange(new byte[] { 0x8B, 0x54, 0x24, 0x0C }); // mov edx,[esp+0C] input type
        b.AddRange(new byte[] { 0x89, 0x15 }); U32(b, (uint)lastInput);
        b.AddRange(new byte[] { 0x8B, 0x91, 0x58, 0x02, 0x00, 0x00 }); // mov edx,[ecx+258]
        b.AddRange(new byte[] { 0x89, 0x15 }); U32(b, (uint)lastDef);
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)total); // inc total

        b.Add(0x3D); U32(b, callerA); // cmp eax, callerA
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jneA = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)ca);
        b.Add(0xE9); int jDoneA = b.Count; I32(b, 0);

        int checkB = b.Count;
        b.Add(0x3D); U32(b, callerB);
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jneB = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)cb);
        b.Add(0xE9); int jDoneB = b.Count; I32(b, 0);

        int otherLabel = b.Count;
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)other);
        int done = b.Count;
        b.Add(0x5A); // pop edx
        b.Add(0x58); // pop eax
        b.AddRange(TrainMapOriginal); // exact displaced stock prologue
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);

        PatchRel(b, jneA, stub + jneA + 4, stub + checkB);
        PatchRel(b, jDoneA, stub + jDoneA + 4, stub + done);
        PatchRel(b, jneB, stub + jneB + 4, stub + otherLabel);
        PatchRel(b, jDoneB, stub + jDoneB + 4, stub + done);
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

    static void InstallTraceHook()
    {
        if (hookInstalled || h == IntPtr.Zero) return;
        long target = moduleBase + RVA_TRAIN_MAP;
        var stock = new byte[TrainMapOriginal.Length];
        if (!ReadExact(target, stock) || !stock.SequenceEqual(TrainMapOriginal))
        {
            hookStatus = "HOOK NOT INSTALLED — stock bytes mismatch";
            return;
        }

        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)512, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (cave == IntPtr.Zero)
        {
            hookStatus = "HOOK NOT INSTALLED — cave alloc failed";
            return;
        }
        long telemetry = cave.ToInt64();
        long stub = telemetry + STUB_OFFSET;
        WriteBytes(telemetry, new byte[64]);
        var code = BuildTraceStub(stub, telemetry, target);
        if (!WriteBytes(stub, code) || !WriteCode(target, JmpPatch(target, stub, TrainMapOriginal.Length)))
        {
            hookStatus = "HOOK NOT INSTALLED — write failed";
            VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE);
            cave = IntPtr.Zero;
            return;
        }
        hookInstalled = true;
        hookStatus = "TRACE HOOK ACTIVE — behavior unchanged";
    }

    public static void ResetCounters()
    {
        lock (gate)
        {
            if (EnsureAttached() && cave != IntPtr.Zero)
                WriteBytes(cave.ToInt64(), new byte[32]);
        }
    }

    static uint FirstSelectedUnit()
    {
        long list = moduleBase + RVA_SELECTION_LIST;
        uint count = R32(list + 0x18);
        uint node = R32(list);
        if (count == 0 || node == 0) return 0;
        return R32((long)node + 8);
    }

    static string UnitContext(uint unit, uint localId)
    {
        if (unit == 0) return "Selected unit: none";
        uint def = R32((long)unit + OFF_UNIT_DEF);
        uint type = def == 0 ? 0xFFFFFFFFu : R32(def);
        uint owner = R32((long)unit + OFF_UNIT_OWNER);
        return $"Selected unit: 0x{unit:X8} owner:{owner} {(owner == localId ? "LOCAL" : "")} def:0x{def:X8} type:0x{type:X}";
    }

    static string BuildingContext(uint b, uint localId)
    {
        if (b == 0) return "Selected building: none";
        uint owner = R32((long)b + OFF_BUILD_OWNER);
        uint type = R32((long)b + OFF_BUILD_TYPE);
        uint def = R32((long)b + OFF_BUILD_DEF);
        uint train = R32((long)b + OFF_TRAIN_TYPE);
        uint prog = R32((long)b + OFF_TRAIN_PROGRESS);
        return $"Selected building: 0x{b:X8} owner:{owner} {(owner == localId ? "LOCAL" : "")} type:0x{type:X} def:0x{def:X8}\r\ntrainType:0x{train:X8} progress:0x{prog:X8}";
    }

    static string RecipeTable(uint building)
    {
        if (building == 0) return "No selected building.";
        uint def = R32((long)building + OFF_BUILD_DEF);
        if (def == 0) return "BuildingDef unresolved.";
        int[] baseOff = { 0x68, 0x78, 0x88, 0x98, 0xA8, 0xB8 };
        var sb = new StringBuilder();
        sb.AppendLine($"BuildingDef 0x{def:X8} — 6 native UnitIn → UnitOut entries");
        sb.AppendLine("SLOT   UNIT IN       UNIT OUT      RATE          TIME");
        for (int i = 0; i < 6; i++)
        {
            int o = baseOff[i];
            uint input = R32((long)def + o);
            uint output = R32((long)def + o + 4);
            uint rate = R32((long)def + o + 8);
            uint time = R32((long)def + o + 12);
            bool empty = input == 0xFFFFFFFFu || input == 0;
            sb.AppendLine($" {i + 1}     0x{input:X8}   0x{output:X8}   0x{rate:X8}   0x{time:X8}{(empty ? "   <EMPTY?>" : "")}");
        }
        return sb.ToString();
    }

    static string TraceSnapshot()
    {
        if (!hookInstalled || cave == IntPtr.Zero) return hookStatus;
        long t = cave.ToInt64();
        uint total = R32(t + T_TOTAL);
        uint lastCaller = R32(t + T_LAST_CALLER);
        uint lastBuilding = R32(t + T_LAST_BUILDING);
        uint lastInput = R32(t + T_LAST_INPUT);
        uint lastDef = R32(t + T_LAST_DEF);
        uint a = R32(t + T_CALLER_A);
        uint b = R32(t + T_CALLER_B);
        uint other = R32(t + T_OTHER);
        string callerTag = lastCaller == moduleBase + RVA_CALLER_A ? "candidate A / 0x531A50" :
                           lastCaller == moduleBase + RVA_CALLER_B ? "candidate B / 0x555459" : "other";
        return $"{hookStatus}\r\n" +
               $"calls total:{total} | candidate-A:{a} | candidate-B:{b} | other:{other}\r\n" +
               $"last caller:0x{lastCaller:X8} ({callerTag}) | building:0x{lastBuilding:X8} | inputType:0x{lastInput:X8} | def:0x{lastDef:X8}";
    }

    public static Snapshot Snapshot()
    {
        if (!EnsureAttached())
            return new("GAME: waiting for Battle_Realms_F.exe", "Selected unit: none\r\nSelected building: none", "No selected building.", hookStatus);

        uint localId = R32(moduleBase + RVA_LOCAL_ID);
        uint unit = FirstSelectedUnit();
        uint ba = R32(moduleBase + RVA_SELECTED_BUILDING_A);
        uint bb = R32(moduleBase + RVA_SELECTED_BUILDING_B);
        uint building = ba != 0 ? ba : bb;

        string g = $"GAME: attached • PID {process!.Id} • base 0x{moduleBase:X8} • local player {localId}";
        string s = UnitContext(unit, localId) + "\r\n" + BuildingContext(building, localId);
        return new(g, s, RecipeTable(building), TraceSnapshot());
    }
}
