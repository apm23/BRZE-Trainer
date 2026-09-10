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
    [STAThread] static void Main(){ ApplicationConfiguration.Initialize(); Application.Run(new MainForm()); }
}

internal sealed class MainForm : Form
{
    readonly ComboBox mode = new(){ DropDownStyle=ComboBoxStyle.DropDownList, Width=180 };
    readonly Button off1 = new(){ Text="1) Capture OFF baseline", Width=220, Height=38 };
    readonly Button on = new(){ Text="2) Capture ON", Width=220, Height=38 };
    readonly Button off2 = new(){ Text="3) Capture OFF again + Analyze", Width=260, Height=38 };
    readonly TextBox output = new(){ Multiline=true, ScrollBars=ScrollBars.Both, WordWrap=false, Dock=DockStyle.Fill, Font=new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace,9f) };
    readonly Label note = new(){ AutoSize=true, MaximumSize=new System.Drawing.Size(1080,0), Text="READ-ONLY. Test ONE WeMod cheat at a time. Horse: keep the same Stable selected. Stamina: keep the same unit selected. OFF baseline -> enable only that cheat -> ON -> disable it -> OFF again + Analyze." };

    public MainForm()
    {
        Text="BRZE 1.60 — WeMod Horse/Stamina Forensic Observer v2 (READ ONLY)"; Width=1180; Height=760; StartPosition=FormStartPosition.CenterScreen;
        mode.Items.AddRange(new object[]{"HORSE","STAMINA"}); mode.SelectedIndex=0;
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=110,Padding=new Padding(10),WrapContents=true};
        top.Controls.Add(new Label{Text="Test:",AutoSize=true,Padding=new Padding(0,10,0,0)}); top.Controls.Add(mode); top.Controls.Add(off1); top.Controls.Add(on); top.Controls.Add(off2); top.Controls.Add(note);
        Controls.Add(output); Controls.Add(top);
        off1.Click += (_,_) => output.Text=Observer.Step1(mode.Text);
        on.Click += (_,_) => output.Text=Observer.Step2(mode.Text);
        off2.Click += (_,_) => output.Text=Observer.Step3(mode.Text);
    }
}

