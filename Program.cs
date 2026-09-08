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
    readonly CheckBox f7 = new() { Text = "F7 Instant Unit Training (TEST 2)", AutoSize = true };
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

    const uint Access = 0x10 | 0x20 | 0x8 | 0x400;
    const int RVA_PLAYER_PTR = 0x4416A0, RVA_LOCAL_ID = 0x4416D0, PLAYER_STRIDE = 0x5E8;
    const int OFF_RICE = 0xD8, OFF_WATER = 0xDC, OFF_YIN = 0x2E8, OFF_YANG = 0x2EC;
    const int RVA_MAX_UNITS = 0x467B90;

    const int RVA_UNIT_POOL = 0x4796A0, UNIT_STRIDE = 0x818, UNIT_COUNT = 2000;
    const int OFF_DEF = 0x74, OFF_OWNER = 0x240, OFF_SEL_A = 0x3A8, OFF_SEL_B = 0x3AC, OFF_HP = 0x404, OFF_ST = 0x408;

    // Exact BRZE 1.60 building pool: 500 objects x 0x6A4 from *(base+0x4814E0).
    const int RVA_BUILDING_POOL = 0x4814E0, BUILDING_STRIDE = 0x6A4, BUILDING_COUNT = 500;
    const int OFF_BUILD_OWNER = 0x84, OFF_TRAIN_TYPE = 0x488, OFF_TRAIN_PROGRESS = 0x490, OFF_TRAIN_GATE = 0x4B8, OFF_BUILD_SPECIAL = 0x68C;
    const int RVA_TRAIN_SPECIAL_GLOBAL = 0x46779C; // absolute 0x86779C in the 0x400000 image.
    const uint TRAIN_COMPLETE_FIXED = 0x00640000;

    static readonly object attachLock = new();
    static IntPtr h = IntPtr.Zero;
    static Process? p;
    static long moduleBase;
    static volatile uint localId;
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
        public readonly uint MaxHpFixed;
        public readonly uint MaxStFixed;
        public UnitInfo(long addr, uint hp, uint st) { Addr = addr; MaxHpFixed = hp; MaxStFixed = st; }
    }

    public static void Start()
    {
        if (running) return;
        running = true;
        lockThread = new Thread(FastLockLoop) { IsBackground = true, Name = "BRZE-HP-ST-Lock" };
        lockThread.Start();
    }

    public static void Stop()
    {
        running = false;
        try { lockThread?.Join(250); } catch { }
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
        if (!force && (DateTime.UtcNow - lastUnitCache).TotalMilliseconds < 500) return true;

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
            found.Add(new UnitInfo((long)pool + o, Fixed16(mh), Fixed16(ms)));
        }

        localId = lid;
        localUnits = found.ToArray();
        lastUnitCache = DateTime.UtcNow;
        return true;
    }

    static void FastLockLoop()
    {
        var sel = new byte[8];
        var cur = new byte[8];
        while (running)
        {
            if (!wantHp && !wantStamina) { selectedLocked = 0; Thread.Sleep(25); continue; }
            if (!RefreshUnits()) { selectedLocked = 0; Thread.Sleep(50); continue; }

            int count = 0;
            uint lid = localId;
            var units = localUnits;
            foreach (var u in units)
            {
                // First do the cheap selection check. Unselected units cause no writes at all.
                if (!ReadExact(u.Addr + OFF_SEL_A, sel)) continue;
                if (BitConverter.ToUInt32(sel, 0) != 1 || BitConverter.ToUInt32(sel, 4) != 1) continue;

                // Revalidate owner only for selected units, protecting against a slot reused by another player.
                if (R32(u.Addr + OFF_OWNER) != lid) continue;
                if (!ReadExact(u.Addr + OFF_HP, cur)) continue; // HP and stamina are adjacent.

                count++;
                uint hpNow = BitConverter.ToUInt32(cur, 0);
                uint stNow = BitConverter.ToUInt32(cur, 4);

                // Critical performance fix: do NOT spam WriteProcessMemory every 5 ms for every selected unit.
                // Write only when the value actually dropped. This avoids simulation stalls with very large selections.
                // Also never lower a value that is already above the base max (buffs/modifiers remain untouched).
                if (wantHp && u.MaxHpFixed != 0 && hpNow < u.MaxHpFixed) W32(u.Addr + OFF_HP, u.MaxHpFixed);
                if (wantStamina && u.MaxStFixed != 0 && stNow < u.MaxStFixed) W32(u.Addr + OFF_ST, u.MaxStFixed);
            }

            selectedLocked = count;
            // Large groups need less aggressive polling so the game's simulation thread is not starved by RPM/WPM calls.
            Thread.Sleep(count >= 32 ? 16 : 8);
        }
    }

    static void ApplyInstantUnitTraining(uint lid)
    {
        if ((DateTime.UtcNow - lastBuildingScan).TotalMilliseconds < 25) return;
        lastBuildingScan = DateTime.UtcNow;

        uint pool = R32(moduleBase + RVA_BUILDING_POOL);
        if (pool == 0 || !ReadExact(pool, buildingRaw)) { trainingBuildings = 0; return; }

        uint specialGlobal = R32(moduleBase + RVA_TRAIN_SPECIAL_GLOBAL);
        int active = 0;
        for (int i = 0; i < BUILDING_COUNT; i++)
        {
            int o = i * BUILDING_STRIDE;
            uint owner = BitConverter.ToUInt32(buildingRaw, o + OFF_BUILD_OWNER);
            if (owner != lid) continue;

            uint trainType = BitConverter.ToUInt32(buildingRaw, o + OFF_TRAIN_TYPE);
            if (trainType == 0xFFFFFFFF) continue;

            // Mirror the game's own active-training predicate at 0x4D675F/0x4D673D:
            // type must be valid, and normally +0x4B8 must be valid. A special global/+0x68C state suppresses it.
            uint gate = BitConverter.ToUInt32(buildingRaw, o + OFF_TRAIN_GATE);
            uint special = BitConverter.ToUInt32(buildingRaw, o + OFF_BUILD_SPECIAL);
            bool gameSaysTrainingActive = !(specialGlobal != 0 && special != 0) && gate != 0xFFFFFFFF;
            if (!gameSaysTrainingActive) continue;

            active++;
            long addr = (long)pool + o;
            uint progress = BitConverter.ToUInt32(buildingRaw, o + OFF_TRAIN_PROGRESS);
            if (progress < TRAIN_COMPLETE_FIXED) W32(addr + OFF_TRAIN_PROGRESS, TRAIN_COMPLETE_FIXED);
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

        return $"Attached | selected lock: {selectedLocked} | active training: {trainingBuildings} | F1:{rice} F2:{water} F3:{yinYang} F4:{pop} F5:{wantStamina} F6:{wantHp} F7:{instantTrain}";
    }
}
