using System;
using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectResetForensicsV23;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm:Form
{
    readonly Button scan=new(){Text="SCAN ACTIVE ISSYL",Width=190,Height=38};
    readonly Button copy=new(){Text="COPY REPORT",Width=150,Height=38,Enabled=false};
    readonly Label status=new(){AutoSize=false,Dock=DockStyle.Top,Height=52,TextAlign=ContentAlignment.MiddleLeft};
    readonly TextBox report=new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9f)};

    public MainForm()
    {
        Text="BRZE Hero Effect Reset Forensics V23 — READ ONLY";
        ClientSize=new Size(980,720);MinimumSize=new Size(820,600);StartPosition=FormStartPosition.CenterScreen;
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=54,Padding=new Padding(8),FlowDirection=FlowDirection.LeftToRight,WrapContents=false};
        top.Controls.Add(scan);top.Controls.Add(copy);
        status.Text="Select exactly ONE unit that currently has Issyl active, then click SCAN. This probe performs READS ONLY.";
        Controls.Add(report);Controls.Add(status);Controls.Add(top);
        scan.Click+=(_,_)=>RunScan();
        copy.Click+=(_,_)=>{if(report.TextLength>0)Clipboard.SetText(report.Text);};
    }

    void RunScan()
    {
        scan.Enabled=false;copy.Enabled=false;status.Text="Scanning active A5 record + .text teardown candidates...";Application.DoEvents();
        var r=ProbeCore.ScanActiveIssyl();report.Text=r.Report;status.Text=r.Summary;copy.Enabled=report.TextLength>0;scan.Enabled=true;
    }
}
