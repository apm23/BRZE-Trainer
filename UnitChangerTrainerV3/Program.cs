using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEUnitChangerLabV3;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class UnitChoice
{
    public uint Type { get; }
    public string Name { get; }
    public UnitChoice(uint type, string name) { Type = type; Name = name; }
    public override string ToString() => $"0x{Type:X2}  {Name}";
}

internal sealed class MainForm : Form
{
    readonly CheckBox master = new() { Text = "OUTPUT OVERRIDE" };
    readonly CheckBox slot1 = new() { Text = "ON" };
    readonly ComboBox output1 = new();
    readonly Label game = new();
    readonly Label context = new();
    readonly Label monitor = new();
    readonly Label recipes = new();
    readonly Button reset = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 250 };

    static readonly UnitChoice[] Units =
    {
        new(0, "Dragon Archer"), new(1, "Dragon Chemist"), new(2, "Dragon Dragon Warrior"),
        new(3, "Dragon Geisha"), new(4, "Dragon Kabuki Warrior"), new(5, "Dragon Peasant"),
        new(6, "Dragon Powder Keg Cannoneer"), new(7, "Dragon Samurai"), new(8, "Dragon Spearman"),
        new(19, "Serpent Bandit"), new(20, "Serpent Cannoneer"), new(21, "Serpent Crossbowman"),
        new(22, "Serpent Fan Geisha"), new(23, "Serpent Musketeer"), new(24, "Serpent Peasant"),
        new(25, "Serpent Raider"), new(26, "Serpent Ronin"), new(29, "Serpent Swordsman"),
        new(30, "Lotus Blade Acolyte"), new(34, "Lotus Channeler"), new(35, "Lotus Diseased One"),
        new(37, "Lotus Infested One"), new(38, "Lotus Leaf Disciple"), new(39, "Lotus Master Warlock"),
        new(40, "Lotus Peasant"), new(41, "Lotus Staff Adept"), new(42, "Lotus Unclean One"), new(43, "Lotus Warlock"),
        new(44, "Wolf Ballistaman"), new(45, "Wolf Berserker"), new(46, "Wolf Brawler"),
        new(47, "Wolf Druidess"), new(48, "Wolf Hurler"), new(49, "Wolf Mauler"),
        new(50, "Wolf Pack Master"), new(51, "Wolf Peasant"), new(52, "Wolf Pitch Slinger"),
        new(53, "Wolf Sledger"), new(54, "Wolf Werewolf")
    };

    public MainForm()
    {
        Text = "BRZE Unit Changer Lab v3 — Completion Only";
        ClientSize = new Size(1080, 790);
        MinimumSize = new Size(1030, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = Card();
        header.Controls.Add(new Label { Text = "UNIT CHANGER V3 // COMPLETION-ONLY", Font = new Font("Segoe UI Semibold", 18f), AutoSize = true, Location = new Point(16, 10) });
        header.Controls.Add(new Label { Text = "Global mapper stays stock. Only the real training-completion call is intercepted.", ForeColor = Color.FromArgb(157,178,202), AutoSize = true, Location = new Point(18, 47) });
        root.Controls.Add(header, 0, 0);

        var cfg = Card();
        cfg.Controls.Add(Title("SLOT 1 — 1→1 PROOF", 10));
        slot1.Location = new Point(18, 49); slot1.Size = new Size(52, 24); slot1.ForeColor = Color.WhiteSmoke;
        output1.DropDownStyle = ComboBoxStyle.DropDownList; output1.Location = new Point(80, 47); output1.Size = new Size(720, 24);
        output1.Items.AddRange(Units.Cast<object>().ToArray()); output1.SelectedIndex = 0;
        master.Location = new Point(820, 49); master.AutoSize = true; master.ForeColor = Color.FromArgb(97,224,179);
        cfg.Controls.Add(slot1); cfg.Controls.Add(output1); cfg.Controls.Add(master);
        cfg.Controls.Add(new Label { Text = "Slots 2–9 remain locked until this completion-only 1→1 path passes runtime.", AutoSize = true, Location = new Point(18, 77), ForeColor = Color.FromArgb(236,183,84) });
        root.Controls.Add(cfg, 0, 1);

        var c = Card(); c.Controls.Add(Title("CURRENT CONTEXT", 10));
        context.Font = new Font(FontFamily.GenericMonospace, 9f); context.Location = new Point(14, 38); context.Size = new Size(1010, 74); context.AutoSize = false;
        c.Controls.Add(context); root.Controls.Add(c, 0, 2);

        var m = Card(); m.Controls.Add(Title("COMPLETION OVERRIDE MONITOR", 10));
        monitor.Font = new Font(FontFamily.GenericMonospace, 9f); monitor.Location = new Point(14, 38); monitor.Size = new Size(1010, 112); monitor.AutoSize = false;
        m.Controls.Add(monitor); root.Controls.Add(m, 0, 3);

        var r = Card(); r.Controls.Add(Title("SELECTED BUILDING — NATIVE RECIPES", 10));
        recipes.Font = new Font(FontFamily.GenericMonospace, 8.8f); recipes.Location = new Point(14, 38); recipes.Size = new Size(1010, 102); recipes.AutoSize = false; recipes.ForeColor = Color.FromArgb(170,181,197);
        r.Controls.Add(recipes); root.Controls.Add(r, 0, 4);

        var foot = Card();
        game.AutoSize = true; game.Location = new Point(14, 14); game.ForeColor = Color.FromArgb(97,224,179);
        reset.Text = "RESET COUNT"; reset.Location = new Point(830, 10); reset.Size = new Size(150, 32); StyleButton(reset);
        reset.Click += (_,_) => CompletionCore.ResetCounter();
        foot.Controls.Add(game); foot.Controls.Add(reset);
        root.Controls.Add(foot, 0, 5);

        void Push()
        {
            if (output1.SelectedItem is UnitChoice u) CompletionCore.Configure(master.Checked && slot1.Checked, u.Type);
        }
        slot1.CheckedChanged += (_,_) => Push();
        master.CheckedChanged += (_,_) => Push();
        output1.SelectedIndexChanged += (_,_) => Push();
        timer.Tick += (_,_) => { Push(); var s = CompletionCore.Snapshot(); game.Text = s.Game; context.Text = s.Context; monitor.Text = s.Monitor; recipes.Text = s.Recipes; };
        timer.Start();
        FormClosed += (_,_) => CompletionCore.Reset();
    }

    static Panel Card() => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18,24,34), Padding = new Padding(10) };
    static Label Title(string text, int y) => new() { Text = text, Font = new Font("Segoe UI Semibold", 11f), ForeColor = Color.FromArgb(97,224,179), AutoSize = true, Location = new Point(14,y) };
    static void StyleButton(Button b) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Color.FromArgb(70,90,115); b.BackColor = Color.FromArgb(42,55,72); b.ForeColor = Color.White; }
}

