using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEUnitCloneLab;

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
    readonly Button copy = new() { Text = "COPY SELECTED" };
    readonly Button paste = new() { Text = "PASTE BESIDE" };
    readonly Button refresh = new() { Text = "REFRESH / REATTACH" };
    readonly NumericUpDown offset = new() { Minimum = 2, Maximum = 40, DecimalPlaces = 1, Increment = 1, Value = 8, Width = 80 };
    readonly Label game = new();
    readonly Label copied = new();
    readonly Label runtime = new();
    readonly Label note = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };

    public MainForm()
    {
        Text = "BRZE Unit Clone Lab — Native Copy/Paste (Hero Allowed)";
        ClientSize = new Size(920, 520);
        MinimumSize = new Size(860, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9.5f);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var head = Card();
        head.Controls.Add(new Label { Text = "UNIT COPY LAB  //  NATIVE POSITION SPAWN", Font = new Font("Segoe UI Semibold", 17f), AutoSize = true, Location = new Point(16, 9), ForeColor = Color.FromArgb(97, 224, 179) });
        head.Controls.Add(new Label { Text = "Copies the current selection — normal units, heroes and unique/story unit types are NOT filtered.", AutoSize = true, Location = new Point(18, 43), ForeColor = Color.FromArgb(165, 181, 202) });
        root.Controls.Add(head, 0, 0);

        var actions = Card();
        copy.SetBounds(16, 18, 180, 38); paste.SetBounds(208, 18, 180, 38); refresh.SetBounds(400, 18, 180, 38);
        Style(copy, Color.FromArgb(38, 137, 111)); Style(paste, Color.FromArgb(38, 137, 111)); Style(refresh, Color.FromArgb(43, 55, 72));
        actions.Controls.Add(copy); actions.Controls.Add(paste); actions.Controls.Add(refresh);
        actions.Controls.Add(new Label { Text = "SIDE OFFSET", AutoSize = true, Location = new Point(618, 15), ForeColor = Color.FromArgb(170, 181, 197) });
        offset.Location = new Point(716, 13); actions.Controls.Add(offset);
        actions.Controls.Add(new Label { Text = "world units", AutoSize = true, Location = new Point(802, 16), ForeColor = Color.FromArgb(140, 151, 168) });
        root.Controls.Add(actions, 0, 1);

        var c = Card(); c.Controls.Add(Title("COPIED SNAPSHOT", 10));
        copied.Font = new Font(FontFamily.GenericMonospace, 9f); copied.Location = new Point(16, 38); copied.Size = new Size(850, 80); copied.AutoSize = false;
        c.Controls.Add(copied); root.Controls.Add(c, 0, 2);

        var r = Card(); r.Controls.Add(Title("PASTE MONITOR", 10));
        runtime.Font = new Font(FontFamily.GenericMonospace, 9f); runtime.Location = new Point(16, 38); runtime.Size = new Size(850, 80); runtime.AutoSize = false;
        r.Controls.Add(runtime); root.Controls.Add(r, 0, 3);

        var foot = Card();
        game.AutoSize = true; game.Location = new Point(16, 12); game.ForeColor = Color.FromArgb(97, 224, 179);
        note.AutoSize = false; note.Location = new Point(16, 36); note.Size = new Size(850, 52); note.ForeColor = Color.FromArgb(236, 183, 84);
        note.Text = "LAB RULE: close the main trainer while testing this lab. It uses the same proven render-frame hook site as Instant Death. Hero copy is intentionally allowed; runtime behavior is experimental until tested.";
        foot.Controls.Add(game); foot.Controls.Add(note); root.Controls.Add(foot, 0, 4);

        copy.Click += (_, _) =>
        {
            var result = CloneCore.CopySelection();
            copied.Text = result.Message;
        };
        paste.Click += (_, _) =>
        {
            var result = CloneCore.Paste((float)offset.Value);
            runtime.Text = result;
        };
        refresh.Click += (_, _) =>
        {
            CloneCore.ResetRuntimeKeepCopy();
            runtime.Text = "Runtime detached. The next action will resolve process/module base again.";
        };

        timer.Tick += (_, _) =>
        {
            game.Text = CloneCore.GameStatus();
            copied.Text = CloneCore.CopyStatus();
            runtime.Text = CloneCore.RuntimeStatus();
        };
        timer.Start();
        FormClosed += (_, _) => CloneCore.ResetAll();
    }

    static Panel Card() => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 24, 34), Padding = new Padding(8) };
    static Label Title(string text, int y) => new() { Text = text, Font = new Font("Segoe UI Semibold", 10.5f), ForeColor = Color.FromArgb(97, 224, 179), AutoSize = true, Location = new Point(14, y) };
    static void Style(Button b, Color bg) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Color.FromArgb(72, 91, 116); b.BackColor = bg; b.ForeColor = Color.White; b.Font = new Font("Segoe UI Semibold", 9f); }
}

