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
    readonly CheckBox f4 = new() { Text = "F4 Max Population + FIRST BLOCK 120 + manual cap 120 — SINGLE BLOCK PROBE", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(720, 0),
        Text = "Diagnostic only. Growth stays 0. This probe changes the ACTIVE selection first allocation 90→120 only while the allocator is still pristine, then opens the proven manual guard 90→120. If the 90→91 boundary works here, the previous crash points at second-block growth/transition rather than count >90 itself."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 86, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection First-Block 120 Probe";
        ClientSize = new Size(790, 245);
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
    const int RVA_SELECTION_LIST = 0x441708;
    const int RVA_MANUAL_CAP_IMM = 0x1A7006;

    const int OFF_FREE_NODE = 0x08;
    const int OFF_SELECTED_COUNT = 0x18;
    const int OFF_BLOCK_COUNT = 0x1C;
    const int OFF_FIRST_BLOCK = 0x20;
    const int OFF_GROWTH_BLOCK = 0x24;

    static IntPtr h = IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static int pid;
    static string patchError = "";
    static bool armed120;

    static bool Attach()
    {
        try
        {
            if (p != null && !p.HasExited && h != IntPtr.Zero) return true;
        }
        catch { }

        Detach();
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

    public static string Tick(bool enabled)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";

        long list = moduleBase + RVA_SELECTION_LIST;
        long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;

        uint free = R32(list + OFF_FREE_NODE);
        uint selected = R32(list + OFF_SELECTED_COUNT);
        uint blocks = R32(list + OFF_BLOCK_COUNT);
        uint first = R32(list + OFF_FIRST_BLOCK);
        uint growth = R32(list + OFF_GROWTH_BLOCK);
        byte cap = R8(capAddr);

        if (enabled)
        {
            uint lid = R32(moduleBase + RVA_LOCAL_ID);
            W32(moduleBase + RVA_MAX_UNITS + lid * 4L, 99_999_999u);

            // Arm only before the allocator has ever created its first node block.
            if (!armed120)
            {
                if (selected == 0 && blocks == 0 && free == 0 && first == 90 && growth == 0)
                {
                    if (!W32(list + OFF_FIRST_BLOCK, 120u))
                        patchError = "first-block write failed";
                    else
                        armed120 = R32(list + OFF_FIRST_BLOCK) == 120u;
                }
                else if (first == 120 && growth == 0)
                {
                    armed120 = true;
                }
                else
                {
                    patchError = "allocator already used or unexpected state — restart BRZE, enable F4 before selecting anything";
                }
            }

            // Never enable growth in this specimen. We want one 120-node block only.
            growth = R32(list + OFF_GROWTH_BLOCK);
            first = R32(list + OFF_FIRST_BLOCK);
            if (armed120 && first == 120 && growth == 0)
            {
                if (cap == 0x5A)
                {
                    if (!WriteCodeByte(capAddr, 0x78)) patchError = "manual cap patch failed";
                    cap = R8(capAddr);
                }
                else if (cap != 0x78)
                {
                    patchError = $"unexpected cap byte 0x{cap:X2}; not patching";
                }
            }
        }
        else
        {
            // Close admission again when F4 is disabled. Leave an allocated 120-node block alone.
            if (cap == 0x78)
            {
                WriteCodeByte(capAddr, 0x5A);
                cap = R8(capAddr);
            }
            if (blocks == 0 && selected == 0 && first == 120 && growth == 0)
            {
                W32(list + OFF_FIRST_BLOCK, 90u);
                armed120 = false;
                first = R32(list + OFF_FIRST_BLOCK);
            }
        }

        free = R32(list + OFF_FREE_NODE);
        selected = R32(list + OFF_SELECTED_COUNT);
        blocks = R32(list + OFF_BLOCK_COUNT);
        first = R32(list + OFF_FIRST_BLOCK);
        growth = R32(list + OFF_GROWTH_BLOCK);
        cap = R8(capAddr);

        string mode = armed120 ? "ARMED single-block-120" : "NOT ARMED";
        string err = patchError.Length == 0 ? "" : $" | ERROR:{patchError}";
        return $"Attached pid:{pid} | F4:{enabled} | {mode} | selected:{selected} | blocks:{blocks} | first:{first} | growth:{growth} | free:0x{free:X8} | cap:0x{cap:X2} ({cap}){err}";
    }

    public static void Stop() => Detach();

    static void Detach()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero;
        p = null;
        moduleBase = 0;
        pid = 0;
        patchError = "";
        armed120 = false;
    }
}
