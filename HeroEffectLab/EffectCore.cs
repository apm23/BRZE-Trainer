using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BRZEHeroEffectLab;

internal static class EffectCore
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

    const int RVA_SELECTION_LIST = 0x441708;
    const int RVA_FRAME_MOUSE_DRAW = 0x135C43;
    const int RVA_APPLY_TARGET_ABILITY = 0x1F0C32; // preferred VA 0x5F0C32
    const int RVA_BG_BASE_PTR = 0x43FE34;          // preferred VA 0x83FE34
    const int RVA_BG_MAP_PTR = 0x43FF3C;           // preferred VA 0x83FF3C
    const int RVA_ABILITY_BASE_PTR = 0x43FE28;     // preferred VA 0x83FE28
    const int RVA_ABILITY_MAP_PTR = 0x43FF30;      // preferred VA 0x83FF30

    const int OFF_UNIT_DEF = 0x74;
    const int OFF_BG1 = 0x14C, OFF_BG2 = 0x150, OFF_BG3 = 0x154;
    const int BG_STRIDE = 0x38, BG_ABILITY_TYPE = 0x0C;
    const int ABILITY_STRIDE = 0x2E4;
    const int ABILITY_APPLICATION_EFFECT = 0x38;
    const int ABILITY_PROXIMITY_EFFECT = 0x3C;
    const int ABILITY_CREATE_MAGIC_AT_TARGET = 0x288;
    const int MAX_SELECTED = 120;

    static readonly byte[] FrameOriginal = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x5C };
    static readonly byte[] ApplyPrologue = { 0x55, 0x8B, 0xEC, 0x56, 0x8B, 0xF1 };

    const int ACTIVE = 0x400, COUNT = 0x404, INDEX = 0x408, ABILITY1 = 0x40C, ABILITY2 = 0x410, MODE = 0x414, CALLS = 0x418;
    const int ENTRIES = 0x800, FXSCRATCH = 0x2000, CAVE_SIZE = 0x4000;

    static readonly object sync = new();
    static Process? process;
    static IntPtr h = IntPtr.Zero, cave = IntPtr.Zero;
    static long moduleBase;
    static bool installed;
    static byte[]? hookPatch;
    static string status = "not attached", lastQueue = "idle";
    static DateTime lastTry;

    static IntPtr A(long x) => new(unchecked((int)(uint)x));
    static bool ReadExact(long a, byte[] b) => h != IntPtr.Zero && ReadProcessMemory(h, A(a), b, b.Length, out var n) && n.ToInt64() == b.Length;
    static uint R32(long a) { var b = new byte[4]; return ReadExact(a, b) ? BitConverter.ToUInt32(b, 0) : 0; }
    static bool WriteBytes(long a, byte[] b) => h != IntPtr.Zero && WriteProcessMemory(h, A(a), b, b.Length, out var n) && n.ToInt64() == b.Length;
    static bool W32(long a, uint v) => WriteBytes(a, BitConverter.GetBytes(v));
    static bool Same(byte[] a, byte[] b) => a.Length == b.Length && a.SequenceEqual(b);

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
        if ((DateTime.UtcNow - lastTry).TotalMilliseconds < 300) return false;
        lastTry = DateTime.UtcNow;
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) { status = "waiting for Battle_Realms_F.exe"; return false; }
        process = ps[0];
        try { moduleBase = process.MainModule!.BaseAddress.ToInt64(); } catch { process = null; status = "cannot resolve module base"; return false; }
        h = OpenProcess(ACCESS, false, process.Id);
        if (h == IntPtr.Zero) { status = "OpenProcess failed"; return false; }
        status = $"attached PID {process.Id} base 0x{moduleBase:X8}";
        return true;
    }

    static List<uint> SelectedUnits()
    {
        var result = new List<uint>();
        if (!Attach()) return result;
        long list = moduleBase + RVA_SELECTION_LIST;
        uint count = R32(list + 0x18), node = R32(list);
        var seenNodes = new HashSet<uint>();
        var seenUnits = new HashSet<uint>();
        int limit = (int)Math.Min(count, MAX_SELECTED);
        for (int n = 0; n < limit && node != 0; n++)
        {
            if (!seenNodes.Add(node)) break;
            uint next = R32((long)node + 0x00);
            uint unit = R32((long)node + 0x08);
            if (unit != 0 && seenUnits.Add(unit) && R32((long)unit + OFF_UNIT_DEF) != 0) result.Add(unit);
            node = next;
        }
        return result;
    }

    static GearChoice? ResolveGear(uint gear, int slot)
    {
        if (gear == uint.MaxValue || gear == 0xFFFFu || gear > 0x10000u) return null;
        uint bgMap = R32(moduleBase + RVA_BG_MAP_PTR), bgBase = R32(moduleBase + RVA_BG_BASE_PTR);
        uint abMap = R32(moduleBase + RVA_ABILITY_MAP_PTR), abBase = R32(moduleBase + RVA_ABILITY_BASE_PTR);
        if (bgMap == 0 || bgBase == 0 || abMap == 0 || abBase == 0) return null;
        uint bgIndex = R32((long)bgMap + gear * 4L);
        if (bgIndex == uint.MaxValue || bgIndex > 0x10000u) return null;
        long bg = (long)bgBase + bgIndex * BG_STRIDE;
        uint ability = R32(bg + BG_ABILITY_TYPE);
        if (ability == uint.MaxValue || ability == 0xFFFFu || ability > 0x10000u)
            return new GearChoice { Slot = slot, BattleGear = gear, RootAbility = ability, TargetAbility = uint.MaxValue, ApplicationOfEffect = uint.MaxValue };
        uint abIndex = R32((long)abMap + ability * 4L);
        if (abIndex == uint.MaxValue || abIndex > 0x10000u)
            return new GearChoice { Slot = slot, BattleGear = gear, RootAbility = ability, TargetAbility = uint.MaxValue, ApplicationOfEffect = uint.MaxValue };
        long def = (long)abBase + abIndex * ABILITY_STRIDE;
        return new GearChoice
        {
            Slot = slot,
            BattleGear = gear,
            RootAbility = ability,
            TargetAbility = R32(def + ABILITY_CREATE_MAGIC_AT_TARGET),
            ApplicationOfEffect = R32(def + ABILITY_APPLICATION_EFFECT),
            ProximityEffect = R32(def + ABILITY_PROXIMITY_EFFECT)
        };
    }

    public static SourceCapture CaptureSelectedSource(string label, uint[] expectedTypes)
    {
        lock (sync)
        {
            var selected = SelectedUnits();
            if (selected.Count == 0) return new SourceCapture { Ok = false, Message = $"{label}: select the source hero first." };
            uint unit = selected[0], def = R32((long)selected[0] + OFF_UNIT_DEF);
            if (def == 0) return new SourceCapture { Ok = false, Message = $"{label}: selected UnitDef unresolved." };
            uint type = R32(def);
            uint[] gears = { R32((long)def + OFF_BG1), R32((long)def + OFF_BG2), R32((long)def + OFF_BG3) };
            var choices = new List<GearChoice>();
            for (int s = 0; s < gears.Length; s++) { var c = ResolveGear(gears[s], s + 1); if (c != null) choices.Add(c); }
            bool expected = expectedTypes.Contains(type);
            int valid = choices.Count(x => x.CanApply);
            string warn = expected ? "" : $" WARNING: source type 0x{type:X} is not expected for {label}.";
            string msg = $"{label}: source 0x{unit:X8} type 0x{type:X}; BG entries {choices.Count}, target-applicable {valid}.{warn}";
            if (choices.Count > 0) msg += " Target ability comes from AbilityDef.CreateMagicAtTarget (+0x288).";
            else msg += " No BattleGear slots resolved.";
            return new SourceCapture { Ok = choices.Count > 0, Unit = unit, UnitType = type, Choices = choices, Message = msg };
        }
    }

    static byte[] BuildStub(long stub)
    {
        long active = stub + ACTIVE, count = stub + COUNT, index = stub + INDEX, a1 = stub + ABILITY1, a2 = stub + ABILITY2, mode = stub + MODE, calls = stub + CALLS;
        long entries = stub + ENTRIES, fx = stub + FXSCRATCH;
        uint apply = (uint)(moduleBase + RVA_APPLY_TARGET_ABILITY);
        var b = new List<byte>();
        b.Add(0x9C); b.Add(0x60);
        b.AddRange(new byte[] { 0x0F, 0xAE, 0x05 }); U32(b, (uint)fx);
        b.Add(0xA1); U32(b, (uint)active);
        b.AddRange(new byte[] { 0x85, 0xC0, 0x0F, 0x84 }); int jInactive = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0xBB, 0x04, 0x00, 0x00, 0x00 });
        int loop = b.Count;
        b.Add(0xA1); U32(b, (uint)index);
        b.AddRange(new byte[] { 0x8B, 0x15 }); U32(b, (uint)count);
        b.AddRange(new byte[] { 0x3B, 0xC2, 0x0F, 0x83 }); int jFinished = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x8B, 0xC8, 0xC1, 0xE1, 0x02, 0x81, 0xC1 }); U32(b, (uint)entries);
        b.AddRange(new byte[] { 0x8B, 0x31, 0x85, 0xF6, 0x0F, 0x84 }); int jSkipCalls = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x8B, 0xCE, 0xFF, 0x35 }); U32(b, (uint)a1);
        b.Add(0xB8); U32(b, apply); b.AddRange(new byte[] { 0xFF, 0xD0, 0xFF, 0x05 }); U32(b, (uint)calls);
        b.AddRange(new byte[] { 0x83, 0x3D }); U32(b, (uint)mode); b.Add(0x02);
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jAfterSecond = b.Count; I32(b, 0);
        b.AddRange(new byte[] { 0x8B, 0xCE, 0xFF, 0x35 }); U32(b, (uint)a2);
        b.Add(0xB8); U32(b, apply); b.AddRange(new byte[] { 0xFF, 0xD0, 0xFF, 0x05 }); U32(b, (uint)calls);
        int afterSecond = b.Count;
        int skipCalls = b.Count;
        b.AddRange(new byte[] { 0xFF, 0x05 }); U32(b, (uint)index);
        b.Add(0x4B);
        b.AddRange(new byte[] { 0x0F, 0x85 }); int jLoop = b.Count; I32(b, 0);
        b.Add(0xA1); U32(b, (uint)index);
        b.AddRange(new byte[] { 0x8B, 0x15 }); U32(b, (uint)count);
        b.AddRange(new byte[] { 0x3B, 0xC2, 0x0F, 0x82 }); int jKeep = b.Count; I32(b, 0);
        int finished = b.Count;
        b.AddRange(new byte[] { 0xC7, 0x05 }); U32(b, (uint)active); U32(b, 0);
        int restore = b.Count;
        b.AddRange(new byte[] { 0x0F, 0xAE, 0x0D }); U32(b, (uint)fx);
        b.Add(0x61); b.Add(0x9D); b.AddRange(FrameOriginal);
        b.Add(0xE9); int jBack = b.Count; I32(b, 0);
        PatchRel(b, jInactive, stub + jInactive + 4, stub + restore);
        PatchRel(b, jFinished, stub + jFinished + 4, stub + finished);
        PatchRel(b, jSkipCalls, stub + jSkipCalls + 4, stub + skipCalls);
        PatchRel(b, jAfterSecond, stub + jAfterSecond + 4, stub + afterSecond);
        PatchRel(b, jLoop, stub + jLoop + 4, stub + loop);
        PatchRel(b, jKeep, stub + jKeep + 4, stub + restore);
        PatchRel(b, jBack, stub + jBack + 4, moduleBase + RVA_FRAME_MOUSE_DRAW + FrameOriginal.Length);
        return b.ToArray();
    }

    static bool InstallHook()
    {
        if (installed) return true;
        if (!Attach()) return false;
        var ap = new byte[ApplyPrologue.Length];
        if (!ReadExact(moduleBase + RVA_APPLY_TARGET_ABILITY, ap) || !Same(ap, ApplyPrologue)) { status = "APPLY-TARGET helper bytes mismatch"; return false; }
        long site = moduleBase + RVA_FRAME_MOUSE_DRAW;
        var now = new byte[FrameOriginal.Length];
        if (!ReadExact(site, now) || !Same(now, FrameOriginal)) { status = "FRAME HOOK BUSY/MISMATCH — close main trainer / Clone Lab"; return false; }
        cave = VirtualAllocEx(h, IntPtr.Zero, (UIntPtr)CAVE_SIZE, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (cave == IntPtr.Zero) { status = "VirtualAllocEx failed"; return false; }
        long c = cave.ToInt64();
        if ((c + FXSCRATCH) % 16 != 0) { status = "FX scratch alignment failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; return false; }
        if (!WriteBytes(c + ACTIVE, new byte[0x40])) { status = "state init failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; return false; }
        if (!WriteBytes(c, BuildStub(c))) { status = "stub write failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; return false; }
        hookPatch = new byte[6]; hookPatch[0] = 0xE9; Array.Copy(BitConverter.GetBytes(unchecked((int)(c - (site + 5)))), 0, hookPatch, 1, 4); hookPatch[5] = 0x90;
        if (!WriteCode(site, hookPatch)) { status = "frame hook patch failed"; VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE); cave = IntPtr.Zero; hookPatch = null; return false; }
        installed = true; status = "HERO EFFECT GAME-THREAD HOOK ACTIVE"; return true;
    }

    public static string QueueSelected(uint ability1, uint ability2, string label)
    {
        lock (sync)
        {
            if (ability1 == uint.MaxValue || ability1 == 0xFFFFu) return $"{label}: invalid target ability.";
            var units = SelectedUnits();
            if (units.Count == 0) return $"{label}: no units selected.";
            if (!InstallHook()) return $"{label}: blocked — {status}";
            long c = cave.ToInt64();
            if (R32(c + ACTIVE) != 0) return $"{label}: previous apply queue is still active.";
            var bytes = new byte[units.Count * 4];
            for (int n = 0; n < units.Count; n++) Buffer.BlockCopy(BitConverter.GetBytes(units[n]), 0, bytes, n * 4, 4);
            if (!WriteBytes(c + ENTRIES, bytes)) return $"{label}: queue write failed.";
            bool both = ability2 != uint.MaxValue && ability2 != 0xFFFFu;
            W32(c + COUNT, (uint)units.Count); W32(c + INDEX, 0); W32(c + CALLS, 0); W32(c + ABILITY1, ability1); W32(c + ABILITY2, both ? ability2 : uint.MaxValue); W32(c + MODE, both ? 2u : 1u); W32(c + ACTIVE, 1);
            lastQueue = $"{label}: queued {units.Count} selected | target ability 0x{ability1:X}" + (both ? $" + 0x{ability2:X}" : "");
            return lastQueue;
        }
    }

    public static RuntimeSnapshot Snapshot()
    {
        lock (sync)
        {
            if (!Attach()) return new RuntimeSnapshot("GAME: waiting for Battle_Realms_F.exe", lastQueue);
            string g = $"GAME: {status}";
            if (!installed || cave == IntPtr.Zero) return new RuntimeSnapshot(g, "QUEUE: idle — capture source hero, then apply to selected units.");
            long c = cave.ToInt64(); uint active = R32(c + ACTIVE), count = R32(c + COUNT), index = R32(c + INDEX), calls = R32(c + CALLS), a1 = R32(c + ABILITY1), a2 = R32(c + ABILITY2), mode = R32(c + MODE);
            string q = $"QUEUE: {(active != 0 ? "ACTIVE" : "DONE")} {index}/{count} | native calls:{calls} | a1:0x{a1:X}" + (mode == 2 ? $" a2:0x{a2:X}" : "");
            return new RuntimeSnapshot(g, q);
        }
    }

    public static void ResetRuntime() { lock (sync) DetachRuntime(); }
    static void DetachRuntime()
    {
        try
        {
            if (h != IntPtr.Zero && installed && hookPatch != null)
            {
                long site = moduleBase + RVA_FRAME_MOUSE_DRAW; var now = new byte[hookPatch.Length];
                if (ReadExact(site, now) && Same(now, hookPatch)) WriteCode(site, FrameOriginal);
            }
            if (h != IntPtr.Zero && cave != IntPtr.Zero) VirtualFreeEx(h, cave, UIntPtr.Zero, MEM_RELEASE);
            if (h != IntPtr.Zero) CloseHandle(h);
        }
        catch { }
        process = null; h = IntPtr.Zero; cave = IntPtr.Zero; moduleBase = 0; installed = false; hookPatch = null; status = "not attached"; lastTry = DateTime.MinValue;
    }
}
