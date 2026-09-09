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
    readonly CheckBox f4 = new() { Text = "F4 Max Population + ACTIVE selection growth only — SURGICAL", AutoSize = true };
    readonly Label note = new() { AutoSize = true, MaximumSize = new Size(620, 0), Text = "No HP/stamina hooks, no F7, no selection-event hooks, no manual-cap patch, no temp-container patch. Native manual cap stays 90. Team recall may exceed 90; only the active selection list growth field is rescued from 0 to 90." };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 64, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection Surgical Growth Probe";
        ClientSize = new Size(680, 210);
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
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);

    const uint Access = 0x10 | 0x20 | 0x8 | 0x400;
    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_MAX_UNITS = 0x467B90;
    const int RVA_SELECTION_LIST = 0x441708;
    const int OFF_SELECTED_COUNT = 0x18;
    const int OFF_FIRST_BLOCK = 0x20;
    const int OFF_GROWTH_BLOCK = 0x24;

    static IntPtr h = IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static int pid;

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

    static bool W32(long addr, uint value)
    {
        if (h == IntPtr.Zero) return false;
        var b = BitConverter.GetBytes(value);
        return WriteProcessMemory(h, new IntPtr(unchecked((int)(uint)addr)), b, 4, out var n) && n.ToInt64() == 4;
    }

    public static string Tick(bool enabled)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";

        long list = moduleBase + RVA_SELECTION_LIST;
        uint selected = R32(list + OFF_SELECTED_COUNT);
        uint first = R32(list + OFF_FIRST_BLOCK);
        uint growth = R32(list + OFF_GROWTH_BLOCK);

        if (enabled)
        {
            uint lid = R32(moduleBase + RVA_LOCAL_ID);
            W32(moduleBase + RVA_MAX_UNITS + lid * 4L, 99_999_999u);

            // Surgical intervention: do NOT touch the 90 manual guard or constructor code.
            // Only prevent the authoritative active-selection allocator from having growth=0.
            // Match the native first block size (90), allowing #91+ to request another 90-node block.
            if (growth == 0)
            {
                W32(list + OFF_GROWTH_BLOCK, 90u);
                growth = R32(list + OFF_GROWTH_BLOCK);
            }
        }

        return $"Attached pid:{pid} | F4:{enabled} | selected:{selected} | first:{first} | growth:{growth} | manual cap untouched:90";
    }

    public static void Stop() => Detach();

    static void Detach()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero;
        p = null;
        moduleBase = 0;
        pid = 0;
    }
}
