using System;
using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectCurrentTimeSourceV27;

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
    readonly Button scan=new(){Text="SCAN CURRENT-TIME SOURCE",Width=235,Height=38};
    readonly Button copy=new(){Text="COPY REPORT",Width=150,Height=38,Enabled=false};
    readonly Label status=new(){AutoSize=false,Dock=DockStyle.Top,Height=58,TextAlign=ContentAlignment.MiddleLeft};
    readonly TextBox report=new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9f)};

    public MainForm()
    {
        Text="BRZE Hero Effect Current-Time Source V27 — READ ONLY";
        ClientSize=new Size(1120,780);MinimumSize=new Size(900,640);StartPosition=FormStartPosition.CenterScreen;
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=56,Padding=new Padding(8),FlowDirection=FlowDirection.LeftToRight,WrapContents=false};
        top.Controls.Add(scan);top.Controls.Add(copy);
        status.Text="READ ONLY. Active Issyl on exactly one selected unit is useful context but not required for the static caller scan.";
        Controls.Add(report);Controls.Add(status);Controls.Add(top);
        scan.Click+=(_,_)=>RunScan();
        copy.Click+=(_,_)=>{if(report.TextLength>0)Clipboard.SetText(report.Text);};
    }

    void RunScan()
    {
        scan.Enabled=false;copy.Enabled=false;status.Text="Scanning callers/pointer refs and sampling candidate clock sources...";Application.DoEvents();
        var r=CurrentTimeSourceCore.Run();report.Text=r.Report;status.Text=r.Summary;copy.Enabled=report.TextLength>0;scan.Enabled=true;
    }
}
