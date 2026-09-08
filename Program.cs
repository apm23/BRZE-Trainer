using System;
using System.Collections.Generic;
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
    readonly CheckBox f1 = new() { Text = "F1 Infinite Rice", AutoSize = true };
    readonly CheckBox f2 = new() { Text = "F2 Infinite Water", AutoSize = true };
    readonly CheckBox f3 = new() { Text = "F3 Infinite Yin + Yang", AutoSize = true };
    readonly CheckBox f4 = new() { Text = "F4 Unlimited Population (9,999,999)", AutoSize = true };
    readonly CheckBox f5 = new() { Text = "F5 Infinite Stamina (selected only)", AutoSize = true };
    readonly CheckBox f6 = new() { Text = "F6 HP Lock (selected only)", AutoSize = true };
    readonly Label status = new() { AutoSize = false, Height = 48, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    readonly bool[] held = new bool[12];

    public MainForm()
    {
        Text = "BRZE Trainer 1.60";
        ClientSize = new Size(450, 370);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
        panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6 });
        panel.Controls.Add(new Label { Text = "F7 Instant Training — pending runtime mapping", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F8 Instant Building — pending runtime mapping", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F9 Enable / Disable ALL implemented cheats", AutoSize = true });
        panel.Controls.Add(new Label { Text = "F10 Unlimited Horses — pending runtime mapping", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F11 Unlimited Wolves — pending runtime mapping", AutoSize = true, ForeColor = Color.DimGray });
        Controls.Add(panel);
        Controls.Add(status);

        timer.Tick += (_, _) => TickTrainer();
        timer.Start();
        FormClosed += (_, _) => Native.Detach();
    }

    void Toggle(int vk, int idx, Action action)
    {
        bool now = (Native.GetAsyncKeyState(vk) & 0x8000) != 0;
        if (now && !held[idx]) action();
        held[idx] = now;
    }

    void SetAllImplemented(bool enabled)
    {
        f1.Checked = enabled;
        f2.Checked = enabled;
        f3.Checked = enabled;
        f4.Checked = enabled;
        f5.Checked = enabled;
        f6.Checked = enabled;
    }

    void TickTrainer()
    {
        Toggle(0x70, 1, () => f1.Checked = !f1.Checked);
        Toggle(0x71, 2, () => f2.Checked = !f2.Checked);
        Toggle(0x72, 3, () => f3.Checked = !f3.Checked);
        Toggle(0x73, 4, () => f4.Checked = !f4.Checked);
        Toggle(0x74, 5, () => f5.Checked = !f5.Checked);
        Toggle(0x75, 6, () => f6.Checked = !f6.Checked);
        Toggle(0x78, 9, () =>
        {
            bool allEnabled = f1.Checked && f2.Checked && f3.Checked && f4.Checked && f5.Checked && f6.Checked;
            SetAllImplemented(!allEnabled);
        });

        status.Text = Native.Apply(f1.Checked, f2.Checked, f3.Checked, f4.Checked, f5.Checked, f6.Checked);
    }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);

    const uint Access = 0x10 | 0x20 | 0x8 | 0x400;
    const int RVA_PLAYER_PTR = 0x4416A0, RVA_LOCAL_ID = 0x4416D0, PLAYER_STRIDE = 0x5E8;
    const int OFF_RICE = 0xD8, OFF_WATER = 0xDC, OFF_YIN = 0x2E8, OFF_YANG = 0x2EC;
    const int RVA_MAX_UNITS = 0x467B90;
    const int RVA_UNIT_POOL = 0x4796A0, UNIT_STRIDE = 0x818, UNIT_COUNT = 2000;
    const int OFF_DEF = 0x74, OFF_OWNER = 0x240, OFF_SEL_A = 0x3A8, OFF_SEL_B = 0x3AC, OFF_HP = 0x404, OFF_ST = 0x408;

    static IntPtr h = IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static uint localId;
    static uint unitPool;
    static DateTime lastCache = DateTime.MinValue;
    static readonly List<UnitInfo> localUnits = new();

    sealed class UnitInfo { public long Addr; public uint MaxHpFixed; public uint MaxStFixed; }

    static bool Attach()
    {
        try { if (p != null && !p.HasExited && h != IntPtr.Zero) return true; } catch { }
        Detach();
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;
        p = ps[0];
        try { moduleBase = p.MainModule!.BaseAddress.ToInt64(); } catch { return false; }
        h = OpenProcess(Access, false, p.Id);
        lastCache = DateTime.MinValue;
        return h != IntPtr.Zero;
    }

    public static void Detach()
    {
        if (h != IntPtr.Zero) { CloseHandle(h); h = IntPtr.Zero; }
        p = null; localUnits.Clear(); unitPool = 0;
    }

    static bool ReadExact(long addr, byte[] b)
    {
        return ReadProcessMemory(h, new IntPtr(addr), b, b.Length, out var n) && n.ToInt64() == b.Length;
    }
    static uint R32(long addr) { var b = new byte[4]; return ReadExact(addr, b) ? BitConverter.ToUInt32(b, 0) : 0; }
    static bool W32(long addr, uint v) { var b = BitConverter.GetBytes(v); return WriteProcessMemory(h, new IntPtr(addr), b, 4, out var n) && n.ToInt64() == 4; }
    static uint Fixed16(uint v) { ulong x = ((ulong)v) << 16; return x > uint.MaxValue ? uint.MaxValue : (uint)x; }

    static bool RefreshUnits()
    {
        if (!Attach()) return false;
        if ((DateTime.Now - lastCache).TotalMilliseconds < 2000) return true;
        localId = R32(moduleBase + RVA_LOCAL_ID);
        unitPool = R32(moduleBase + RVA_UNIT_POOL);
        if (unitPool == 0) return false;
        var raw = new byte[UNIT_COUNT * UNIT_STRIDE];
        if (!ReadExact(unitPool, raw)) return false;
        localUnits.Clear();
        for (int i = 0; i < UNIT_COUNT; i++)
        {
            int o = i * UNIT_STRIDE;
            uint def = BitConverter.ToUInt32(raw, o + OFF_DEF);
            if (def == 0 || BitConverter.ToUInt32(raw, o + OFF_OWNER) != localId) continue;
            uint mh = R32((long)def + 0x6C), ms = R32((long)def + 0x80);
            localUnits.Add(new UnitInfo { Addr = (long)unitPool + o, MaxHpFixed = Fixed16(mh), MaxStFixed = Fixed16(ms) });
        }
        lastCache = DateTime.Now;
        return true;
    }

    public static string Apply(bool rice, bool water, bool yinYang, bool pop, bool stamina, bool hp)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";
        localId = R32(moduleBase + RVA_LOCAL_ID);
        uint playerPtr = R32(moduleBase + RVA_PLAYER_PTR);
        if (playerPtr == 0) return "Attached, waiting for match/player data...";
        long player = (long)playerPtr + (long)localId * PLAYER_STRIDE;
        if (rice) W32(player + OFF_RICE, 50000);
        if (water) W32(player + OFF_WATER, 50000);
        if (yinYang) { W32(player + OFF_YIN, 10); W32(player + OFF_YANG, 10); }
        if (pop) W32(moduleBase + RVA_MAX_UNITS + localId * 4, 9_999_999);

        int sel = 0;
        if ((stamina || hp) && RefreshUnits())
        {
            foreach (var u in localUnits)
            {
                if (R32(u.Addr + OFF_OWNER) != localId || R32(u.Addr + OFF_SEL_A) != 1 || R32(u.Addr + OFF_SEL_B) != 1) continue;
                sel++;
                if (hp && u.MaxHpFixed != 0) W32(u.Addr + OFF_HP, u.MaxHpFixed);
                if (stamina && u.MaxStFixed != 0) W32(u.Addr + OFF_ST, u.MaxStFixed);
            }
        }
        return $"Attached | selected local units: {sel} | F1:{rice} F2:{water} F3:{yinYang} F4:{pop} F5:{stamina} F6:{hp}";
    }
}
