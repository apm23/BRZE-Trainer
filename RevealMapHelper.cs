using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BRZERevealMapHelper;

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
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr addr,UIntPtr size,uint type,uint protect);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool VirtualFreeEx(IntPtr h,IntPtr addr,UIntPtr size,uint freeType);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool WriteProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr written);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr CreateRemoteThread(IntPtr h,IntPtr sa,uint stack,IntPtr start,IntPtr param,uint flags,out uint tid);
    [DllImport("kernel32.dll", SetLastError=true)] static extern uint WaitForSingleObject(IntPtr h,uint ms);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    const uint ACCESS=0x0002|0x0008|0x0020|0x0400; // CREATE_THREAD|VM_OPERATION|VM_WRITE|QUERY
    const uint MEM_COMMIT=0x1000, MEM_RESERVE=0x2000, MEM_RELEASE=0x8000, PAGE_EXECUTE_READWRITE=0x40;
    const int RVA_DISABLE_FOW=0x0CB7B5;
    const int RVA_ENABLE_FOW=0x0CB7AD;

    readonly Label status=new(){Dock=DockStyle.Bottom,Height=62,TextAlign=System.Drawing.ContentAlignment.MiddleCenter,Text="BRZE 1.60 — native FogOfWar helper"};
    readonly Button reveal=new(){Dock=DockStyle.Top,Height=78,Text="REVEAL ALL MAP — TEMP"};
    readonly Button restore=new(){Dock=DockStyle.Top,Height=78,Text="RESTORE FOG OF WAR"};

    public MainForm()
    {
        Text="BRZE Reveal Map Helper"; Width=520; Height=280; StartPosition=FormStartPosition.CenterScreen; TopMost=true;
        reveal.Font=new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif,12,System.Drawing.FontStyle.Bold);
        restore.Font=reveal.Font;
        Controls.Add(status); Controls.Add(restore); Controls.Add(reveal);
        reveal.Click+=(_,_)=>InvokeNative(RVA_DISABLE_FOW,"MAP REVEALED — sekarang pakai Hover Observer dari jauh");
        restore.Click+=(_,_)=>InvokeNative(RVA_ENABLE_FOW,"FOG OF WAR RESTORED");
    }

    void InvokeNative(int targetRva,string ok)
    {
        try
        {
            var ps=Process.GetProcessesByName("Battle_Realms_F");
            if(ps.Length==0)throw new InvalidOperationException("Battle_Realms_F.exe tidak ditemukan");
            var p=ps[0]; long baseAddr=p.MainModule!.BaseAddress.ToInt64();
            IntPtr h=OpenProcess(ACCESS,false,p.Id); if(h==IntPtr.Zero)throw new InvalidOperationException("OpenProcess gagal");
            try
            {
                IntPtr cave=VirtualAllocEx(h,IntPtr.Zero,(UIntPtr)16,MEM_COMMIT|MEM_RESERVE,PAGE_EXECUTE_READWRITE);
                if(cave==IntPtr.Zero)throw new InvalidOperationException("VirtualAllocEx gagal");
                try
                {
                    long caveAddr=cave.ToInt64(); long target=baseAddr+targetRva;
                    int rel=checked((int)(target-(caveAddr+5)));
                    byte[] code=new byte[10];
                    code[0]=0xE8; BitConverter.GetBytes(rel).CopyTo(code,1); // call native Disable/EnableFogOfWar wrapper
                    code[5]=0x33; code[6]=0xC0;                         // xor eax,eax
                    code[7]=0xC2; code[8]=0x04; code[9]=0x00;          // ret 4 for remote-thread ABI
                    if(!WriteProcessMemory(h,cave,code,code.Length,out var n)||n.ToInt64()!=code.Length)throw new InvalidOperationException("WriteProcessMemory gagal");
                    IntPtr th=CreateRemoteThread(h,IntPtr.Zero,0,cave,IntPtr.Zero,0,out _); if(th==IntPtr.Zero)throw new InvalidOperationException("CreateRemoteThread gagal");
                    try{WaitForSingleObject(th,3000);}finally{CloseHandle(th);}                    
                    status.Text=ok;
                }
                finally{VirtualFreeEx(h,cave,UIntPtr.Zero,MEM_RELEASE);}
            }
            finally{CloseHandle(h);}
        }
        catch(Exception ex){status.Text="ERROR: "+ex.Message;}
    }
}
