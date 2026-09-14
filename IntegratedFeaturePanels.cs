using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BRZEHeroEffectReplayDurationV11;

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
        BackColor=CardBack;
        ForeColor=TextMain;
        Margin=Padding.Empty;
        Padding=new Padding(12);
        DoubleBuffered=true;
        Font=new Font("Segoe UI",9f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var p=new Pen(Border);
        e.Graphics.DrawRectangle(p,0,0,Math.Max(0,Width-1),Math.Max(0,Height-1));
    }

    protected static Button ActionButton(string text,int width,bool primary=false)
    {
        var b=new Button{Text=text,Width=width,Height=34,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,0,8,0),BackColor=primary?ButtonGreen:ButtonBack,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",8.7f),AutoEllipsis=false,UseMnemonic=false};
        b.FlatAppearance.BorderColor=Border;
        return b;
    }

    protected static Label SectionTitle(string text)=>new(){Text=text,Dock=DockStyle.Fill,AutoSize=false,ForeColor=Accent,Font=new Font("Segoe UI Semibold",10.2f),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=false};
}

internal sealed class HeroEffectIntegratedPanel : IntegratedFeatureCard
{
    readonly Button capture=ActionButton("CAPTURE ISSYL BASELINE",184,true);
    readonly Button one=ActionButton("ISSYL 1X",82);
    readonly Button two=ActionButton("ISSYL 2X",82);
    readonly Button three=ActionButton("ISSYL 3X",82);
    readonly Button custom=ActionButton("ISSYL CUSTOM",112,true);
    readonly Button gray=ActionButton("GRAYBACK NORMAL",138);
    readonly Button issyl=ActionButton("ISSYL NORMAL",126);
    readonly Button both=ActionButton("BOTH NORMAL",118);
    readonly Button restore=ActionButton("RESTORE 15000",126);
    readonly NumericUpDown customX=new(){Minimum=0.25m,Maximum=20m,Increment=0.25m,DecimalPlaces=2,Value=4m,Width=76,Height=30,BackColor=Color.FromArgb(32,40,52),ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};
    readonly Label phase=new(){Dock=DockStyle.Fill,AutoSize=false,ForeColor=TextDim,Font=new Font("Segoe UI",8.5f),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=false};
    readonly System.Windows.Forms.Timer timer=new(){Interval=100};
    readonly IntegratedReplayLifetimeWatcher watcher=new();
    bool holdActive;
    uint activeDuration=ConfigurableDurationCore.ORIGINAL_DURATION;
    string activeLabel="none";

    public HeroEffectIntegratedPanel()
    {
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,Padding=Padding.Empty,BackColor=Color.Transparent};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        root.Controls.Add(SectionTitle("HERO EFFECT  //  ISSYL DURATION"),0,0);

