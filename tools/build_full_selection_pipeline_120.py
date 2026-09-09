from pathlib import Path

p=Path('Program.cs')
s=p.read_text(encoding='utf-8')

# Replace the existing minimal manual-120 probe with a fully coherent selection-pipeline probe.
start=s.index('internal sealed class MainForm')
head=s[:start]
body=r'''internal sealed class MainForm : Form
{
    readonly CheckBox f4 = new() { Text = "F4 Max Population + FULL selection pipeline 120 — DIAGNOSTIC", AutoSize = true };
    readonly Label note = new()
    {
        AutoSize = true,
        MaximumSize = new Size(760, 0),
        Text = "Minimal probe only. No HP/stamina/F7/selection-event hooks. Raises active + drag candidate guards to 120, raises candidate/active/temp/rebuild first pools to 120, and rescues already-built candidate/active growth once. Telemetry shows all four selection containers."
    };
    readonly Label status = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 86, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    bool f4Held;

    public MainForm()
    {
        Text = "BRZE 1.60 — Full Selection Pipeline 120";
        ClientSize = new Size(810, 245);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
        panel.Controls.Add(f4); panel.Controls.Add(note);
        Controls.Add(panel); Controls.Add(status);
        timer.Tick += (_, _) => TickTrainer(); timer.Start(); FormClosed += (_, _) => Native.Stop();
    }

    void TickTrainer()
    {
        bool down=(Native.GetAsyncKeyState(0x73)&0x8000)!=0;
        if(down&&!f4Held)f4.Checked=!f4.Checked;
        f4Held=down;
        status.Text=Native.Tick(f4.Checked);
    }
}

internal static class Native
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool VirtualProtectEx(IntPtr h,IntPtr addr,UIntPtr size,uint newProtect,out uint oldProtect);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool FlushInstructionCache(IntPtr h,IntPtr addr,UIntPtr size);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);

    const uint Access=0x10|0x20|0x8|0x400, PAGE_EXECUTE_READWRITE=0x40;
    const int RVA_LOCAL_ID=0x4416D0, RVA_MAX_UNITS=0x467B90;
    const int RVA_CAND=0x4416E0, RVA_ACTIVE=0x441708, RVA_TEMPA=0x441784, RVA_TEMPB=0x4417AC;
    const int OFF_COUNT=0x18, OFF_FIRST=0x20, OFF_GROWTH=0x24;

    // exact immediate-byte addresses in BRZE 1.60
    const int RVA_CAND_CAP_IMM=0x1A6F5B;       // 5A6F55 cmp [8416F8], 5A
    const int RVA_ACTIVE_CAP_IMM=0x1A7006;     // 5A7000 cmp [841720], 5A
    const int RVA_CAND_INIT_IMM=0x1A6BC4;      // 5A6BC3 push 5A
    const int RVA_ACTIVE_INIT_IMM=0x1A6BCC;    // 5A6BCB push 5A
    const int RVA_TEMPA_INIT_IMM=0x1A71B1;     // 5A71B0 push 5A
    const int RVA_TEMPB_INIT_IMM=0x1A71B9;     // 5A71B8 push 5A
    const int RVA_ACTIVE_REBUILD_IMM=0x1A7299; // 5A7298 push 5A

    static IntPtr h=IntPtr.Zero; static Process? p; static long moduleBase; static int pid; static bool codePatched; static string err="";

    static bool Attach(){try{if(p!=null&&!p.HasExited&&h!=IntPtr.Zero)return true;}catch{} Detach();var ps=Process.GetProcessesByName("Battle_Realms_F");if(ps.Length==0)return false;p=ps[0];pid=p.Id;try{moduleBase=p.MainModule!.BaseAddress.ToInt64();}catch{p=null;return false;}h=OpenProcess(Access,false,pid);return h!=IntPtr.Zero;}
    static uint R32(long a){if(h==IntPtr.Zero)return 0;var b=new byte[4];return ReadProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,4,out var n)&&n.ToInt64()==4?BitConverter.ToUInt32(b,0):0;}
    static byte R8(long a){if(h==IntPtr.Zero)return 0;var b=new byte[1];return ReadProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,1,out var n)&&n.ToInt64()==1?b[0]:(byte)0;}
    static bool W32(long a,uint v){if(h==IntPtr.Zero)return false;var b=BitConverter.GetBytes(v);return WriteProcessMemory(h,new IntPtr(unchecked((int)(uint)a)),b,4,out var n)&&n.ToInt64()==4;}
    static bool Code8(int rva,byte v){long a=moduleBase+rva;var pA=new IntPtr(unchecked((int)(uint)a));if(!VirtualProtectEx(h,pA,(UIntPtr)1,PAGE_EXECUTE_READWRITE,out uint old))return false;bool ok=WriteProcessMemory(h,pA,new[]{v},1,out var n)&&n.ToInt64()==1;if(ok)FlushInstructionCache(h,pA,(UIntPtr)1);VirtualProtectEx(h,pA,(UIntPtr)1,old,out _);return ok;}

    static void PatchCodeOnce()
    {
        if(codePatched)return;
        (int rva,string name)[] sites={
            (RVA_CAND_CAP_IMM,"cand cap"),(RVA_ACTIVE_CAP_IMM,"active cap"),
            (RVA_CAND_INIT_IMM,"cand init"),(RVA_ACTIVE_INIT_IMM,"active init"),
            (RVA_TEMPA_INIT_IMM,"tempA init"),(RVA_TEMPB_INIT_IMM,"tempB init"),(RVA_ACTIVE_REBUILD_IMM,"active rebuild")};
        foreach(var x in sites){byte cur=R8(moduleBase+x.rva);if(cur==0x5A){if(!Code8(x.rva,0x78)){err=x.name+" write failed";return;}}else if(cur!=0x78){err=$"{x.name} unexpected 0x{cur:X2}";return;}}
        codePatched=true;
    }

    public static string Tick(bool enabled)
    {
        if(!Attach())return "Waiting for Battle_Realms_F.exe...";
        if(enabled)
        {
            PatchCodeOnce();
            uint lid=R32(moduleBase+RVA_LOCAL_ID); W32(moduleBase+RVA_MAX_UNITS+lid*4L,99_999_999u);
            // Existing objects were born 90/0 before trainer attach. Give only candidate + active a rescue growth block.
            long cand=moduleBase+RVA_CAND, active=moduleBase+RVA_ACTIVE;
            if(R32(cand+OFF_GROWTH)==0)W32(cand+OFF_GROWTH,120u);
            if(R32(active+OFF_GROWTH)==0)W32(active+OFF_GROWTH,120u);
        }
        long c=moduleBase+RVA_CAND,a=moduleBase+RVA_ACTIVE,ta=moduleBase+RVA_TEMPA,tb=moduleBase+RVA_TEMPB;
        string e=err.Length==0?"":$" | ERROR:{err}";
        return $"pid:{pid} F4:{enabled} | ACTIVE count:{R32(a+OFF_COUNT)} first:{R32(a+OFF_FIRST)} grow:{R32(a+OFF_GROWTH)} cap:{R8(moduleBase+RVA_ACTIVE_CAP_IMM)}\n"+
               $"CAND count:{R32(c+OFF_COUNT)} first:{R32(c+OFF_FIRST)} grow:{R32(c+OFF_GROWTH)} cap:{R8(moduleBase+RVA_CAND_CAP_IMM)} | tempA:{R32(ta+OFF_COUNT)} tempB:{R32(tb+OFF_COUNT)}{e}";
    }

    public static void Stop()=>Detach();
    static void Detach(){if(h!=IntPtr.Zero)CloseHandle(h);h=IntPtr.Zero;p=null;moduleBase=0;pid=0;codePatched=false;err="";}
}
'''
p.write_text(head+body,encoding='utf-8')
print('replaced Program.cs with full selection pipeline 120 diagnostic')
