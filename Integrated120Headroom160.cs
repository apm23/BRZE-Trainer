using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZEIntegratedSelection;

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
        Text = "F4 Selection 120 + Safe Bulk Drag (event headroom 160)",
        AutoSize = true
    };

    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(940, 0),
        Text = "Integrated runtime candidate from the proven Sim-Pipeline-120 + Event Headroom-160 fix. Physical event backing stays native 256 bytes; only the logical reset window becomes 160 so BRZE flushes earlier. Enable F4 before selecting anything. Restart BRZE to fully restore native code."
    };

    readonly Label status = new()
    {
        AutoSize = false,
        Dock = DockStyle.Bottom,
        Height = 170,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font(FontFamily.GenericMonospace, 9f)
    };

    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection 120 + Safe Bulk Drag Integrated";
        ClientSize = new Size(1000, 360);
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
        if (down && !f4Held) f4.Checked = !f4.Checked;
        f4Held = down;
        status.Text = Native.Tick(f4.Checked);
    }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);

    const uint ACCESS = 0x10 | 0x20 | 0x8 | 0x400;
    const uint PAGE_EXECUTE_READWRITE = 0x40;

    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_MAX_UNITS = 0x467B90;
    const int RVA_ACTIVE = 0x441708;
    const int RVA_SORT_A = 0x441784;
    const int RVA_SORT_B = 0x4417AC;
    const int RVA_SIM_LISTS_PTR = 0x441730;

    const int RVA_MANUAL_CAP_IMM = 0x1A7006;
    const int RVA_SORT_A_FIRST_IMM = 0x1A71B1;
    const int RVA_SORT_B_FIRST_IMM = 0x1A71B9;
    const int RVA_SORT_ACTIVE_FIRST_IMM = 0x1A7299;
    const int RVA_SIM_INIT_FIRST_IMM = 0x1A6C53;
    const int RVA_SIM_RESET_FIRST_IMM = 0x1A6E37;
    const int SIM_STRIDE = 0x28;

    const int RVA_EVENT_USED = 0x441C94;
    const int RVA_EVENT_REMAIN = 0x441C98;
    const int RVA_EVENT_PTR = 0x441C9C;
    const int RVA_EVENT_GATE = 0x441194;
    const int RVA_NET = 0x440C28;
    const int RVA_INIT_REMAIN_IMM = 0x161770;
    const int RVA_RESET_REMAIN_IMM = 0x161D77;
    const uint LOGICAL_WINDOW = 160;

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
    static bool activeArmed;
    static bool sortPatched;
    static bool simCodePatched;
    static bool simArmed;
    static bool headroomArmed;
    static string error = "";

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
        return h != IntPtr.Zero && ReadProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4 ? BitConverter.ToUInt32(b, 0) : 0u;
    }

    static byte R8(long addr)
    {
        var b = new byte[1];
        return h != IntPtr.Zero && ReadProcessMemory(h, A(addr), b, 1, out var n) && n.ToInt64() == 1 ? b[0] : (byte)0;
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

    static bool WriteCodeByte(long addr, byte value) => WriteCode(addr, new[] { value });

    static bool PatchExpectedByte(int rva, byte from, byte to, string name)
    {
        long addr = moduleBase + rva;
        byte cur = R8(addr);
        if (cur == to) return true;
        if (cur != from) { error = $"{name}: unexpected byte 0x{cur:X2}"; return false; }
        if (!WriteCodeByte(addr, to)) { error = $"{name}: write failed"; return false; }
        return R8(addr) == to;
    }

    static bool PatchEventImmediate(long addr, string name)
    {
        var cur = RBytes(addr, 4);
        if (Eq(cur, EVENT_NEW_IMM)) return true;
        if (!Eq(cur, EVENT_OLD_IMM)) { error = $"{name}: unexpected bytes"; return false; }
        if (!WriteCode(addr, EVENT_NEW_IMM)) { error = $"{name}: write failed"; return false; }
        if (!Eq(RBytes(addr, 4), EVENT_NEW_IMM)) { error = $"{name}: verify failed"; return false; }
        return true;
    }

    static bool PatchSortPipeline()
    {
        if (!PatchExpectedByte(RVA_SORT_A_FIRST_IMM, 0x5A, 0x78, "sort-A")) return false;
        if (!PatchExpectedByte(RVA_SORT_B_FIRST_IMM, 0x5A, 0x78, "sort-B")) return false;
        if (!PatchExpectedByte(RVA_SORT_ACTIVE_FIRST_IMM, 0x5A, 0x78, "sort-active")) return false;
        sortPatched = true;
        return true;
    }

    static bool PatchSimulationCode()
    {
        if (!PatchExpectedByte(RVA_SIM_INIT_FIRST_IMM, 0x5A, 0x78, "sim-init")) return false;
        if (!PatchExpectedByte(RVA_SIM_RESET_FIRST_IMM, 0x5A, 0x78, "sim-reset")) return false;
        simCodePatched = true;
        return true;
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
            if (!W32(list + OFF_FIRST, 120u)) { error = $"{name}: first-block write failed"; return false; }
            return R32(list + OFF_FIRST) == 120u;
        }

        if (first == 120 && growth == 0) return true;
        error = $"{name}: allocator already used/unexpected (n={count}, b={blocks}, first={first}, grow={growth}) — restart BRZE and enable F4 before selecting anything";
        return false;
    }

    static long LocalSimulationList(uint localId)
    {
        uint simBase = R32(moduleBase + RVA_SIM_LISTS_PTR);
        return simBase == 0 ? 0 : (long)simBase + localId * SIM_STRIDE;
    }

    static bool ArmHeadroom160()
    {
        uint used = R32(moduleBase + RVA_EVENT_USED);
        uint remain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint ptr = R32(moduleBase + RVA_EVENT_PTR);
        if (ptr == 0) { error = "event buffer pointer is null"; return false; }
        if (used != 0) { error = $"event queue not empty (used={used}, remain={remain}) — restart BRZE / wait for used:0 before F4"; return false; }

        if (!PatchEventImmediate(moduleBase + RVA_INIT_REMAIN_IMM, "event-init-remain")) return false;
        if (!PatchEventImmediate(moduleBase + RVA_RESET_REMAIN_IMM, "event-flush-remain")) return false;
        if (!W32(moduleBase + RVA_EVENT_REMAIN, LOGICAL_WINDOW)) { error = "live event remain write failed"; return false; }
        headroomArmed = R32(moduleBase + RVA_EVENT_REMAIN) == LOGICAL_WINDOW;
        if (!headroomArmed) error = "live event remain verify failed";
        return headroomArmed;
    }

    static string ListState(long list)
    {
        if (list == 0) return "unavailable";
        return $"n:{R32(list + OFF_COUNT)} b:{R32(list + OFF_BLOCK_COUNT)} first:{R32(list + OFF_FIRST)} grow:{R32(list + OFF_GROWTH)}";
    }

    public static string Tick(bool enabled)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";

        long active = moduleBase + RVA_ACTIVE;
        long sortA = moduleBase + RVA_SORT_A;
        long sortB = moduleBase + RVA_SORT_B;
        long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
        uint lid = R32(moduleBase + RVA_LOCAL_ID);
        long simLocal = LocalSimulationList(lid);

        if (enabled)
        {
            error = "";
            W32(moduleBase + RVA_MAX_UNITS + lid * 4L, 99_999_999u);

            if (!sortPatched && !PatchSortPipeline()) return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            if (!simCodePatched && !PatchSimulationCode()) return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            if (!activeArmed) activeArmed = ArmPristineList(active, "active");

            if (simLocal == 0) { error = "simulation selection list not initialized yet"; return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED"); }
            if (!simArmed) simArmed = ArmPristineList(simLocal, "sim-local");
            if (!headroomArmed && !ArmHeadroom160()) return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");

            if (activeArmed && sortPatched && simCodePatched && simArmed && headroomArmed &&
                R32(active + OFF_GROWTH) == 0 && R32(simLocal + OFF_GROWTH) == 0)
            {
                if (!PatchExpectedByte(RVA_MANUAL_CAP_IMM, 0x5A, 0x78, "manual-cap")) return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            }
        }
        else
        {
            if (R8(capAddr) == 0x78) WriteCodeByte(capAddr, 0x5A);
        }

        string mode = activeArmed && sortPatched && simCodePatched && simArmed && headroomArmed
            ? "ARMED integrated-120+headroom160"
            : "NOT ARMED";
        return Status(active, sortA, sortB, simLocal, capAddr, lid, mode);
    }

    static string Status(long active, long sortA, long sortB, long simLocal, long capAddr, uint lid, string mode)
    {
        uint eventUsed = R32(moduleBase + RVA_EVENT_USED);
        uint eventRemain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint eventPtr = R32(moduleBase + RVA_EVENT_PTR);
        uint gate = R32(moduleBase + RVA_EVENT_GATE);
        long net = moduleBase + RVA_NET;
        uint netState = R32(net + 0x00);
        uint netMode = R32(net + 0x178);
        uint netMax = R32(net + 0x194);
        string init = Eq(RBytes(moduleBase + RVA_INIT_REMAIN_IMM, 4), EVENT_NEW_IMM) ? "A0" : "stock";
        string flush = Eq(RBytes(moduleBase + RVA_RESET_REMAIN_IMM, 4), EVENT_NEW_IMM) ? "A0" : "stock";
        string err = error.Length == 0 ? "" : $"\r\nERROR: {error}";

        return $"pid:{pid} | {mode} | player:{lid} | cap:0x{R8(capAddr):X2} | sort:{R8(moduleBase + RVA_SORT_A_FIRST_IMM):X2}/{R8(moduleBase + RVA_SORT_B_FIRST_IMM):X2}/{R8(moduleBase + RVA_SORT_ACTIVE_FIRST_IMM):X2} | sim:{R8(moduleBase + RVA_SIM_INIT_FIRST_IMM):X2}/{R8(moduleBase + RVA_SIM_RESET_FIRST_IMM):X2}\r\n" +
               $"ACTIVE {ListState(active)} | SIM {ListState(simLocal)}\r\n" +
               $"EVENT ptr:0x{eventPtr:X8} used:{eventUsed} remain:{eventRemain} logical:160 physical:256 gate:{gate} reset:{init}/{flush}\r\n" +
               $"NET state:{netState} mode:{netMode} maxPayload:{netMax} | SORT-A {ListState(sortA)} | SORT-B {ListState(sortB)}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero)
        {
            long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
            if (R8(capAddr) == 0x78) WriteCodeByte(capAddr, 0x5A);
        }
        DetachHandleOnly();
        error = "";
        activeArmed = sortPatched = simCodePatched = simArmed = headroomArmed = false;
    }

    static void DetachHandleOnly()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero; p = null; moduleBase = 0; pid = 0;
    }
}
