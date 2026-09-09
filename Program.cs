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
    readonly CheckBox f4 = new() { Text = "F4 Max Population + UI/SORT/SIM 120 + RECTANGLE THROTTLE 1ms", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(940, 0),
        Text = "Diagnostic only. Clean Sim120 base. Manual selection is untouched. Only the accepted-unit AddUnit call inside rectangle/drag selection is wrapped: original AddUnit runs normally, then the game thread yields for 1 ms before processing the next rectangle candidate. All type-0 events and simulation refresh logic stay LIVE. Enable F4 before selecting anything."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 142, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Rectangle AddUnit Throttle 1ms Probe";
        ClientSize = new Size(1000, 330);
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
    static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr address, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool VirtualFreeEx(IntPtr h, IntPtr address, UIntPtr size, uint freeType);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandle(string moduleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr module, string procName);
    [DllImport("kernel32.dll")]
    static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);

    const uint Access = 0x10 | 0x20 | 0x8 | 0x400;
    const uint PAGE_EXECUTE_READWRITE = 0x40;
    const uint MEM_COMMIT = 0x1000;
    const uint MEM_RESERVE = 0x2000;
    const uint MEM_RELEASE = 0x8000;

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
    const int RVA_SIM_INIT_FIRST_IMM = 0x1A6C53;
    const int RVA_SIM_RESET_FIRST_IMM = 0x1A6E37;
    const int SIM_LIST_STRIDE = 0x28;

    // Rectangle-only accepted-unit call site:
    // 0x5D4559 push esi
    // 0x5D455A E8 79 2A FD FF -> call 0x5A6FD8
    const int RVA_RECT_ADDUNIT_CALL = 0x1D455A;
    const int RVA_ADDUNIT = 0x1A6FD8;
    static readonly byte[] RECT_ORIGINAL_CALL = { 0xE8, 0x79, 0x2A, 0xFD, 0xFF };

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
    static bool rectThrottlePatched;
    static IntPtr rectStub = IntPtr.Zero;
    static byte[]? rectPatchedCall;

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

    static IntPtr A(long addr) => new(unchecked((int)(uint)addr));

    static uint R32(long addr)
    {
        if (h == IntPtr.Zero) return 0;
        var b = new byte[4];
        return ReadProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4
            ? BitConverter.ToUInt32(b, 0) : 0;
    }

    static byte R8(long addr)
    {
        if (h == IntPtr.Zero) return 0;
        var b = new byte[1];
        return ReadProcessMemory(h, A(addr), b, 1, out var n) && n.ToInt64() == 1 ? b[0] : (byte)0;
    }

    static byte[]? RBytes(long addr, int size)
    {
        if (h == IntPtr.Zero) return null;
        var b = new byte[size];
        return ReadProcessMemory(h, A(addr), b, size, out var n) && n.ToInt64() == size ? b : null;
    }

    static bool W32(long addr, uint value)
    {
        if (h == IntPtr.Zero) return false;
        var b = BitConverter.GetBytes(value);
        return WriteProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4;
    }

    static bool BytesEqual(byte[]? a, byte[] b)
    {
        if (a == null || a.Length != b.Length) return false;
        for (int i = 0; i < b.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static bool WriteCode(long addr, byte[] value)
    {
        if (h == IntPtr.Zero) return false;
        IntPtr a = A(addr);
        if (!VirtualProtectEx(h, a, (UIntPtr)value.Length, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteProcessMemory(h, a, value, value.Length, out var n) && n.ToInt64() == value.Length;
        if (ok) FlushInstructionCache(h, a, (UIntPtr)value.Length);
        VirtualProtectEx(h, a, (UIntPtr)value.Length, old, out _);
        return ok;
    }

    static bool WriteCodeByte(long addr, byte value) => WriteCode(addr, new[] { value });

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

    static long ResolveRemoteSleep()
    {
        if (p == null) return 0;
        IntPtr k32 = GetModuleHandle("kernel32.dll");
        if (k32 == IntPtr.Zero) return 0;
        IntPtr localSleep = GetProcAddress(k32, "Sleep");
        if (localSleep == IntPtr.Zero) return 0;

        ProcessModule? owner = null;
        foreach (ProcessModule m in Process.GetCurrentProcess().Modules)
        {
            long start = m.BaseAddress.ToInt64();
            long end = start + m.ModuleMemorySize;
            long f = localSleep.ToInt64();
            if (f >= start && f < end) { owner = m; break; }
        }
        if (owner == null) return 0;

        long offset = localSleep.ToInt64() - owner.BaseAddress.ToInt64();
        foreach (ProcessModule m in p.Modules)
        {
            if (string.Equals(m.ModuleName, owner.ModuleName, StringComparison.OrdinalIgnoreCase))
                return m.BaseAddress.ToInt64() + offset;
        }
        return 0;
    }

    static bool PatchRectangleThrottle()
    {
        if (rectThrottlePatched) return true;
        long callSite = moduleBase + RVA_RECT_ADDUNIT_CALL;
        byte[]? cur = RBytes(callSite, 5);
        if (!BytesEqual(cur, RECT_ORIGINAL_CALL))
        {
            patchError = "rect-addunit: unexpected original call bytes";
            return false;
        }

        long sleepAddr = ResolveRemoteSleep();
        if (sleepAddr == 0)
        {
            patchError = "rect-addunit: could not resolve remote Sleep";
            return false;
        }

        rectStub = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)0x100, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (rectStub == IntPtr.Zero)
        {
            patchError = "rect-addunit: code-cave allocation failed";
            return false;
        }

        uint addUnit = unchecked((uint)(moduleBase + RVA_ADDUNIT));
        uint sleep = unchecked((uint)sleepAddr);

        // Wrapper has the same stdcall shape as AddUnit: caller already did `push esi`.
        // mov eax,[esp+4]; push eax; mov eax,AddUnit; call eax;
        // push 1; mov eax,Sleep; call eax; ret 4
        var code = new byte[24];
        int i = 0;
        code[i++] = 0x8B; code[i++] = 0x44; code[i++] = 0x24; code[i++] = 0x04;
        code[i++] = 0x50;
        code[i++] = 0xB8; Array.Copy(BitConverter.GetBytes(addUnit), 0, code, i, 4); i += 4;
        code[i++] = 0xFF; code[i++] = 0xD0;
        code[i++] = 0x6A; code[i++] = 0x01;
        code[i++] = 0xB8; Array.Copy(BitConverter.GetBytes(sleep), 0, code, i, 4); i += 4;
        code[i++] = 0xFF; code[i++] = 0xD0;
        code[i++] = 0xC2; code[i++] = 0x04; code[i++] = 0x00;

        if (!WriteProcessMemory(h, rectStub, code, code.Length, out var nw) || nw.ToInt64() != code.Length)
        {
            patchError = "rect-addunit: code-cave write failed";
            VirtualFreeEx(h, rectStub, UIntPtr.Zero, MEM_RELEASE);
            rectStub = IntPtr.Zero;
            return false;
        }
        FlushInstructionCache(h, rectStub, (UIntPtr)code.Length);

        long rel = rectStub.ToInt64() - (callSite + 5);
        rectPatchedCall = new byte[5];
        rectPatchedCall[0] = 0xE8;
        Array.Copy(BitConverter.GetBytes(unchecked((int)rel)), 0, rectPatchedCall, 1, 4);

        if (!WriteCode(callSite, rectPatchedCall))
        {
            patchError = "rect-addunit: call patch failed";
            VirtualFreeEx(h, rectStub, UIntPtr.Zero, MEM_RELEASE);
            rectStub = IntPtr.Zero;
            rectPatchedCall = null;
            return false;
        }

        rectThrottlePatched = BytesEqual(RBytes(callSite, 5), rectPatchedCall);
        if (!rectThrottlePatched) patchError = "rect-addunit: verification failed";
        return rectThrottlePatched;
    }

    static void RestoreRectangleThrottle()
    {
        if (h == IntPtr.Zero) return;
        long callSite = moduleBase + RVA_RECT_ADDUNIT_CALL;
        if (rectThrottlePatched && rectPatchedCall != null && BytesEqual(RBytes(callSite, 5), rectPatchedCall))
            WriteCode(callSite, RECT_ORIGINAL_CALL);
        rectThrottlePatched = false;
        rectPatchedCall = null;
        if (rectStub != IntPtr.Zero)
        {
            VirtualFreeEx(h, rectStub, UIntPtr.Zero, MEM_RELEASE);
            rectStub = IntPtr.Zero;
        }
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
        if (first == 120 && growth == 0) return true;
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
        return $"n:{R32(list + OFF_COUNT)} b:{R32(list + OFF_BLOCK_COUNT)} first:{R32(list + OFF_FIRST)} grow:{R32(list + OFF_GROWTH)}";
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

            if (!sortPatched && !PatchSortPipeline()) return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            if (!simCodePatched && !PatchSimulationCode()) return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            if (!activeArmed) activeArmed = ArmPristineList(active, "active");

            if (simLocal == 0)
            {
                patchError = "simulation selection list not initialized yet";
                return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            }
            if (!simArmed) simArmed = ArmPristineList(simLocal, "sim-local");
            if (!rectThrottlePatched && !PatchRectangleThrottle()) return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");

            if (activeArmed && sortPatched && simCodePatched && simArmed && rectThrottlePatched &&
                R32(active + OFF_GROWTH) == 0 && R32(simLocal + OFF_GROWTH) == 0)
            {
                if (!PatchExpectedByte(RVA_MANUAL_CAP_IMM, 0x5A, 0x78, "manual-cap"))
                    return Status(active, sortA, sortB, simLocal, capAddr, lid, "NOT ARMED");
            }
        }
        else
        {
            RestoreRectangleThrottle();
            if (cap == 0x78) WriteCodeByte(capAddr, 0x5A);
        }

        string mode = activeArmed && sortPatched && simCodePatched && simArmed && rectThrottlePatched
            ? "ARMED rect-throttle-1ms"
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
        string rect = rectThrottlePatched ? "1ms" : "OFF";
        string err = patchError.Length == 0 ? "" : $" | ERROR:{patchError}";

        return $"pid:{pid} | {mode} | player:{lid} | cap:0x{cap:X2} | sort:{a:X2}/{b:X2}/{r:X2} | sim-code:{si:X2}/{sr:X2}\r\n" +
               $"rect-addunit-throttle:{rect} | event0:LIVE | sim-refresh:LIVE\r\n" +
               $"ACTIVE {ListState(active)} | SIM {ListState(simLocal)} | SORT-A {ListState(sortA)} | SORT-B {ListState(sortB)}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero)
        {
            RestoreRectangleThrottle();
            long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
            if (R8(capAddr) == 0x78) WriteCodeByte(capAddr, 0x5A);
        }
        DetachHandleOnly();
        patchError = "";
        activeArmed = false;
        sortPatched = false;
        simCodePatched = false;
        simArmed = false;
        rectThrottlePatched = false;
        rectStub = IntPtr.Zero;
        rectPatchedCall = null;
    }

    static void DetachHandleOnly()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero;
        p = null;
        moduleBase = 0;
        pid = 0;
        rectThrottlePatched = false;
        rectStub = IntPtr.Zero;
        rectPatchedCall = null;
    }
}