        var primary=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,3,0,3),BackColor=Color.Transparent};
        primary.Controls.Add(capture);primary.Controls.Add(one);primary.Controls.Add(two);primary.Controls.Add(three);
        primary.Controls.Add(new Label{Text="CUSTOM",Width=58,Height=34,TextAlign=ContentAlignment.MiddleRight,ForeColor=TextDim,Margin=new Padding(0,0,5,0),AutoEllipsis=false});
        customX.Margin=new Padding(0,3,6,0);primary.Controls.Add(customX);primary.Controls.Add(custom);
        root.Controls.Add(primary,0,1);

        var secondary=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Margin=Padding.Empty,Padding=new Padding(0,3,0,3),BackColor=Color.Transparent};
        secondary.Controls.Add(gray);secondary.Controls.Add(issyl);secondary.Controls.Add(both);secondary.Controls.Add(restore);
        root.Controls.Add(secondary,0,2);
        root.Controls.Add(phase,0,3);
        Controls.Add(root);

        capture.Click+=(_,_)=>ArmBaseline();
        one.Click+=(_,_)=>StartReplay(1.0);
        two.Click+=(_,_)=>StartReplay(2.0);
        three.Click+=(_,_)=>StartReplay(3.0);
        custom.Click+=(_,_)=>StartReplay((double)customX.Value);
        gray.Click+=(_,_)=>QueueNormal(IntegratedFrameDispatcherCore.GRAYBACK_RUNTIME_ABILITY,uint.MaxValue,"GRAYBACK 0xC0");
        issyl.Click+=(_,_)=>QueueNormal(IntegratedFrameDispatcherCore.ISSYL_RUNTIME_ABILITY,uint.MaxValue,"ISSYL 0xA5");
        both.Click+=(_,_)=>QueueNormal(IntegratedFrameDispatcherCore.GRAYBACK_RUNTIME_ABILITY,IntegratedFrameDispatcherCore.ISSYL_RUNTIME_ABILITY,"BOTH 0xC0 + 0xA5");
        restore.Click+=(_,_)=>ManualRestore();

        timer.Tick+=(_,_)=>TickFeature();
        timer.Start();
        phase.Text="Capture baseline once, then use Issyl presets/custom. Custom multiplier range: 0.25X–20X.";
    }

    void ArmBaseline()
    {
        if(holdActive){phase.Text="BLOCKED — current Issyl replay is still active.";return;}
        watcher.Reset();
        phase.Text=ConfigurableDurationCore.ArmBaseline();
    }

    void StartReplay(double multiplier)
    {
        if(holdActive){phase.Text="BLOCKED — wait for current replay to expire or RESTORE 15000.";return;}
        if(IntegratedFrameDispatcherCore.NativeQueueBusy()){phase.Text="BLOCKED — shared native queue is busy (Copy Unit / Replay).";return;}
        uint desired=checked((uint)Math.Round(ConfigurableDurationCore.ORIGINAL_DURATION*multiplier,MidpointRounding.AwayFromZero));
        string label=multiplier==1.0?"ISSYL 1X":multiplier==2.0?"ISSYL 2X":multiplier==3.0?"ISSYL 3X":$"ISSYL CUSTOM {multiplier:0.##}X";
        string arm=ConfigurableDurationCore.ArmReplay(desired,label);
        phase.Text=arm;
        var s=ConfigurableDurationCore.Snapshot();
        if(s.Stage!=DurationStage.ReplayArmed)return;
        if(!watcher.Arm(s.Unit,out string error))
        {
            phase.Text=ConfigurableDurationCore.AbortAndRestore("watcher arm failed: "+error);
            return;
        }
        string q=IntegratedFrameDispatcherCore.QueueReplaySelected(IntegratedFrameDispatcherCore.ISSYL_RUNTIME_ABILITY,uint.MaxValue,label);
        if(!q.Contains("queued",StringComparison.OrdinalIgnoreCase))
        {
            watcher.Reset();phase.Text=ConfigurableDurationCore.AbortAndRestore("dispatcher queue failed: "+q);return;
        }
        activeDuration=desired;activeLabel=label;holdActive=true;SetActions(false);
        phase.Text=$"{label} ACTIVE — nominal {desired}; held until natural expiry.";
    }

    void QueueNormal(uint a1,uint a2,string label)
    {
        if(holdActive){phase.Text="BLOCKED — extended Issyl hold is active.";return;}
        phase.Text=IntegratedFrameDispatcherCore.QueueReplaySelected(a1,a2,label);
    }

    void ManualRestore()
    {
        holdActive=false;watcher.Reset();SetActions(true);
        phase.Text=ConfigurableDurationCore.RestoreNow();
    }

    void TickFeature()
    {
        if(holdActive)
        {
            var w=watcher.Tick();
            if(w.State==IntegratedWatchState.Active)
                phase.Text=$"{activeLabel} ACTIVE — nominal {activeDuration} · elapsed {w.ElapsedMs/1000.0:0.0}s · full-lifetime hold";
            else if(w.State==IntegratedWatchState.Complete)
            {
                holdActive=false;phase.Text=ConfigurableDurationCore.CompleteReplay(w.ElapsedMs);watcher.Reset();SetActions(true);
            }
            else if(w.State==IntegratedWatchState.Error)
            {
                holdActive=false;phase.Text=ConfigurableDurationCore.AbortAndRestore(w.Message);watcher.Reset();SetActions(true);
            }
            return;
        }
        ConfigurableDurationCore.Tick();
        var s=ConfigurableDurationCore.Snapshot();
        if(s.Stage is DurationStage.BaselineArmed or DurationStage.BaselineActive or DurationStage.Ready or DurationStage.Error)phase.Text=s.Phase;
    }

    void SetActions(bool enabled)
    {
        capture.Enabled=enabled;one.Enabled=enabled;two.Enabled=enabled;three.Enabled=enabled;custom.Enabled=enabled;customX.Enabled=enabled;gray.Enabled=enabled;issyl.Enabled=enabled;both.Enabled=enabled;
        restore.Enabled=true;
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing)
        {
            timer.Stop();
            if(holdActive)ConfigurableDurationCore.AbortAndRestore("trainer closing");
            watcher.Reset();ConfigurableDurationCore.Shutdown();timer.Dispose();
        }
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

    void RefreshStatus()
    {
        var s=IntegratedFrameDispatcherCore.Snapshot();
        statusLabel.Text=s.Copy+"\r\n"+s.Queue;
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing){timer.Stop();timer.Dispose();}
        base.Dispose(disposing);
    }
}