internal readonly record struct CoreSnapshot(string Game, string Context, string Monitor, string Recipes);

internal static class CompletionCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h, IntPtr addr, byte[] buf, int size, out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr addr, UIntPtr size, uint allocationType, uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h, IntPtr addr, UIntPtr size, uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h, IntPtr addr, UIntPtr size, uint newProtect, out uint oldProtect);
    [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h, IntPtr addr, UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS = 0x0010 | 0x0020 | 0x0008 | 0x0400;
    const uint MEM_COMMIT=0x1000, MEM_RESERVE=0x2000, MEM_RELEASE=0x8000, PAGE_EXECUTE_READWRITE=0x40;

    const int RVA_LOCAL_ID = 0x4416D0;
    const int RVA_SELECTION_LIST = 0x441708;
    const int RVA_SELECTED_BUILDING_A = 0x4417D4;
    const int RVA_SELECTED_BUILDING_B = 0x4417D8;
    const int RVA_TRAIN_MAP = 0x0D69F6;
    const int RVA_COMPLETION_CALL = 0x0D5E08;

    const int OFF_UNIT_DEF=0x74, OFF_UNIT_OWNER=0x240;
    const int OFF_BUILD_OWNER=0x84, OFF_BUILD_TYPE=0x78, OFF_BUILD_DEF=0x258;
    const int OFF_TRAIN_TYPE=0x488, OFF_TRAIN_PROGRESS=0x490;
    static readonly int[] UnitInOffsets={0x68,0x78,0x88,0x98,0xA8,0xB8};
    static readonly int[] UnitOutOffsets={0x6C,0x7C,0x8C,0x9C,0xAC,0xBC};

    // 004D5E08: E8 E9 0B 00 00 -> call 004D69F6
    static readonly byte[] CompletionCallOriginal={0xE8,0xE9,0x0B,0x00,0x00};
    // V3 intentionally does NOT patch RVA_TRAIN_MAP.

    const int CFG_ENABLED=0x00, CFG_OUTPUT=0x04, T_COUNT=0x08, T_BUILDING=0x0C, T_INPUT=0x10, T_NATIVE=0x14;
    const int STUB_OFFSET=0x80;

    static readonly object sync=new();
    static Process? process;
    static IntPtr h;
    static long moduleBase;
    static IntPtr cave;
    static bool installed;
    static byte[]? installedPatch;
    static string status="not attached";
    static DateTime lastTry;
    static bool wantedEnabled;
    static uint wantedOutput;

    public static void Configure(bool enabled,uint output)
    {
        lock(sync)
        {
            wantedEnabled=enabled; wantedOutput=output;
            if(!EnsureAttached() || cave==IntPtr.Zero) return;
            W32(cave.ToInt64()+CFG_OUTPUT,output);
            W32(cave.ToInt64()+CFG_ENABLED,enabled?1u:0u);
        }
    }

    public static void ResetCounter()
    {
        lock(sync)
        {
            if(!EnsureAttached() || cave==IntPtr.Zero) return;
            W32(cave.ToInt64()+T_COUNT,0); W32(cave.ToInt64()+T_BUILDING,0); W32(cave.ToInt64()+T_INPUT,0xFFFFFFFF); W32(cave.ToInt64()+T_NATIVE,0xFFFFFFFF);
        }
    }

    public static void Reset(){lock(sync) ResetUnlocked();}
    static void ResetUnlocked()
    {
        try
        {
            if(h!=IntPtr.Zero && installed && installedPatch!=null)
            {
                long target=moduleBase+RVA_COMPLETION_CALL;
                var now=new byte[5];
                if(ReadExact(target,now) && now.SequenceEqual(installedPatch)) WriteCode(target,CompletionCallOriginal);
            }
            if(h!=IntPtr.Zero && cave!=IntPtr.Zero) VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);
            if(h!=IntPtr.Zero) CloseHandle(h);
        } catch{}
        process=null; h=IntPtr.Zero; moduleBase=0; cave=IntPtr.Zero; installed=false; installedPatch=null; status="not attached"; lastTry=DateTime.MinValue;
    }

    static bool EnsureAttached()
    {
        try { if(process!=null && !process.HasExited && h!=IntPtr.Zero){ if(!installed) Install(); return true; } } catch{}
        ResetUnlocked();
        if((DateTime.UtcNow-lastTry).TotalMilliseconds<400) return false;
        lastTry=DateTime.UtcNow;
        var ps=Process.GetProcessesByName("Battle_Realms_F"); if(ps.Length==0) return false;
        process=ps[0];
        try { moduleBase=process.MainModule!.BaseAddress.ToInt64(); } catch { process=null; return false; }
        h=OpenProcess(ACCESS,false,process.Id); if(h==IntPtr.Zero) return false;
        Install(); return true;
    }

    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero && ReadProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,b.Length,out var n) && n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4]; return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero && WriteProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,b.Length,out var n) && n.ToInt64()==b.Length;
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
    static bool WriteCode(long a,byte[] b)
    {
        if(!VirtualProtectEx(h,new IntPtr(unchecked((int)(uint)a)),(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old)) return false;
        bool ok=WriteBytes(a,b); FlushInstructionCache(h,new IntPtr(unchecked((int)(uint)a)),(UIntPtr)b.Length); VirtualProtectEx(h,new IntPtr(unchecked((int)(uint)a)),(UIntPtr)b.Length,old,out _); return ok;
    }
    static void U32(List<byte>b,uint v)=>b.AddRange(BitConverter.GetBytes(v));
    static void I32(List<byte>b,int v)=>b.AddRange(BitConverter.GetBytes(v));
    static void PatchRel(List<byte>b,int at,long fromNext,long to){var x=BitConverter.GetBytes(unchecked((int)(to-fromNext))); for(int i=0;i<4;i++)b[at+i]=x[i];}

    static byte[] BuildStub(long stub,long cfg)
    {
        long mapper=moduleBase+RVA_TRAIN_MAP;
        long enabled=cfg+CFG_ENABLED, output=cfg+CFG_OUTPUT, count=cfg+T_COUNT, lastBuilding=cfg+T_BUILDING, lastInput=cfg+T_INPUT, lastNative=cfg+T_NATIVE;
        var b=new List<byte>();

        // At replacement CALL entry: [esp]=return, [esp+4]=training input, [esp+8]=owner, [esp+0C]=1.
        // Preserve EDX exactly; keep Building* in EDX while calling the untouched native mapper.
        b.Add(0x52);                         // push edx
        b.AddRange(new byte[]{0x8B,0xD1});  // mov edx,ecx (Building*)
        b.AddRange(new byte[]{0xFF,0x74,0x24,0x08}); // push dword ptr [esp+8] (copy original input)
        b.Add(0xE8); int callMap=b.Count; I32(b,0);   // call untouched mapper; it ret 4

        b.AddRange(new byte[]{0x83,0x3D}); U32(b,(uint)enabled); b.Add(0x00);
        b.AddRange(new byte[]{0x0F,0x84}); int jNative1=b.Count; I32(b,0);
        b.AddRange(new byte[]{0x83,0xF8,0xFF});
        b.AddRange(new byte[]{0x0F,0x84}); int jNative2=b.Count; I32(b,0);

        b.Add(0x8B); b.Add(0x0D); U32(b,(uint)(moduleBase+RVA_LOCAL_ID)); // mov ecx,[local id]
        b.AddRange(new byte[]{0x39,0x8A,0x84,0x00,0x00,0x00});          // cmp [edx+84],ecx
        b.AddRange(new byte[]{0x0F,0x85}); int jNative3=b.Count; I32(b,0);

        // Telemetry only on the actual completion call after a valid native mapping.
        b.AddRange(new byte[]{0x89,0x15}); U32(b,(uint)lastBuilding);   // mov [lastBuilding],edx
        b.AddRange(new byte[]{0x8B,0x4C,0x24,0x08});                   // mov ecx,[esp+8] original input
        b.AddRange(new byte[]{0x89,0x0D}); U32(b,(uint)lastInput);
        b.Add(0xA3); U32(b,(uint)lastNative);                          // mov [lastNative],eax
        b.AddRange(new byte[]{0xFF,0x05}); U32(b,(uint)count);
        b.Add(0xA1); U32(b,(uint)output);                              // eax=configured Unit Type

        int finish=b.Count;
        b.AddRange(new byte[]{0x8B,0x4C,0x24,0x08}); // mirror mapper's final ECX=input
        b.Add(0x5A);                                 // pop edx
        b.AddRange(new byte[]{0xC2,0x04,0x00});      // clean original input, return to 004D5E0D

        PatchRel(b,callMap,stub+callMap+4,mapper);
        PatchRel(b,jNative1,stub+jNative1+4,stub+finish);
        PatchRel(b,jNative2,stub+jNative2+4,stub+finish);
        PatchRel(b,jNative3,stub+jNative3+4,stub+finish);
        return b.ToArray();
    }

    static byte[] CallPatch(long target,long stub)
    {
        var p=new byte[5]; p[0]=0xE8; Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,p,1,4); return p;
    }

    static void Install()
    {
        if(installed || h==IntPtr.Zero) return;
        long target=moduleBase+RVA_COMPLETION_CALL;
        var stock=new byte[5];
        if(!ReadExact(target,stock) || !stock.SequenceEqual(CompletionCallOriginal)) { status="HOOK BLOCKED — completion call bytes mismatch. Close V2/V1/other Unit Changer builds."; return; }

        // Explicitly require global mapper entry to still be stock: 55 8B EC 8B 81 58 02 00 00.
        var mapperStock=new byte[]{0x55,0x8B,0xEC,0x8B,0x81,0x58,0x02,0x00,0x00};
        var mapperNow=new byte[mapperStock.Length];
        if(!ReadExact(moduleBase+RVA_TRAIN_MAP,mapperNow) || !mapperNow.SequenceEqual(mapperStock)) { status="HOOK BLOCKED — native mapper is not stock. Close V2 first, then restart BRZE if needed."; return; }

        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)1024,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
        if(cave==IntPtr.Zero){status="HOOK BLOCKED — cave allocation failed"; return;}
        long cfg=cave.ToInt64(), stub=cfg+STUB_OFFSET;
        WriteBytes(cfg,new byte[64]); W32(cfg+CFG_OUTPUT,wantedOutput); W32(cfg+CFG_ENABLED,wantedEnabled?1u:0u); W32(cfg+T_INPUT,0xFFFFFFFF); W32(cfg+T_NATIVE,0xFFFFFFFF);
        var code=BuildStub(stub,cfg); installedPatch=CallPatch(target,stub);
        if(!WriteBytes(stub,code) || !WriteCode(target,installedPatch)) { status="HOOK BLOCKED — write failed"; VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE); cave=IntPtr.Zero; installedPatch=null; return; }
        installed=true; status="V3 COMPLETION HOOK ACTIVE — global mapper untouched";
    }

    static uint FirstSelectedUnit()
    {
        long list=moduleBase+RVA_SELECTION_LIST; uint count=R32(list+0x18), node=R32(list); if(count==0||node==0)return 0; return R32((long)node+8);
    }
    static uint SelectedBuilding(){uint a=R32(moduleBase+RVA_SELECTED_BUILDING_A); return a!=0?a:R32(moduleBase+RVA_SELECTED_BUILDING_B);}
    static uint UnitType(uint u){if(u==0)return 0xFFFFFFFF; uint d=R32((long)u+OFF_UNIT_DEF); return d==0?0xFFFFFFFF:R32(d);}
    static uint NativeOutput(uint b,uint input)
    {
        if(b==0||input==0xFFFFFFFF)return 0xFFFFFFFF; uint d=R32((long)b+OFF_BUILD_DEF); if(d==0)return 0xFFFFFFFF;
        for(int i=0;i<6;i++)if(R32((long)d+UnitInOffsets[i])==input)return R32((long)d+UnitOutOffsets[i]); return 0xFFFFFFFF;
    }

    static string Context(uint local)
    {
        uint u=FirstSelectedUnit(), b=SelectedBuilding(); uint ut=UnitType(u); uint uo=u==0?0xFFFFFFFF:R32((long)u+OFF_UNIT_OWNER);
        if(b==0)return $"Selected unit: {(u==0?"none":$"0x{u:X8} type:0x{ut:X} owner:{uo}")}\r\nSelected building: none";
        uint bo=R32((long)b+OFF_BUILD_OWNER), bt=R32((long)b+OFF_BUILD_TYPE), tt=R32((long)b+OFF_TRAIN_TYPE), pr=R32((long)b+OFF_TRAIN_PROGRESS);
        return $"Selected unit: {(u==0?"none":$"0x{u:X8} type:0x{ut:X} owner:{uo}{(uo==local?" LOCAL":"")}")}\r\nSelected building: 0x{b:X8} type:0x{bt:X} owner:{bo}{(bo==local?" LOCAL":"")} | trainType:0x{tt:X8} progress:0x{pr:X8}";
    }

    static string Recipes(uint b)
    {
        if(b==0)return "Select a training building."; uint d=R32((long)b+OFF_BUILD_DEF); if(d==0)return "BuildingDef unresolved.";
        var sb=new StringBuilder(); for(int i=0;i<6;i++){sb.Append($"#{i+1} 0x{R32((long)d+UnitInOffsets[i]):X8}→0x{R32((long)d+UnitOutOffsets[i]):X8}"); if(i!=5)sb.Append("    "); if(i==2)sb.AppendLine();} return sb.ToString();
    }

    static string Monitor()
    {
        if(!installed||cave==IntPtr.Zero)return status+"\r\nNo output override is active.";
        long c=cave.ToInt64(); uint en=R32(c+CFG_ENABLED), o=R32(c+CFG_OUTPUT), n=R32(c+T_COUNT), b=R32(c+T_BUILDING), i=R32(c+T_INPUT), native=R32(c+T_NATIVE);
        return $"{status}\r\noverride:{(en!=0?"ON":"OFF")} | output:0x{o:X8} | completed intercepts:{n}\r\nlast completion: building:0x{b:X8} input:0x{i:X8} nativeOut:0x{native:X8} → override:0x{o:X8}\r\nEligibility/UI/setup remain stock; only completion call 0x4D5E08 is redirected.";
    }

    public static CoreSnapshot Snapshot()
    {
        lock(sync)
        {
            if(!EnsureAttached())return new("GAME: waiting for Battle_Realms_F.exe","Selected unit: none\r\nSelected building: none",status,"No building selected.");
            if(installed&&cave!=IntPtr.Zero){W32(cave.ToInt64()+CFG_OUTPUT,wantedOutput);W32(cave.ToInt64()+CFG_ENABLED,wantedEnabled?1u:0u);}
            uint local=R32(moduleBase+RVA_LOCAL_ID), b=SelectedBuilding();
            return new($"GAME: attached • PID {process!.Id} • base 0x{moduleBase:X8} • local player {local}",Context(local),Monitor(),Recipes(b));
        }
    }
}
