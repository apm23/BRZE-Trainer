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
    readonly CheckBox f4 = new() { Text = "F4 Max Population + UI/SORT/SIM 120 + EVENT BUFFER 1024 — BULK DRAG PROBE", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(940, 0),
        Text = "Diagnostic only. Keeps UI/sort/simulation selection at 120 with LIVE events, then replaces the native 256-byte event buffer with a real 1024-byte allocation from BRZE's own allocator. This avoids the forced mid-drag flush after roughly 64 selection-add events. Enable F4 before selecting anything."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 158, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Selection Event Buffer 1024 Probe";
        ClientSize = new Size(1010, 350);
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
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr CreateRemoteThread(IntPtr h, IntPtr attrs, UIntPtr stackSize, IntPtr startAddress, IntPtr parameter, uint creationFlags, out uint threadId);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetExitCodeThread(IntPtr thread, out uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int key);

    const uint PROCESS_CREATE_THREAD = 0x0002;
    const uint Access = PROCESS_CREATE_THREAD | 0x10 | 0x20 | 0x8 | 0x400;
    const uint PAGE_EXECUTE_READWRITE = 0x40;
    const uint MEM_COMMIT = 0x1000;
    const uint MEM_RESERVE = 0x2000;
    const uint MEM_RELEASE = 0x8000;
    const uint WAIT_OBJECT_0 = 0;

    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_MAX_UNITS = 0x467B90;
    const int RVA_ACTIVE = 0x441708;
    const int RVA_SORT_A = 0x441784;
    const int RVA_SORT_B = 0x4417AC;
    const int RVA_SIM_LISTS_PTR = 0x441730;
    const int RVA_DRAG_CANDIDATES = 0x479748;

    const int RVA_MANUAL_CAP_IMM = 0x1A7006;
    const int RVA_SORT_A_FIRST_IMM = 0x1A71B1;
    const int RVA_SORT_B_FIRST_IMM = 0x1A71B9;
    const int RVA_SORT_ACTIVE_FIRST_IMM = 0x1A7299;
    const int RVA_SIM_INIT_FIRST_IMM = 0x1A6C53;
    const int RVA_SIM_RESET_FIRST_IMM = 0x1A6E37;
    const int SIM_LIST_STRIDE = 0x28;

    // Global command/event queue used by selection-add event 0x552FFA.
    const int RVA_EVENT_GATE = 0x441194;
    const int RVA_EVENT_USED = 0x441C94;
    const int RVA_EVENT_REMAIN = 0x441C98;
    const int RVA_EVENT_PTR = 0x441C9C;
    const uint EVENT_BUFFER_NEW_SIZE = 0x400;

    // All three are the 0x01 byte in a little-endian 0x00000100 immediate.
    // Changing only this byte 01 -> 04 converts 0x100 -> 0x400 without touching instruction layout.
    const int RVA_EVENT_CTOR_SIZE_BYTE = 0x150CCD;  // mov eax,0x100 at 0x550CCB
    const int RVA_EVENT_INIT_SIZE_BYTE = 0x161771;  // remaining=0x100 at 0x56176A
    const int RVA_EVENT_RESET_SIZE_BYTE = 0x161D78; // remaining=0x100 at 0x561D71
    const int RVA_GAME_MALLOC = 0x32FC28;           // 0x72FC28, cdecl malloc wrapper

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
    static bool eventBufferExpanded;
    static uint oldEventBuffer;
    static uint newEventBuffer;

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

    static bool W32(long addr, uint value)
    {
        if (h == IntPtr.Zero) return false;
        var b = BitConverter.GetBytes(value);
        return WriteProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4;
    }

    static bool WriteRaw(long addr, byte[] value)
    {
        if (h == IntPtr.Zero) return false;
        return WriteProcessMemory(h, A(addr), value, value.Length, out var n) && n.ToInt64() == value.Length;
    }

    static bool WriteCodeByte(long addr, byte value)
    {
        if (h == IntPtr.Zero) return false;
        var a = A(addr);
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

    static bool PatchSimulationCode()
    {
        if (!PatchExpectedByte(RVA_SIM_INIT_FIRST_IMM, 0x5A, 0x78, "sim-init")) return false;
        if (!PatchExpectedByte(RVA_SIM_RESET_FIRST_IMM, 0x5A, 0x78, "sim-reset")) return false;
        simCodePatched = true;
        return true;
    }

    static bool PatchEventBufferCode()
    {
        if (!PatchExpectedByte(RVA_EVENT_CTOR_SIZE_BYTE, 0x01, 0x04, "event-ctor-size")) return false;
        if (!PatchExpectedByte(RVA_EVENT_INIT_SIZE_BYTE, 0x01, 0x04, "event-init-size")) return false;
        if (!PatchExpectedByte(RVA_EVENT_RESET_SIZE_BYTE, 0x01, 0x04, "event-reset-size")) return false;
        return true;
    }

    static uint RemoteGameMalloc(uint size)
    {
        if (h == IntPtr.Zero) return 0;

        IntPtr stub = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)0x100, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (stub == IntPtr.Zero)
        {
            patchError = "remote malloc stub allocation failed";
            return 0;
        }

        try
        {
            uint mallocAddr = unchecked((uint)(moduleBase + RVA_GAME_MALLOC));
            var code = new List<byte>();
            code.Add(0x68); code.AddRange(BitConverter.GetBytes(size));          // push size
            code.Add(0xB8); code.AddRange(BitConverter.GetBytes(mallocAddr));    // mov eax,malloc
            code.Add(0xFF); code.Add(0xD0);                                     // call eax
            code.Add(0x83); code.Add(0xC4); code.Add(0x04);                     // add esp,4
            code.Add(0xC2); code.Add(0x04); code.Add(0x00);                     // ret 4 (thread param)

            if (!WriteRaw(stub.ToInt64(), code.ToArray()))
            {
                patchError = "remote malloc stub write failed";
                return 0;
            }
            FlushInstructionCache(h, stub, (UIntPtr)code.Count);

            IntPtr thread = CreateRemoteThread(h, IntPtr.Zero, UIntPtr.Zero, stub, IntPtr.Zero, 0, out _);
            if (thread == IntPtr.Zero)
            {
                patchError = "CreateRemoteThread for game malloc failed";
                return 0;
            }

            try
            {
                if (WaitForSingleObject(thread, 5000) != WAIT_OBJECT_0)
                {
                    patchError = "game malloc thread timeout";
                    return 0;
                }
                if (!GetExitCodeThread(thread, out uint result) || result == 0)
                {
                    patchError = "game malloc returned null";
                    return 0;
                }
                return result;
            }
            finally
            {
                CloseHandle(thread);
            }
        }
        finally
        {
            VirtualFreeEx(h, stub, UIntPtr.Zero, MEM_RELEASE);
        }
    }

    static bool ExpandEventBuffer()
    {
        uint used = R32(moduleBase + RVA_EVENT_USED);
        uint remain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint ptr = R32(moduleBase + RVA_EVENT_PTR);

        if (ptr == 0)
        {
            patchError = "event buffer pointer is null";
            return false;
        }

        if (eventBufferExpanded || (used == 0 && remain == EVENT_BUFFER_NEW_SIZE))
        {
            eventBufferExpanded = true;
            newEventBuffer = ptr;
            return PatchEventBufferCode();
        }

        // This probe intentionally swaps the backing buffer only while the queue is empty.
        // Do not copy an in-flight deterministic command stream from another thread.
        if (used != 0)
        {
            patchError = $"event queue busy (used={used}, free={remain}) — wait for used:0 or restart BRZE before F4";
            return false;
        }

        uint gate = R32(moduleBase + RVA_EVENT_GATE);
        if (!W32(moduleBase + RVA_EVENT_GATE, 0u))
        {
            patchError = "failed to pause event emission for buffer swap";
            return false;
        }

        try
        {
            // Re-check after emission is paused.
            used = R32(moduleBase + RVA_EVENT_USED);
            if (used != 0)
            {
                patchError = $"event queue became busy during arm (used={used})";
                return false;
            }

            uint fresh = RemoteGameMalloc(EVENT_BUFFER_NEW_SIZE);
            if (fresh == 0) return false;

            oldEventBuffer = ptr;
            newEventBuffer = fresh;

            if (!W32(moduleBase + RVA_EVENT_PTR, fresh) ||
                !W32(moduleBase + RVA_EVENT_USED, 0u) ||
                !W32(moduleBase + RVA_EVENT_REMAIN, EVENT_BUFFER_NEW_SIZE))
            {
                patchError = "event buffer state swap failed";
                return false;
            }

            // Only after the real 1024-byte backing allocation is active do we widen native reset constants.
            if (!PatchEventBufferCode()) return false;

            eventBufferExpanded = true;
            return true;
        }
        finally
        {
            W32(moduleBase + RVA_EVENT_GATE, gate);
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

        if (first == 120 && growth == 0)
            return true;

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
        long dragCandidates = moduleBase + RVA_DRAG_CANDIDATES;
        long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
        uint lid = R32(moduleBase + RVA_LOCAL_ID);
        long simLocal = LocalSimulationList(lid);
        byte cap = R8(capAddr);

        if (enabled)
        {
            patchError = "";
            W32(moduleBase + RVA_MAX_UNITS + lid * 4L, 99_999_999u);

            if (!sortPatched && !PatchSortPipeline())
                return Status(active, sortA, sortB, simLocal, dragCandidates, capAddr, lid, "NOT ARMED");

            if (!simCodePatched && !PatchSimulationCode())
                return Status(active, sortA, sortB, simLocal, dragCandidates, capAddr, lid, "NOT ARMED");

            if (!activeArmed)
                activeArmed = ArmPristineList(active, "active");

            if (simLocal == 0)
            {
                patchError = "simulation selection list not initialized yet";
                return Status(active, sortA, sortB, simLocal, dragCandidates, capAddr, lid, "NOT ARMED");
            }

            if (!simArmed)
                simArmed = ArmPristineList(simLocal, "sim-local");

            if (!eventBufferExpanded && !ExpandEventBuffer())
                return Status(active, sortA, sortB, simLocal, dragCandidates, capAddr, lid, "NOT ARMED");

            if (activeArmed && sortPatched && simCodePatched && simArmed && eventBufferExpanded &&
                R32(active + OFF_GROWTH) == 0 && R32(simLocal + OFF_GROWTH) == 0)
            {
                if (!PatchExpectedByte(RVA_MANUAL_CAP_IMM, 0x5A, 0x78, "manual-cap"))
                    return Status(active, sortA, sortB, simLocal, dragCandidates, capAddr, lid, "NOT ARMED");
            }
        }
        else
        {
            if (cap == 0x78) WriteCodeByte(capAddr, 0x5A);
        }

        string mode = activeArmed && sortPatched && simCodePatched && simArmed && eventBufferExpanded
            ? "ARMED eventbuf1024+sim120"
            : "NOT ARMED";
        return Status(active, sortA, sortB, simLocal, dragCandidates, capAddr, lid, mode);
    }

    static string Status(long active, long sortA, long sortB, long simLocal, long dragCandidates, long capAddr, uint lid, string mode)
    {
        byte cap = R8(capAddr);
        byte a = R8(moduleBase + RVA_SORT_A_FIRST_IMM);
        byte b = R8(moduleBase + RVA_SORT_B_FIRST_IMM);
        byte r = R8(moduleBase + RVA_SORT_ACTIVE_FIRST_IMM);
        byte si = R8(moduleBase + RVA_SIM_INIT_FIRST_IMM);
        byte sr = R8(moduleBase + RVA_SIM_RESET_FIRST_IMM);
        uint eventUsed = R32(moduleBase + RVA_EVENT_USED);
        uint eventRemain = R32(moduleBase + RVA_EVENT_REMAIN);
        uint eventPtr = R32(moduleBase + RVA_EVENT_PTR);
        string err = patchError.Length == 0 ? "" : $" | ERROR:{patchError}";

        return $"pid:{pid} | {mode} | player:{lid} | cap:0x{cap:X2} | sort:{a:X2}/{b:X2}/{r:X2} | sim-code:{si:X2}/{sr:X2} | add-notify:LIVE\r\n" +
               $"EVENT ptr:0x{eventPtr:X8} used:{eventUsed} free:{eventRemain} target:{EVENT_BUFFER_NEW_SIZE} | old:0x{oldEventBuffer:X8} new:0x{newEventBuffer:X8}\r\n" +
               $"ACTIVE {ListState(active)} | SIM {ListState(simLocal)} | DRAG-CAND {ListState(dragCandidates)}\r\n" +
               $"SORT-A {ListState(sortA)} | SORT-B {ListState(sortB)}{err}";
    }

    public static void Stop()
    {
        if (h != IntPtr.Zero)
        {
            long capAddr = moduleBase + RVA_MANUAL_CAP_IMM;
            if (R8(capAddr) == 0x78) WriteCodeByte(capAddr, 0x5A);
            // Keep expanded event backing + reset constants alive until BRZE exits.
            // The replacement buffer came from BRZE's own allocator, so native cleanup can own it.
        }
        DetachHandleOnly();
        patchError = "";
        activeArmed = false;
        sortPatched = false;
        simCodePatched = false;
        simArmed = false;
        eventBufferExpanded = false;
        oldEventBuffer = 0;
        newEventBuffer = 0;
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
