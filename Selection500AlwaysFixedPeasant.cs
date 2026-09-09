using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZESelection500AlwaysFixedPeasant;

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
    readonly CheckBox maxPop = new() { Text = "F4 Max Population 500 (Selection 500 is ALWAYS ON)", AutoSize = true };
    readonly CheckBox fastPeasant = new() { Text = "Fast Peasant Spawn — LOCAL PLAYER only — FIXED 1.0 second", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(980, 0),
        Text = "Selection 500 + safe bulk-drag headroom160 arms automatically and is independent from F4. F4 only toggles Max Population 500. Fast Peasant keeps BRZE native spawning, fixes the local automatic schedule to 1.0 second, and bypasses only PeasantManager's two population-stop checks for the local player. Open this trainer before selecting units."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 215, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(FontFamily.GenericMonospace, 9f) };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — ALWAYS Selection 500 + MaxPop Toggle + Fixed 1s Peasant";
        ClientSize = new Size(1080, 440);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
        panel.Controls.Add(maxPop);
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
        if (down && !f4Held) maxPop.Checked = !maxPop.Checked;
        f4Held = down;
        status.Text = Native.Tick(maxPop.Checked, fastPeasant.Checked);
    }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);

    const uint ACCESS = 0x10 | 0x20 | 0x8 | 0x400;
    const uint PAGE_EXECUTE_READWRITE = 0x40, MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000;
    const uint SELECTION_LIMIT = 500, POPULATION_LIMIT = 500, LOGICAL_WINDOW = 160, PEASANT_FIXED_MS = 1000;

    const int RVA_LOCAL_ID = 0x4416D0, RVA_MAX_UNITS = 0x467B90, RVA_ACTIVE = 0x441708;
    const int RVA_SORT_A = 0x441784, RVA_SORT_B = 0x4417AC, RVA_SIM_LISTS_PTR = 0x441730, SIM_STRIDE = 0x28;
    const int RVA_EVENT_USED = 0x441C94, RVA_EVENT_REMAIN = 0x441C98, RVA_EVENT_PTR = 0x441C9C, RVA_EVENT_GATE = 0x441194, RVA_NET = 0x440C28;
    const int RVA_INIT_REMAIN_IMM = 0x161770, RVA_RESET_REMAIN_IMM = 0x161D77;
    const int RVA_CAP_GATE = 0x1A7000, RVA_CAP_CONTINUE = 0x1A700D, RVA_CAP_REJECT = 0x1A70D5;
    const int RVA_LIST_RESET_FN = 0x0ABFE6;
    const int RVA_SIM_INIT_CALL = 0x1A6C57, RVA_SIM_RESET_CALL = 0x1A6E3B, RVA_SORT_A_CALL = 0x1A71B2, RVA_SORT_B_CALL = 0x1A71BF, RVA_SORT_ACTIVE_CALL = 0x1A729F;
    const int RVA_PEASANT_AUTO_SCHEDULE_CALL = 0x17FFA5, RVA_PEASANT_SCHEDULE_FN = 0x17FFB2;
    const int RVA_PEASANT_COUNT_GATE1_CALL = 0x17FF1B, RVA_PEASANT_COUNT_GATE2_CALL = 0x17FF71, RVA_PEASANT_POPCOUNT_FN = 0x182D5E;
    const int RVA_PEASANT_NEXT_PTR = 0x467AF0, RVA_PEASANT_CREATION = 0x467AF4;
    const int OFF_FREE = 0x08, OFF_COUNT = 0x18, OFF_BLOCKS = 0x1C, OFF_FIRST = 0x20, OFF_GROWTH = 0x24;

    static readonly byte[] EVENT_OLD = { 0x00, 0x01, 0x00, 0x00 };
    static readonly byte[] EVENT_NEW = { 0xA0, 0x00, 0x00, 0x00 };

    static IntPtr h;
    static Process? p;
    static long moduleBase;
    static int pid;
    static IntPtr cave;
    static long ctorWrapper, capWrapper, peasantWrapper, peasantCountWrapper, selectionFlag, peasantFlag;
    static bool infrastructureInstalled, activeArmed, simArmed, headroomArmed;
    static bool maxPopSaved;
    static uint originalMaxPop, originalMaxPopPlayer = 0xFFFFFFFF;
    static string error = "";

    static IntPtr A(long x) => new(unchecked((int)(uint)x));

    static bool Attach()
    {
        try { if (p != null && !p.HasExited && h != IntPtr.Zero) return true; } catch { }
        Detach();
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
        return h != IntPtr.Zero && ReadProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4 ? BitConverter.ToUInt32(b, 0) : 0u;
    }

    static byte[]? RB(long addr, int n)
    {
        var b = new byte[n];
        return h != IntPtr.Zero && ReadProcessMemory(h, A(addr), b, n, out var got) && got.ToInt64() == n ? b : null;
    }

    static bool Eq(byte[]? a, byte[] b)
    {
        if (a == null || a.Length != b.Length) return false;
        for (int i = 0; i < b.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static bool W32(long addr, uint v)
    {
        var b = BitConverter.GetBytes(v);
        return h != IntPtr.Zero && WriteProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4;
    }

    static bool WriteRaw(long addr, byte[] b) => h != IntPtr.Zero && WriteProcessMemory(h, A(addr), b, b.Length, out var n) && n.ToInt64() == b.Length;

    static bool WriteCode(long addr, byte[] b)
    {
        if (h == IntPtr.Zero) return false;
        var a = A(addr);
        if (!VirtualProtectEx(h, a, (UIntPtr)b.Length, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteProcessMemory(h, a, b, b.Length, out var n) && n.ToInt64() == b.Length;
        if (ok) FlushInstructionCache(h, a, (UIntPtr)b.Length);
        VirtualProtectEx(h, a, (UIntPtr)b.Length, old, out _);
        return ok;
    }

    static void U32(List<byte> b, uint v) => b.AddRange(BitConverter.GetBytes(v));
    static void I32(List<byte> b, int v) => b.AddRange(BitConverter.GetBytes(v));
    static void Rel(List<byte> b, int at, long fromNext, long target)
    {
        var x = BitConverter.GetBytes(unchecked((int)(target - fromNext)));
        for (int i = 0; i < 4; i++) b[at + i] = x[i];
    }

    static byte[] Call(long site, long target)
    {
        var b = new List<byte> { 0xE8 }; I32(b, unchecked((int)(target - (site + 5)))); return b.ToArray();
    }

    static byte[] Jmp(long site, long target, int len)
    {
        var b = new List<byte> { 0xE9 }; I32(b, unchecked((int)(target - (site + 5)))); while (b.Count < len) b.Add(0x90); return b.ToArray();
    }

    static long CallTarget(long site)
    {
        var b = RB(site, 5); if (b == null || b[0] != 0xE8) return 0; return site + 5 + BitConverter.ToInt32(b, 1);
    }

    static bool VerifyCall(int rva, int targetRva, string name)
    {
        long actual = CallTarget(moduleBase + rva), expected = moduleBase + targetRva;
        if (actual == expected) return true;
        error = $"{name}: expected 0x{expected:X8}, found 0x{actual:X8} — fresh restart required";
        return false;
    }

    static byte[] StockCapBytes()
    {
        var b = new List<byte> { 0x83, 0x3D };
        U32(b, unchecked((uint)(moduleBase + RVA_ACTIVE + OFF_COUNT))); b.Add(0x5A);
        b.AddRange(new byte[] { 0x0F, 0x84 }); I32(b, unchecked((int)((moduleBase + RVA_CAP_REJECT) - (moduleBase + RVA_CAP_GATE + 13))));
        return b.ToArray();
    }

    static byte[] BuildCtor(long stub)
    {
        var b = new List<byte>();
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, unchecked((uint)selectionFlag)); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int stockJ = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xC7, 0x44, 0x24, 0x04 }); U32(b, SELECTION_LIMIT);
        int stock = b.Count; b.Add(0xE9); int origJ = b.Count; I32(b, 0);
        Rel(b, stockJ, stub + stockJ + 4, stub + stock);
        Rel(b, origJ, stub + origJ + 4, moduleBase + RVA_LIST_RESET_FN);
        return b.ToArray();
    }

    static byte[] BuildCap(long stub)
    {
        var b = new List<byte>(); uint count = unchecked((uint)(moduleBase + RVA_ACTIVE + OFF_COUNT));
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, unchecked((uint)selectionFlag)); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int stockJ = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x81, 0x3D }); U32(b, count); U32(b, SELECTION_LIMIT);
        b.AddRange(new byte[] { 0x0F, 0x83 }); int reject500 = b.Count; I32(b, 0);
        b.Add(0xE9); int cont500 = b.Count; I32(b, 0);
        int stock = b.Count;
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, count); b.Add(0x5A);
        b.AddRange(new byte[] { 0x0F, 0x83 }); int reject90 = b.Count; I32(b, 0);
        b.Add(0xE9); int cont90 = b.Count; I32(b, 0);
        long reject = moduleBase + RVA_CAP_REJECT, cont = moduleBase + RVA_CAP_CONTINUE;
        Rel(b, stockJ, stub + stockJ + 4, stub + stock); Rel(b, reject500, stub + reject500 + 4, reject); Rel(b, cont500, stub + cont500 + 4, cont);
        Rel(b, reject90, stub + reject90 + 4, reject); Rel(b, cont90, stub + cont90 + 4, cont);
        return b.ToArray();
    }

    static byte[] BuildPeasantSchedule(long stub)
    {
        var b = new List<byte>(); var done = new List<int>();
        b.Add(0x55); b.AddRange(new byte[] { 0x8B, 0xEC }); b.Add(0x53); b.Add(0x56); b.Add(0x57);
        b.AddRange(new byte[] { 0x8B, 0x75, 0x08 });
        b.AddRange(new byte[] { 0x8B, 0x45, 0x00, 0x8B, 0x58, 0x0C });
        b.AddRange(new byte[] { 0xFF, 0x75, 0x14, 0xFF, 0x75, 0x10, 0xFF, 0x75, 0x0C, 0xFF, 0x75, 0x08 });
        b.Add(0xE8); int orig = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, unchecked((uint)peasantFlag)); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); done.Add(b.Count); I32(b, 0);
        b.Add(0xA1); U32(b, unchecked((uint)(moduleBase + RVA_LOCAL_ID))); b.AddRange(new byte[] { 0x3B, 0xF0 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); done.Add(b.Count); I32(b, 0);
        b.Add(0xA1); U32(b, unchecked((uint)(moduleBase + RVA_PEASANT_NEXT_PTR))); b.AddRange(new byte[] { 0x85, 0xC0 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); done.Add(b.Count); I32(b, 0);
        b.AddRange(new byte[] { 0x81, 0xC3 }); U32(b, PEASANT_FIXED_MS);
        b.AddRange(new byte[] { 0x89, 0x1C, 0xB0 });
        int end = b.Count; b.Add(0x5F); b.Add(0x5E); b.Add(0x5B); b.Add(0x5D); b.AddRange(new byte[] { 0xC2, 0x10, 0x00 });
        Rel(b, orig, stub + orig + 4, moduleBase + RVA_PEASANT_SCHEDULE_FN);
        foreach (int j in done) Rel(b, j, stub + j + 4, stub + end);
        return b.ToArray();
    }

    static byte[] BuildPeasantCount(long stub)
    {
        var b = new List<byte>();
        b.Add(0xE8); int orig = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, unchecked((uint)peasantFlag)); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int done1 = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x3B, 0x3D }); U32(b, unchecked((uint)(moduleBase + RVA_LOCAL_ID)));
        b.AddRange(new byte[] { 0x0F, 0x85 }); int done2 = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x33, 0xC0 });
        int end = b.Count; b.Add(0xC3);
        Rel(b, orig, stub + orig + 4, moduleBase + RVA_PEASANT_POPCOUNT_FN);
        Rel(b, done1, stub + done1 + 4, stub + end); Rel(b, done2, stub + done2 + 4, stub + end);
        return b.ToArray();
    }

    static bool EnsureInfrastructure()
    {
        if (infrastructureInstalled) return true;
        if (!Eq(RB(moduleBase + RVA_CAP_GATE, 13), StockCapBytes())) { error = "selection cap gate not stock — restart BRZE"; return false; }
        int[] listCalls = { RVA_SIM_INIT_CALL, RVA_SIM_RESET_CALL, RVA_SORT_A_CALL, RVA_SORT_B_CALL, RVA_SORT_ACTIVE_CALL };
        string[] names = { "sim-init", "sim-reset", "sort-A", "sort-B", "sort-active" };
        for (int i = 0; i < listCalls.Length; i++) if (!VerifyCall(listCalls[i], RVA_LIST_RESET_FN, names[i])) return false;
        if (!VerifyCall(RVA_PEASANT_AUTO_SCHEDULE_CALL, RVA_PEASANT_SCHEDULE_FN, "peasant-schedule") ||
            !VerifyCall(RVA_PEASANT_COUNT_GATE1_CALL, RVA_PEASANT_POPCOUNT_FN, "peasant-pop-gate1") ||
            !VerifyCall(RVA_PEASANT_COUNT_GATE2_CALL, RVA_PEASANT_POPCOUNT_FN, "peasant-pop-gate2")) return false;

        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)0x1000, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (cave == IntPtr.Zero) { error = "code cave allocation failed"; return false; }
        long c = cave.ToInt64(); ctorWrapper = c; capWrapper = c + 0x80; peasantWrapper = c + 0x180; peasantCountWrapper = c + 0x300; selectionFlag = c + 0x700; peasantFlag = c + 0x704;
        if (!W32(selectionFlag, 0) || !W32(peasantFlag, 0) || !WriteRaw(ctorWrapper, BuildCtor(ctorWrapper)) || !WriteRaw(capWrapper, BuildCap(capWrapper)) ||
            !WriteRaw(peasantWrapper, BuildPeasantSchedule(peasantWrapper)) || !WriteRaw(peasantCountWrapper, BuildPeasantCount(peasantCountWrapper))) { error = "cave setup failed"; return false; }
        FlushInstructionCache(h, cave, (UIntPtr)0x708);
        foreach (int rva in listCalls) if (!WriteCode(moduleBase + rva, Call(moduleBase + rva, ctorWrapper))) { error = $"list call patch failed RVA 0x{rva:X}"; return false; }
        if (!WriteCode(moduleBase + RVA_CAP_GATE, Jmp(moduleBase + RVA_CAP_GATE, capWrapper, 13))) { error = "cap wrapper patch failed"; return false; }
        if (!WriteCode(moduleBase + RVA_PEASANT_AUTO_SCHEDULE_CALL, Call(moduleBase + RVA_PEASANT_AUTO_SCHEDULE_CALL, peasantWrapper))) { error = "peasant schedule patch failed"; return false; }
        foreach (int rva in new[] { RVA_PEASANT_COUNT_GATE1_CALL, RVA_PEASANT_COUNT_GATE2_CALL })
            if (!WriteCode(moduleBase + rva, Call(moduleBase + rva, peasantCountWrapper))) { error = $"peasant gate patch failed RVA 0x{rva:X}"; return false; }
        infrastructureInstalled = true;
        return true;
    }

    static bool PatchEvent(long addr, string name)
    {
        var cur = RB(addr, 4); if (Eq(cur, EVENT_NEW)) return true;
        if (!Eq(cur, EVENT_OLD)) { error = $"{name}: unexpected bytes"; return false; }
        return WriteCode(addr, EVENT_NEW) && Eq(RB(addr, 4), EVENT_NEW);
    }

    static bool ArmHeadroom()
    {
        uint used = R32(moduleBase + RVA_EVENT_USED), remain = R32(moduleBase + RVA_EVENT_REMAIN), ptr = R32(moduleBase + RVA_EVENT_PTR);
        if (ptr == 0) { error = "event buffer pointer null"; return false; }
        if (used != 0) { error = $"waiting event queue empty (used={used}, remain={remain})"; return false; }
        if (!PatchEvent(moduleBase + RVA_INIT_REMAIN_IMM, "event-init") || !PatchEvent(moduleBase + RVA_RESET_REMAIN_IMM, "event-reset")) return false;
        if (!W32(moduleBase + RVA_EVENT_REMAIN, LOGICAL_WINDOW)) { error = "event remain write failed"; return false; }
        return headroomArmed = R32(moduleBase + RVA_EVENT_REMAIN) == LOGICAL_WINDOW;
    }

    static bool ArmList(long list, string name)
    {
        uint free = R32(list + OFF_FREE), n = R32(list + OFF_COUNT), blocks = R32(list + OFF_BLOCKS), first = R32(list + OFF_FIRST), growth = R32(list + OFF_GROWTH);
        if (n == 0 && blocks == 0 && free == 0 && first == 90 && growth == 0)
        {
            if (!W32(list + OFF_FIRST, SELECTION_LIMIT)) { error = $"{name}: first500 write failed"; return false; }
            return R32(list + OFF_FIRST) == SELECTION_LIMIT;
        }
        if (first == SELECTION_LIMIT && growth == 0) return true;
        error = $"{name}: already used/unexpected n={n} b={blocks} first={first} grow={growth} — restart BRZE and open trainer before selecting";
        return false;
    }

    static long Sim(uint lid)
    {
        uint b = R32(moduleBase + RVA_SIM_LISTS_PTR); return b == 0 ? 0 : (long)b + lid * SIM_STRIDE;
    }

    static string LS(long list) => list == 0 ? "unavailable" : $"n:{R32(list + OFF_COUNT)} b:{R32(list + OFF_BLOCKS)} first:{R32(list + OFF_FIRST)} grow:{R32(list + OFF_GROWTH)}";

    public static string Tick(bool maxPop500, bool fastPeasant)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";
        error = "";
        if (!EnsureInfrastructure()) return Status(maxPop500, fastPeasant, "NOT ARMED");
        W32(selectionFlag, 1); W32(peasantFlag, fastPeasant ? 1u : 0u);
        uint lid = R32(moduleBase + RVA_LOCAL_ID); long active = moduleBase + RVA_ACTIVE, sim = Sim(lid);
        if (sim == 0) return Status(maxPop500, fastPeasant, "WAITING SIM");
        if (!activeArmed) activeArmed = ArmList(active, "active");
        if (!simArmed) simArmed = ArmList(sim, "sim-local");
        if (!activeArmed || !simArmed) return Status(maxPop500, fastPeasant, "NOT ARMED");
        if (!headroomArmed && !ArmHeadroom()) return Status(maxPop500, fastPeasant, "WAITING EVENT");

        long maxAddr = moduleBase + RVA_MAX_UNITS + lid * 4L;
        uint curMax = R32(maxAddr);
        if ((!maxPopSaved || originalMaxPopPlayer != lid) && curMax != 0 && curMax != POPULATION_LIMIT)
        {
            originalMaxPop = curMax; originalMaxPopPlayer = lid; maxPopSaved = true;
        }
        if (maxPop500) W32(maxAddr, POPULATION_LIMIT);
        else if (maxPopSaved && originalMaxPopPlayer == lid) W32(maxAddr, originalMaxPop);

        return Status(maxPop500, fastPeasant, "ARMED selection500 ALWAYS + headroom160");
    }

    static string Status(bool maxPop500, bool fastPeasant, string mode)
    {
        uint lid = R32(moduleBase + RVA_LOCAL_ID); long active = moduleBase + RVA_ACTIVE, sim = Sim(lid);
        uint used = R32(moduleBase + RVA_EVENT_USED), remain = R32(moduleBase + RVA_EVENT_REMAIN), ptr = R32(moduleBase + RVA_EVENT_PTR), gate = R32(moduleBase + RVA_EVENT_GATE);
        uint max = R32(moduleBase + RVA_MAX_UNITS + lid * 4L), nextBase = R32(moduleBase + RVA_PEASANT_NEXT_PTR), next = nextBase == 0 ? 0 : R32((long)nextBase + lid * 4L);
        uint creation = R32(moduleBase + RVA_PEASANT_CREATION + lid * 4L), sf = selectionFlag == 0 ? 0 : R32(selectionFlag), pf = peasantFlag == 0 ? 0 : R32(peasantFlag);
        uint netMax = R32(moduleBase + RVA_NET + 0x194); string err = error.Length == 0 ? "" : $"\r\nERROR: {error}";
        return $"pid:{pid} | {mode} | player:{lid} | selFlag:{sf} | popLimit:{max} | F4max:{maxPop500}\r\n" +
               $"ACTIVE {LS(active)} | SIM {LS(sim)}\r\n" +
               $"EVENT used:{used} remain:{remain} ptr:0x{ptr:X8} gate:{gate} logical:{(headroomArmed ? 160 : 256)} netMax:{netMax}\r\n" +
               $"PEASANT fixed1s:{fastPeasant}/{pf} bypassPopGate:{fastPeasant} creation:{creation} nextRaw:{next} | savedStockMax:{(maxPopSaved ? originalMaxPop : 0)}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero && peasantFlag != 0) W32(peasantFlag, 0);
        Detach();
    }

    static void Detach()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero; p = null; moduleBase = 0; pid = 0; cave = IntPtr.Zero;
        ctorWrapper = capWrapper = peasantWrapper = peasantCountWrapper = selectionFlag = peasantFlag = 0;
        infrastructureInstalled = activeArmed = simArmed = headroomArmed = false;
        maxPopSaved = false; originalMaxPop = 0; originalMaxPopPlayer = 0xFFFFFFFF; error = "";
    }
}
