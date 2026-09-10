using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEWeModDiff;

internal static class Program
{
    [STAThread] static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new MainForm()); }
}

internal sealed class MainForm : Form
{
    readonly Button baseline = new() { Text = "1) Capture WeMod OFF baseline", Width = 260, Height = 38 };
    readonly Button diff = new() { Text = "2) Capture WeMod ON + Diff", Width = 260, Height = 38 };
    readonly TextBox output = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, 9f) };
    readonly Label note = new() { AutoSize = true, Text = "READ-ONLY. Horse: select the Stable. Stamina: select one unit. Capture baseline with the corresponding WeMod cheat OFF, turn it ON, wait for the visible effect, then capture diff." };

    public MainForm()
    {
        Text = "BRZE 1.60 — WeMod Horse/Stamina Diff Observer (READ ONLY)";
        Width = 1100; Height = 700; StartPosition = FormStartPosition.CenterScreen;
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
        top.Controls.Add(baseline); top.Controls.Add(diff); top.Controls.Add(note);
        Controls.Add(output); Controls.Add(top);
        baseline.Click += (_, _) => output.Text = Observer.CaptureBaseline();
        diff.Click += (_, _) => output.Text = Observer.CaptureDiff();
    }
}

internal static class Observer
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x10 | 0x400;
    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_SELECTED_BUILDING_A = 0x4417D4;
    const int RVA_SELECTED_BUILDING_B = 0x4417D8;
    const int RVA_SELECTION_LIST = 0x441708;
    const int RVA_HORSE_PLAYER_PTR = 0x4416A4;
    const int BUILDING_SIZE = 0x6A4, UNIT_SIZE = 0x818, HORSE_PLAYER_STRIDE = 0xA0;

    sealed record Snap(int Pid, long Base, long TextVA, uint TextRva, byte[] Text, uint BuildingPtr, byte[] Building, uint UnitPtr, byte[] Unit, uint HorseBase, uint LocalId, byte[] HorsePlayer);
    static Snap? off;

    static bool Read(IntPtr h, long a, byte[] b) => ReadProcessMemory(h, new IntPtr(unchecked((int)(uint)a)), b, b.Length, out var n) && n.ToInt64() == b.Length;
    static uint R32(IntPtr h, long a) { var b = new byte[4]; return Read(h, a, b) ? BitConverter.ToUInt32(b, 0) : 0; }

    static (uint rva,uint size)? FindText(IntPtr h,long b)
    {
        var dos = new byte[0x40]; if (!Read(h,b,dos) || dos[0]!=0x4D || dos[1]!=0x5A) return null;
        int pe = BitConverter.ToInt32(dos,0x3C); var fh = new byte[24]; if (!Read(h,b+pe,fh) || fh[0]!=0x50 || fh[1]!=0x45) return null;
        ushort sections = BitConverter.ToUInt16(fh,6), opt = BitConverter.ToUInt16(fh,20); long sh = b+pe+24+opt;
        for(int i=0;i<sections;i++)
        {
            var s=new byte[40]; if(!Read(h,sh+i*40,s)) return null;
            string name=Encoding.ASCII.GetString(s,0,8).TrimEnd('\0');
            if(name==".text") { uint vs=BitConverter.ToUInt32(s,8), va=BitConverter.ToUInt32(s,12), raw=BitConverter.ToUInt32(s,16); return (va,Math.Max(vs,raw)); }
        }
        return null;
    }

    static Snap? Capture(out string err)
    {
        err=""; var ps=Process.GetProcessesByName("Battle_Realms_F"); if(ps.Length==0){err="Battle_Realms_F.exe not running";return null;}
        var p=ps[0]; long b; try{b=p.MainModule!.BaseAddress.ToInt64();}catch(Exception e){err=e.Message;return null;}
        IntPtr h=OpenProcess(ACCESS,false,p.Id); if(h==IntPtr.Zero){err="OpenProcess failed";return null;}
        try
        {
            var t=FindText(h,b); if(t==null){err=".text not found";return null;}
            var text=new byte[t.Value.size]; if(!Read(h,b+t.Value.rva,text)){err=".text read failed";return null;}
            uint lid=R32(h,b+RVA_LOCAL_ID);
            uint ba=R32(h,b+RVA_SELECTED_BUILDING_A), bb=R32(h,b+RVA_SELECTED_BUILDING_B); uint bp=ba!=0?ba:bb;
            var build=bp==0?Array.Empty<byte>():new byte[BUILDING_SIZE]; if(bp!=0&&!Read(h,bp,build)) build=Array.Empty<byte>();
            uint node=R32(h,b+RVA_SELECTION_LIST), up=node==0?0:R32(h,node+8);
            var unit=up==0?Array.Empty<byte>():new byte[UNIT_SIZE]; if(up!=0&&!Read(h,up,unit)) unit=Array.Empty<byte>();
            uint horseBase=R32(h,b+RVA_HORSE_PLAYER_PTR); var horse=horseBase==0?Array.Empty<byte>():new byte[HORSE_PLAYER_STRIDE];
            if(horseBase!=0&&!Read(h,(long)horseBase+lid*HORSE_PLAYER_STRIDE,horse)) horse=Array.Empty<byte>();
            return new Snap(p.Id,b,b+t.Value.rva,t.Value.rva,text,bp,build,up,unit,horseBase,lid,horse);
        }
        finally{CloseHandle(h);}
    }

    static IEnumerable<string> DiffRanges(byte[] a, byte[] b, uint baseOff, int context=8, int maxRanges=300)
    {
        int n=Math.Min(a.Length,b.Length), ranges=0, i=0;
        while(i<n && ranges<maxRanges)
        {
            if(a[i]==b[i]){i++;continue;}
            int s=i; while(i<n && a[i]!=b[i]) i++; int e=i;
            int lo=Math.Max(0,s-context), hi=Math.Min(n,e+context);
            string oldHex=Convert.ToHexString(a.AsSpan(lo,hi-lo)); string newHex=Convert.ToHexString(b.AsSpan(lo,hi-lo));
            yield return $"+0x{baseOff+(uint)s:X}..+0x{baseOff+(uint)(e-1):X} len:{e-s} | old[{oldHex}] new[{newHex}]";
            ranges++;
        }
    }

    static string DwordDiff(byte[] a,byte[] b,string label)
    {
        var sb=new StringBuilder(); sb.AppendLine($"[{label} DWORD DIFF]"); int n=Math.Min(a.Length,b.Length);
        int count=0; for(int i=0;i+4<=n;i+=4){uint x=BitConverter.ToUInt32(a,i),y=BitConverter.ToUInt32(b,i);if(x!=y){sb.AppendLine($"+0x{i:X3}: 0x{x:X8} ({x}) -> 0x{y:X8} ({y})");if(++count>=250){sb.AppendLine("...truncated...");break;}}}
        if(count==0) sb.AppendLine("no DWORD changes"); return sb.ToString();
    }

    public static string CaptureBaseline()
    {
        off=Capture(out string err); if(off==null)return "BASELINE FAILED: "+err;
        string s=$"BASELINE OK pid:{off.Pid} base:0x{off.Base:X8} textRVA:0x{off.TextRva:X} textSize:0x{off.Text.Length:X}\r\nselectedBuilding:0x{off.BuildingPtr:X8} selectedUnit:0x{off.UnitPtr:X8} horseBase:0x{off.HorseBase:X8} localId:{off.LocalId}\r\nNow enable ONLY the WeMod Horse or Stamina cheat being tested, wait for its visible effect, then click Capture ON + Diff.";
        Save(s); return s;
    }

    public static string CaptureDiff()
    {
        if(off==null)return "Capture OFF baseline first."; var on=Capture(out string err);if(on==null)return "ON CAPTURE FAILED: "+err;
        var sb=new StringBuilder(); sb.AppendLine($"DIFF pid OFF:{off.Pid} ON:{on.Pid} base OFF:0x{off.Base:X8} ON:0x{on.Base:X8}");
        if(off.Pid!=on.Pid||off.Base!=on.Base){sb.AppendLine("PROCESS/BASE CHANGED — invalid comparison; recapture baseline.");return sb.ToString();}
        sb.AppendLine("[.TEXT CODE DIFF]"); var td=DiffRanges(off.Text,on.Text,off.TextRva).ToList(); if(td.Count==0)sb.AppendLine("no executable .text changes");else foreach(var x in td)sb.AppendLine(x);
        if(off.BuildingPtr==on.BuildingPtr&&off.Building.Length>0&&on.Building.Length>0) sb.Append(DwordDiff(off.Building,on.Building,$"SELECTED BUILDING 0x{on.BuildingPtr:X8}")); else sb.AppendLine("[BUILDING] pointer changed/unavailable; keep same Stable selected for both captures.");
        if(off.UnitPtr==on.UnitPtr&&off.Unit.Length>0&&on.Unit.Length>0) sb.Append(DwordDiff(off.Unit,on.Unit,$"SELECTED UNIT 0x{on.UnitPtr:X8}")); else sb.AppendLine("[UNIT] pointer changed/unavailable; keep same unit selected for stamina test.");
        if(off.HorseBase==on.HorseBase&&off.HorsePlayer.Length>0&&on.HorsePlayer.Length>0) sb.Append(DwordDiff(off.HorsePlayer,on.HorsePlayer,$"PLAYER HORSE BLOCK id:{on.LocalId}")); else sb.AppendLine("[HORSE PLAYER BLOCK] pointer changed/unavailable.");
        string s=sb.ToString();Save(s);return s;
    }

    static void Save(string s)
    {
        try{File.AppendAllText(Path.Combine(Path.GetTempPath(),"BRZE-WeMod-Horse-Stamina-Diff.log"),DateTime.Now.ToString("O")+Environment.NewLine+s+Environment.NewLine+new string('=',90)+Environment.NewLine);}catch{}
    }
}