internal static class Observer
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    const uint ACCESS=0x10|0x400;
    const int RVA_LOCAL_ID=0x4416D0,RVA_SELECTED_BUILDING_A=0x4417D4,RVA_SELECTED_BUILDING_B=0x4417D8,RVA_SELECTION_LIST=0x441708,RVA_HORSE_PLAYER_PTR=0x4416A4;
    const int BUILDING_SIZE=0x6A4,UNIT_SIZE=0x818,HORSE_PLAYER_STRIDE=0xA0;
    const uint IMAGE_SCN_MEM_WRITE=0x80000000;

    sealed record Sec(string Name,uint Rva,uint Characteristics,byte[] Data);
    sealed record Snap(int Pid,long Base,List<Sec> Sections,uint BuildingPtr,byte[] Building,uint UnitPtr,byte[] Unit,uint HorseBase,uint LocalId,byte[] HorsePlayer);
    sealed class Session { public Snap? A; public Snap? B; }
    static readonly Dictionary<string,Session> sessions=new(StringComparer.OrdinalIgnoreCase){{"HORSE",new Session()},{"STAMINA",new Session()}};

    static bool Read(IntPtr h,long a,byte[] b)=>ReadProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(IntPtr h,long a){var b=new byte[4];return Read(h,a,b)?BitConverter.ToUInt32(b,0):0;}

    static List<Sec> ReadInterestingSections(IntPtr h,long b)
    {
        var result=new List<Sec>(); var dos=new byte[0x40]; if(!Read(h,b,dos)||dos[0]!=0x4D||dos[1]!=0x5A)return result;
        int pe=BitConverter.ToInt32(dos,0x3C); var fh=new byte[24]; if(!Read(h,b+pe,fh)||fh[0]!=0x50||fh[1]!=0x45)return result;
        ushort count=BitConverter.ToUInt16(fh,6),opt=BitConverter.ToUInt16(fh,20); long sh=b+pe+24+opt;
        for(int i=0;i<count;i++)
        {
            var s=new byte[40]; if(!Read(h,sh+i*40,s))break;
            string name=Encoding.ASCII.GetString(s,0,8).TrimEnd('\0'); uint vs=BitConverter.ToUInt32(s,8),rva=BitConverter.ToUInt32(s,12),raw=BitConverter.ToUInt32(s,16),ch=BitConverter.ToUInt32(s,36);
            uint size=Math.Max(vs,raw); if(size==0||size>0x02000000)continue;
            bool wanted=name==".text"||(ch&IMAGE_SCN_MEM_WRITE)!=0; if(!wanted)continue;
            var data=new byte[size]; if(Read(h,b+rva,data))result.Add(new Sec(name,rva,ch,data));
        }
        return result;
    }

    static Snap? Capture(out string err)
    {
        err=""; var ps=Process.GetProcessesByName("Battle_Realms_F"); if(ps.Length==0){err="Battle_Realms_F.exe not running";return null;}
        var p=ps[0]; long b; try{b=p.MainModule!.BaseAddress.ToInt64();}catch(Exception e){err=e.Message;return null;}
        IntPtr h=OpenProcess(ACCESS,false,p.Id); if(h==IntPtr.Zero){err="OpenProcess failed";return null;}
        try
        {
            var secs=ReadInterestingSections(h,b); if(!secs.Any(x=>x.Name==".text")){err=".text snapshot failed";return null;}
            uint lid=R32(h,b+RVA_LOCAL_ID),ba=R32(h,b+RVA_SELECTED_BUILDING_A),bb=R32(h,b+RVA_SELECTED_BUILDING_B),bp=ba!=0?ba:bb;
            var build=bp==0?Array.Empty<byte>():new byte[BUILDING_SIZE]; if(bp!=0&&!Read(h,bp,build))build=Array.Empty<byte>();
            uint node=R32(h,b+RVA_SELECTION_LIST),up=node==0?0:R32(h,node+8); var unit=up==0?Array.Empty<byte>():new byte[UNIT_SIZE]; if(up!=0&&!Read(h,up,unit))unit=Array.Empty<byte>();
            uint hb=R32(h,b+RVA_HORSE_PLAYER_PTR); var hp=hb==0?Array.Empty<byte>():new byte[HORSE_PLAYER_STRIDE]; if(hb!=0&&!Read(h,(long)hb+lid*HORSE_PLAYER_STRIDE,hp))hp=Array.Empty<byte>();
            return new Snap(p.Id,b,secs,bp,build,up,unit,hb,lid,hp);
        }
        finally{CloseHandle(h);}
    }

    static bool Compatible(Snap a,Snap b)=>a.Pid==b.Pid&&a.Base==b.Base;
    static IEnumerable<(int S,int E)> Changed(byte[] a,byte[] b)
    {
        int n=Math.Min(a.Length,b.Length),i=0; while(i<n){if(a[i]==b[i]){i++;continue;}int s=i;while(i<n&&a[i]!=b[i])i++;yield return(s,i);}
    }
    static string HexContext(byte[] x,int s,int e,int c=8){int lo=Math.Max(0,s-c),hi=Math.Min(x.Length,e+c);return Convert.ToHexString(x.AsSpan(lo,hi-lo));}

    static void DirectByteDiff(StringBuilder sb,string label,byte[] a,byte[] b,uint rvaBase,int max=300)
    {
        sb.AppendLine($"[{label} A->B CHANGES]"); int n=0; foreach(var (s,e) in Changed(a,b)){sb.AppendLine($"RVA/+0x{rvaBase+(uint)s:X}..0x{rvaBase+(uint)(e-1):X} len:{e-s} old[{HexContext(a,s,e)}] new[{HexContext(b,s,e)}]");if(++n>=max){sb.AppendLine("...truncated...");break;}}if(n==0)sb.AppendLine("no changes");
    }

    static void ReversibleByteDiff(StringBuilder sb,string label,byte[] a,byte[] b,byte[] c,uint rvaBase,int max=300)
    {
        sb.AppendLine($"[{label} REVERSIBLE A==C, B DIFFERENT]"); int n=0,i=0,lim=Math.Min(a.Length,Math.Min(b.Length,c.Length));
        while(i<lim&&n<max){if(a[i]==c[i]&&b[i]!=a[i]){int s=i;while(i<lim&&a[i]==c[i]&&b[i]!=a[i])i++;int e=i;sb.AppendLine($"RVA/+0x{rvaBase+(uint)s:X}..0x{rvaBase+(uint)(e-1):X} len:{e-s} OFF[{HexContext(a,s,e)}] ON[{HexContext(b,s,e)}]");n++;}else i++;}
        if(n==0)sb.AppendLine("no reversible changes");
    }

    static void DwordTriplet(StringBuilder sb,string label,byte[] a,byte[] b,byte[] c,int max=250)
    {
        sb.AppendLine($"[{label} DWORD A/OFF -> B/ON -> C/OFF]"); int n=0,lim=Math.Min(a.Length,Math.Min(b.Length,c.Length));
        for(int i=0;i+4<=lim;i+=4){uint x=BitConverter.ToUInt32(a,i),y=BitConverter.ToUInt32(b,i),z=BitConverter.ToUInt32(c,i);if(x!=y||y!=z){string tag=(x==z&&x!=y)?" REVERSIBLE":"";sb.AppendLine($"+0x{i:X3}: 0x{x:X8} ({x}) -> 0x{y:X8} ({y}) -> 0x{z:X8} ({z}){tag}");if(++n>=max){sb.AppendLine("...truncated...");break;}}}if(n==0)sb.AppendLine("no DWORD changes");
    }

    static string Summary(Snap s)=>$"pid:{s.Pid} base:0x{s.Base:X8} sections:{string.Join(",",s.Sections.Select(x=>$"{x.Name}@RVA0x{x.Rva:X}/0x{x.Data.Length:X}"))} selectedBuilding:0x{s.BuildingPtr:X8} selectedUnit:0x{s.UnitPtr:X8} horseBase:0x{s.HorseBase:X8} localId:{s.LocalId}";

    public static string Step1(string mode)
    {
        var s=Capture(out var e);if(s==null)return"STEP1 FAILED: "+e;var q=sessions[mode];q.A=s;q.B=null;return $"{mode} STEP1 OFF captured\r\n{Summary(s)}\r\nNow enable ONLY WeMod Unlimited {mode}, keep the same selected object/unit, then click Step 2.";
    }
    public static string Step2(string mode)
    {
        var q=sessions[mode];if(q.A==null)return"Do Step 1 first.";var s=Capture(out var e);if(s==null)return"STEP2 FAILED: "+e;if(!Compatible(q.A,s))return"Process/base changed. Redo Step 1.";q.B=s;
        var sb=new StringBuilder();sb.AppendLine($"{mode} STEP2 ON captured");sb.AppendLine(Summary(s));var at=q.A.Sections.First(x=>x.Name==".text");var bt=s.Sections.FirstOrDefault(x=>x.Name==".text");if(bt!=null)DirectByteDiff(sb,".TEXT",at.Data,bt.Data,at.Rva,120);sb.AppendLine("Now disable the SAME WeMod cheat, wait ~1 second, then click Step 3. Step 3 filters reversible cheat patches.");return sb.ToString();
    }
    public static string Step3(string mode)
    {
        var q=sessions[mode];if(q.A==null||q.B==null)return"Do Steps 1 and 2 first.";var c=Capture(out var e);if(c==null)return"STEP3 FAILED: "+e;if(!Compatible(q.A,c))return"Process/base changed. Redo test.";
        var a=q.A;var b=q.B;var sb=new StringBuilder();sb.AppendLine($"BRZE WeMod forensic tri-diff — {mode}");sb.AppendLine($"A/OFF {Summary(a)}");sb.AppendLine($"B/ON  {Summary(b)}");sb.AppendLine($"C/OFF {Summary(c)}");sb.AppendLine();
        foreach(var sa in a.Sections){var sb1=b.Sections.FirstOrDefault(x=>x.Name==sa.Name&&x.Rva==sa.Rva);var sc=c.Sections.FirstOrDefault(x=>x.Name==sa.Name&&x.Rva==sa.Rva);if(sb1==null||sc==null)continue;if(sa.Name==".text"){DirectByteDiff(sb,".TEXT",sa.Data,sb1.Data,sa.Rva,180);ReversibleByteDiff(sb,".TEXT",sa.Data,sb1.Data,sc.Data,sa.Rva,180);}else ReversibleByteDiff(sb,$"SECTION {sa.Name}",sa.Data,sb1.Data,sc.Data,sa.Rva,220);}
        if(a.BuildingPtr==b.BuildingPtr&&b.BuildingPtr==c.BuildingPtr&&a.Building.Length>0&&b.Building.Length>0&&c.Building.Length>0)DwordTriplet(sb,$"SELECTED BUILDING 0x{a.BuildingPtr:X8}",a.Building,b.Building,c.Building);else sb.AppendLine("[BUILDING] pointer changed/unavailable — keep same Stable selected for all 3 captures.");
        if(a.UnitPtr==b.UnitPtr&&b.UnitPtr==c.UnitPtr&&a.Unit.Length>0&&b.Unit.Length>0&&c.Unit.Length>0)DwordTriplet(sb,$"SELECTED UNIT 0x{a.UnitPtr:X8}",a.Unit,b.Unit,c.Unit);else sb.AppendLine("[UNIT] pointer changed/unavailable — keep same unit selected for all 3 captures.");
        if(a.HorseBase==b.HorseBase&&b.HorseBase==c.HorseBase&&a.HorsePlayer.Length>0&&b.HorsePlayer.Length>0&&c.HorsePlayer.Length>0)DwordTriplet(sb,$"PLAYER HORSE BLOCK localId:{a.LocalId}",a.HorsePlayer,b.HorsePlayer,c.HorsePlayer);
        string text=sb.ToString();string path=Save(mode,text);return text+"\r\nSAVED: "+path;
    }
    static string Save(string mode,string text)
    {
        try{string desk=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);string p=Path.Combine(desk,$"BRZE-WeMod-{mode}-Diff-{DateTime.Now:yyyyMMdd-HHmmss}.txt");File.WriteAllText(p,text);return p;}catch(Exception e){return"save failed: "+e.Message;}
    }
}