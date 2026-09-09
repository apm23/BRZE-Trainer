using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BRZETrainer;

internal static class HookCore
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualFreeEx(IntPtr h, IntPtr addr, UIntPtr size, uint freeType);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x10 | 0x20 | 0x8 | 0x400;
    const uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, MEM_RELEASE = 0x8000, PAGE_EXECUTE_READWRITE = 0x40;

    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_SELECTION_LIST = 0x441708;
    const int RVA_ADD_HEALTH = 0x1CCD4D;
    const int RVA_ADD_STAMINA = 0x1CCDFB;
    const int RVA_TRAIN_PROGRESS_READ = 0x0D5DDB;
    const int RVA_SELECT_ONE = 0x1A70C6;
    const int RVA_SELECT_BOX = 0x1A78CC;
    const int OFF_DEF = 0x74, OFF_OWNER = 0x240, OFF_SEL_A = 0x3A8, OFF_SEL_B = 0x3AC, OFF_HP = 0x404, OFF_ST = 0x408;

    static readonly byte[] HpOriginal = { 0x55, 0x8B, 0xEC, 0x83, 0xE4, 0xF8, 0x56, 0x8B, 0xF1, 0x57 };
    static readonly byte[] StOriginal = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1, 0x57, 0x56 };
    static readonly byte[] TrainOriginal = { 0x8B, 0x83, 0x90, 0x04, 0x00, 0x00 };
    static readonly byte[] SelectOriginal = { 0xC7, 0x86, 0xA8, 0x03, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };

    static IntPtr h;
    static Process? p;
    static long moduleBase;
    static IntPtr cave;
    static long hpFlag, stFlag, trainFlag;
    static bool hooksInstalled;
    static bool lastHp, lastStamina, lastTraining;
    static int lastTopUp;
    static string error = "";

    static IntPtr A(long a) => new(unchecked((int)(uint)a));

    static bool Attach()
    {
        try { if (p != null && !p.HasExited && h != IntPtr.Zero) return true; } catch { }
        Detach(false);
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) return false;
        p = ps[0];
        try { moduleBase = p.MainModule!.BaseAddress.ToInt64(); } catch { p = null; return false; }
        h = OpenProcess(ACCESS, false, p.Id);
        return h != IntPtr.Zero;
    }

    static bool ReadExact(long addr, byte[] b) => h != IntPtr.Zero && ReadProcessMemory(h, A(addr), b, b.Length, out var n) && n.ToInt64() == b.Length;
    static uint R32(long addr) { var b = new byte[4]; return ReadExact(addr, b) ? BitConverter.ToUInt32(b, 0) : 0u; }
    static bool W32(long addr, uint v) { var b = BitConverter.GetBytes(v); return h != IntPtr.Zero && WriteProcessMemory(h, A(addr), b, 4, out var n) && n.ToInt64() == 4; }
    static bool WriteRaw(long addr, byte[] b) => h != IntPtr.Zero && WriteProcessMemory(h, A(addr), b, b.Length, out var n) && n.ToInt64() == b.Length;

    static bool WriteCode(long addr, byte[] b)
    {
        if (h == IntPtr.Zero) return false;
        var a = A(addr);
        if (!VirtualProtectEx(h, a, (UIntPtr)b.Length, PAGE_EXECUTE_READWRITE, out uint old)) return false;
        bool ok = WriteProcessMemory(h, a, b, b.Length, out var n) && n.ToInt64() == b.Length;
        if (ok) FlushInstructionCache(h, a, (UIntPtr)b.Length);
        VirtualProtectEx(h, a, (UIntPtr)b.Length, old, out _);
        return ok;
    }

    static uint Fixed16(uint v)
    {
        ulong x = ((ulong)v) << 16;
        return x > uint.MaxValue ? uint.MaxValue : (uint)x;
    }

    static void I32(List<byte> b, int v) => b.AddRange(BitConverter.GetBytes(v));
    static void U32(List<byte> b, uint v) => b.AddRange(BitConverter.GetBytes(v));
    static void Rel(List<byte> b, int at, long fromNext, long target)
    {
        var x = BitConverter.GetBytes(unchecked((int)(target - fromNext)));
        for (int i = 0; i < 4; i++) b[at + i] = x[i];
    }

    static byte[] JmpPatch(long target, long stub, int len)
    {
        var b = new byte[len];
        b[0] = 0xE9;
        Array.Copy(BitConverter.GetBytes(unchecked((int)(stub - (target + 5)))), 0, b, 1, 4);
        for (int i = 5; i < len; i++) b[i] = 0x90;
        return b;
    }

    static byte[] BuildHardLockHook(long stub, long flag, long target, byte[] original)
    {
        // Negative HP/stamina deltas are rejected before the native function executes.
        // Selection is accepted when either BRZE UI-selected (+3A8) OR SIM-selected (+3AC) is set.
        var b = new List<byte>();
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)flag); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jDisabled = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0x7C, 0x24, 0x04, 0x00 });
        b.AddRange(new byte[] { 0x0F, 0x8D }); int jNonNeg = b.Count; I32(b, 0);
        b.Add(0x50);
        b.Add(0xA1); U32(b, (uint)(moduleBase + RVA_LOCAL_ID));
        b.AddRange(new byte[] { 0x39, 0x81, 0x40, 0x02, 0x00, 0x00 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jOwner = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0xB9, 0xA8, 0x03, 0x00, 0x00, 0x01 });
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jSelA = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0xB9, 0xAC, 0x03, 0x00, 0x00, 0x01 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jNotSelected = b.Count; I32(b, 0);
        int block = b.Count;
        b.Add(0x58); b.AddRange(new byte[] { 0xC2, 0x04, 0x00 });
        int popOriginal = b.Count;
        b.Add(0x58);
        int originalLabel = b.Count;
        b.AddRange(original);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);
        Rel(b, jDisabled, stub + jDisabled + 4, stub + originalLabel);
        Rel(b, jNonNeg, stub + jNonNeg + 4, stub + originalLabel);
        Rel(b, jOwner, stub + jOwner + 4, stub + popOriginal);
        Rel(b, jSelA, stub + jSelA + 4, stub + block);
        Rel(b, jNotSelected, stub + jNotSelected + 4, stub + popOriginal);
        Rel(b, jBack, stub + jBack + 4, target + original.Length);
        return b.ToArray();
    }

    static byte[] BuildTrainingHook(long stub, long flag, long target)
    {
        var b = new List<byte>();
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)flag); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jDisabled = b.Count; I32(b, 0);
        b.Add(0x52);
        b.AddRange(new byte[] { 0x8B, 0x15 }); U32(b, (uint)(moduleBase + RVA_LOCAL_ID));
        b.AddRange(new byte[] { 0x39, 0x93, 0x84, 0x00, 0x00, 0x00 });
        b.Add(0x5A);
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jNotOwner = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xC7, 0x83, 0x90, 0x04, 0x00, 0x00, 0x40, 0xB5, 0x64, 0x00 });
        int originalLabel = b.Count;
        b.AddRange(TrainOriginal);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);
        Rel(b, jDisabled, stub + jDisabled + 4, stub + originalLabel);
        Rel(b, jNotOwner, stub + jNotOwner + 4, stub + originalLabel);
        Rel(b, jBack, stub + jBack + 4, target + TrainOriginal.Length);
        return b.ToArray();
    }

    static byte[] BuildSelectionRefillHook(long stub, long target)
    {
        var b = new List<byte>();
        b.AddRange(SelectOriginal);
        b.Add(0x50);
        b.Add(0xA1); U32(b, (uint)(moduleBase + RVA_LOCAL_ID));
        b.AddRange(new byte[] { 0x39, 0x86, 0x40, 0x02, 0x00, 0x00 });
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jDone = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)hpFlag); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jSt = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x8B, 0x46, 0x74, 0x8B, 0x40, 0x6C, 0xC1, 0xE0, 0x10, 0x89, 0x86, 0x04, 0x04, 0x00, 0x00 });
        int staminaLabel = b.Count;
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)stFlag); b.Add(0);
        b.AddRange(new byte[] { 0x0F, 0x84 }); int jDone2 = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x8B, 0x46, 0x74, 0x8B, 0x80, 0x80, 0x00, 0x00, 0x00, 0xC1, 0xE0, 0x10, 0x89, 0x86, 0x08, 0x04, 0x00, 0x00 });
        int done = b.Count;
        b.Add(0x58);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);
        Rel(b, jDone, stub + jDone + 4, stub + done);
        Rel(b, jSt, stub + jSt + 4, stub + staminaLabel);
        Rel(b, jDone2, stub + jDone2 + 4, stub + done);
        Rel(b, jBack, stub + jBack + 4, target + SelectOriginal.Length);
        return b.ToArray();
    }

    static bool EnsureHooks()
    {
        if (hooksInstalled) return true;
        if (!Attach()) return false;
        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)2048, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (cave == IntPtr.Zero) { error = "hook cave allocation failed"; return false; }
        long c = cave.ToInt64();
        hpFlag = c; stFlag = c + 4; trainFlag = c + 8;
        long hpStub = c + 32, stStub = c + 224, trainStub = c + 416, sel1Stub = c + 640, sel2Stub = c + 896;
        long hpTarget = moduleBase + RVA_ADD_HEALTH, stTarget = moduleBase + RVA_ADD_STAMINA;
        long trainTarget = moduleBase + RVA_TRAIN_PROGRESS_READ, s1 = moduleBase + RVA_SELECT_ONE, s2 = moduleBase + RVA_SELECT_BOX;

        var hn = new byte[HpOriginal.Length]; var sn = new byte[StOriginal.Length]; var tn = new byte[TrainOriginal.Length];
        var q1 = new byte[SelectOriginal.Length]; var q2 = new byte[SelectOriginal.Length];
        if (!ReadExact(hpTarget, hn) || !System.Linq.Enumerable.SequenceEqual(hn, HpOriginal)) { error = "HP hook byte mismatch"; return false; }
        if (!ReadExact(stTarget, sn) || !System.Linq.Enumerable.SequenceEqual(sn, StOriginal)) { error = "stamina hook byte mismatch"; return false; }
        if (!ReadExact(trainTarget, tn) || !System.Linq.Enumerable.SequenceEqual(tn, TrainOriginal)) { error = "training hook byte mismatch"; return false; }
        if (!ReadExact(s1, q1) || !System.Linq.Enumerable.SequenceEqual(q1, SelectOriginal) ||
            !ReadExact(s2, q2) || !System.Linq.Enumerable.SequenceEqual(q2, SelectOriginal)) { error = "selection refill hook byte mismatch"; return false; }

        W32(hpFlag, 0); W32(stFlag, 0); W32(trainFlag, 0);
        byte[] hs = BuildHardLockHook(hpStub, hpFlag, hpTarget, HpOriginal);
        byte[] ss = BuildHardLockHook(stStub, stFlag, stTarget, StOriginal);
        byte[] ts = BuildTrainingHook(trainStub, trainFlag, trainTarget);
        byte[] a = BuildSelectionRefillHook(sel1Stub, s1);
        byte[] b = BuildSelectionRefillHook(sel2Stub, s2);
        if (!WriteRaw(hpStub, hs) || !WriteRaw(stStub, ss) || !WriteRaw(trainStub, ts) || !WriteRaw(sel1Stub, a) || !WriteRaw(sel2Stub, b))
        { error = "hook cave write failed"; return false; }
        if (!WriteCode(hpTarget, JmpPatch(hpTarget, hpStub, HpOriginal.Length)) ||
            !WriteCode(stTarget, JmpPatch(stTarget, stStub, StOriginal.Length)) ||
            !WriteCode(trainTarget, JmpPatch(trainTarget, trainStub, TrainOriginal.Length)) ||
            !WriteCode(s1, JmpPatch(s1, sel1Stub, SelectOriginal.Length)) ||
            !WriteCode(s2, JmpPatch(s2, sel2Stub, SelectOriginal.Length)))
        { error = "hook install failed"; return false; }
        hooksInstalled = true;
        error = "";
        return true;
    }

    static int TopUpSelected(bool hp, bool stamina)
    {
        if (!hp && !stamina) return 0;
        uint lid = R32(moduleBase + RVA_LOCAL_ID);
        long list = moduleBase + RVA_SELECTION_LIST;
        uint node = R32(list);
        int count = (int)Math.Min(R32(list + 0x18), 500u);
        int fixedCount = 0;
        var seen = new HashSet<uint>();
        for (int i = 0; i < count && node != 0 && seen.Add(node); i++)
        {
            uint unit = R32((long)node + 8);
            if (unit != 0 && R32((long)unit + OFF_OWNER) == lid)
            {
                uint def = R32((long)unit + OFF_DEF);
                if (def != 0)
                {
                    if (hp)
                    {
                        uint maxHp = R32((long)def + 0x6C);
                        if (maxHp != 0) W32((long)unit + OFF_HP, Fixed16(maxHp));
                    }
                    if (stamina)
                    {
                        uint maxSt = R32((long)def + 0x80);
                        if (maxSt != 0) W32((long)unit + OFF_ST, Fixed16(maxSt));
                    }
                    fixedCount++;
                }
            }
            node = R32(node);
        }
        return fixedCount;
    }

    public static string Tick(bool stamina, bool hp, bool training)
    {
        if (!Attach()) return "LOCKS: waiting for Battle_Realms_F.exe...";
        error = "";
        bool any = stamina || hp || training;
        if (any && !EnsureHooks()) return $"LOCKS NOT ARMED | {error}";
        if (hooksInstalled)
        {
            bool hpRise = hp && !lastHp;
            bool stRise = stamina && !lastStamina;
            W32(hpFlag, hp ? 1u : 0u);
            W32(stFlag, stamina ? 1u : 0u);
            W32(trainFlag, training ? 1u : 0u);
            if (hpRise || stRise) lastTopUp = TopUpSelected(hpRise, stRise);
        }
        lastHp = hp; lastStamina = stamina; lastTraining = training;
        return $"LOCKS hooks:{hooksInstalled} | HP-hard:{hp} ST-hard:{stamina} F7:{training} | lastTopUp:{lastTopUp}" + (error.Length == 0 ? "" : $" | ERR:{error}");
    }

    public static void Stop() => Detach(true);

    static void Detach(bool restore)
    {
        if (h != IntPtr.Zero)
        {
            if (restore && hooksInstalled)
            {
                WriteCode(moduleBase + RVA_ADD_HEALTH, HpOriginal);
                WriteCode(moduleBase + RVA_ADD_STAMINA, StOriginal);
                WriteCode(moduleBase + RVA_TRAIN_PROGRESS_READ, TrainOriginal);
                WriteCode(moduleBase + RVA_SELECT_ONE, SelectOriginal);
                WriteCode(moduleBase + RVA_SELECT_BOX, SelectOriginal);
            }
            if (cave != IntPtr.Zero) VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE);
            CloseHandle(h);
        }
        h = IntPtr.Zero; p = null; moduleBase = 0; cave = IntPtr.Zero;
        hpFlag = stFlag = trainFlag = 0;
        hooksInstalled = false; lastHp = lastStamina = lastTraining = false; lastTopUp = 0; error = "";
    }
}
