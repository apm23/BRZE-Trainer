using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
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
    readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training (TEST)", AutoSize = true };
    readonly Label status = new() { AutoSize = false, Height = 54, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    readonly bool[] held = new bool[12];

    public MainForm()
    {
        Text = "BRZE Trainer 1.60";
        ClientSize = new Size(500, 385);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
        panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6, f7 });
        panel.Controls.Add(new Label { Text = "F7 building skills/upgrades — not enabled yet", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F8 Instant Building — pending", AutoSize = true, ForeColor = Color.DimGray });
        panel.Controls.Add(new Label { Text = "F9 Enable / Disable ALL implemented cheats", AutoSize = true });
        panel.Controls.Add(new Label { Text = "F10 Unlimited Horses + Wolves — pending", AutoSize = true, ForeColor = Color.DimGray });
        Controls.Add(panel);
        Controls.Add(status);

        Native.Start();
        timer.Tick += (_, _) => TickTrainer();
        timer.Start();
        FormClosed += (_, _) => Native.Stop();
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
        f7.Checked = enabled;
    }

    void TickTrainer()
    {
        Toggle(0x70, 1, () => f1.Checked = !f1.Checked);
        Toggle(0x71, 2, () => f2.Checked = !f2.Checked);
        Toggle(0x72, 3, () => f3.Checked = !f3.Checked);
        Toggle(0x73, 4, () => f4.Checked = !f4.Checked);
        Toggle(0x74, 5, () => f5.Checked = !f5.Checked);
        Toggle(0x75, 6, () => f6.Checked = !f6.Checked);
        Toggle(0x76, 7, () => f7.Checked = !f7.Checked);
        Toggle(0x78, 9, () =>
        {
            bool allEnabled = f1.Checked && f2.Checked && f3.Checked && f4.Checked && f5.Checked && f6.Checked && f7.Checked;
            SetAllImplemented(!allEnabled);
        });

        Native.SetFastLocks(f5.Checked, f6.Checked);
        status.Text = Native.Apply(f1.Checked, f2.Checked, f3.Checked, f4.Checked, f7.Checked);
    }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
    [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);

    const uint Access = 0x10 | 0x20 | 0x8 | 0x400;
    const int RVA_PLAYER_PTR = 0x4416A0, RVA_LOCAL_ID = 0x4416D0, PLAYER_STRIDE = 0x5E8;
    const int OFF_RICE = 0xD8, OFF_WATER = 0xDC, OFF_YIN = 0x2E8, OFF_YANG = 0x2EC;
    const int RVA_MAX_UNITS = 0x467B90;

    const int RVA_UNIT_POOL = 0x4796A0, UNIT_STRIDE = 0x818, UNIT_COUNT = 2000;
    const int OFF_DEF = 0x74, OFF_OWNER = 0x240, OFF_SEL_A = 0x3A8, OFF_SEL_B = 0x3AC, OFF_HP = 0x404, OFF_ST = 0x408;

    // Exact BRZE 1.60 building resolver uses 500 slots x 0x6A4 from *(base+0x4814E0).
    const int RVA_BUILDING_POOL = 0x4814E0, BUILDING_STRIDE = 0x6A4, BUILDING_COUNT = 500;
    const int OFF_BUILD_OWNER = 0x84, OFF_TRAIN_UNIT = 0x484, OFF_TRAIN_TYPE = 0x488, OFF_TRAIN_PROGRESS = 0x490;
    const uint TRAIN_COMPLETE_FIXED = 0x00640000; // 100.0 in 16.16; game completion path compares progress against this.

    static readonly object attachLock = new();
    static IntPtr h = IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static volatile uint localId;
    static volatile uint unitPool;
    static volatile bool wantStamina;
    static volatile bool wantHp;
    static volatile bool running;
    static Thread? lockThread;
    static DateTime lastUnitCache = DateTime.MinValue;
    static UnitInfo[] localUnits = Array.Empty<UnitInfo>();
    static readonly byte[] unitRaw = new byte[UNIT_COUNT * UNIT_STRIDE];
    static readonly byte[] buildingRaw = new byte[BUILDING_COUNT * BUILDING_STRIDE];
    static DateTime lastBuildingScan = DateTime.MinValue;
    static int selectedLocked;
    static int trainingBuildings;

    readonly struct UnitInfo
    {
        public readonly long Addr;
        public readonly uint Def;
        public readonly uint MaxHpFixed;
        public readonly uint MaxStFixed;
        public UnitInfo(long addr, uint def, uint hp, uint st) { Addr = addr; Def = def; MaxHpFixed = hp; MaxStFixed = st; }
    }

    public static void Start()
    {
        if (running) return;
        running = true;
        timeBeginPeriod(1);
        lockThread = new Thread(FastLockLoop) { IsBackground = true, Name = "BRZE-HP-ST-Lock" };
        lockThread.Start();
    }

    public static void Stop()
    {
        running = false;
        try { lockThread?.Join(250); } catch { }
        timeEndPeriod(1);
        Detach();
    }

    public static void SetFastLocks(bool stamina, bool hp)
    {
        wantStamina = stamina;
        wantHp = hp;
    }

    static bool Attach()
    {
        lock (attachLock)
        {
            try { if (p != null && !p.HasExited && h != IntPtr.Zero) return true; } catch { }
            DetachUnlocked();
            var ps = Process.GetProcessesByName("Battle_Realms_F");
            if (ps.Length == 0) return false;
            p = ps[0];
            try { moduleBase = p.MainModule!.BaseAddress.ToInt64(); } catch { p = null; return false; }
            h = OpenProcess(Access, false, p.Id);
            lastUnitCache = DateTime.MinValue;
            lastBuildingScan = DateTime.MinValue;
            localUnits = Array.Empty<UnitInfo>();
            return h != IntPtr.Zero;
        }
    }

    public static void Detach()
    {
        lock (attachLock) DetachUnlocked();
    }

    static void DetachUnlocked()
    {
        if (h != IntPtr.Zero) { CloseHandle(h); h = IntPtr.Zero; }
        p = null;
        localUnits = Array.Empty<UnitInfo>();
        unitPool = 0;
    }

    static bool ReadExact(long addr, byte[] b)
    {
        IntPtr hh = h;
        return hh != IntPtr.Zero && ReadProcessMemory(hh, new IntPtr(addr), b, b.Length, out var n) && n.ToInt64() == b.Length;
    }

    static uint R32(long addr)
    {
        var b = new byte[4];
        return ReadExact(addr, b) ? BitConverter.ToUInt32(b, 0) : 0;
    }

    static bool W32(long addr, uint v)
    {
        IntPtr hh = h;
        if (hh == IntPtr.Zero) return false;
        var b = BitConverter.GetBytes(v);
        return WriteProcessMemory(hh, new IntPtr(addr), b, 4, out var n) && n.ToInt64() == 4;
    }

    static uint Fixed16(uint v)
    {
        ulong x = ((ulong)v) << 16;
        return x > uint.MaxValue ? uint.MaxValue : (uint)x;
    }

    static bool RefreshUnits(bool force = false)
    {
        if (!Attach()) return false;
        if (!force && (DateTime.UtcNow - lastUnitCache).TotalMilliseconds < 250) return true;

        uint lid = R32(moduleBase + RVA_LOCAL_ID);
        uint pool = R32(moduleBase + RVA_UNIT_POOL);
        if (pool == 0) return false;
        if (!ReadExact(pool, unitRaw)) return false;

        var found = new List<UnitInfo>(256);
        for (int i = 0; i < UNIT_COUNT; i++)
        {
            int o = i * UNIT_STRIDE;
            uint def = BitConverter.ToUInt32(unitRaw, o + OFF_DEF);
            if (def == 0 || BitConverter.ToUInt32(unitRaw, o + OFF_OWNER) != lid) continue;

            uint mh = R32((long)def + 0x6C);
            uint ms = R32((long)def + 0x80);
            if (mh == 0 && ms == 0) continue;
            found.Add(new UnitInfo((long)pool + o, def, Fixed16(mh), Fixed16(ms)));
        }

        localId = lid;
        unitPool = pool;
        localUnits = found.ToArray();
        lastUnitCache = DateTime.UtcNow;
        return true;
    }

    static void FastLockLoop()
    {
        var sel = new byte[8];
        while (running)
        {
            if (!wantHp && !wantStamina) { selectedLocked = 0; Thread.Sleep(25); continue; }
            if (!RefreshUnits()) { selectedLocked = 0; Thread.Sleep(50); continue; }

            int count = 0;
            uint lid = localId;
            var units = localUnits;
            foreach (var u in units)
            {
                // Owner is revalidated so a reused slot can never affect an enemy/new owner.
                if (R32(u.Addr + OFF_OWNER) != lid) continue;
                if (!ReadExact(u.Addr + OFF_SEL_A, sel)) continue;
                if (BitConverter.ToUInt32(sel, 0) != 1 || BitConverter.ToUInt32(sel, 4) != 1) continue;

                count++;
                // Only CURRENT HP/stamina are touched. Max-health fields and +0x6A4 invincibility are never written.
                if (wantHp && u.MaxHpFixed != 0) W32(u.Addr + OFF_HP, u.MaxHpFixed);
                if (wantStamina && u.MaxStFixed != 0) W32(u.Addr + OFF_ST, u.MaxStFixed);
            }
            selectedLocked = count;
            Thread.Sleep(5);
        }
    }

    static void ApplyInstantUnitTraining(uint lid)
    {
        // One scan every 50 ms is enough: the game's training tick sees progress=100% and completes normally.
        if ((DateTime.UtcNow - lastBuildingScan).TotalMilliseconds < 50) return;
        lastBuildingScan = DateTime.UtcNow;

        uint pool = R32(moduleBase + RVA_BUILDING_POOL);
        if (pool == 0 || !ReadExact(pool, buildingRaw)) { trainingBuildings = 0; return; }

        int active = 0;
        for (int i = 0; i < BUILDING_COUNT; i++)
        {
            int o = i * BUILDING_STRIDE;
            uint owner = BitConverter.ToUInt32(buildingRaw, o + OFF_BUILD_OWNER);
            uint trainee = BitConverter.ToUInt32(buildingRaw, o + OFF_TRAIN_UNIT);
            uint trainType = BitConverter.ToUInt32(buildingRaw, o + OFF_TRAIN_TYPE);
            if (owner != lid || trainee == 0 || trainType == 0xFFFFFFFF) continue;

            active++;
            long addr = (long)pool + o;
            W32(addr + OFF_TRAIN_PROGRESS, TRAIN_COMPLETE_FIXED);
        }
        trainingBuildings = active;
    }

    public static string Apply(bool rice, bool water, bool yinYang, bool pop, bool instantTrain)
    {
        if (!Attach()) return "Waiting for Battle_Realms_F.exe...";
        uint lid = R32(moduleBase + RVA_LOCAL_ID);
        uint playerPtr = R32(moduleBase + RVA_PLAYER_PTR);
        if (playerPtr == 0) return "Attached, waiting for match/player data...";

        localId = lid;
        long player = (long)playerPtr + (long)lid * PLAYER_STRIDE;
        if (rice) W32(player + OFF_RICE, 50000);
        if (water) W32(player + OFF_WATER, 50000);
        if (yinYang) { W32(player + OFF_YIN, 10); W32(player + OFF_YANG, 10); }
        if (pop) W32(moduleBase + RVA_MAX_UNITS + lid * 4, 9_999_999);
        if (instantTrain) ApplyInstantUnitTraining(lid); else trainingBuildings = 0;

        return $"Attached | selected lock: {selectedLocked} | unit training: {trainingBuildings} | F1:{rice} F2:{water} F3:{yinYang} F4:{pop} F5:{wantStamina} F6:{wantHp} F7:{instantTrain}";
    }
}
