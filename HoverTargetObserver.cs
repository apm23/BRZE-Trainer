using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BRZEHoverObserver;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ObserverForm());
    }
}

internal sealed class ObserverForm : Form
{
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll", SetLastError=true)] static extern bool RegisterHotKey(IntPtr hWnd,int id,uint fsModifiers,uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd,int id);

    const uint PROCESS_VM_READ=0x10, PROCESS_QUERY_INFORMATION=0x400;
    const int WM_HOTKEY=0x0312;
    const int RVA_LOCAL_ID=0x4416D0;
    const int RVA_UNIT_POOL_PTR=0x4796A0;
    const int UNIT_STRIDE=0x818, UNIT_COUNT=2000;
    const int OFF_DEF=0x74, OFF_OWNER=0x240, OFF_HP=0x404, OFF_ST=0x408;
    const int DATA_RVA=0x3D5000, DATA_SIZE=0x1F128C;

    readonly Label headline = new(){Dock=DockStyle.Top,Height=70,TextAlign=ContentAlignment.MiddleCenter,Font=new Font(FontFamily.GenericSansSerif,18,FontStyle.Bold)};
    readonly Label instruction = new(){Dock=DockStyle.Top,Height=72,TextAlign=ContentAlignment.MiddleCenter,Font=new Font(FontFamily.GenericSansSerif,11,FontStyle.Bold)};
    readonly Label step1 = MakeStep("1. EMPTY GROUND"), step2 = MakeStep("2. ENEMY A"), step3 = MakeStep("3. ENEMY B"), step4 = MakeStep("4. TXT RESULT");
    readonly Label footer = new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font(FontFamily.GenericSansSerif,9)};

    Snapshot? empty, enemyA, enemyB;
    string? lastSavedPath;

    sealed class Snapshot
    {
        public int Pid; public long Base; public uint Pool; public uint LocalId; public byte[] Data=Array.Empty<byte>(); public DateTime Time;
    }

    static Label MakeStep(string title) => new()
    {
        Dock=DockStyle.Top,
        Height=58,
        Text=title+"  —  WAITING",
        TextAlign=ContentAlignment.MiddleLeft,
        Padding=new Padding(18,0,8,0),
        Font=new Font(FontFamily.GenericSansSerif,11,FontStyle.Bold),
        BackColor=Color.FromArgb(45,45,48),
        ForeColor=Color.White
    };

    public ObserverForm()
    {
        Text="BRZE Hover Observer v2 — ONE KEY / READ ONLY";
        Width=680; Height=445; StartPosition=FormStartPosition.Manual; Left=20; Top=80; TopMost=true;
        BackColor=Color.FromArgb(30,30,30); ForeColor=Color.White;

        headline.Text="NUMPAD 5 = RECORD NEXT";
        headline.ForeColor=Color.Gold;
        instruction.Text="Keep BRZE focused. Move cursor to what the yellow instruction asks,\nthen press NUMPAD 5 once. You will hear a sound + see RECORDED ✓.";
        instruction.ForeColor=Color.White;
        footer.Text="NUMPAD 0 = RESET ALL   |   Backup direct keys: NUMPAD 1=Empty, 2=Enemy A, 3=Enemy B\nREAD ONLY: this observer never writes to Battle Realms.";
        footer.ForeColor=Color.Silver;

        Controls.Add(footer);
        Controls.Add(step4); Controls.Add(step3); Controls.Add(step2); Controls.Add(step1);
        Controls.Add(instruction); Controls.Add(headline);

        Shown += (_,_) => { RegisterKeys(); RefreshInstruction(); };
        FormClosed += (_,_) => UnregisterKeys();
    }

    void RegisterKeys()
    {
        bool ok=true;
        ok &= RegisterHotKey(Handle,5,0,(uint)Keys.NumPad5); // staged capture/analyze
        ok &= RegisterHotKey(Handle,0,0,(uint)Keys.NumPad0); // reset
        ok &= RegisterHotKey(Handle,1,0,(uint)Keys.NumPad1); // direct empty
        ok &= RegisterHotKey(Handle,2,0,(uint)Keys.NumPad2); // direct A
        ok &= RegisterHotKey(Handle,3,0,(uint)Keys.NumPad3); // direct B
        if(!ok) ShowError("One or more NUMPAD hotkeys could not be registered. Turn NumLock ON and close apps using those keys.");
    }

    void UnregisterKeys(){foreach(int id in new[]{0,1,2,3,5})UnregisterHotKey(Handle,id);}

    protected override void WndProc(ref Message m)
    {
        if(m.Msg==WM_HOTKEY)
        {
            int id=m.WParam.ToInt32();
            try
            {
                if(id==0) ResetAll();
                else if(id==1) CaptureEmpty();
                else if(id==2) CaptureEnemyA();
                else if(id==3) CaptureEnemyB();
                else if(id==5) RecordNext();
            }
            catch(Exception ex){ShowError(ex.Message);}
        }
        base.WndProc(ref m);
    }

    void RecordNext()
    {
        if(empty==null){CaptureEmpty();return;}
        if(enemyA==null){CaptureEnemyA();return;}
        if(enemyB==null){CaptureEnemyB();return;}
        AnalyzeAndSave();
    }

    void CaptureEmpty()
    {
        empty=Capture();
        MarkRecorded(step1,"EMPTY",empty);
        SystemSounds.Asterisk.Play();
        RefreshInstruction();
    }

    void CaptureEnemyA()
    {
        enemyA=Capture();
        MarkRecorded(step2,"ENEMY A",enemyA);
        SystemSounds.Asterisk.Play();
        RefreshInstruction();
    }

    void CaptureEnemyB()
    {
        enemyB=Capture();
        MarkRecorded(step3,"ENEMY B",enemyB);
        SystemSounds.Asterisk.Play();
        RefreshInstruction();
    }

    void MarkRecorded(Label l,string name,Snapshot s)
    {
        l.Text=$"{name}  —  RECORDED ✓   {s.Time:HH:mm:ss}   PID {s.Pid}";
        l.BackColor=Color.FromArgb(20,105,55);
        l.ForeColor=Color.White;
    }

    void ResetAll()
    {
        empty=null;enemyA=null;enemyB=null;lastSavedPath=null;
        ResetStep(step1,"1. EMPTY GROUND"); ResetStep(step2,"2. ENEMY A"); ResetStep(step3,"3. ENEMY B"); ResetStep(step4,"4. TXT RESULT");
        SystemSounds.Question.Play();
        RefreshInstruction();
    }

    static void ResetStep(Label l,string title)
    {
        l.Text=title+"  —  WAITING";
        l.BackColor=Color.FromArgb(45,45,48);
        l.ForeColor=Color.White;
    }

    void RefreshInstruction()
    {
        if(empty==null){headline.Text="HOVER EMPTY GROUND → NUMPAD 5";headline.ForeColor=Color.Gold;return;}
        if(enemyA==null){headline.Text="HOVER STATIONARY ENEMY A → NUMPAD 5";headline.ForeColor=Color.Gold;return;}
        if(enemyB==null){headline.Text="HOVER DIFFERENT ENEMY B → NUMPAD 5";headline.ForeColor=Color.Gold;return;}
        headline.Text="ALL 3 RECORDED ✓  →  NUMPAD 5 TO SAVE TXT";
        headline.ForeColor=Color.LightGreen;
    }

    void ShowError(string message)
    {
        headline.Text="ERROR — NOT RECORDED";
        headline.ForeColor=Color.OrangeRed;
        footer.Text=message+"\nNothing was marked RECORDED for the failed step.";
        footer.ForeColor=Color.OrangeRed;
        SystemSounds.Hand.Play();
    }

    static Snapshot Capture()
    {
        var ps=Process.GetProcessesByName("Battle_Realms_F");
        if(ps.Length==0) throw new InvalidOperationException("Battle_Realms_F.exe not found");
        var p=ps[0]; long b=p.MainModule!.BaseAddress.ToInt64();
        IntPtr h=OpenProcess(PROCESS_VM_READ|PROCESS_QUERY_INFORMATION,false,p.Id);
        if(h==IntPtr.Zero) throw new InvalidOperationException("OpenProcess failed");
        try
        {
            uint lid=ReadU32(h,b+RVA_LOCAL_ID);
            uint pool=ReadU32(h,b+RVA_UNIT_POOL_PTR);
            if(pool==0) throw new InvalidOperationException("unit pool pointer is zero");
            var data=new byte[DATA_SIZE];
            if(!ReadProcessMemory(h,new IntPtr(b+DATA_RVA),data,data.Length,out var n) || n.ToInt64()!=data.Length)
                throw new InvalidOperationException($".data snapshot short read {n.ToInt64()}/{data.Length}");
            return new Snapshot{Pid=p.Id,Base=b,Pool=pool,LocalId=lid,Data=data,Time=DateTime.Now};
        }
        finally{CloseHandle(h);}
    }

    static uint ReadU32(IntPtr h,long a)
    {
        var x=new byte[4];
        if(!ReadProcessMemory(h,new IntPtr(a),x,4,out var n)||n.ToInt64()!=4)return 0;
        return BitConverter.ToUInt32(x,0);
    }

    static bool IsUnitPtr(Snapshot s,uint v)
    {
        ulong lo=s.Pool, hi=lo+(ulong)UNIT_STRIDE*UNIT_COUNT;
        if(v<lo || v>=hi) return false;
        return ((ulong)v-lo)%UNIT_STRIDE==0;
    }

    static uint Dword(byte[] d,int o)=>o>=0&&o+4<=d.Length?BitConverter.ToUInt32(d,o):0;
    sealed record Candidate(int Rva,string Kind,uint Empty,uint A,uint B,int Score);

    IEnumerable<Candidate> FindCandidates()
    {
        var A=empty!; var B=enemyA!; var C=enemyB!;
        int n=Math.Min(A.Data.Length,Math.Min(B.Data.Length,C.Data.Length));
        for(int o=0;o+4<=n;o+=4)
        {
            uint a=Dword(A.Data,o), b=Dword(B.Data,o), c=Dword(C.Data,o);
            bool vb=IsUnitPtr(B,b), vc=IsUnitPtr(C,c);
            if(vb&&vc&&b!=c)
            {
                int score=100;
                if(!IsUnitPtr(A,a))score+=40;
                if(a==0)score+=20;
                int r=DATA_RVA+o;
                if(r>=0x440000&&r<=0x443000)score+=15;
                yield return new Candidate(r,"DIRECT",a,b,c,score);
            }

            uint pa=a,pb=b,pc=c;
            long dlo=(long)A.Base+DATA_RVA, dhi=dlo+n;
            bool ia=pa>=dlo&&pa+4<=dhi, ib=pb>=dlo&&pb+4<=dhi, ic=pc>=dlo&&pc+4<=dhi;
            if(ib&&ic)
            {
                uint ua=ia?Dword(A.Data,(int)(pa-dlo)):0;
                uint ub=Dword(B.Data,(int)(pb-dlo));
                uint uc=Dword(C.Data,(int)(pc-dlo));
                if(IsUnitPtr(B,ub)&&IsUnitPtr(C,uc)&&ub!=uc)
                {
                    int score=70;
                    if(!ia || !IsUnitPtr(A,ua))score+=30;
                    int r=DATA_RVA+o;
                    if(r>=0x440000&&r<=0x443000)score+=15;
                    yield return new Candidate(r,"INDIRECT",ua,ub,uc,score);
                }
            }
        }
    }

    static string UnitInfo(Snapshot s,uint ptr)
    {
        if(!IsUnitPtr(s,ptr))return "not-unit";
        var ps=Process.GetProcessesByName("Battle_Realms_F"); if(ps.Length==0)return "process-gone";
        IntPtr h=OpenProcess(PROCESS_VM_READ|PROCESS_QUERY_INFORMATION,false,ps[0].Id); if(h==IntPtr.Zero)return "read-failed";
        try
        {
            uint def=ReadU32(h,(long)ptr+OFF_DEF), owner=ReadU32(h,(long)ptr+OFF_OWNER), hp=ReadU32(h,(long)ptr+OFF_HP), st=ReadU32(h,(long)ptr+OFF_ST);
            return $"def=0x{def:X8} owner={owner} {(owner==s.LocalId?"LOCAL":"NONLOCAL")} hp=0x{hp:X8} st=0x{st:X8}";
        }
        finally{CloseHandle(h);}
    }

    void AnalyzeAndSave()
    {
        if(empty==null||enemyA==null||enemyB==null)throw new InvalidOperationException("Need all 3 captures first");
        if(empty.Pid!=enemyA.Pid||empty.Pid!=enemyB.Pid)throw new InvalidOperationException("BRZE process changed between captures; press NUMPAD 0 and recapture");
        var all=FindCandidates().OrderByDescending(x=>x.Score).ThenBy(x=>x.Rva).Take(200).ToList();
        var sb=new StringBuilder();
        sb.AppendLine("BRZE HOVER TARGET POINTER FORENSIC — READ ONLY");
        sb.AppendLine($"pid={empty.Pid} base=0x{empty.Base:X8} pool=0x{empty.Pool:X8} localId={empty.LocalId}");
        sb.AppendLine($"empty={empty.Time:O} enemyA={enemyA.Time:O} enemyB={enemyB.Time:O}");
        sb.AppendLine("Required pattern: same global points to ENEMY A then a DIFFERENT ENEMY B while empty-ground baseline does not.");
        sb.AppendLine($"candidate_count={all.Count}"); sb.AppendLine();
        int i=0;
        foreach(var c in all)
        {
            sb.AppendLine($"[{++i}] SCORE={c.Score} {c.Kind} GLOBAL_RVA=0x{c.Rva:X} EMPTY=0x{c.Empty:X8} A=0x{c.A:X8} B=0x{c.B:X8}");
            sb.AppendLine("    A: "+UnitInfo(enemyA,c.A));
            sb.AppendLine("    B: "+UnitInfo(enemyB,c.B));
        }
        if(all.Count==0)sb.AppendLine("NO DIRECT/ONE-LEVEL .data hover candidate found. Next probe must trace the native cursor hit-test path.");
        string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),$"BRZE-HOVER-FORENSIC-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(path,sb.ToString(),Encoding.UTF8);
        lastSavedPath=path;
        step4.Text=$"TXT RESULT  —  SAVED ✓   Candidates: {all.Count}";
        step4.BackColor=Color.FromArgb(20,105,55); step4.ForeColor=Color.White;
        headline.Text="DONE ✓  TXT SAVED TO DESKTOP"; headline.ForeColor=Color.LightGreen;
        footer.Text=path+"\nSend this TXT to ChatGPT. NUMPAD 0 resets if you want to repeat.";
        footer.ForeColor=Color.White;
        SystemSounds.Exclamation.Play();
    }
}
