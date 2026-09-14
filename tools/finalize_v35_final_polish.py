from pathlib import Path

# V35 = final UI polish on top of V34/V30 proven runtime.
# Gameplay cores remain untouched. Changes:
#   1) main-window COPY UNIT offset now matches overlay: 0.1..64.0, step 0.1
#   2) premium in-game overlay can be dragged by its header without taking game focus

# Main generated form: version/controller handoff only.
ui=Path('FinalV18MainForm.cs')
s=ui.read_text(encoding='utf-8')
for old,new in [
    ('Text="BRZE Trainer — V34 Diagnostics";','Text="BRZE Trainer — V35 Diagnostics";'),
    ('Text="BRZE Trainer — V34 Clean";','Text="BRZE Trainer — V35 Clean";'),
    ('V34  ·  FINAL CANDIDATE + HARD BRZE REBIND  ·  INTEGRATED','V35  ·  FINAL RC + 0.1 OFFSET + MOVABLE OVERLAY  ·  INTEGRATED'),
    ('OverlayHotkeyControllerV34.Attach(this);','OverlayHotkeyControllerV35.Attach(this);')
]:
    if old not in s:
        raise SystemExit('V35 main marker missing: '+old)
    s=s.replace(old,new,1)
ui.write_text(s,encoding='utf-8')

# Compile a UI-only copy of the V30 feature panels. The original proven V30 panel
# source remains byte-identical in the repo/build workspace.
panel_src=Path('IntegratedFeaturePanelsV30.cs').read_text(encoding='utf-8')
old='readonly NumericUpDown offset=new(){Minimum=2,Maximum=40,Increment=1,DecimalPlaces=1,Value=8,Width=74,Height=30,BackColor=Color.FromArgb(32,40,52),ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};'
new='readonly NumericUpDown offset=new(){Minimum=0.1m,Maximum=64m,Increment=0.1m,DecimalPlaces=1,Value=8m,Width=74,Height=30,BackColor=Color.FromArgb(32,40,52),ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};'
if old not in panel_src:
    raise SystemExit('V35 main COPY offset marker missing')
panel_src=panel_src.replace(old,new,1)
Path('IntegratedFeaturePanelsV35.cs').write_text(panel_src,encoding='utf-8')

# V34 already contains the 91% premium overlay, 0.1 overlay COPY offset, Unit Changer,
# global Alt+W, and hard BRZE rebind. Layer draggable-header behavior on that exact source.
ov=Path('OverlayHotkeyControllerV34.cs').read_text(encoding='utf-8')
ov=ov.replace('V34','V35')
ov=ov.replace('const int HOTKEY_ID=0x3477;','const int HOTKEY_ID=0x3577;',1)

# Preserve user-chosen overlay position across hide/show. Initial show still docks top-right.
old='''        var game=GameWindow();\n        var b=OverlayBounds(overlay,game);\n        overlay.Location=b.Location;\n        overlay.SyncNow();'''
new='''        var game=GameWindow();\n        var b=overlay.UserMoved?new Rectangle(overlay.Location,overlay.Size):OverlayBounds(overlay,game);\n        overlay.Location=b.Location;\n        overlay.SyncNow();'''
if old not in ov:
    raise SystemExit('V35 overlay Show position marker missing')
ov=ov.replace(old,new,1)

# Add drag state beside existing overlay state fields.
old='''    bool ucBridgeReady,ucSyncing;\n\n    static readonly Color Bg='''
new='''    bool ucBridgeReady,ucSyncing;\n    bool dragging;\n    Point dragCursorStart,dragWindowStart;\n    Control? dragCapture;\n    public bool UserMoved { get; private set; }\n\n    static readonly Color Bg='''
if old not in ov:
    raise SystemExit('V35 drag state insertion marker missing')
ov=ov.replace(old,new,1)

