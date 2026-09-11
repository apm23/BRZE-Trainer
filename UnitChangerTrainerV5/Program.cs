using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEUnitChangerLabV5;

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
    readonly CheckBox master = new() { Text = "MULTI OUTPUT" };
    readonly CheckBox[] slotOn = Enumerable.Range(0, 9).Select(_ => new CheckBox { Text = "ON" }).ToArray();
    readonly ComboBox[] slotOut = Enumerable.Range(0, 9).Select(_ => new ComboBox()).ToArray();
    readonly Label game = new(), context = new(), monitor = new(), recipes = new();
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
        Text = "BRZE Unit Changer Lab v5 — Full 1→9 Stress Test";
        ClientSize = new Size(1120, 980);
        MinimumSize = new Size(1080, 940);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(10, 14, 22);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 340));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = Card();
        header.Controls.Add(new Label { Text = "UNIT CHANGER V5 // FULL 1 → 9", Font = new Font("Segoe UI Semibold", 18f), AutoSize = true, Location = new Point(16, 10) });
        header.Controls.Add(new Label { Text = "First active slot uses the V3-proven completion path. Every other active slot uses the V4-proven native extra-unit path.", ForeColor = Color.FromArgb(157,178,202), AutoSize = true, Location = new Point(18, 47) });
        root.Controls.Add(header, 0, 0);

        var cfg = Card();
        cfg.Controls.Add(Title("OUTPUT SLOTS 1–9", 10));
        master.Location = new Point(890, 12); master.AutoSize = true; master.ForeColor = Color.FromArgb(97,224,179); cfg.Controls.Add(master);
        cfg.Controls.Add(new Label { Text = "Each slot is independent. The first ON slot becomes primary; every other ON slot becomes an extra output.", AutoSize = true, Location = new Point(18, 39), ForeColor = Color.FromArgb(236,183,84) });
        for (int i = 0; i < 9; i++)
        {
            int y = 70 + i * 28;
            cfg.Controls.Add(new Label { Text=$"SLOT {i+1}", AutoSize=false, Size=new Size(70,24), Location=new Point(18,y), TextAlign=ContentAlignment.MiddleLeft });
            slotOn[i].Location = new Point(95,y+1); slotOn[i].Size = new Size(52,24); slotOn[i].ForeColor = Color.WhiteSmoke;
            slotOut[i].DropDownStyle = ComboBoxStyle.DropDownList; slotOut[i].Location = new Point(155,y-1); slotOut[i].Size = new Size(865,24);
            slotOut[i].Items.AddRange(Units.Cast<object>().ToArray()); slotOut[i].SelectedIndex = i % Units.Length;
            cfg.Controls.Add(slotOn[i]); cfg.Controls.Add(slotOut[i]);
        }
        // convenient mixed-clan defaults for full stress test
        SetDefault(0, 0); SetDefault(1, 39); SetDefault(2, 54); SetDefault(3, 23); SetDefault(4, 7); SetDefault(5, 43); SetDefault(6, 50); SetDefault(7, 29); SetDefault(8, 2);
        root.Controls.Add(cfg, 0, 1);

        var c = Card(); c.Controls.Add(Title("CURRENT CONTEXT", 10));
        context.Font = new Font(FontFamily.GenericMonospace, 9f); context.Location = new Point(14,38); context.Size = new Size(1050,66); context.AutoSize = false; c.Controls.Add(context); root.Controls.Add(c,0,2);

        var m = Card(); m.Controls.Add(Title("FULL MULTI-OUTPUT MONITOR", 10));
        monitor.Font = new Font(FontFamily.GenericMonospace, 9f); monitor.Location = new Point(14,38); monitor.Size = new Size(1050,155); monitor.AutoSize = false; m.Controls.Add(monitor); root.Controls.Add(m,0,3);

        var r = Card(); r.Controls.Add(Title("SELECTED BUILDING — NATIVE RECIPES", 10));
        recipes.Font = new Font(FontFamily.GenericMonospace, 8.8f); recipes.Location = new Point(14,38); recipes.Size = new Size(1050,74); recipes.AutoSize = false; recipes.ForeColor = Color.FromArgb(170,181,197); r.Controls.Add(recipes); root.Controls.Add(r,0,4);

        var f = Card(); game.AutoSize=true; game.Location=new Point(14,14); game.ForeColor=Color.FromArgb(97,224,179);
        reset.Text="RESET COUNTERS"; reset.Location=new Point(860,10); reset.Size=new Size(165,32); StyleButton(reset); reset.Click += (_,_) => CompletionCore.ResetCounters();
        f.Controls.Add(game); f.Controls.Add(reset); root.Controls.Add(f,0,5);

        void Push()
        {
            var enabled = new bool[9]; var outputs = new uint[9];
            for (int i = 0; i < 9; i++) { enabled[i] = master.Checked && slotOn[i].Checked; outputs[i] = slotOut[i].SelectedItem is UnitChoice u ? u.Type : 0; }
            CompletionCore.Configure(master.Checked, enabled, outputs);
        }
        master.CheckedChanged += (_,_) => Push();
        for (int i = 0; i < 9; i++) { slotOn[i].CheckedChanged += (_,_) => Push(); slotOut[i].SelectedIndexChanged += (_,_) => Push(); }
        timer.Tick += (_,_) => { Push(); var s=CompletionCore.Snapshot(); game.Text=s.Game; context.Text=s.Context; monitor.Text=s.Monitor; recipes.Text=s.Recipes; };
        timer.Start(); FormClosed += (_,_) => CompletionCore.Reset();
    }

    void SetDefault(int slot, uint type)
    {
        int ix = Array.FindIndex(Units, u => u.Type == type); if (ix >= 0) slotOut[slot].SelectedIndex = ix;
    }

    static Panel Card()=>new(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(18,24,34),Padding=new Padding(10)};
    static Label Title(string text,int y)=>new(){Text=text,Font=new Font("Segoe UI Semibold",11f),ForeColor=Color.FromArgb(97,224,179),AutoSize=true,Location=new Point(14,y)};
    static void StyleButton(Button b){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(70,90,115);b.BackColor=Color.FromArgb(42,55,72);b.ForeColor=Color.White;}
}

