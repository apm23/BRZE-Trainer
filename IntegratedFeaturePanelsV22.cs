using System;
using System.Drawing;
using System.Windows.Forms;

namespace BRZETrainer;

internal abstract class IntegratedFeatureCard : UserControl
{
    protected static readonly Color CardBack=Color.FromArgb(18,24,34);
    protected static readonly Color Accent=Color.FromArgb(97,224,179);
    protected static readonly Color TextMain=Color.WhiteSmoke;
    protected static readonly Color TextDim=Color.FromArgb(174,187,204);
    protected static readonly Color ButtonBack=Color.FromArgb(43,55,72);
    protected static readonly Color ButtonGreen=Color.FromArgb(38,137,111);
    protected static readonly Color Border=Color.FromArgb(64,82,104);

    protected IntegratedFeatureCard()
    {
        BackColor=CardBack;ForeColor=TextMain;Margin=Padding.Empty;Padding=new Padding(12);DoubleBuffered=true;Font=new Font("Segoe UI",9f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);using var p=new Pen(Border);e.Graphics.DrawRectangle(p,0,0,Math.Max(0,Width-1),Math.Max(0,Height-1));
    }

    protected static Button ActionButton(string text,int width,bool primary=false)
    {
        var b=new Button{Text=text,Width=width,Height=36,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,0,8,0),BackColor=primary?ButtonGreen:ButtonBack,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",8.7f),AutoEllipsis=false,UseMnemonic=false};
        b.FlatAppearance.BorderColor=Border;return b;
    }

    protected static Label SectionTitle(string text)=>new(){Text=text,Dock=DockStyle.Fill,AutoSize=false,ForeColor=Accent,Font=new Font("Segoe UI Semibold",10.2f),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=false};
}

internal sealed class HeroEffectIntegratedPanel : IntegratedFeatureCard
{
    readonly Button issyl=ActionButton("APPLY ISSYL",132,true);
    readonly Button gray=ActionButton("APPLY GRAYBACK",148,true);
    readonly Button both=ActionButton("APPLY BOTH",126,true);
    readonly NumericUpDown seconds=new(){Minimum=1m,Maximum=420m,Increment=5m,DecimalPlaces=1,Value=30m,Width=86,Height=32,BackColor=Color.FromArgb(32,40,52),ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};
    readonly Label phase=new(){Dock=DockStyle.Fill,AutoSize=false,ForeColor=TextDim,Font=new Font("Segoe UI",8.6f),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=false};
    readonly System.Windows.Forms.Timer timer=new(){Interval=100};

    public HeroEffectIntegratedPanel()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty,Padding=Padding.Empty,BackColor=Color.Transparent};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,30));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        root.Controls.Add(SectionTitle("HERO EFFECT  //  DIRECT APPLY"),0,0);

        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=6,RowCount=1,Margin=Padding.Empty,Padding=new Padding(0,4,0,4),BackColor=Color.Transparent};
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,156));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,134));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,98));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,92));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        issyl.Dock=DockStyle.Fill;gray.Dock=DockStyle.Fill;both.Dock=DockStyle.Fill;
        actions.Controls.Add(issyl,0,0);actions.Controls.Add(gray,1,0);actions.Controls.Add(both,2,0);
        actions.Controls.Add(new Label{Text="DURATION (SEC)",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,ForeColor=TextDim,Font=new Font("Segoe UI Semibold",8.2f),AutoEllipsis=false},3,0);
        seconds.Dock=DockStyle.Fill;seconds.Margin=new Padding(6,3,0,3);actions.Controls.Add(seconds,4,0);root.Controls.Add(actions,0,1);

        phase.Text="Select unit(s), set duration, then APPLY. Other buffs are ignored. You can immediately apply the same ability to another group if the duration is unchanged.";
        root.Controls.Add(phase,0,2);Controls.Add(root);

        issyl.Click+=(_,_)=>Apply(DirectHeroMode.Issyl);gray.Click+=(_,_)=>Apply(DirectHeroMode.Grayback);both.Click+=(_,_)=>Apply(DirectHeroMode.Both);
        timer.Tick+=(_,_)=>phase.Text=HeroEffectDirectCoreV22.Tick();timer.Start();
    }

    void Apply(DirectHeroMode mode)=>phase.Text=HeroEffectDirectCoreV22.Start(mode,seconds.Value);

    protected override void Dispose(bool disposing)
    {
        if(disposing){timer.Stop();HeroEffectDirectCoreV22.Shutdown();timer.Dispose();}
        base.Dispose(disposing);
    }
}

internal sealed class CopyUnitIntegratedPanel : IntegratedFeatureCard
{
    readonly Button copy=ActionButton("COPY SELECTED UNITS",170,true);
    readonly Button paste=ActionButton("PASTE BESIDE",132,true);
    readonly Button clear=ActionButton("CLEAR COPY",110);
    readonly NumericUpDown offset=new(){Minimum=2,Maximum=40,Increment=1,DecimalPlaces=1,Value=8,Width=74,Height=30,BackColor=Color.FromArgb(32,40,52),ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};
    readonly Label statusLabel=new(){Dock=DockStyle.Fill,AutoSize=false,ForeColor=TextDim,Font=new Font("Segoe UI",8.5f),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=false};
    readonly System.Windows.Forms.Timer timer=new(){Interval=120};

    public CopyUnitIntegratedPanel()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty,Padding=Padding.Empty,BackColor=Color.Transparent};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,28));root.RowStyles.Add(new RowStyle(SizeType.Absolute,44));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        root.Controls.Add(SectionTitle("COPY UNIT  //  UNIT CLONE LAB"),0,0);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,5,0,3),BackColor=Color.Transparent};
        actions.Controls.Add(copy);actions.Controls.Add(paste);
        actions.Controls.Add(new Label{Text="SIDE OFFSET",Width=88,Height=34,TextAlign=ContentAlignment.MiddleRight,ForeColor=TextDim,Margin=new Padding(0,0,6,0),AutoEllipsis=false});
        offset.Margin=new Padding(0,3,10,0);actions.Controls.Add(offset);actions.Controls.Add(clear);
        root.Controls.Add(actions,0,1);root.Controls.Add(statusLabel,0,2);Controls.Add(root);

        copy.Click+=(_,_)=>{var r=IntegratedFrameDispatcherCore.CopySelection();statusLabel.Text=r.Message;};
        paste.Click+=(_,_)=>statusLabel.Text=IntegratedFrameDispatcherCore.PasteCopied((float)offset.Value);
        clear.Click+=(_,_)=>{IntegratedFrameDispatcherCore.ClearCopied();statusLabel.Text="Copy snapshot cleared.";};
        timer.Tick+=(_,_)=>RefreshStatus();timer.Start();RefreshStatus();
    }

    void RefreshStatus(){var s=IntegratedFrameDispatcherCore.Snapshot();statusLabel.Text=s.Copy+"\r\n"+s.Queue;}

    protected override void Dispose(bool disposing){if(disposing){timer.Stop();timer.Dispose();}base.Dispose(disposing);}
}
