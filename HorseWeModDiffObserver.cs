using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEHorseWeModDiff;

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
    readonly Button baseline = new() { Text = "1. Capture Baseline — WeMod Horse OFF", AutoSize = true };
    readonly Button after = new() { Text = "2. Capture After + Diff — WeMod Horse ON", AutoSize = true, Enabled = false };
    readonly Label state = new() { AutoSize = true };
    readonly TextBox output = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, ReadOnly = true, Font = new Font(FontFamily.GenericMonospace, 9f), Dock = DockStyle.Fill };

    public MainForm()
    {
        Text = "BRZE 1.60 — WeMod Horse Diff Observer — READ ONLY";
        ClientSize = new Size(1180, 720);
        StartPosition = FormStartPosition.CenterScreen;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10), WrapContents = false };
        top.Controls.Add(baseline); top.Controls.Add(after); top.Controls.Add(state);
        Controls.Add(output); Controls.Add(top);
        baseline.Click += (_, _) => CaptureBase();
        after.Click += (_, _) => CaptureAfter();
        FormClosed += (_, _) => Native.Close();
        output.Text = "READ ONLY — this observer never writes BRZE memory.\r\n\r\n" +
                      "Procedure:\r\n" +
                      "- Start BRZE 1.60 and WeMod; keep ONLY the Horse cheat OFF.\r\n" +
                      "- Build a Stable and SELECT that same Stable.\r\n" +
                      "- Click button 1 here.\r\n" +
                      "- Toggle ONLY WeMod Horse ON. Do not click elsewhere in BRZE.\r\n" +
                      "- Click button 2 here and send the report/log.\r\n";
    }

    void CaptureBase()
    {
        try
        {
            var s = Native.Capture();
            Native.Baseline = s;
            after.Enabled = true;
            state.Text = $"Baseline pid:{s.Pid} stable:0x{s.Building:X8} execSections:{s.Sections.Count}";
            output.AppendText($"\r\nBASELINE captured pid={s.Pid}, moduleBase=0x{s.ModuleBase:X8}, selectedStable=0x{s.Building:X8}\r\n");
            output.AppendText(Native.StableSlotsText("BASE", s.BuildingBytes));
        }
        catch (Exception ex)
        {
            output.AppendText("\r\nBASELINE ERROR: " + ex.Message + "\r\n");
        }
    }

    void CaptureAfter()
    {
        try
        {
            if (Native.Baseline == null) throw new InvalidOperationException("Capture baseline first.");
            var now = Native.Capture();
            string report = Native.Diff(Native.Baseline, now);
            output.Text = report;
            string path = Path.Combine(Path.GetTempPath(), "BRZE-Horse-WeMod-Diff.txt");
            File.WriteAllText(path, report, Encoding.UTF8);
            state.Text = "Diff complete — " + path;
        }
        catch (Exception ex)
        {
            output.AppendText("\r\nDIFF ERROR: " + ex.Message + "\r\n");
        }
    }
}

