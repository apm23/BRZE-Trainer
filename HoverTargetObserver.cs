using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    const uint MOD_ALT=0x1, MOD_CONTROL=0x2;
    const int WM_HOTKEY=0x0312;
    const int RVA_LOCAL_ID=0x4416D0;
    const int RVA_UNIT_POOL_PTR=0x4796A0;
    const int UNIT_STRIDE=0x818, UNIT_COUNT=2000;
    const int OFF_DEF=0x74, OFF_OWNER=0x240, OFF_HP=0x404, OFF_ST=0x408;
    const int DATA_RVA=0x3D5000, DATA_SIZE=0x1F128C;

    readonly Label status = new(){Dock=DockStyle.Fill,AutoSize=false,TextAlign=System.Drawing.ContentAlignment.MiddleLeft,Font=new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace,10)};
    Snapshot? empty, enemyA, enemyB;

    sealed class Snapshot
    {
        public int Pid; public long Base; public uint Pool; public uint LocalId; public byte[] Data=Array.Empty<byte>(); public DateTime Time;
    }

    public ObserverForm()
    {
        Text="BRZE Hover Target Observer — READ ONLY";
        Width=820; Height=360; StartPosition=FormStartPosition.CenterScreen; TopMost=true;
        status.Text="READ-ONLY — no game writes.\r\n\r\nKeep BRZE focused and DO NOT click this window for captures.\r\n\r\nCTRL+ALT+1 = cursor over EMPTY GROUND\r\nCTRL+ALT+2 = hover stationary ENEMY A\r\nCTRL+ALT+3 = hover a DIFFERENT stationary ENEMY B\r\nCTRL+ALT+0 = ANALYZE + SAVE TXT to Desktop\r\n\r\nUse two different enemies so the real hover global changes A -> B.";
        Controls.Add(status);
        Shown += (_,_) => RegisterKeys();
        FormClosed += (_,_) => UnregisterKeys();
    }

    void RegisterKeys()
    {
        RegisterHotKey(Handle,1,MOD_CONTROL|MOD_ALT,(uint)Keys.D1);
        RegisterHotKey(Handle,2,MOD_CONTROL|MOD_ALT,(uint)Keys.D2);
        RegisterHotKey(Handle,3,MOD_CONTROL|MOD_ALT,(uint)Keys.D3);
        RegisterHotKey(Handle,4,MOD_CONTROL|MOD_ALT,(uint)Keys.D0);
    }
    void UnregisterKeys(){for(int i=1;i<=4;i++)UnregisterHotKey(Handle,i);}

    protected override void WndProc(ref Message m)
    {
        if(m.Msg==WM_HOTKEY)
        {
            int id=m.WParam.ToInt32();
            try
            {
                if(id==1){empty=Capture(); status.Text="STEP 1 OK — EMPTY captured. Now hover ENEMY A and press CTRL+ALT+2.";}
                else if(id==2){enemyA=Capture(); status.Text="STEP 2 OK — ENEMY A captured. Hover a DIFFERENT ENEMY B and press CTRL+ALT+3.";}
                else if(id==3){enemyB=Capture(); status.Text="STEP 3 OK — ENEMY B captured. Press CTRL+ALT+0 to analyze/save.";}
                else if(id==4) AnalyzeAndSave();
            }
            catch(Exception ex){status.Text="ERROR: "+ex.Message;}
        }
        base.WndProc(ref m);
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

            // One-level indirection entirely inside captured .data.
            uint pa=a,pb=b,pc=c;
            long dlo=(long)A.Base+DATA_RVA, dhi=dlo+n;
            bool ia=pa>=dlo&&pa+4<=dhi, ib=pb>=dlo&&pb+4<=dhi, ic=pc>=dlo&&pc+4<=dhi;
            if(ib&&ic)
            {
                uint ua=Dword(A.Data,(int)(pa-dlo));
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
        if(empty.Pid!=enemyA.Pid||empty.Pid!=enemyB.Pid)throw new InvalidOperationException("BRZE process changed between captures; restart observer and recapture");
        var all=FindCandidates().OrderByDescending(x=>x.Score).ThenBy(x=>x.Rva).Take(200).ToList();
        var sb=new StringBuilder();
        sb.AppendLine("BRZE HOVER TARGET POINTER FORENSIC — READ ONLY");
        sb.AppendLine($"pid={empty.Pid} base=0x{empty.Base:X8} pool=0x{empty.Pool:X8} localId={empty.LocalId}");
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
        status.Text=$"ANALYSIS SAVED\r\n{path}\r\n\r\nCandidates: {all.Count}. Send this TXT to ChatGPT.";
    }
}
