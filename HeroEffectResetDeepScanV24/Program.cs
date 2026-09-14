using System;
using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectResetDeepScanV24;

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

internal sealed class MainForm : Form
{
    readonly Button scan = new(){Text="DEEP SCAN ACTIVE ISSYL",Width=220,Height=38};
    readonly Button copy = new(){Text="COPY REPORT",Width=150,Height=38,Enabled=false};
    readonly Label status = new(){AutoSize=false,Dock=DockStyle.Top,Height=58,TextAlign=ContentAlignment.MiddleLeft};
    readonly TextBox report = new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9f)};

    public MainForm()
    {
        Text="BRZE Hero Effect Reset Deep Scan V24 — READ ONLY";
        ClientSize=new Size(1180,780);MinimumSize=new Size(900,620);StartPosition=FormStartPosition.CenterScreen;
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=56,Padding=new Padding(8),FlowDirection=FlowDirection.LeftToRight,WrapContents=false};
        top.Controls.Add(scan);top.Controls.Add(copy);
        status.Text="Select exactly ONE unit while Issyl is visibly active. V24 only reads live code/data and disassembles cleanup candidates.";
        Controls.Add(report);Controls.Add(status);Controls.Add(top);
        scan.Click+=(_,_)=>RunScan();
        copy.Click+=(_,_)=>{if(report.TextLength>0)Clipboard.SetText(report.Text);};
    }

    void RunScan()
    {
        scan.Enabled=false;copy.Enabled=false;status.Text="Resolving A5 record, candidate function boundaries, vtable methods, and local call targets...";Application.DoEvents();
        var r=DeepScanCore.Run();
        report.Text=r.Report;status.Text=r.Summary;copy.Enabled=report.TextLength>0;scan.Enabled=true;
    }
}
