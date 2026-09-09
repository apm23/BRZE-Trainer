using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZESelection500FastPeasant;

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
    readonly CheckBox f4 = new()
    {
        Text = "F4 Population + Selection 500 + Safe Bulk Drag (restart BRZE to disable)",
        AutoSize = true
    };

    readonly CheckBox fastPeasant = new()
    {
        Text = "Fast Peasant Spawn — LOCAL PLAYER only, 20x faster (minimum 1.0 s)",
        AutoSize = true
    };

    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(980, 0),
        Text = "Diagnostic successor to the runtime-proven Selection120 + Headroom160 baseline. Selection 500 uses narrow call-site wrappers instead of global constructor edits; event headroom remains 160. Fast Peasant keeps native PeasantManager spawning but shortens only the local player's newly scheduled spawn interval. Enable F4 before selecting anything."
    };

    readonly Label status = new()
    {
        AutoSize = false,
        Dock = DockStyle.Bottom,
        Height = 210,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font(FontFamily.GenericMonospace, 9f)
    };

    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection 500 + Safe Bulk Drag + Fast Peasant Probe";
        ClientSize = new Size(1060, 430);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(18),
            WrapContents = false
        };
        panel.Controls.Add(f4);
        panel.Controls.Add(fastPeasant);
        panel.Controls.Add(note);
        Controls.Add(panel);
        Controls.Add(status);

        timer.Tick += (_, _) => TickTrainer();
        timer.Start();
        FormClosed += (_, _) => Native.Stop();
    }

    void TickTrainer()
    {
        bool down = (Native.GetAsyncKeyState(0x73) & 0x8000) != 0;
        if (down && !f4Held) f4.Checked = true;
        f4Held = down;

        status.Text = Native.Tick(f4.Checked, fastPeasant.Checked);
        if (Native.SelectionLatched) f4.Checked = true;
    }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr address, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);

    const uint ACCESS = 0x10 | 0x20 | 0x8 | 0x400;
    const uint PAGE_EXECUTE_READWRITE = 0x40;
    const uint MEM_COMMIT = 0x1000;
    const uint MEM_RESERVE = 0x2000;

    const uint SELECTION_LIMIT = 500;
    const uint POPULATION_LIMIT = 500;
    const uint LOGICAL_WINDOW = 160;
    const uint PEASANT_FACTOR = 20;
    const uint PEASANT_MIN_MS = 1000;

    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_MAX_UNITS = 0x467B90;
    const int RVA_ACTIVE = 0x441708;
    const int RVA_SORT_A = 0x441784;
    const int RVA_SORT_B = 0x4417AC;
    const int RVA_SIM_LISTS_PTR = 0x441730;
    const int SIM_STRIDE = 0x28;

    const int RVA_EVENT_USED = 0x441C94;
    const int RVA_EVENT_REMAIN = 0x441C98;
    const int RVA_EVENT_PTR = 0x441C9C;
    const int RVA_EVENT_GATE = 0x441194;
    const int RVA_NET = 0x440C28;
    const int RVA_INIT_REMAIN_IMM = 0x161770;
    const int RVA_RESET_REMAIN_IMM = 0x161D77;

    const int RVA_CAP_GATE = 0x1A7000;
    const int RVA_CAP_CONTINUE = 0x1A700D;
    const int RVA_CAP_REJECT = 0x1A70D5;

    const int RVA_LIST_RESET_FN = 0x0ABFE6;
    const int RVA_SIM_INIT_CALL = 0x1A6C57;
    const int RVA_SIM_RESET_CALL = 0x1A6E3B;
    const int RVA_SORT_A_CALL = 0x1A71B2;
    const int RVA_SORT_B_CALL = 0x1A71BF;
    const int RVA_SORT_ACTIVE_CALL = 0x1A729F;

    const int RVA_PEASANT_AUTO_SCHEDULE_CALL = 0x17FFA5;
    const int RVA_PEASANT_SCHEDULE_FN = 0x17FFB2;
    const int RVA_PEASANT_NEXT_PTR = 0x467AF0;
    const int RVA_PEASANT_CREATION = 0x467AF4;

    const int OFF_FREE_NODE = 0x08;
    const int OFF_COUNT = 0x18;
    const int OFF_BLOCK_COUNT = 0x1C;
    const int OFF_FIRST = 0x20;
    const int OFF_GROWTH = 0x24;

    static readonly byte[] EVENT_OLD_IMM = { 0x00, 0x01, 0x00, 0x00 };
    static readonly byte[] EVENT_NEW_IMM = { 0xA0, 0x00, 0x00, 0x00 };

    static IntPtr h;
    static Process? p;
    static long moduleBase;
    static int pid;
    static IntPtr cave;
    static long ctorWrapper;
    static long capWrapper;
    static long peasantWrapper;
    static long selectionFlag;
    static long peasantFlag;
    static bool infrastructureInstalled;
    static bool activeArmed;
    static bool simArmed;
    static bool headroomArmed;
    static bool selectionLatched;
    static string error = "";

    public static bool SelectionLatched => selectionLatched;

    static IntPtr A(long addr) => new(unchecked((int)(uint)addr));

    static bool Attach()
    {
        try { if (p != null && !p.HasExited && h != IntPtr.Zero) return true; } catch { }
        DetachHandleOnly();
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;
        p = ps[0]; pid = p.Id;
        try { moduleBase = p.MainModule!.BaseAddress.ToInt64(); } catch { p = null; return false; }
        h = OpenProcess(ACCESS, false, pid);
        return h != IntPtr.Zero;
    }

    static uint R32(long addr)
    {
        var b = new byte[4];
        return h != IntPtr.Zero && ReadProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4
            ? BitConverter.ToUInt32(b, 0) : 0u;
    }

    static byte[]? RBytes(long addr, int size)
    {
        var b = new byte[size];
        return h != IntPtr.Zero && ReadProcessMemory(h, A(addr), b, size, out var n) && n.ToInt64() == size ? b : null;
    }

    static bool Eq(byte[]? a, byte[] b)
    {
        if (a == null || a.Length != b.Length) return false;
        for (int i = 0; i < b.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static bool W32(long addr, uint value)
    {
        var b = BitConverter.GetBytes(value);
        return h != IntPtr.Zero && WriteProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4;
    }

    static bool WriteRaw(long addr, byte[] bytes)
    {
        return h != IntPtr.Zero && WriteProcessMemory(h, A(addr), bytes, bytes.Length, out var n) && n.ToInt64() == bytes.Length;
    }

    static bool WriteCode(long addr, byte[] bytes)
    {
        if (h == IntPtr.Zero) return false;
        var a = A(addr);
        if (!VirtualProtectEx(h, a, (UIntPtr)bytes.Length, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteProcessMemory(h, a, bytes, bytes.Length, out var n) && n.ToInt64() == bytes.Length;
        if (ok) FlushInstructionCache(h, a, (UIntPtr)bytes.Length);
        VirtualProtectEx(h, a, (UIntPtr)bytes.Length, old, out _);
        return ok;
    }

    static void U32(List<byte> b, uint value) => b.AddRange(BitConverter.GetBytes(value));
    static void I32(List<byte> b, int value) => b.AddRange(BitConverter.GetBytes(value));

    static void PatchRel32(List<byte> b, int at, long fromNext, long target)
    {
        var rel = BitConverter.GetBytes(unchecked((int)(target - fromNext)));
        for (int i = 0; i < 4; i++) b[at + i] = rel[i];
    }

    static byte[] BuildCall(long site, long target)
    {
        var b = new List<byte> { 0xE8 };
        I32(b, unchecked((int)(target - (site + 5))));
        return b.ToArray();
    }

    static byte[] BuildJmp(long site, long target, int totalLength)
    {
        var b = new List<byte> { 0xE9 };
        I32(b, unchecked((int)(target - (site + 5))));
        while (b.Count < totalLength) b.Add(0x90);
        return b.ToArray();
    }

    static long ReadCallTarget(long site)
    {
        var b = RBytes(site, 5);
        if (b == null || b[0] != 0xE8) return 0;
        int rel = BitConverter.ToInt32(b, 1);
        return site + 5 + rel;
    }

    static byte[] StockCapGateBytes()
    {
        var b = new List<byte> { 0x83, 0x3D };
        U32(b, unchecked((uint)(moduleBase + RVA_ACTIVE + OFF_COUNT)));
        b.Add(0x5A);
        b.AddRange(new byte[] { 0x0F, 0x84 });
        I32(b, unchecked((int)((moduleBase + RVA_CAP_REJECT) - (moduleBase + RVA_CAP_GATE + 13))));
        return b.ToArray();
    }

    static bool VerifyStockCall(int rva, int targetRva, string name)
    {
        long site = moduleBase + rva;
        long expected = moduleBase + targetRva;
        long actual = ReadCallTarget(site);
        if (actual != expected)
        {
            error = $"{name}: expected call 0x{expected:X8}, found 0x{actual:X8} — restart fresh BRZE";
            return false;
        }
        return true;
    }

    static byte[] BuildCtorWrapper(long stub)
    {
        var b = new List<byte>();
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, unchecked((uint)selectionFlag)); b.Add(0x00);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jStock = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xC7, 0x44, 0x24, 0x04 }); U32(b, SELECTION_LIMIT);
        int stock = b.Count;
        b.Add(0xE9); int jOriginal = b.Count; I32(b, 0);
        PatchRel32(b, jStock, stub + jStock + 4, stub + stock);
        PatchRel32(b, jOriginal, stub + jOriginal + 4, moduleBase + RVA_LIST_RESET_FN);
        return b.ToArray();
    }

    static byte[] BuildCapWrapper(long stub)
    {
        var b = new List<byte>();
        uint countAddr = unchecked((uint)(moduleBase + RVA_ACTIVE + OFF_COUNT));

        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, unchecked((uint)selectionFlag)); b.Add(0x00);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jStock = b.Count; I32(b, 0);

        b.AddRange(new byte[] { 0x81, 0x3D }); U32(b, countAddr); U32(b, SELECTION_LIMIT);
        b.AddRange(new byte[] { 0x0F, 0x83 }); int jReject500 = b.Count; I32(b, 0);
        b.Add(0xE9); int jContinue500 = b.Count; I32(b, 0);

        int stock = b.Count;
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, countAddr); b.Add(0x5A);
        b.AddRange(new byte[] { 0x0F, 0x83 }); int jReject90 = b.Count; I32(b, 0);
        b.Add(0xE9); int jContinue90 = b.Count; I32(b, 0);

        long reject = moduleBase + RVA_CAP_REJECT;
        long cont = moduleBase + RVA_CAP_CONTINUE;
        PatchRel32(b, jStock, stub + jStock + 4, stub + stock);
        PatchRel32(b, jReject500, stub + jReject500 + 4, reject);
        PatchRel32(b, jContinue500, stub + jContinue500 + 4, cont);
        PatchRel32(b, jReject90, stub + jReject90 + 4, reject);
        PatchRel32(b, jContinue90, stub + jContinue90 + 4, cont);
        return b.ToArray();
    }

    static byte[] BuildPeasantWrapper(long stub)
    {
        var b = new List<byte>();
        var doneJumps = new List<int>();

        b.Add(0x55);
        b.AddRange(new byte[] { 0x8B, 0xEC });
        b.Add(0x53); b.Add(0x56); b.Add(0x57);
        b.AddRange(new byte[] { 0x8B, 0x75, 0x08 });
        b.Add(0xBF); U32(b, 0xFFFFFFFF);
        b.Add(0xA1); U32(b, unchecked((uint)(moduleBase + RVA_PEASANT_NEXT_PTR)));
        b.AddRange(new byte[] { 0x85, 0xC0 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jNoPtr = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x8B, 0x3C, 0xB0 });
        int callOriginal = b.Count;

        b.AddRange(new byte[] { 0xFF, 0x75, 0x14 });
        b.AddRange(new byte[] { 0xFF, 0x75, 0x10 });
        b.AddRange(new byte[] { 0xFF, 0x75, 0x0C });
        b.AddRange(new byte[] { 0xFF, 0x75, 0x08 });
        b.Add(0xE8); int callRel = b.Count; I32(b, 0);

        b.AddRange(new byte[] { 0x83, 0xFF, 0xFF });
        b.AddRange(new byte[] { 0x0F, 0x84 }); doneJumps.Add(b.Count); I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, unchecked((uint)peasantFlag)); b.Add(0x00);
        b.AddRange(new byte[] { 0x0F, 0x84 }); doneJumps.Add(b.Count); I32(b, 0);
        b.Add(0xA1); U32(b, unchecked((uint)(moduleBase + RVA_LOCAL_ID)));
        b.AddRange(new byte[] { 0x3B, 0xF0 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); doneJumps.Add(b.Count); I32(b, 0);

        b.Add(0xA1); U32(b, unchecked((uint)(moduleBase + RVA_PEASANT_NEXT_PTR)));
        b.AddRange(new byte[] { 0x85, 0xC0 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); doneJumps.Add(b.Count); I32(b, 0);
        b.AddRange(new byte[] { 0x8B, 0x1C, 0xB0 });
        b.AddRange(new byte[] { 0x3B, 0xDF });
        b.AddRange(new byte[] { 0x0F, 0x86 }); doneJumps.Add(b.Count); I32(b, 0);
        b.AddRange(new byte[] { 0x2B, 0xDF });
        b.AddRange(new byte[] { 0x8B, 0xC3 });
        b.AddRange(new byte[] { 0x33, 0xD2 });
        b.Add(0xB9); U32(b, PEASANT_FACTOR);
        b.AddRange(new byte[] { 0xF7, 0xF1 });
        b.Add(0x3D); U32(b, PEASANT_MIN_MS);
        b.AddRange(new byte[] { 0x0F, 0x83 }); int jHaveMin = b.Count; I32(b, 0);
        b.Add(0xB8); U32(b, PEASANT_MIN_MS);
        int haveMin = b.Count;
        b.AddRange(new byte[] { 0x03, 0xC7 });
        b.AddRange(new byte[] { 0x8B, 0x0D }); U32(b, unchecked((uint)(moduleBase + RVA_PEASANT_NEXT_PTR)));
        b.AddRange(new byte[] { 0x85, 0xC9 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); doneJumps.Add(b.Count); I32(b, 0);
        b.AddRange(new byte[] { 0x89, 0x04, 0xB1 });

        int done = b.Count;
        b.Add(0x5F); b.Add(0x5E); b.Add(0x5B); b.Add(0x5D);
        b.AddRange(new byte[] { 0xC2, 0x10, 0x00 });

        PatchRel32(b, jNoPtr, stub + jNoPtr + 4, stub + callOriginal);
        PatchRel32(b, callRel, stub + callRel + 4, moduleBase + RVA_PEASANT_SCHEDULE_FN);
        PatchRel32(b, jHaveMin, stub + jHaveMin + 4, stub + haveMin);
        foreach (int j in doneJumps) PatchRel32(b, j, stub + j + 4, stub + done);
        return b.ToArray();
    }

    static bool EnsureInfrastructure()
    {
        if (infrastructureInstalled) return true;
        if (!Attach()) return false;
        error = "";

        if (!Eq(RBytes(moduleBase + RVA_CAP_GATE, 13), StockCapGateBytes()))
        {
            error = "selection cap gate bytes not stock — restart BRZE before this probe";
            return false;
        }
        int[] calls = { RVA_SIM_INIT_CALL, RVA_SIM_RESET_CALL, RVA_SORT_A_CALL, RVA_SORT_B_CALL, RVA_SORT_ACTIVE_CALL };
        string[] names = { "sim-init", "sim-reset", "sort-A", "sort-B", "sort-active" };
        for (int i = 0; i < calls.Length; i++) if (!VerifyStockCall(calls[i], RVA_LIST_RESET_FN, names[i])) return false;
        if (!VerifyStockCall(RVA_PEASANT_AUTO_SCHEDULE_CALL, RVA_PEASANT_SCHEDULE_FN, "peasant-schedule")) return false;

        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)0x1000, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (cave == IntPtr.Zero) { error = "code cave allocation failed"; return false; }
        long c = cave.ToInt64();
        ctorWrapper = c + 0x000;
        capWrapper = c + 0x080;
        peasantWrapper = c + 0x180;
        selectionFlag = c + 0x700;
        peasantFlag = c + 0x704;

        if (!W32(selectionFlag, 0) || !W32(peasantFlag, 0)) { error = "cave flag init failed"; return false; }
        if (!WriteRaw(ctorWrapper, BuildCtorWrapper(ctorWrapper)) ||
            !WriteRaw(capWrapper, BuildCapWrapper(capWrapper)) ||
            !WriteRaw(peasantWrapper, BuildPeasantWrapper(peasantWrapper)))
        {
            error = "code cave write failed";
            return false;
        }
        FlushInstructionCache(h, cave, (UIntPtr)0x700);

        foreach (int rva in calls)
        {
            long site = moduleBase + rva;
            if (!WriteCode(site, BuildCall(site, ctorWrapper))) { error = $"constructor call patch failed at RVA 0x{rva:X}"; return false; }
        }
        long capSite = moduleBase + RVA_CAP_GATE;
        if (!WriteCode(capSite, BuildJmp(capSite, capWrapper, 13))) { error = "selection cap wrapper patch failed"; return false; }
        long peasantSite = moduleBase + RVA_PEASANT_AUTO_SCHEDULE_CALL;
        if (!WriteCode(peasantSite, BuildCall(peasantSite, peasantWrapper))) { error = "peasant schedule wrapper patch failed"; return false; }

        infrastructureInstalled = true;
        return true;
    }

    static bool PatchEventImmediate(long addr, string name)
    {
        var cur = RBytes(addr, 4);
        if (Eq(cur, EVENT_NEW_IMM)) return true;
        if (!Eq(cur, EVENT_OLD_IMM)) { error = $"{name}: unexpected bytes"; return false; }
        if (!WriteCode(addr, EVENT_NEW_IMM)) { error = $"{name}: write failed"; return false; }
        return Eq(RBytes(addr, 4), EVENT_NEW_IMM);
    }

    static bool ArmHeadroom160()
    {
        uint used = R32(moduleBase + RVA_EVENT_USED);
        uint remain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint ptr = R32(moduleBase + RVA_EVENT_PTR);
        if (ptr == 0) { error = "event buffer pointer is null"; return false; }
        if (used != 0) { error = $"event queue not empty (used={used}, remain={remain}) — restart / wait for used:0 before F4"; return false; }
        if (!PatchEventImmediate(moduleBase + RVA_INIT_REMAIN_IMM, "event-init-remain")) return false;
        if (!PatchEventImmediate(moduleBase + RVA_RESET_REMAIN_IMM, "event-flush-remain")) return false;
        if (!W32(moduleBase + RVA_EVENT_REMAIN, LOGICAL_WINDOW)) { error = "live event remain write failed"; return false; }
        headroomArmed = R32(moduleBase + RVA_EVENT_REMAIN) == LOGICAL_WINDOW;
        return headroomArmed;
    }

    static bool ArmPristineList(long list, string name)
    {
        uint free = R32(list + OFF_FREE_NODE);
        uint count = R32(list + OFF_COUNT);
        uint blocks = R32(list + OFF_BLOCK_COUNT);
        uint first = R32(list + OFF_FIRST);
        uint growth = R32(list + OFF_GROWTH);

        if (count == 0 && blocks == 0 && free == 0 && first == 90 && growth == 0)
        {
            if (!W32(list + OFF_FIRST, SELECTION_LIMIT)) { error = $"{name}: first=500 write failed"; return false; }
            return R32(list + OFF_FIRST) == SELECTION_LIMIT;
        }
        if (first == SELECTION_LIMIT && growth == 0) return true;

        error = $"{name}: allocator already used/unexpected (n={count}, b={blocks}, first={first}, grow={growth}) — fresh restart + F4 before selection";
        return false;
    }

    static long LocalSimulationList(uint localId)
    {
        uint simBase = R32(moduleBase + RVA_SIM_LISTS_PTR);
        return simBase == 0 ? 0 : (long)simBase + localId * SIM_STRIDE;
    }

    static string ListState(long list)
    {
        if (list == 0) return "unavailable";
        return $"n:{R32(list + OFF_COUNT)} b:{R32(list + OFF_BLOCK_COUNT)} first:{R32(list + OFF_FIRST)} grow:{R32(list + OFF_GROWTH)}";
    }

    public static string Tick(bool requestSelection500, bool fastPeasant)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";
        error = "";

        if ((requestSelection500 || fastPeasant || selectionLatched) && !EnsureInfrastructure())
            return Status(fastPeasant, "NOT ARMED");

        if (infrastructureInstalled) W32(peasantFlag, fastPeasant ? 1u : 0u);

        if (requestSelection500 || selectionLatched)
        {
            selectionLatched = true;
            if (!infrastructureInstalled && !EnsureInfrastructure()) return Status(fastPeasant, "NOT ARMED");
            W32(selectionFlag, 1u);

            uint lid = R32(moduleBase + RVA_LOCAL_ID);
            long active = moduleBase + RVA_ACTIVE;
            long sim = LocalSimulationList(lid);
            if (sim == 0) { error = "simulation selection list not initialized yet"; return Status(fastPeasant, "NOT ARMED"); }

            if (!activeArmed) activeArmed = ArmPristineList(active, "active");
            if (!simArmed) simArmed = ArmPristineList(sim, "sim-local");
            if (!activeArmed || !simArmed) return Status(fastPeasant, "NOT ARMED");
            if (!headroomArmed && !ArmHeadroom160()) return Status(fastPeasant, "NOT ARMED");

            W32(moduleBase + RVA_MAX_UNITS + lid * 4L, POPULATION_LIMIT);
        }

        string mode = selectionLatched && activeArmed && simArmed && headroomArmed
            ? "ARMED selection500+headroom160"
            : infrastructureInstalled ? "INFRA READY" : "STOCK";
        return Status(fastPeasant, mode);
    }

    static string Status(bool fastPeasant, string mode)
    {
        uint lid = R32(moduleBase + RVA_LOCAL_ID);
        long active = moduleBase + RVA_ACTIVE;
        long sim = LocalSimulationList(lid);
        uint used = R32(moduleBase + RVA_EVENT_USED);
        uint remain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint eventPtr = R32(moduleBase + RVA_EVENT_PTR);
        uint gate = R32(moduleBase + RVA_EVENT_GATE);
        uint maxPop = R32(moduleBase + RVA_MAX_UNITS + lid * 4L);
        uint nextBase = R32(moduleBase + RVA_PEASANT_NEXT_PTR);
        uint next = nextBase == 0 ? 0u : R32((long)nextBase + lid * 4L);
        uint creation = R32(moduleBase + RVA_PEASANT_CREATION + lid * 4L);
        long net = moduleBase + RVA_NET;
        uint netMax = R32(net + 0x194);
        uint selFlag = selectionFlag == 0 ? 0u : R32(selectionFlag);
        uint peaFlag = peasantFlag == 0 ? 0u : R32(peasantFlag);
        string err = error.Length == 0 ? "" : $"\r\nERROR: {error}";

        return $"pid:{pid} | {mode} | player:{lid} | popLimit:{maxPop} | selFlag:{selFlag} | cave:0x{unchecked((uint)cave.ToInt64()):X8}\r\n" +
               $"ACTIVE {ListState(active)} | SIM {ListState(sim)}\r\n" +
               $"EVENT used:{used} remain:{remain} ptr:0x{eventPtr:X8} gate:{gate} logical:{(headroomArmed ? LOGICAL_WINDOW : 256)} netMax:{netMax}\r\n" +
               $"PEASANT fast:{fastPeasant}/{peaFlag} factor:{PEASANT_FACTOR}x min:{PEASANT_MIN_MS}ms creation:{creation} nextRaw:{next}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero && peasantFlag != 0) W32(peasantFlag, 0u);
        DetachHandleOnly();
    }

    static void DetachHandleOnly()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero;
        p = null;
        moduleBase = 0;
        pid = 0;
        cave = IntPtr.Zero;
        ctorWrapper = capWrapper = peasantWrapper = selectionFlag = peasantFlag = 0;
        infrastructureInstalled = false;
        activeArmed = false;
        simArmed = false;
        headroomArmed = false;
        selectionLatched = false;
        error = "";
    }
}