internal static class Native
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x10 | 0x400; // VM_READ | QUERY_INFORMATION only
    const int RVA_SELECTED_BUILDING_A = 0x4417D4;
    const int RVA_SELECTED_BUILDING_B = 0x4417D8;
    const int BUILDING_SIZE = 0x6A4;
    const int STABLE_SLOTS = 0x5AC;
    const int SLOT_STRIDE = 0x1C;
    const int SLOT_COUNT = 6;

    static IntPtr h;
    static Process? p;
    public static Snapshot? Baseline;

    public sealed class SectionSnap
    {
        public string Name = "";
        public int Rva;
        public byte[] Bytes = Array.Empty<byte>();
    }

    public sealed class Snapshot
    {
        public int Pid;
        public long ModuleBase;
        public List<SectionSnap> Sections = new();
        public uint Building;
        public byte[] BuildingBytes = Array.Empty<byte>();
        public DateTime Time;
    }

    static IntPtr A(long x) => new(unchecked((int)(uint)x));

    static void Attach()
    {
        try { if (p != null && !p.HasExited && h != IntPtr.Zero) return; } catch { }
        Close();
        var ps = Process.GetProcessesByName("Battle_Realms_F");
        if (ps.Length == 0) throw new InvalidOperationException("Battle_Realms_F.exe not running.");
        p = ps[0];
        h = OpenProcess(ACCESS, false, p.Id);
        if (h == IntPtr.Zero) throw new InvalidOperationException("OpenProcess failed: " + Marshal.GetLastWin32Error());
    }

    static byte[] Read(long addr, int size)
    {
        var b = new byte[size];
        if (!ReadProcessMemory(h, A(addr), b, size, out var n) || n.ToInt64() != size)
            throw new InvalidOperationException($"ReadProcessMemory failed at 0x{addr:X8} size=0x{size:X}: {Marshal.GetLastWin32Error()}");
        return b;
    }

    static uint R32(long addr) => BitConverter.ToUInt32(Read(addr, 4), 0);

    public static Snapshot Capture()
    {
        Attach();
        if (p == null) throw new InvalidOperationException("No process.");
        long b = p.MainModule!.BaseAddress.ToInt64();
        var hdr = Read(b, 0x1000);
        if (hdr[0] != (byte)'M' || hdr[1] != (byte)'Z') throw new InvalidOperationException("Target module is not MZ.");
        int pe = BitConverter.ToInt32(hdr, 0x3C);
        if (pe < 0 || pe + 0x100 >= hdr.Length) throw new InvalidOperationException("Unexpected PE header offset.");
        int nsec = BitConverter.ToUInt16(hdr, pe + 6);
        int opt = BitConverter.ToUInt16(hdr, pe + 20);
        int sh = pe + 24 + opt;
        var snap = new Snapshot { Pid = p.Id, ModuleBase = b, Time = DateTime.Now };
        for (int i = 0; i < nsec; i++)
        {
            int o = sh + i * 40;
            if (o + 40 > hdr.Length) break;
            string name = Encoding.ASCII.GetString(hdr, o, 8).TrimEnd('\0');
            int virtualSize = BitConverter.ToInt32(hdr, o + 8);
            int rva = BitConverter.ToInt32(hdr, o + 12);
            int rawSize = BitConverter.ToInt32(hdr, o + 16);
            uint chars = BitConverter.ToUInt32(hdr, o + 36);
            if ((chars & 0x20000000u) == 0) continue; // executable sections only
            int size = Math.Max(virtualSize, rawSize);
            if (size <= 0 || size > 16 * 1024 * 1024) continue;
            snap.Sections.Add(new SectionSnap { Name = name, Rva = rva, Bytes = Read(b + rva, size) });
        }
        if (snap.Sections.Count == 0) throw new InvalidOperationException("No executable PE sections captured.");

        uint a = R32(b + RVA_SELECTED_BUILDING_A);
        uint bb = R32(b + RVA_SELECTED_BUILDING_B);
        snap.Building = a != 0 ? a : bb;
        if (snap.Building != 0)
        {
            try { snap.BuildingBytes = Read(snap.Building, BUILDING_SIZE); }
            catch { snap.BuildingBytes = Array.Empty<byte>(); }
        }
        return snap;
    }

    static string Hex(byte[] b, int start, int len)
    {
        int n = Math.Min(len, Math.Min(48, b.Length - start));
        if (n <= 0) return "";
        string x = BitConverter.ToString(b, start, n).Replace('-', ' ');
        if (len > n) x += " ...";
        return x;
    }

    static List<(int start,int len)> Ranges(byte[] a, byte[] b)
    {
        var r = new List<(int,int)>();
        int n = Math.Min(a.Length, b.Length), i = 0;
        while (i < n)
        {
            if (a[i] == b[i]) { i++; continue; }
            int s = i++;
            while (i < n && a[i] != b[i]) i++;
            r.Add((s, i - s));
        }
        return r;
    }

    public static string StableSlotsText(string tag, byte[] x)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{tag} stable six-slot window (+0x5AC, stride 0x1C):");
        if (x.Length < STABLE_SLOTS + SLOT_STRIDE * SLOT_COUNT) { sb.AppendLine("  unavailable (select the Stable before capture)"); return sb.ToString(); }
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            int o = STABLE_SLOTS + i * SLOT_STRIDE;
            uint d0 = BitConverter.ToUInt32(x, o + 0x00);
            uint d4 = BitConverter.ToUInt32(x, o + 0x04);
            uint d8 = BitConverter.ToUInt32(x, o + 0x08);
            uint dc = BitConverter.ToUInt32(x, o + 0x0C);
            uint d10 = BitConverter.ToUInt32(x, o + 0x10);
            uint d14 = BitConverter.ToUInt32(x, o + 0x14);
            uint d18 = BitConverter.ToUInt32(x, o + 0x18);
            sb.AppendLine($"  slot{i}: +{o:X3} [0]={d0} [4]={d4} [8]={d8} [C]={dc} [10]={d10} [14]={d14} [18]=0x{d18:X8}");
        }
        return sb.ToString();
    }

    public static string Diff(Snapshot before, Snapshot after)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BRZE 1.60 — WeMod Horse OFF -> ON differential");
        sb.AppendLine("READ ONLY observer; no target memory was modified by this program.");
        sb.AppendLine($"Before: {before.Time:O} pid={before.Pid} base=0x{before.ModuleBase:X8} selected=0x{before.Building:X8}");
        sb.AppendLine($"After : {after.Time:O} pid={after.Pid} base=0x{after.ModuleBase:X8} selected=0x{after.Building:X8}");
        sb.AppendLine();
        if (before.Pid != after.Pid || before.ModuleBase != after.ModuleBase)
            sb.AppendLine("WARNING: process/base changed between captures; restart procedure and retry.");

        int totalRanges = 0;
        sb.AppendLine("=== EXECUTABLE MODULE BYTE DIFF ===");
        foreach (var a in before.Sections)
        {
            var b = after.Sections.FirstOrDefault(z => z.Name == a.Name && z.Rva == a.Rva);
            if (b == null) { sb.AppendLine($"section {a.Name} missing after"); continue; }
            var rr = Ranges(a.Bytes, b.Bytes);
            totalRanges += rr.Count;
            sb.AppendLine($"section {a.Name} RVA 0x{a.Rva:X}: changed ranges={rr.Count}");
            foreach (var (start,len) in rr.Take(200))
            {
                sb.AppendLine($"  RVA 0x{a.Rva + start:X8} VA 0x{after.ModuleBase + a.Rva + start:X8} len={len}");
                sb.AppendLine("    OFF: " + Hex(a.Bytes, start, len));
                sb.AppendLine("    ON : " + Hex(b.Bytes, start, len));
            }
            if (rr.Count > 200) sb.AppendLine($"  ... {rr.Count - 200} more ranges omitted");
        }
        sb.AppendLine($"TOTAL executable changed ranges: {totalRanges}");
        sb.AppendLine();

        sb.AppendLine("=== SELECTED STABLE OBJECT DIFF ===");
        if (before.Building == 0 || after.Building == 0 || before.BuildingBytes.Length == 0 || after.BuildingBytes.Length == 0)
        {
            sb.AppendLine("Stable snapshot unavailable. Keep the same Stable selected for both captures.");
        }
        else
        {
            if (before.Building != after.Building) sb.AppendLine("WARNING: selected building pointer changed between captures.");
            int n = Math.Min(before.BuildingBytes.Length, after.BuildingBytes.Length);
            int changes = 0;
            for (int o = 0; o + 4 <= n; o += 4)
            {
                uint x = BitConverter.ToUInt32(before.BuildingBytes, o);
                uint y = BitConverter.ToUInt32(after.BuildingBytes, o);
                if (x == y) continue;
                changes++;
                sb.AppendLine($"  +0x{o:X3}: 0x{x:X8} ({x}) -> 0x{y:X8} ({y})");
            }
            sb.AppendLine($"Stable dword changes: {changes}");
            sb.AppendLine();
            sb.Append(StableSlotsText("OFF", before.BuildingBytes));
            sb.Append(StableSlotsText("ON ", after.BuildingBytes));
        }
        sb.AppendLine();
        sb.AppendLine("Log path: %TEMP%\\BRZE-Horse-WeMod-Diff.txt");
        return sb.ToString();
    }

    public static void Close()
    {
        if (h != IntPtr.Zero) CloseHandle(h);
        h = IntPtr.Zero; p = null;
    }
}
