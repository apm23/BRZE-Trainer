using System;
using System.Drawing;
using System.Windows.Forms;

namespace BRZEHeroEffectSniffer;

internal static class Program
{
    [STAThread]
    static void Main(){ApplicationConfiguration.Initialize();Application.Run(new MainForm());}
}

internal sealed class MainForm:Form
{
    readonly Button arm=new(){Text="ARM SNIFFER"};
    readonly Button clear=new(){Text="CLEAR LOG"};
    readonly Label game=new(), target=new(), magic=new(), note=new();
    readonly System.Windows.Forms.Timer timer=new(){Interval=100};

    public MainForm()
    {
        Text="BRZE Hero Effect Sniffer — Grayback / Issyl Runtime Proof";
        ClientSize=new Size(980,470);MinimumSize=new Size(900,430);StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(10,14,22);ForeColor=Color.WhiteSmoke;Font=new Font("Segoe UI",10f);DoubleBuffered=true;

        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),ColumnCount=1,RowCount=5};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,92));root.RowStyles.Add(new RowStyle(SizeType.Absolute,68));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,85));root.RowStyles.Add(new RowStyle(SizeType.Absolute,85));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        Controls.Add(root);

        var head=Card();
        head.Controls.Add(new Label{Text="HERO EFFECT RUNTIME SNIFFER",Font=new Font("Segoe UI Semibold",19f),AutoSize=true,Location=new Point(16,10)});
        head.Controls.Add(new Label{Text="Observation only — logs the exact native ability IDs BRZE uses when Grayback / Issyl effects really fire.",ForeColor=Color.FromArgb(157,178,202),AutoSize=true,Location=new Point(18,52)});
        root.Controls.Add(head,0,0);

        var actions=Card();
        arm.SetBounds(16,16,180,34);clear.SetBounds(210,16,150,34);Style(arm);Style(clear);
        actions.Controls.Add(arm);actions.Controls.Add(clear);game.SetBounds(385,18,550,28);game.AutoSize=false;game.ForeColor=Color.FromArgb(97,224,179);actions.Controls.Add(game);
        root.Controls.Add(actions,0,1);

        var t=Card();t.Controls.Add(Title("TARGET HELPER 0x5F0C32",12));target.SetBounds(16,42,920,30);target.AutoSize=false;target.Font=new Font(FontFamily.GenericMonospace,9.3f);t.Controls.Add(target);root.Controls.Add(t,0,2);
        var m=Card();m.Controls.Add(Title("MAGIC CREATE 0x53FFD1",12));magic.SetBounds(16,42,920,30);magic.AutoSize=false;magic.Font=new Font(FontFamily.GenericMonospace,9.3f);m.Controls.Add(magic);root.Controls.Add(m,0,3);

        var n=Card();
        note.Text="TEST: ARM → CLEAR LOG → pakai skill/buff Grayback secara NORMAL di game → catat dua baris di atas. CLEAR lagi → pakai Issyl Haste secara NORMAL → catat lagi.\r\nKalau count naik, ability ID yang tampil adalah runtime ID asli. Kalau dua count tetap 0, berarti skill itu lewat special-case lain dan kita pindah hook ke jalur khususnya.";
        note.SetBounds(16,14,920,100);note.AutoSize=false;note.ForeColor=Color.FromArgb(236,183,84);n.Controls.Add(note);root.Controls.Add(n,0,4);

        arm.Click+=(_,_)=>{game.Text=SnifferCore.Install();};
        clear.Click+=(_,_)=>{SnifferCore.Clear();};
        timer.Tick+=(_,_)=>RefreshState();timer.Start();
        FormClosed+=(_,_)=>SnifferCore.Stop();
        RefreshState();
    }

    void RefreshState()
    {
        var s=SnifferCore.Snapshot();game.Text=s.Game;
        target.Text=s.Target.Format("TARGET");magic.Text=s.Magic.Format("MAGIC ");
    }

    static Panel Card()=>new(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(18,24,34),Padding=new Padding(10),Margin=new Padding(0,0,0,8)};
    static Label Title(string s,int y)=>new(){Text=s,AutoSize=true,Location=new Point(16,y),Font=new Font("Segoe UI Semibold",11f),ForeColor=Color.FromArgb(97,224,179)};
    static void Style(Button b){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(70,90,115);b.BackColor=Color.FromArgb(42,55,72);b.ForeColor=Color.White;}
}
