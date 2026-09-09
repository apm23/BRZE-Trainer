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
    readonly CheckBox f4 = new() { Text = "F4 Max Population + SORT PIPELINE 120 — SURGICAL", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(790, 0),
        Text = "Diagnostic only. Growth remains 0. Keeps active selection and the two native sort/rebuild temp lists on one 120-node first block. Does NOT patch the shared constructor or unrelated auxiliary lists. Enable F4 before selecting anything."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 108, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection Sort Pipeline 120 Probe";
        ClientSize = new Size(850, 280);
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

    const int RVA_MANUAL_CAP_IMM = 0x1A7006;      // cmp selected, 90
    const int RVA_SORT_A_FIRST_IMM = 0x1A71B1;    // push 90 for 0x841784
    const int RVA_SORT_B_FIRST_IMM = 0x1A71B9;    // push 90 for 0x8417AC
    const int RVA_SORT_ACTIVE_FIRST_IMM = 0x1A7299;// push 90 when rebuilding 0x841708

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
    static bool armed;
    static bool sortPatched;

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

    static string ListState(long list)
    {
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

        uint free = R32(active + OFF_FREE_NODE);
        uint selected = R32(active + OFF_COUNT);
        uint blocks = R32(active + OFF_BLOCK_COUNT);
        uint first = R32(active + OFF_FIRST);
        uint growth = R32(active + OFF_GROWTH);
        byte cap = R8(capAddr);

        if (enabled)
        {
            uint lid = R32(moduleBase + RVA_LOCAL_ID);
            W32(moduleBase + RVA_MAX_UNITS + lid * 4L, 99_999_999u);

            if (!sortPatched && !PatchSortPipeline())
                return Status(active, sortA, sortB, capAddr, "NOT ARMED");

            if (!armed)
            {
                // The active list must still be pristine. Changing +0x20 after a 90-node block
                // already exists would lie about the actual allocation size and is unsafe.
                if (selected == 0 && blocks == 0 && free == 0 && first == 90 && growth == 0)
                {
                    if (!W32(active + OFF_FIRST, 120u))
                        patchError = "active first-block write failed";
                    else
                        armed = R32(active + OFF_FIRST) == 120u;
                }
                else if (first == 120 && growth == 0)
                {
                    armed = true;
                }
                else
                {
                    patchError = "active allocator already used/unexpected — restart BRZE and enable F4 before selecting anything";
                }
            }

            if (armed && sortPatched && R32(active + OFF_GROWTH) == 0)
            {
                if (!PatchExpectedByte(RVA_MANUAL_CAP_IMM, 0x5A, 0x78, "manual-cap"))
                    return Status(active, sortA, sortB, capAddr, "NOT ARMED");
            }
        }
        else
        {
            // Closing admission is safe. We intentionally keep the sort-pipeline code patch
            // while the process is alive; restoring it with >90 selected could make the next
            // native rebuild reset back to 90 and crash. Full reset = restart BRZE.
            if (cap == 0x78) WriteCodeByte(capAddr, 0x5A);
        }

        return Status(active, sortA, sortB, capAddr, armed && sortPatched ? "ARMED sort-pipeline-120" : "NOT ARMED");
    }

    static string Status(long active, long sortA, long sortB, long capAddr, string mode)
    {
        byte cap = R8(capAddr);
        byte a = R8(moduleBase + RVA_SORT_A_FIRST_IMM);
        byte b = R8(moduleBase + RVA_SORT_B_FIRST_IMM);
        byte r = R8(moduleBase + RVA_SORT_ACTIVE_FIRST_IMM);
        string err = patchError.Length == 0 ? "" : $" | ERROR:{patchError}";
        return $"pid:{pid} | {mode} | cap:0x{cap:X2} | sort bytes:{a:X2}/{b:X2}/{r:X2}\r\n" +
               $"ACTIVE {ListState(active)} | SORT-A {ListState(sortA)} | SORT-B {ListState(sortB)}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero)
        {
            // Close >90 admission. Keep sort pipeline at 120 until BRZE restart for safety.
            long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
            if (R8(capAddr) == 0x78) WriteCodeByte(capAddr, 0x5A);
        }
        DetachHandleOnly();
        patchError = "";
        armed = false;
        sortPatched = false;
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