internal enum IntegratedWatchState{Waiting,Active,Complete,Error}
internal readonly record struct IntegratedWatchResult(IntegratedWatchState State,double ElapsedMs,string Message);

internal sealed class IntegratedReplayLifetimeWatcher
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr h,IntPtr addr,byte[] buf,int size,out IntPtr read);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    const uint PROCESS_VM_READ=0x0010,PROCESS_QUERY_INFORMATION=0x0400;
    static readonly int[] RootOffsets={0x1E4,0x1E8,0x20C,0x210,0x214};
    static readonly int[] TransientIndexes={0,1,2,3};
    IntPtr h=IntPtr.Zero;Process? process;uint unit;readonly uint[] baseline=new uint[RootOffsets.Length];bool armed,active;int stable;long startStamp;
    static IntPtr A(long x)=>new(unchecked((int)(uint)x));

    bool Attach()
    {
        try{if(process!=null&&!process.HasExited&&h!=IntPtr.Zero)return true;}catch{}
        Detach();var ps=Process.GetProcessesByName("Battle_Realms_F");if(ps.Length==0)return false;process=ps[0];h=OpenProcess(PROCESS_VM_READ|PROCESS_QUERY_INFORMATION,false,process.Id);return h!=IntPtr.Zero;
    }
    uint R32(long address){if(h==IntPtr.Zero)return 0;var b=new byte[4];return ReadProcessMemory(h,A(address),b,4,out var n)&&n.ToInt64()==4?BitConverter.ToUInt32(b,0):0;}
    uint[] Roots(){var r=new uint[RootOffsets.Length];for(int i=0;i<r.Length;i++)r[i]=R32((long)unit+RootOffsets[i]);return r;}
    static bool Same(uint[] a,uint[] b){for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    bool EffectPattern(uint[] now)
    {
        int changed=0;for(int i=0;i<now.Length;i++)if(now[i]!=baseline[i])changed++;
        bool transientChanged=TransientIndexes.Any(i=>now[i]!=baseline[i]&&now[i]>=0x00010000&&now[i]<0x7FFF0000);
        return changed>=2&&transientChanged;
    }
    public bool Arm(uint targetUnit,out string error)
    {
        error="";Reset();if(targetUnit==0){error="unit pointer is zero";return false;}if(!Attach()){error="cannot attach read-only watcher";return false;}
        unit=targetUnit;var r=Roots();for(int i=0;i<r.Length;i++)baseline[i]=r[i];
        if(TransientIndexes.Any(i=>baseline[i]!=0)){error="target transient roots are not clean";Reset();return false;}
        armed=true;active=false;stable=0;startStamp=0;return true;
    }
    public IntegratedWatchResult Tick()
    {
        if(!armed)return new(IntegratedWatchState.Error,0,"watcher not armed");if(!Attach())return new(IntegratedWatchState.Error,0,"game process unavailable");var now=Roots();
        if(!active)
        {
            if(EffectPattern(now)){active=true;stable=0;startStamp=Stopwatch.GetTimestamp();return new(IntegratedWatchState.Active,0,"replay lifecycle detected");}
            return new(IntegratedWatchState.Waiting,0,"waiting lifecycle");
        }
        double ms=(Stopwatch.GetTimestamp()-startStamp)*1000.0/Stopwatch.Frequency;
        if(Same(now,baseline))stable++;else stable=0;
        if(stable>=4){armed=false;return new(IntegratedWatchState.Complete,ms,"natural expiry observed");}
        return new(IntegratedWatchState.Active,ms,"active");
    }
    public void Reset(){armed=active=false;stable=0;startStamp=0;unit=0;Array.Clear(baseline,0,baseline.Length);Detach();}
    void Detach(){try{if(h!=IntPtr.Zero)CloseHandle(h);}catch{}h=IntPtr.Zero;process=null;}
}