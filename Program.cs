using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZETrainer;

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
    readonly CheckBox f4 = new() { Text = "F4 Max Population + UI/SORT/SIM SELECTION 120 — SURGICAL", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(900, 0),
        Text = "Diagnostic only. Keeps the local UI selection, native sort/rebuild lists, and the local-player simulation selection container at first=120 with growth=0. The selection-add event remains LIVE so right-click move/attack commands stay authoritative. Enable F4 before selecting anything."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 132, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection Simulation Pipeline 120 Probe";
        ClientSize = new Size(960, 320);
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
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);

    const uint Access = 0x10 | 0x20 | 0x8 | 0x400;
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

    // Simulation-side per-player selection list construction/reset.
    // 0x5A6C52 = push 0x5A during 10-list initialization, immediate at +1.
    // 0x5A6E36 = push 0x5A during 10-list reset, immediate at +1.
    const int RVA_SIM_INIT_FIRST_IMM = 0x1A6C53;
    const int RVA_SIM_RESET_FIRST_IMM = 0x1A6E37;
    const int SIM_LIST_STRIDE = 0x28;

    const int OFF_FREE_NODE = 0x08;
    const int OFF_COUNT = 0x18;
    const int OFF_BLOCK_COUNT = 0x1C;
    const int OFF_FIRST = 0x20;
    const int OFF_GROWTH = 0x24;

    static IntPtr h = IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static int pid;
    static string patchError = "";
    static bool activeArmed;
    static bool sortPatched;
    static bool simCodePatched;
    static bool simArmed;

    static bool Attach()
    {
        try
        {
            if (p != null && !p.HasExited && h != IntPtr.Zero) return true;
        }
        catch { }

        DetachHandleOnly();
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;
        p = ps[0];
        pid = p.Id;
        try { moduleBase = p.MainModule!.BaseAddress.ToInt64(); }
        catch { p = null; return false; }
        h = OpenProcess(Access, false, pid);
        return h != IntPtr.Zero;
    }

    static uint R32(long addr)
    {
        if (h == IntPtr.Zero) return 0;
        var b = new byte[4];
        return ReadProcessMemory(h, new IntPtr(unchecked((int)(uint)addr)), b, 4, out var n) && n.ToInt64() == 4
            ? BitConverter.ToUInt32(b, 0) : 0;
    }

    static byte R8(long addr)
    {
        if (h == IntPtr.Zero) return 0;
        var b = new byte[1];
        return ReadProcessMemory(h, new IntPtr(unchecked((int)(uint)addr)), b, 1, out var n) && n.ToInt64() == 1 ? b[0] : (byte)0;
    }

    static bool W32(long addr, uint value)
    {
        if (h == IntPtr.Zero) return false;
        var b = BitConverter.GetBytes(value);
        return WriteProcessMemory(h, new IntPtr(unchecked((int)(uint)addr)), b, 4, out var n) && n.ToInt64() == 4;
    }

    static bool WriteCodeByte(long addr, byte value)
    {
        if (h == IntPtr.Zero) return false;
        var a = new IntPtr(unchecked((int)(uint)addr));
        if (!VirtualProtectEx(h, a, (UIntPtr)1, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteProcessMemory(h, a, new[] { value }, 1, out var n) && n.ToInt64() == 1;
        if (ok) FlushInstructionCache(h, a, (UIntPtr)1);
        VirtualProtectEx(h, a, (UIntPtr)1, old, out _);
        return ok;
    }

    static bool PatchExpectedByte(int rva, byte from, byte to, string name)
    {
        long a = moduleBase + rva;
        byte cur = R8(a);
        if (cur == to) return true;
        if (cur != from)
        {
            patchError = $"{name}: unexpected byte 0x{cur:X2}";
            return false;
        }
        if (!WriteCodeByte(a, to))
        {
            patchError = $"{name}: write failed";
            return false;
        }
        return R8(a) == to;
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
            if (!W32(list + OFF_FIRST, 120u))
            {
                patchError = $"{name}: first-block write failed";
                return false;
            }
            return R32(list + OFF_FIRST) == 120u;
        }

        if (first == 120 && growth == 0)
            return true;

        patchError = $"{name}: allocator already used/unexpected (n={count}, b={blocks}, first={first}, grow={growth}) — restart BRZE and enable F4 before selecting anything";
        return false;
    }

    static long LocalSimulationList(uint localId)
    {
        uint simBase = R32(moduleBase + RVA_SIM_LISTS_PTR);
        if (simBase == 0) return 0;
        return (long)simBase + localId * SIM_LIST_STRIDE;
    }

    static string ListState(long list)
    {
        if (list == 0) return "unavailable";
        uint count = R32(list + OFF_COUNT);
        uint blocks = R32(list + OFF_BLOCK_COUNT);
        uint first = R32(list + OFF_FIRST);
        uint growth = R32(list + OFF_GROWTH);
        return $"n:{count} b:{blocks} first:{first} grow:{growth}";
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
        byte cap = R8(capAddr);

        if (enabled)
        {
            W32(moduleBase + RVA_MAX_UNITS + lid * 4L, 99_999_999u);

            if (!sortPatched && !PatchSortPipeline())
                return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");

            if (!simCodePatched && !PatchSimulationCode())
                return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");

            if (!activeArmed)
                activeArmed = ArmPristineList(active, "active");

            if (simLocal == 0)
            {
                patchError = "simulation selection list not initialized yet";
                return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            }

            if (!simArmed)
                simArmed = ArmPristineList(simLocal, "sim-local");

            if (activeArmed && sortPatched && simCodePatched && simArmed &&
                R32(active + OFF_GROWTH) == 0 && R32(simLocal + OFF_GROWTH) == 0)
            {
                if (!PatchExpectedByte(RVA_MANUAL_CAP_IMM, 0x5A, 0x78, "manual-cap"))
                    return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            }
        }
        else
        {
            // Close >90 local admission only. Keep sort + simulation reset code at 120
            // until BRZE restart; restoring them while >90 state exists could reintroduce
            // a 90-node reset underneath live selection state.
            if (cap == 0x78) WriteCodeByte(capAddr, 0x5A);
        }

        string mode = activeArmed && sortPatched && simCodePatched && simArmed
            ? "ARMED sim-pipeline-120"
            : "NOT ARMED";
        return Status(active, sortA, sortB, simLocal, capAddr, lid, mode);
    }

    static string Status(long active, long sortA, long sortB, long simLocal, long capAddr, uint lid, string mode)
    {
        byte cap = R8(capAddr);
        byte a = R8(moduleBase + RVA_SORT_A_FIRST_IMM);
        byte b = R8(moduleBase + RVA_SORT_B_FIRST_IMM);
        byte r = R8(moduleBase + RVA_SORT_ACTIVE_FIRST_IMM);
        byte si = R8(moduleBase + RVA_SIM_INIT_FIRST_IMM);
        byte sr = R8(moduleBase + RVA_SIM_RESET_FIRST_IMM);
        string err = patchError.Length == 0 ? "" : $" | ERROR:{patchError}";

        return $"pid:{pid} | {mode} | player:{lid} | cap:0x{cap:X2} | sort:{a:X2}/{b:X2}/{r:X2} | sim-code:{si:X2}/{sr:X2} | add-notify:LIVE\r\n" +
               $"ACTIVE {ListState(active)} | SIM {ListState(simLocal)}\r\n" +
               $"SORT-A {ListState(sortA)} | SORT-B {ListState(sortB)}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero)
        {
            long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
            if (R8(capAddr) == 0x78) WriteCodeByte(capAddr, 0x5A);
        }
        DetachHandleOnly();
        patchError = "";
        activeArmed = false;
        sortPatched = false;
        simCodePatched = false;
        simArmed = false;
    }

    static void DetachHandleOnly()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero;
        p = null;
        moduleBase = 0;
        pid = 0;
    }
}
