using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZEEventHeadroom160;

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
    readonly CheckBox arm = new() { Text = "ARM logical event window 160 (native backing remains 256)", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(900, 0),
        Text = "Diagnostic companion for the proven Sim-Pipeline-120 probe. It does NOT change selection capacity. It only lowers the event queue's logical remaining-byte reset from 256 to 160 so BRZE requests its native flush earlier, before the queue reaches the known 254-byte failing boundary. Restart BRZE to fully restore native code."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 145, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(FontFamily.GenericMonospace, 9f) };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };

    public MainForm()
    {
        Text = "BRZE 1.60 — Event Headroom 160 Probe";
        ClientSize = new Size(980, 330);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
        panel.Controls.Add(arm);
        panel.Controls.Add(note);
        Controls.Add(panel);
        Controls.Add(status);

        timer.Tick += (_, _) => status.Text = Native.Tick(arm.Checked);
        timer.Start();
        FormClosed += (_, _) => Native.Stop();
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

    const uint ACCESS = 0x10 | 0x20 | 0x8 | 0x400;
    const uint PAGE_EXECUTE_READWRITE = 0x40;

    const int RVA_EVENT_GATE = 0x441194;
    const int RVA_EVENT_USED = 0x441C94;
    const int RVA_EVENT_REMAIN = 0x441C98;
    const int RVA_EVENT_PTR = 0x441C9C;
    const int RVA_NET = 0x440C28;

    // Immediate DWORDs only. Physical allocation at 0x550CCB remains 0x100 bytes.
    // 0x56176A: C7 05 98 1C 84 00 00 01 00 00
    // 0x561D71: C7 05 98 1C 84 00 00 01 00 00
    const int RVA_INIT_REMAIN_IMM = 0x161770;
    const int RVA_RESET_REMAIN_IMM = 0x161D77;

    const uint LOGICAL_WINDOW = 160;
    static readonly byte[] OLD_IMM = { 0x00, 0x01, 0x00, 0x00 };
    static readonly byte[] NEW_IMM = { 0xA0, 0x00, 0x00, 0x00 };

    static IntPtr h;
    static Process? p;
    static long moduleBase;
    static int pid;
    static bool patched;
    static string error = "";

    static IntPtr A(long addr) => new(unchecked((int)(uint)addr));

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

    static bool W32(long addr, uint v)
    {
        var b = BitConverter.GetBytes(v);
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

    static bool PatchExpected(long addr, string name)
    {
        var cur = RBytes(addr, 4);
        if (Eq(cur, NEW_IMM)) return true;
        if (!Eq(cur, OLD_IMM)) { error = $"{name}: unexpected bytes"; return false; }
        if (!WriteCode(addr, NEW_IMM)) { error = $"{name}: write failed"; return false; }
        if (!Eq(RBytes(addr, 4), NEW_IMM)) { error = $"{name}: verify failed"; return false; }
        return true;
    }

    static bool Arm()
    {
        error = "";
        uint used = R32(moduleBase + RVA_EVENT_USED);
        uint remain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint ptr = R32(moduleBase + RVA_EVENT_PTR);
        if (ptr == 0) { error = "event buffer pointer is null"; return false; }
        if (used != 0) { error = $"queue not empty (used={used}, remain={remain}) — restart BRZE / wait for used:0 before arming"; return false; }

        if (!PatchExpected(moduleBase + RVA_INIT_REMAIN_IMM, "init-remain")) return false;
        if (!PatchExpected(moduleBase + RVA_RESET_REMAIN_IMM, "flush-reset-remain")) return false;

        // Current live queue was already constructed with native remain=256. Lower only the logical window.
        if (!W32(moduleBase + RVA_EVENT_REMAIN, LOGICAL_WINDOW)) { error = "live remain write failed"; return false; }
        patched = R32(moduleBase + RVA_EVENT_REMAIN) == LOGICAL_WINDOW;
        if (!patched) error = "live remain verify failed";
        return patched;
    }

    public static string Tick(bool enable)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";
        if (enable && !patched) Arm();

        uint used = R32(moduleBase + RVA_EVENT_USED);
        uint remain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint ptr = R32(moduleBase + RVA_EVENT_PTR);
        uint gate = R32(moduleBase + RVA_EVENT_GATE);
        long net = moduleBase + RVA_NET;
        uint netState = R32(net + 0x00);
        uint netMode = R32(net + 0x178);
        uint netMax = R32(net + 0x194);
        string codeA = Eq(RBytes(moduleBase + RVA_INIT_REMAIN_IMM, 4), NEW_IMM) ? "A0" : "stock";
        string codeB = Eq(RBytes(moduleBase + RVA_RESET_REMAIN_IMM, 4), NEW_IMM) ? "A0" : "stock";
        string mode = patched ? "ARMED headroom160" : "NOT ARMED";
        string err = error.Length == 0 ? "" : $"\r\nERROR: {error}";

        return $"pid:{pid} | {mode} | gate:{gate} | event ptr:0x{ptr:X8}\r\n" +
               $"EVENT used:{used} remain:{remain} logical-target:{LOGICAL_WINDOW} physical-backing:native-256\r\n" +
               $"reset-code init:{codeA} flush:{codeB} | NET state:{netState} mode:{netMode} maxPayload:{netMax}{err}";
    }

    public static void Stop() => Detach();

    static void Detach()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero; p = null; moduleBase = 0; pid = 0; patched = false; error = "";
    }
}
