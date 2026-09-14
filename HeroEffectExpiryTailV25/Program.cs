using System;
using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectExpiryTailV25;

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
    readonly Button scan=new(){Text="SCAN EXPIRY TAIL",Width=190,Height=38};
    readonly Button copy=new(){Text="COPY REPORT",Width=150,Height=38,Enabled=false};
    readonly Label status=new(){AutoSize=false,Dock=DockStyle.Top,Height=58,TextAlign=ContentAlignment.MiddleLeft};
    readonly TextBox report=new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9f)};

    public MainForm()
    {
        Text="BRZE Hero Effect Expiry Tail V25 — READ ONLY";
        ClientSize=new Size(1080,760);MinimumSize=new Size(880,620);StartPosition=FormStartPosition.CenterScreen;
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=56,Padding=new Padding(8),FlowDirection=FlowDirection.LeftToRight,WrapContents=false};
        top.Controls.Add(scan);top.Controls.Add(copy);
        status.Text="Select exactly ONE unit while Issyl is visibly active, then SCAN EXPIRY TAIL. READS ONLY.";
        Controls.Add(report);Controls.Add(status);Controls.Add(top);
        scan.Click+=(_,_)=>RunScan();
        copy.Click+=(_,_)=>{if(report.TextLength>0)Clipboard.SetText(report.Text);};
    }

    void RunScan()
    {
        scan.Enabled=false;copy.Enabled=false;status.Text="Decoding V24 tick tail + config duration flow...";Application.DoEvents();
        var r=ExpiryTailCore.Run();report.Text=r.Report;status.Text=r.Summary;copy.Enabled=report.TextLength>0;scan.Enabled=true;
    }
}