# Insert focus-safe manual drag helpers before rounded-region drawing.
marker='''    void UpdateRoundedRegion()\n    {'''
helpers=r'''    void EnableHeaderDrag(Control c)
    {
        c.Cursor=Cursors.SizeAll;
        c.MouseDown+=DragMouseDown;
        c.MouseMove+=DragMouseMove;
        c.MouseUp+=DragMouseUp;
    }

    void DragMouseDown(object? sender,MouseEventArgs e)
    {
        if(e.Button!=MouseButtons.Left||sender is not Control c)return;
        dragging=true;dragCursorStart=Cursor.Position;dragWindowStart=Location;dragCapture=c;c.Capture=true;
    }

    void DragMouseMove(object? sender,MouseEventArgs e)
    {
        if(!dragging||Control.MouseButtons!=MouseButtons.Left)return;
        Point cur=Cursor.Position;
        int x=dragWindowStart.X+(cur.X-dragCursorStart.X),y=dragWindowStart.Y+(cur.Y-dragCursorStart.Y);
        Rectangle wa=Screen.FromPoint(cur).WorkingArea;
        int maxX=Math.Max(wa.Left+4,wa.Right-Width-4),maxY=Math.Max(wa.Top+4,wa.Bottom-Height-4);
        x=Math.Max(wa.Left+4,Math.Min(x,maxX));y=Math.Max(wa.Top+4,Math.Min(y,maxY));
        Location=new Point(x,y);UserMoved=true;
    }

    void DragMouseUp(object? sender,MouseEventArgs e)
    {
        if(e.Button!=MouseButtons.Left)return;
        dragging=false;
        try{if(dragCapture!=null)dragCapture.Capture=false;}catch{}
        dragCapture=null;
        OverlayHotkeyControllerV35.ReturnGameFocus();
    }

'''
if marker not in ov:
    raise SystemExit('V35 rounded-region marker missing')
ov=ov.replace(marker,helpers+marker,1)

# Turn only the empty/header-title zone into a drag surface; navigation/hide buttons
# remain ordinary clickable controls.
old='''        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};\n        head.Controls.Add(new Label{Text="BATTLE REALMS  //  LIVE CONTROL DECK",Location=new Point(0,2),Size=new Size(470,29),ForeColor=TextColor,Font=new Font("Segoe UI Semibold",15.2f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft});'''
new='''        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};\n        var dragTitle=new Label{Text="BATTLE REALMS  //  LIVE CONTROL DECK",Location=new Point(0,2),Size=new Size(470,29),ForeColor=TextColor,Font=new Font("Segoe UI Semibold",15.2f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};\n        EnableHeaderDrag(head);EnableHeaderDrag(dragTitle);head.Controls.Add(dragTitle);'''
if old not in ov:
    raise SystemExit('V35 header title marker missing')
ov=ov.replace(old,new,1)

old='''        head.Controls.Add(new Label{Text=$"V35  ·  {mode}  ·  91% GLASS  ·  GAME FOCUS LOCKED",Location=new Point(2,34),Size=new Size(490,20),ForeColor=Dim,Font=new Font("Segoe UI",8.4f),TextAlign=ContentAlignment.MiddleLeft});'''
new='''        var dragSub=new Label{Text=$"V35  ·  {mode}  ·  91% GLASS  ·  DRAG HEADER  ·  FOCUS LOCKED",Location=new Point(2,34),Size=new Size(490,20),ForeColor=Dim,Font=new Font("Segoe UI",8.4f),TextAlign=ContentAlignment.MiddleLeft};\n        EnableHeaderDrag(dragSub);head.Controls.Add(dragSub);'''
if old not in ov:
    raise SystemExit('V35 header subtitle marker missing')
ov=ov.replace(old,new,1)

Path('OverlayHotkeyControllerV35.cs').write_text(ov,encoding='utf-8')
print('V35 generated: main COPY offset 0.1..64 step 0.1 + draggable 91% no-activate overlay with position persistence')
