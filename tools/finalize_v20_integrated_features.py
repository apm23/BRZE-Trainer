from pathlib import Path
import re

ui_path=Path('FinalV18MainForm.cs')
if not ui_path.exists():
    raise SystemExit('V20 requires generated FinalV18MainForm.cs after V19 layer')
s=ui_path.read_text(encoding='utf-8')

# Version/title. Keep V19 mechanics intact; V20 is integration + layout only.
s=s.replace('Text="BRZE Trainer — V19 Diagnostics";','Text="BRZE Trainer — V20 Diagnostics";',1)
s=s.replace('Text="BRZE Trainer — V19 Clean";','Text="BRZE Trainer — V20 Clean";',1)
s=s.replace('V19  ·  HOLD DEATH + CLEAN LAYOUT','V20  ·  COPY UNIT + HERO EFFECT  ·  INTEGRATED')

# More vertical room is intentional: no clipped labels/buttons, no cramped status area.
if 'ClientSize=new Size(1640,720);' in s:
    s=s.replace('ClientSize=new Size(1640,720);','ClientSize=new Size(1640,900);',1)
else:
    s,n=re.subn(r'ClientSize\s*=\s*new Size\(1640\s*,\s*\d+\);','ClientSize=new Size(1640,900);',s,count=1)
    if n!=1: raise SystemExit('V20 could not locate ClientSize')

# Replace the two-row V15/V19 content shell with a precise three-row layout:
# top = existing Cheats + Unit Changer; middle = Hero Effect + Copy Unit;
# bottom = full-width System Status aligned to both outer top columns.
start=s.find('        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2')
if start<0:
    raise SystemExit('V20 could not locate main content layout start')
needle='        shell.Controls.Add(content,0,1);'
end=s.find(needle,start)
if end<0:
    raise SystemExit('V20 could not locate main content layout end')
end+=len(needle)
new_layout='''        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=Padding.Empty};
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,700));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute,258));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute,194));
        content.RowStyles.Add(new RowStyle(SizeType.Percent,100));

        content.Controls.Add(BuildCheats(),0,0);
        content.Controls.Add(BuildRight(),1,0);

        var heroPanel=new HeroEffectIntegratedPanel{Dock=DockStyle.Fill,Margin=new Padding(0,0,8,8)};
        var copyUnitPanel=new CopyUnitIntegratedPanel{Dock=DockStyle.Fill,Margin=new Padding(8,0,0,8)};
        content.Controls.Add(heroPanel,0,1);
        content.Controls.Add(copyUnitPanel,1,1);

        var statusPanel=BuildStatusPanel();
        statusPanel.Margin=Padding.Empty;
        content.Controls.Add(statusPanel,0,2);
        content.SetColumnSpan(statusPanel,2);
        shell.Controls.Add(content,0,1);'''
s=s[:start]+new_layout+s[end:]

# SYSTEM STATUS is still a full-width status box, but center its heading so the
# section reads visually centered while its diagnostics text remains left-aligned.
old='new Label{Text="SYSTEM STATUS",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(109,214,167),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10f),TextAlign=ContentAlignment.MiddleLeft}'
new='new Label{Text="SYSTEM STATUS",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(109,214,167),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10f),TextAlign=ContentAlignment.MiddleCenter,AutoEllipsis=false}'
if old not in s:
    raise SystemExit('V20 SYSTEM STATUS heading marker missing')
s=s.replace(old,new,1)

ui_path.write_text(s,encoding='utf-8')

# V20 makes the shared dispatcher the ONLY owner of render hook RVA 0x135C43.
# Existing V19 UI keeps its exact InstantDeathCore call surface through this wrapper.
Path('InstantDeathCore.cs').write_text(r'''namespace BRZETrainer;

internal static class InstantDeathCore
{
    public static bool TriggerSingle()=>IntegratedFrameDispatcherCore.DeathTriggerSingle(false);
    public static bool TriggerSingle(bool killAll)=>IntegratedFrameDispatcherCore.DeathTriggerSingle(killAll);
    public static string Tick(bool burstEnabled)=>IntegratedFrameDispatcherCore.DeathTick(burstEnabled,false);
    public static string Tick(bool burstEnabled,bool killAll)=>IntegratedFrameDispatcherCore.DeathTick(burstEnabled,killAll);
    public static void Stop()=>IntegratedFrameDispatcherCore.StopAll();
}
''',encoding='utf-8')

# The architecture verifier intentionally rejects a real remote-thread API import.
# Normalize the prose-only wording in the dispatcher so a plain text guard cannot
# mistake documentation for an imported API symbol.
dispatch_path=Path('IntegratedFrameDispatcherCore.cs')
dispatch=dispatch_path.read_text(encoding='utf-8').replace('No CreateRemoteThread;','No remote-thread creation;')
dispatch_path.write_text(dispatch,encoding='utf-8')

print('V20 generated: precise 3-row layout + Hero Effect + COPY UNIT + full-width centered status + shared frame dispatcher wrapper')