internal readonly record struct CopyResult(bool Ok, string Message);
internal readonly record struct UnitSnap(uint Address, uint Type, uint Owner, float X, float Y);

internal static class CloneCore
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualFreeEx(IntPtr h, IntPtr addr, UIntPtr size, uint freeType);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x0010 | 0x0020 | 0x0008 | 0x0400;
    const uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, MEM_RELEASE = 0x8000, PAGE_EXECUTE_READWRITE = 0x40;

    // Game-specific RVAs for Battle_Realms_F(5).exe / BRZE 1.60 target.
    const int RVA_SELECTION_LIST = 0x441708;
    const int RVA_FRAME_MOUSE_DRAW = 0x135C43;
    const int RVA_SPAWN_AT_XY = 0x0C4A1C; // VA 004C4A1C, script/native position-spawn wrapper
    const int OFF_DEF = 0x74, OFF_OWNER = 0x240, OFF_X = 0x28, OFF_Y = 0x2C;
    const int MAX_COPY = 120;
    const int ENTRY_SIZE = 16;

    // Same exact six bytes used by the runtime-proven Instant Death frame hook.
    static readonly byte[] FrameOriginal = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x5C };
    // Position-spawn wrapper prologue observed in the target executable.
    static readonly byte[] SpawnPrologue = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C };

    const int STUB = 0x000;
    const int ACTIVE = 0x400;
    const int COUNT = 0x404;
    const int INDEX = 0x408;
    const int SUCCESS = 0x40C;
    const int FAIL = 0x410;
    const int OUT_ID = 0x414;
    const int LAST_OUT_ID = 0x418;
    const int ENTRIES = 0x800;
    const int FXSCRATCH = 0x2000;
    const int CAVE_SIZE = 0x4000;

    static readonly object Sync = new();
    static readonly List<UnitSnap> copied = new();
    static Process? process;
    static IntPtr h = IntPtr.Zero, cave = IntPtr.Zero;
    static long moduleBase;
    static bool installed;
    static string status = "not attached";
    static string copyStatus = "No copied selection yet.";
    static int pasteSerial;

    static IntPtr A(long x) => new(unchecked((int)(uint)x));
    static bool ReadExact(long a, byte[] b) => h != IntPtr.Zero && ReadProcessMemory(h, A(a), b, b.Length, out var n) && n.ToInt64() == b.Length;
    static bool WriteBytes(long a, byte[] b) => h != IntPtr.Zero && WriteProcessMemory(h, A(a), b, b.Length, out var n) && n.ToInt64() == b.Length;
    static uint R32(long a) { var b = new byte[4]; return ReadExact(a, b) ? BitConverter.ToUInt32(b, 0) : 0; }
    static float RF32(long a) { var b = new byte[4]; return ReadExact(a, b) ? BitConverter.ToSingle(b, 0) : 0f; }
    static bool W32(long a, uint v) => WriteBytes(a, BitConverter.GetBytes(v));
    static bool WF32(long a, float v) => WriteBytes(a, BitConverter.GetBytes(v));

    static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static bool WriteCode(long a, byte[] b)
    {
        if (h == IntPtr.Zero) return false;
        if (!VirtualProtectEx(h, A(a), (UIntPtr)b.Length, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteBytes(a, b);
        FlushInstructionCache(h, A(a), (UIntPtr)b.Length);
        VirtualProtectEx(h, A(a), (UIntPtr)b.Length, old, out _);
        return ok;
    }

    static void I32(List<byte> b, int v) => b.AddRange(BitConverter.GetBytes(v));
    static void U32(List<byte> b, uint v) => b.AddRange(BitConverter.GetBytes(v));
    static void PatchRel(List<byte> b, int at, long fromNext, long to)
    {
        var x = BitConverter.GetBytes(unchecked((int)(to - fromNext)));
        for (int i = 0; i < 4; i++) b[at + i] = x[i];
    }

    static bool Attach()
    {
        try { if (process != null && !process.HasExited && h != IntPtr.Zero) return true; } catch { }
        DetachRuntime();
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) { status = "waiting for Battle_Realms_F.exe"; return false; }
        process = ps[0];
        try { moduleBase = process.MainModule!.BaseAddress.ToInt64(); }
        catch { process = null; status = "cannot resolve module base"; return false; }
        h = OpenProcess(ACCESS, false, process.Id);
        if (h == IntPtr.Zero) { status = "OpenProcess failed"; return false; }
        status = $"attached PID {process.Id} base 0x{moduleBase:X8}";
        return true;
    }

    static bool ValidateSpawnWrapper()
    {
        var b = new byte[SpawnPrologue.Length];
        return ReadExact(moduleBase + RVA_SPAWN_AT_XY, b) && Same(b, SpawnPrologue);
    }

    public static CopyResult CopySelection()
    {
        lock (Sync)
        {
            if (!Attach()) return new(false, "COPY failed — game not attached.");
            long list = moduleBase + RVA_SELECTION_LIST;
            uint count = R32(list + 0x18);
            uint node = R32(list);
            if (count == 0 || node == 0)
            {
                copied.Clear(); copyStatus = "Nothing selected."; return new(false, copyStatus);
            }

            var tmp = new List<UnitSnap>();
            var seenNodes = new HashSet<uint>();
            var seenUnits = new HashSet<uint>();
            int limit = (int)Math.Min(count, MAX_COPY);
            for (int i = 0; i < limit && node != 0; i++)
            {
                if (!seenNodes.Add(node)) break;
                uint next = R32((long)node + 0x00);
                uint unit = R32((long)node + 0x08);
                if (unit != 0 && seenUnits.Add(unit))
                {
                    uint def = R32((long)unit + OFF_DEF);
                    uint type = def == 0 ? 0xFFFFFFFFu : R32(def);
                    uint owner = R32((long)unit + OFF_OWNER);
                    float x = RF32((long)unit + OFF_X), y = RF32((long)unit + OFF_Y);
                    if (def != 0 && type != 0xFFFFFFFFu && !float.IsNaN(x) && !float.IsNaN(y) && !float.IsInfinity(x) && !float.IsInfinity(y))
                        tmp.Add(new UnitSnap(unit, type, owner, x, y));
                }
                node = next;
            }

            copied.Clear(); copied.AddRange(tmp); pasteSerial = 0;
            if (copied.Count == 0) { copyStatus = "Selection list resolved, but no valid units were captured."; return new(false, copyStatus); }

            var groups = copied.GroupBy(x => new { x.Type, x.Owner }).Select(g => $"type 0x{g.Key.Type:X} owner {g.Key.Owner} ×{g.Count()}").Take(8).ToArray();
            copyStatus = $"COPIED {copied.Count}/{count} selected units. Hero/unique types are allowed.\r\n" + string.Join("  |  ", groups) + (copied.GroupBy(x => new { x.Type, x.Owner }).Count() > 8 ? "  |  ..." : "");
            return new(true, copyStatus);
        }
    }

    static byte[] BuildStub(long stubBase)
    {
        long active = stubBase + ACTIVE, count = stubBase + COUNT, index = stubBase + INDEX, success = stubBase + SUCCESS, fail = stubBase + FAIL;
        long outId = stubBase + OUT_ID, lastOut = stubBase + LAST_OUT_ID, entries = stubBase + ENTRIES, fx = stubBase + FXSCRATCH;
        uint spawn = (uint)(moduleBase + RVA_SPAWN_AT_XY);

        var b = new List<byte>();
        b.Add(0x9C); b.Add(0x60); // pushfd / pushad
        b.AddRange(new byte[] { 0x0F, 0xAE, 0x05 }); U32(b, (uint)fx); // fxsave [abs]

        b.Add(0xA1); U32(b, (uint)active); // eax=[active]
        b.AddRange(new byte[] { 0x85, 0xC0, 0x0F, 0x84 }); int jInactive = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xBB, 0x04, 0x00, 0x00, 0x00 }); // ebx=4 units/frame

        int loop = b.Count;
        b.AddRange(new byte[] { 0x8B, 0x0D }); U32(b, (uint)index); // ecx=index
        b.AddRange(new byte[] { 0x8B, 0x15 }); U32(b, (uint)count); // edx=count
        b.AddRange(new byte[] { 0x3B, 0xCA, 0x0F, 0x83 }); int jFinished = b.Count; I32(b, 0); // cmp ecx,edx / jae
        b.AddRange(new byte[] { 0x8B, 0xC1, 0xC1, 0xE0, 0x04 }); // eax=ecx<<4
        b.Add(0x05); U32(b, (uint)entries); // add eax,entries

        b.AddRange(new byte[] { 0xC7, 0x05 }); U32(b, (uint)outId); U32(b, 0x0000FFFFu);
        b.AddRange(new byte[] { 0xFF, 0x70, 0x0C }); // push [eax+0xC] y
        b.AddRange(new byte[] { 0xFF, 0x70, 0x08 }); // push [eax+0x8] x
        b.Add(0x68); U32(b, (uint)outId);             // push &outId
        b.AddRange(new byte[] { 0xFF, 0x70, 0x04 }); // push owner
        b.AddRange(new byte[] { 0xFF, 0x30 });       // push type
        b.Add(0xB8); U32(b, spawn);                  // mov eax,spawn
        b.AddRange(new byte[] { 0xFF, 0xD0 });       // call eax (callee ret 0x14)

        b.Add(0xA1); U32(b, (uint)outId);             // eax=outId
        b.Add(0xA3); U32(b, (uint)lastOut);           // lastOut=eax
        b.AddRange(new byte[] { 0x3D, 0xFF, 0xFF, 0x00, 0x00, 0x0F, 0x84 }); int jFail = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)success);
        b.Add(0xEB); int jAfterStat8 = b.Count; b.Add(0);
        int failLabel = b.Count;
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)fail);
        int afterStat = b.Count;
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)index);
        b.Add(0x4B); // dec ebx
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jLoop = b.Count; I32(b, 0);

        // Budget exhausted. If more remain, keep active for next frame.
        b.AddRange(new byte[] { 0x8B, 0x0D }); U32(b, (uint)index);
        b.AddRange(new byte[] { 0x8B, 0x15 }); U32(b, (uint)count);
        b.AddRange(new byte[] { 0x3B, 0xCA, 0x0F, 0x82 }); int jKeepActive = b.Count; I32(b, 0); // jb restore

        int finished = b.Count;
        b.AddRange(new byte[] { 0xC7, 0x05 }); U32(b, (uint)active); U32(b, 0);

        int restore = b.Count;
        b.AddRange(new byte[] { 0x0F, 0xAE, 0x0D }); U32(b, (uint)fx); // fxrstor [abs]
        b.Add(0x61); b.Add(0x9D);
        b.AddRange(FrameOriginal);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);

        PatchRel(b, jInactive, stubBase + jInactive + 4, stubBase + restore);
        PatchRel(b, jFinished, stubBase + jFinished + 4, stubBase + finished);
        PatchRel(b, jFail, stubBase + jFail + 4, stubBase + failLabel);
        b[jAfterStat8] = unchecked((byte)(afterStat - (jAfterStat8 + 1)));
        PatchRel(b, jLoop, stubBase + jLoop + 4, stubBase + loop);
        PatchRel(b, jKeepActive, stubBase + jKeepActive + 4, stubBase + restore);
        PatchRel(b, jBack, stubBase + jBack + 4, moduleBase + RVA_FRAME_MOUSE_DRAW + FrameOriginal.Length);
        return b.ToArray();
    }

    static bool InstallHook()
    {
        if (installed) return true;
        if (!Attach()) return false;
        if (!ValidateSpawnWrapper()) { status = "SPAWN WRAPPER BYTES MISMATCH — target executable/version differs"; return false; }

        long site = moduleBase + RVA_FRAME_MOUSE_DRAW;
        var now = new byte[FrameOriginal.Length];
        if (!ReadExact(site, now) || !Same(now, FrameOriginal))
        {
            status = "FRAME HOOK BUSY/MISMATCH — close the main trainer and restart/refresh this lab";
            return false;
        }

        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)CAVE_SIZE, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (cave == IntPtr.Zero) { status = "VirtualAllocEx failed"; return false; }
        long c = cave.ToInt64();
        if ((c + FXSCRATCH) % 16 != 0) { status = "FX scratch alignment failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; return false; }

        if (!WriteBytes(c + ACTIVE, new byte[0x30])) { status = "state init failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; return false; }
        W32(c + OUT_ID, 0xFFFF); W32(c + LAST_OUT_ID, 0xFFFF);
        var code = BuildStub(c + STUB);
        if (!WriteBytes(c + STUB, code)) { status = "stub write failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; return false; }

        var patch = new List<byte> { 0xE9 }; I32(patch, unchecked((int)((c + STUB) - (site + 5)))); patch.Add(0x90);
        if (!WriteCode(site, patch.ToArray()))
        {
            status = "frame hook patch failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; return false;
        }
        installed = true; status = "NATIVE COPY/PASTE HOOK ACTIVE"; return true;
    }

    public static string Paste(float sideOffset)
    {
        lock (Sync)
        {
            if (copied.Count == 0) return "PASTE ignored — COPY SELECTED first.";
            if (!Attach() || !InstallHook()) return "PASTE blocked — " + status;
            long c = cave.ToInt64();
            if (R32(c + ACTIVE) != 0) return "PASTE busy — previous queue is still spawning.";

            pasteSerial++;
            float dx = sideOffset * pasteSerial;
            int n = Math.Min(copied.Count, MAX_COPY);
            var bytes = new byte[n * ENTRY_SIZE];
            for (int i = 0; i < n; i++)
            {
                UnitSnap s = copied[i]; int o = i * ENTRY_SIZE;
                Buffer.BlockCopy(BitConverter.GetBytes(s.Type), 0, bytes, o + 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.Owner), 0, bytes, o + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.X + dx), 0, bytes, o + 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.Y), 0, bytes, o + 12, 4);
            }
            if (!WriteBytes(c + ENTRIES, bytes)) return "PASTE failed — queue write failed.";
            W32(c + COUNT, (uint)n); W32(c + INDEX, 0); W32(c + SUCCESS, 0); W32(c + FAIL, 0); W32(c + OUT_ID, 0xFFFF); W32(c + LAST_OUT_ID, 0xFFFF);
            W32(c + ACTIVE, 1); // arm last
            status = $"PASTE #{pasteSerial} armed: {n} units, +X {dx:0.0}";
            return status;
        }
    }

    public static string GameStatus()
    {
        lock (Sync)
        {
            if (!Attach()) return "GAME: waiting for Battle_Realms_F.exe";
            return $"GAME: attached • PID {process!.Id} • base 0x{moduleBase:X8}";
        }
    }

    public static string CopyStatus() { lock (Sync) return copyStatus; }

    public static string RuntimeStatus()
    {
        lock (Sync)
        {
            if (!Attach()) return "Runtime: not attached.";
            if (!installed || cave == IntPtr.Zero) return $"Runtime: {status}\r\nCopy is read-only; Paste installs the game-thread native spawn hook only when needed.";
            long c = cave.ToInt64();
            uint active = R32(c + ACTIVE), count = R32(c + COUNT), index = R32(c + INDEX), ok = R32(c + SUCCESS), fail = R32(c + FAIL), id = R32(c + LAST_OUT_ID);
            return $"{status}\r\nqueue: {(active != 0 ? "ACTIVE" : "IDLE")}  index:{index}/{count}  success:{ok}  fail:{fail}  last unit id:0x{id:X4}\r\nPaste executes max 4 native spawns per render frame; copied formation is shifted on +X for each paste.";
        }
    }

    public static void ResetRuntimeKeepCopy() { lock (Sync) DetachRuntime(); }
    public static void ResetAll() { lock (Sync) { DetachRuntime(); copied.Clear(); copyStatus = "No copied selection yet."; pasteSerial = 0; } }

    static void DetachRuntime()
    {
        try
        {
            if (h != IntPtr.Zero && installed)
            {
                long site = moduleBase + RVA_FRAME_MOUSE_DRAW;
                var now = new byte[6];
                if (ReadExact(site, now) && now.Length == 6 && now[0] == 0xE9) WriteCode(site, FrameOriginal);
            }
            if (h != IntPtr.Zero && cave != IntPtr.Zero) VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE);
            if (h != IntPtr.Zero) CloseHandle(h);
        }
        catch { }
        process = null; h = IntPtr.Zero; cave = IntPtr.Zero; moduleBase = 0; installed = false; status = "not attached";
    }
}