internal readonly record struct CoreSnapshot(string Game,string Context,string Monitor,string Recipes);

internal static class CompletionCore
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint allocationType,uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr addr,UIntPtr size,uint newProtect,out uint oldProtect);
    [DllImport("kernel32.dll")] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x0010|0x0020|0x0008|0x0400;
    const uint MEM_COMMIT=0x1000,MEM_RESERVE=0x2000,MEM_RELEASE=0x8000,PAGE_EXECUTE_READWRITE=0x40;
    const int RVA_LOCAL_ID=0x4416D0,RVA_PLAYER_BASE=0x4416A4,RVA_SELECTION_LIST=0x441708,RVA_SELECTED_BUILDING_A=0x4417D4,RVA_SELECTED_BUILDING_B=0x4417D8;
    const int RVA_TRAIN_MAP=0x0D69F6,RVA_CREATE_UNIT=0x0D6A88,RVA_ATTACH_UNIT=0x0D231C,RVA_REGISTER_UNIT=0x0AD6B2,RVA_LOCAL_UNIT_NOTIFY=0x1B605D;
    const int RVA_COMPLETION_CALL=0x0D5E08,RVA_POST_REGISTER_CALL=0x0D5EA4;
    const int OFF_UNIT_DEF=0x74,OFF_UNIT_OWNER=0x240,OFF_BUILD_OWNER=0x84,OFF_BUILD_TYPE=0x78,OFF_BUILD_DEF=0x258,OFF_TRAIN_TYPE=0x488,OFF_TRAIN_PROGRESS=0x490;
    static readonly int[] UnitInOffsets={0x68,0x78,0x88,0x98,0xA8,0xB8}; static readonly int[] UnitOutOffsets={0x6C,0x7C,0x8C,0x9C,0xAC,0xBC};
    static readonly byte[] CompletionCallOriginal={0xE8,0xE9,0x0B,0x00,0x00};
    static readonly byte[] PostRegisterCallOriginal={0xE8,0x09,0x78,0xFD,0xFF};
    static readonly byte[] MapperStock={0x55,0x8B,0xEC,0x8B,0x81,0x58,0x02,0x00,0x00};

    const int CFG_MASTER=0x00,CFG_SLOT_BASE=0x10,CFG_SLOT_STRIDE=8;
    const int T_COMPLETIONS=0x60,T_EXTRA_ATTEMPTS=0x64,T_EXTRA_SUCCESS=0x68,T_EXTRA_FAIL=0x6C,T_BUILDING=0x70,T_INPUT=0x74,T_NATIVE=0x78,T_PENDING=0x7C,T_PRIMARY=0x80,T_LAST_UNIT=0x84,T_SLOT_SUCCESS_BASE=0x90;
    const int STUB1_OFFSET=0x200,STUB2_OFFSET=0x600;

    static readonly object sync=new(); static Process? process; static IntPtr h; static long moduleBase; static IntPtr cave;
    static bool installed; static byte[]? patch1,patch2; static string status="not attached"; static DateTime lastTry;
    static bool wantedMaster; static readonly bool[] wantedEn=new bool[9]; static readonly uint[] wantedOut=new uint[9];

    static long SlotEn(long cfg,int i)=>cfg+CFG_SLOT_BASE+i*CFG_SLOT_STRIDE;
    static long SlotOut(long cfg,int i)=>SlotEn(cfg,i)+4;
    static long SlotSuccess(long cfg,int i)=>cfg+T_SLOT_SUCCESS_BASE+i*4;

    public static void Configure(bool master,bool[] enabled,uint[] outputs)
    {
        lock(sync)
        {
            wantedMaster=master;
            for(int i=0;i<9;i++){wantedEn[i]=enabled[i];wantedOut[i]=outputs[i];}
            if(!EnsureAttached()||cave==IntPtr.Zero)return; PushConfig();
        }
    }
    static void PushConfig(){long c=cave.ToInt64();W32(c+CFG_MASTER,wantedMaster?1u:0u);for(int i=0;i<9;i++){W32(SlotOut(c,i),wantedOut[i]);W32(SlotEn(c,i),wantedEn[i]?1u:0u);}}

    public static void ResetCounters(){lock(sync){if(!EnsureAttached()||cave==IntPtr.Zero)return;long c=cave.ToInt64();foreach(int o in new[]{T_COMPLETIONS,T_EXTRA_ATTEMPTS,T_EXTRA_SUCCESS,T_EXTRA_FAIL,T_BUILDING,T_PENDING,T_PRIMARY,T_LAST_UNIT})W32(c+o,0);W32(c+T_INPUT,0xFFFFFFFF);W32(c+T_NATIVE,0xFFFFFFFF);for(int i=0;i<9;i++)W32(SlotSuccess(c,i),0);}}
    public static void Reset(){lock(sync)ResetUnlocked();}
    static void ResetUnlocked()
    {
        try
        {
            if(h!=IntPtr.Zero&&installed){if(patch2!=null){var n=new byte[5];long a=moduleBase+RVA_POST_REGISTER_CALL;if(ReadExact(a,n)&&n.SequenceEqual(patch2))WriteCode(a,PostRegisterCallOriginal);}if(patch1!=null){var n=new byte[5];long a=moduleBase+RVA_COMPLETION_CALL;if(ReadExact(a,n)&&n.SequenceEqual(patch1))WriteCode(a,CompletionCallOriginal);}}
            if(h!=IntPtr.Zero&&cave!=IntPtr.Zero)VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);if(h!=IntPtr.Zero)CloseHandle(h);
        }catch{}
        process=null;h=IntPtr.Zero;moduleBase=0;cave=IntPtr.Zero;installed=false;patch1=null;patch2=null;status="not attached";lastTry=DateTime.MinValue;
    }
    static bool EnsureAttached()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero){if(!installed)Install();return true;}}catch{}
        ResetUnlocked();if((DateTime.UtcNow-lastTry).TotalMilliseconds<400)return false;lastTry=DateTime.UtcNow;var ps=Process.GetProcessesByName("Battle_Realms_F");if(ps.Length==0)return false;process=ps[0];
        try{moduleBase=process.MainModule!.BaseAddress.ToInt64();}catch{process=null;return false;}h=OpenProcess(ACCESS,false,process.Id);if(h==IntPtr.Zero)return false;Install();return true;
    }

    static bool ReadExact(long a,byte[] b)=>h!=IntPtr.Zero&&ReadProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static uint R32(long a){var b=new byte[4];return ReadExact(a,b)?BitConverter.ToUInt32(b,0):0;}
    static bool WriteBytes(long a,byte[] b)=>h!=IntPtr.Zero&&WriteProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,b.Length,out var n)&&n.ToInt64()==b.Length;
    static bool W32(long a,uint v)=>WriteBytes(a,BitConverter.GetBytes(v));
    static bool WriteCode(long a,byte[] b){if(!VirtualProtectEx(h,new IntPtr(unchecked((int)(uint)a)),(UIntPtr)b.Length,PAGE_EXECUTE_READWRITE,out uint old))return false;bool ok=WriteBytes(a,b);FlushInstructionCache(h,new IntPtr(unchecked((int)(uint)a)),(UIntPtr)b.Length);VirtualProtectEx(h,new IntPtr(unchecked((int)(uint)a)),(UIntPtr)b.Length,old,out _);return ok;}
    static void U32(List<byte>b,uint v)=>b.AddRange(BitConverter.GetBytes(v));static void I32(List<byte>b,int v)=>b.AddRange(BitConverter.GetBytes(v));
    static void PatchRel(List<byte>b,int at,long fromNext,long to){var x=BitConverter.GetBytes(unchecked((int)(to-fromNext)));for(int i=0;i<4;i++)b[at+i]=x[i];}

    static byte[] BuildCompletionStub(long stub,long cfg)
    {
        long mapper=moduleBase+RVA_TRAIN_MAP,pending=cfg+T_PENDING,primary=cfg+T_PRIMARY,lb=cfg+T_BUILDING,li=cfg+T_INPUT,ln=cfg+T_NATIVE,n=cfg+T_COMPLETIONS;
        var b=new List<byte>();
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)pending);U32(b,0); // pending=0 every attempt
        b.Add(0x52);b.AddRange(new byte[]{0x8B,0xD1});b.AddRange(new byte[]{0xFF,0x74,0x24,0x08});b.Add(0xE8);int callMap=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)(cfg+CFG_MASTER));b.Add(0x00);b.AddRange(new byte[]{0x0F,0x84});int jNativeMaster=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0xF8,0xFF});b.AddRange(new byte[]{0x0F,0x84});int jNativeMap=b.Count;I32(b,0);
        b.Add(0x8B);b.Add(0x0D);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));b.AddRange(new byte[]{0x39,0x8A,0x84,0x00,0x00,0x00});b.AddRange(new byte[]{0x0F,0x85});int jNativeOwner=b.Count;I32(b,0);

        var slotJumps=new List<(int at,int slot)>();
        for(int i=0;i<9;i++){b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)SlotEn(cfg,i));b.Add(0x00);b.AddRange(new byte[]{0x0F,0x85});int j=b.Count;I32(b,0);slotJumps.Add((j,i));}
        b.Add(0xE9);int jNoActive=b.Count;I32(b,0);

        var slotLabels=new int[9];
        for(int i=0;i<9;i++)
        {
            slotLabels[i]=b.Count;
            b.AddRange(new byte[]{0x89,0x15});U32(b,(uint)lb);b.AddRange(new byte[]{0x8B,0x4C,0x24,0x08});b.AddRange(new byte[]{0x89,0x0D});U32(b,(uint)li);b.Add(0xA3);U32(b,(uint)ln);
            b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)n);b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)pending);U32(b,1);b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)primary);U32(b,(uint)i);
            b.Add(0xA1);U32(b,(uint)SlotOut(cfg,i));b.Add(0xE9);int jf=b.Count;I32(b,0);slotJumps.Add((jf,100+i));
        }
        int finish=b.Count;b.AddRange(new byte[]{0x8B,0x4C,0x24,0x08});b.Add(0x5A);b.AddRange(new byte[]{0xC2,0x04,0x00});
        PatchRel(b,callMap,stub+callMap+4,mapper);PatchRel(b,jNativeMaster,stub+jNativeMaster+4,stub+finish);PatchRel(b,jNativeMap,stub+jNativeMap+4,stub+finish);PatchRel(b,jNativeOwner,stub+jNativeOwner+4,stub+finish);PatchRel(b,jNoActive,stub+jNoActive+4,stub+finish);
        foreach(var x in slotJumps){long to=x.slot>=100?stub+finish:stub+slotLabels[x.slot];PatchRel(b,x.at,stub+x.at+4,to);}return b.ToArray();
    }

    static byte[] BuildExtrasStub(long stub,long cfg)
    {
        long original=moduleBase+RVA_REGISTER_UNIT,create=moduleBase+RVA_CREATE_UNIT,attach=moduleBase+RVA_ATTACH_UNIT,notify=moduleBase+RVA_LOCAL_UNIT_NOTIFY;
        long pending=cfg+T_PENDING,expectedBuilding=cfg+T_BUILDING,primary=cfg+T_PRIMARY,attempts=cfg+T_EXTRA_ATTEMPTS,success=cfg+T_EXTRA_SUCCESS,fail=cfg+T_EXTRA_FAIL,lastUnit=cfg+T_LAST_UNIT;
        var b=new List<byte>();
        b.AddRange(new byte[]{0xFF,0x74,0x24,0x08});b.AddRange(new byte[]{0xFF,0x74,0x24,0x08});b.Add(0xE8);int callOriginal=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)pending);b.Add(0x01);b.AddRange(new byte[]{0x0F,0x85});int jFinish0=b.Count;I32(b,0);
        b.AddRange(new byte[]{0x3B,0x1D});U32(b,(uint)expectedBuilding);b.AddRange(new byte[]{0x0F,0x85});int jFinish1=b.Count;I32(b,0);
        b.AddRange(new byte[]{0xC7,0x05});U32(b,(uint)pending);U32(b,0); // consume once
        b.Add(0x60);b.AddRange(new byte[]{0x83,0xEC,0x40});
        b.AddRange(new byte[]{0x0F,0x11,0x04,0x24,0x0F,0x11,0x4C,0x24,0x10,0x0F,0x11,0x54,0x24,0x20,0x0F,0x11,0x5C,0x24,0x30});

        var toBlockEnd=new List<(int at,int endId)>(); var blockEnds=new List<int>();
        for(int i=0;i<9;i++)
        {
            int id=i;
            b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)SlotEn(cfg,i));b.Add(0x00);b.AddRange(new byte[]{0x0F,0x84});int jDisabled=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x83,0x3D});U32(b,(uint)primary);b.Add((byte)i);b.AddRange(new byte[]{0x0F,0x84});int jPrimary=b.Count;I32(b,0);
            b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)attempts);
            b.Add(0x6A);b.Add(0x01);b.AddRange(new byte[]{0xFF,0xB3,0x84,0x00,0x00,0x00});b.Add(0xFF);b.Add(0x35);U32(b,(uint)SlotOut(cfg,i));b.AddRange(new byte[]{0x8B,0xCB});b.Add(0xE8);int callCreate=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x85,0xC0});b.AddRange(new byte[]{0x0F,0x85});int jCreated=b.Count;I32(b,0);
            b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)fail);b.Add(0xE9);int jFailEnd=b.Count;I32(b,0);
            int created=b.Count;b.AddRange(new byte[]{0x89,0xC6});b.Add(0xA3);U32(b,(uint)lastUnit);b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)success);b.AddRange(new byte[]{0xFF,0x05});U32(b,(uint)SlotSuccess(cfg,i));
            b.Add(0x56);b.AddRange(new byte[]{0x8B,0xCB});b.Add(0xE8);int callAttach=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x8B,0x83,0xA8,0x04,0x00,0x00,0x89,0x86,0x6C,0x03,0x00,0x00,0x8B,0x83,0xAC,0x04,0x00,0x00,0x89,0x86,0x70,0x03,0x00,0x00});
            b.AddRange(new byte[]{0x69,0x8B,0x84,0x00,0x00,0x00,0xA0,0x00,0x00,0x00});b.Add(0xA1);U32(b,(uint)(moduleBase+RVA_PLAYER_BASE));b.AddRange(new byte[]{0xFF,0x44,0x01,0x28});
            b.AddRange(new byte[]{0x8B,0x83,0x84,0x00,0x00,0x00});b.Add(0x3B);b.Add(0x05);U32(b,(uint)(moduleBase+RVA_LOCAL_ID));b.AddRange(new byte[]{0x0F,0x85});int jSkipNotify=b.Count;I32(b,0);
            b.AddRange(new byte[]{0x8B,0x46,0x74,0xFF,0x30});b.Add(0xE8);int callNotify=b.Count;I32(b,0);int afterNotify=b.Count;
            b.AddRange(new byte[]{0x8B,0x83,0xA0,0x04,0x00,0x00,0x89,0x86,0x98,0x07,0x00,0x00,0x69,0x83,0x84,0x00,0x00,0x00,0xA0,0x00,0x00,0x00});b.Add(0x56);b.Add(0x50);b.Add(0xE8);int callReg=b.Count;I32(b,0);
            int blockEnd=b.Count;blockEnds.Add(blockEnd);
            PatchRel(b,jCreated,stub+jCreated+4,stub+created);PatchRel(b,jSkipNotify,stub+jSkipNotify+4,stub+afterNotify);PatchRel(b,callCreate,stub+callCreate+4,create);PatchRel(b,callAttach,stub+callAttach+4,attach);PatchRel(b,callNotify,stub+callNotify+4,notify);PatchRel(b,callReg,stub+callReg+4,original);
            toBlockEnd.Add((jDisabled,id));toBlockEnd.Add((jPrimary,id));toBlockEnd.Add((jFailEnd,id));
        }
        int restore=b.Count;b.AddRange(new byte[]{0x0F,0x10,0x04,0x24,0x0F,0x10,0x4C,0x24,0x10,0x0F,0x10,0x54,0x24,0x20,0x0F,0x10,0x5C,0x24,0x30,0x83,0xC4,0x40});b.Add(0x61);
        int finish=b.Count;b.AddRange(new byte[]{0xC2,0x08,0x00});
        PatchRel(b,callOriginal,stub+callOriginal+4,original);PatchRel(b,jFinish0,stub+jFinish0+4,stub+finish);PatchRel(b,jFinish1,stub+jFinish1+4,stub+finish);
        foreach(var x in toBlockEnd)PatchRel(b,x.at,stub+x.at+4,stub+blockEnds[x.endId]);return b.ToArray();
    }

    static byte[] CallPatch(long target,long stub){var p=new byte[5];p[0]=0xE8;Array.Copy(BitConverter.GetBytes(unchecked((int)(stub-(target+5)))),0,p,1,4);return p;}
    static void Install()
    {
        if(installed||h==IntPtr.Zero)return;var m=new byte[MapperStock.Length];if(!ReadExact(moduleBase+RVA_TRAIN_MAP,m)||!m.SequenceEqual(MapperStock)){status="HOOK BLOCKED — native mapper not stock. Close V2/V1 and restart BRZE.";return;}
        var a=new byte[5];if(!ReadExact(moduleBase+RVA_COMPLETION_CALL,a)||!a.SequenceEqual(CompletionCallOriginal)){status="HOOK BLOCKED — completion call not stock";return;}var z=new byte[5];if(!ReadExact(moduleBase+RVA_POST_REGISTER_CALL,z)||!z.SequenceEqual(PostRegisterCallOriginal)){status="HOOK BLOCKED — post-register call not stock";return;}
        cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)8192,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);if(cave==IntPtr.Zero){status="HOOK BLOCKED — cave alloc failed";return;}long c=cave.ToInt64(),s1=c+STUB1_OFFSET,s2=c+STUB2_OFFSET;WriteBytes(c,new byte[512]);PushConfig();W32(c+T_INPUT,0xFFFFFFFF);W32(c+T_NATIVE,0xFFFFFFFF);
        var code1=BuildCompletionStub(s1,c);var code2=BuildExtrasStub(s2,c);if(code2.Length>5500){status="HOOK BLOCKED — generated extra stub too large";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;return;}patch1=CallPatch(moduleBase+RVA_COMPLETION_CALL,s1);patch2=CallPatch(moduleBase+RVA_POST_REGISTER_CALL,s2);
        if(!WriteBytes(s1,code1)||!WriteBytes(s2,code2)||!WriteCode(moduleBase+RVA_COMPLETION_CALL,patch1)||!WriteCode(moduleBase+RVA_POST_REGISTER_CALL,patch2)){try{var now=new byte[5];if(ReadExact(moduleBase+RVA_COMPLETION_CALL,now)&&patch1!=null&&now.SequenceEqual(patch1))WriteCode(moduleBase+RVA_COMPLETION_CALL,CompletionCallOriginal);}catch{}status="HOOK BLOCKED — atomic install failed";VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);cave=IntPtr.Zero;patch1=null;patch2=null;return;}
        installed=true;status="V5 FULL 1→9 HOOK ACTIVE — V3/V4 proven paths generalized";
    }

    static uint FirstSelectedUnit(){long l=moduleBase+RVA_SELECTION_LIST;uint count=R32(l+0x18),node=R32(l);if(count==0||node==0)return 0;return R32((long)node+8);}
    static uint SelectedBuilding(){uint a=R32(moduleBase+RVA_SELECTED_BUILDING_A);return a!=0?a:R32(moduleBase+RVA_SELECTED_BUILDING_B);}
    static uint UnitType(uint u){if(u==0)return 0xFFFFFFFF;uint d=R32((long)u+OFF_UNIT_DEF);return d==0?0xFFFFFFFF:R32(d);}
    static string Context(uint local){uint u=FirstSelectedUnit(),b=SelectedBuilding(),ut=UnitType(u),uo=u==0?0xFFFFFFFF:R32((long)u+OFF_UNIT_OWNER);if(b==0)return $"Selected unit: {(u==0?"none":$"0x{u:X8} type:0x{ut:X} owner:{uo}")}\r\nSelected building: none";uint bo=R32((long)b+OFF_BUILD_OWNER),bt=R32((long)b+OFF_BUILD_TYPE),tt=R32((long)b+OFF_TRAIN_TYPE),pr=R32((long)b+OFF_TRAIN_PROGRESS);return $"Selected unit: {(u==0?"none":$"0x{u:X8} type:0x{ut:X} owner:{uo}{(uo==local?" LOCAL":"")}")}\r\nSelected building: 0x{b:X8} type:0x{bt:X} owner:{bo}{(bo==local?" LOCAL":"")} | trainType:0x{tt:X8} progress:0x{pr:X8}";}
    static string Recipes(uint b){if(b==0)return "Select a training building.";uint d=R32((long)b+OFF_BUILD_DEF);if(d==0)return "BuildingDef unresolved.";var sb=new StringBuilder();for(int i=0;i<6;i++){sb.Append($"#{i+1} 0x{R32((long)d+UnitInOffsets[i]):X8}→0x{R32((long)d+UnitOutOffsets[i]):X8}");if(i!=5)sb.Append("    ");if(i==2)sb.AppendLine();}return sb.ToString();}
    static string Monitor()
    {
        if(!installed||cave==IntPtr.Zero)return status+"\r\nNo multi-output override active.";long c=cave.ToInt64();uint n=R32(c+T_COMPLETIONS),a=R32(c+T_EXTRA_ATTEMPTS),s=R32(c+T_EXTRA_SUCCESS),f=R32(c+T_EXTRA_FAIL),b=R32(c+T_BUILDING),i=R32(c+T_INPUT),nat=R32(c+T_NATIVE),p=R32(c+T_PENDING),primary=R32(c+T_PRIMARY),last=R32(c+T_LAST_UNIT);var ss=new StringBuilder();for(int x=0;x<9;x++){if(x>0)ss.Append(' ');ss.Append($"S{x+1}:{R32(SlotSuccess(c,x))}");}
        return $"{status}\r\nactive slots:{Enumerable.Range(0,9).Count(x=>R32(SlotEn(c,x))!=0)} | primary last:S{primary+1} | valid completions:{n}\r\nextra attempts:{a} success:{s} fail:{f} | pending:{p} | last extra unit:0x{last:X8}\r\nslot successes: {ss}\r\nlast completion: building:0x{b:X8} input:0x{i:X8} nativeOut:0x{nat:X8}\r\nGuard: eligibility/mapper stock; failed extra create is skipped and later slots continue.";
    }
    public static CoreSnapshot Snapshot(){lock(sync){if(!EnsureAttached())return new("GAME: waiting for Battle_Realms_F.exe","Selected unit: none\r\nSelected building: none",status,"No building selected.");if(installed&&cave!=IntPtr.Zero)PushConfig();uint local=R32(moduleBase+RVA_LOCAL_ID),b=SelectedBuilding();return new($"GAME: attached • PID {process!.Id} • base 0x{moduleBase:X8} • local player {local}",Context(local),Monitor(),Recipes(b));}}
}
