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
    readonly CheckBox f4 = new() { Text = "F4 Max Population + SORT PIPELINE 120 + NO ADD-NOTIFY — ISOLATION PROBE", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(820, 0),
        Text = "Diagnostic only. Same sort-pipeline-120 specimen as the previous build, plus ONE new change: suppress the per-unit post-add notification call at 0x5A70D0. This call runs after the unit is appended and its selected flag is set; rectangle/drag selection also reaches this path for each accepted unit. Growth remains 0."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 118, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection No-Add-Notify 120 Probe";
        ClientSize = new Size(890, 300);
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

    const int RVA_MANUAL_CAP_IMM = 0x1A7006;
    const int RVA_SINGLE_ADD_NOTIFY_CALL = 0x1A70D0; // call 0x552FFA after append + selected flag
    const int RVA_SORT_A_FIRST_IMM = 0x1A71B1;
    const int RVA_SORT_B_FIRST_IMM = 0x1A71B9;
    const int RVA_SORT_ACTIVE_FIRST_IMM = 0x1A7299;

    static readonly byte[] ADD_NOTIFY_ORIG = { 0xE8, 0x25, 0xBF, 0xFA, 0xFF };
    static readonly byte[] NOP5 = { 0x90, 0x90, 0x90, 0x90, 0x90 };

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
    static bool notifySuppressed;

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

    static byte[] RBytes(long addr, int len)
    {
        var b = new byte[len];
        if (h == IntPtr.Zero) return Array.Empty<byte>();
        return ReadProcessMemory(h, new IntPtr(unchecked((int)(uint)addr)), b, len, out var n) && n.ToInt64() == len
            ? b : Array.Empty<byte>();
    }

    static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static bool W32(long addr, uint value)
    {
        if (h == IntPtr.Zero) return false;
        var b = BitConverter.GetBytes(value);
        return WriteProcessMemory(h, new IntPtr(unchecked((int)(uint)addr)), b, 4, out var n) && n.ToInt64() == 4;
    }

    static bool WriteCodeBytes(long addr, byte[] value)
    {
        if (h == IntPtr.Zero) return false;
        var a = new IntPtr(unchecked((int)(uint)addr));
        if (!VirtualProtectEx(h, a, (UIntPtr)value.Length, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteProcessMemory(h, a, value, value.Length, out var n) && n.ToInt64() == value.Length;
        if (ok) FlushInstructionCache(h, a, (UIntPtr)value.Length);
        VirtualProtectEx(h, a, (UIntPtr)value.Length, old, out _);
        return ok;
    }

    static bool WriteCodeByte(long addr, byte value) => WriteCodeBytes(addr, new[] { value });

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

    static bool PatchExpectedBytes(int rva, byte[] from, byte[] to, string name)
    {
        long a = moduleBase + rva;
        byte[] cur = RBytes(a, from.Length);
        if (Same(cur, to)) return true;
        if (!Same(cur, from))
        {
            patchError = $"{name}: unexpected bytes {BitConverter.ToString(cur)}";
            return false;
        }
        if (!WriteCodeBytes(a, to))
        {
            patchError = $"{name}: write failed";
            return false;
        }
        return Same(RBytes(a, to.Length), to);
    }

    static bool PatchSortPipeline()
    {
        if (!PatchExpectedByte(RVA_SORT_A_FIRST_IMM, 0x5A, 0x78, "sort-A")) return false;
        if (!PatchExpectedByte(RVA_SORT_B_FIRST_IMM, 0x5A, 0x78, "sort-B")) return false;
        if (!PatchExpectedByte(RVA_SORT_ACTIVE_FIRST_IMM, 0x5A, 0x78, "sort-active")) return false;
        sortPatched = true;
        return true;
    }

    static bool SuppressAddNotify()
    {
        if (!PatchExpectedBytes(RVA_SINGLE_ADD_NOTIFY_CALL, ADD_NOTIFY_ORIG, NOP5, "add-notify")) return false;
        notifySuppressed = true;
        return true;
    }

    static void RestoreAddNotify()
    {
        if (h == IntPtr.Zero) return;
        long a = moduleBase + RVA_SINGLE_ADD_NOTIFY_CALL;
        byte[] cur = RBytes(a, 5);
        if (Same(cur, NOP5)) WriteCodeBytes(a, ADD_NOTIFY_ORIG);
        notifySuppressed = false;
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

            if (!notifySuppressed && !SuppressAddNotify())
                return Status(active, sortA, sortB, capAddr, "NOT ARMED");

            if (armed && sortPatched && notifySuppressed && R32(active + OFF_GROWTH) == 0)
            {
                if (!PatchExpectedByte(RVA_MANUAL_CAP_IMM, 0x5A, 0x78, "manual-cap"))
                    return Status(active, sortA, sortB, capAddr, "NOT ARMED");
            }
        }
        else
        {
            if (cap == 0x78) WriteCodeByte(capAddr, 0x5A);
            RestoreAddNotify();
        }

        return Status(active, sortA, sortB, capAddr,
            armed && sortPatched && notifySuppressed ? "ARMED no-add-notify-120" : "NOT ARMED");
    }

    static string Status(long active, long sortA, long sortB, long capAddr, string mode)
    {
        byte cap = R8(capAddr);
        byte a = R8(moduleBase + RVA_SORT_A_FIRST_IMM);
        byte b = R8(moduleBase + RVA_SORT_B_FIRST_IMM);
        byte r = R8(moduleBase + RVA_SORT_ACTIVE_FIRST_IMM);
        bool notifyNopped = Same(RBytes(moduleBase + RVA_SINGLE_ADD_NOTIFY_CALL, 5), NOP5);
        string err = patchError.Length == 0 ? "" : $" | ERROR:{patchError}";
        return $"pid:{pid} | {mode} | cap:0x{cap:X2} | sort bytes:{a:X2}/{b:X2}/{r:X2} | add-notify:{(notifyNopped ? "NOP" : "LIVE")}\r\n" +
               $"ACTIVE {ListState(active)} | SORT-A {ListState(sortA)} | SORT-B {ListState(sortB)}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero)
        {
            long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
            if (R8(capAddr) == 0x78) WriteCodeByte(capAddr, 0x5A);
            RestoreAddNotify();
        }
        DetachHandleOnly();
        patchError = "";
        armed = false;
        sortPatched = false;
        notifySuppressed = false;
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
