using System;
using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectCurrentTickResetV29;

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
    readonly Button reset=new(){Text="RESET ACTIVE ISSYL TO CURRENT BRZE TICK",Width=330,Height=40};
    readonly Button copy=new(){Text="COPY REPORT",Width=150,Height=40,Enabled=false};
    readonly Label status=new(){AutoSize=false,Dock=DockStyle.Top,Height=64,TextAlign=ContentAlignment.MiddleLeft};
    readonly TextBox report=new(){Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9f)};

    public MainForm()
    {
        Text="BRZE Hero Effect Current-Tick Reset V29 — GUARDED";
        ClientSize=new Size(1080,720);MinimumSize=new Size(900,620);StartPosition=FormStartPosition.CenterScreen;
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=58,Padding=new Padding(8),FlowDirection=FlowDirection.LeftToRight,WrapContents=false};
        top.Controls.Add(reset);top.Controls.Add(copy);
        status.Text="Exactly ONE selected unit with active STOCK Issyl. Wait a few seconds after buff starts, then reset once.";
        Controls.Add(report);Controls.Add(status);Controls.Add(top);
        reset.Click+=(_,_)=>RunReset();
        copy.Click+=(_,_)=>{if(report.TextLength>0)Clipboard.SetText(report.Text);};
    }

    void RunReset()
    {
        reset.Enabled=false;copy.Enabled=false;status.Text="Reading proven BRZE current-tick source and performing guarded one-field reset...";Application.DoEvents();
        var r=CurrentTickResetCore.Run();report.Text=r.Report;status.Text=r.Summary;copy.Enabled=report.TextLength>0;reset.Enabled=true;
    }
}
