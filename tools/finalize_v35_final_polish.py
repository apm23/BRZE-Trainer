from pathlib import Path

# V35 = final UI/session polish on top of V34/V30 proven runtime.
# Gameplay cores remain untouched. Changes:
#   1) COPY UNIT offset is 0.1..64.0 in 0.1 steps in BOTH UIs, default/reset = 0.1
#   2) premium in-game overlay can be dragged by its header without taking game focus
#   3) Unit Changer UI state persists across trainer closes/reopens (UI state only; no pointers/hooks)

# Main generated form: version/controller handoff + persistent Unit Changer UI memory.
ui=Path('FinalV18MainForm.cs')
s=ui.read_text(encoding='utf-8')
for old,new in [
    ('Text="BRZE Trainer — V34 Diagnostics";','Text="BRZE Trainer — V35 Diagnostics";'),
    ('Text="BRZE Trainer — V34 Clean";','Text="BRZE Trainer — V35 Clean";'),
    ('V34  ·  FINAL CANDIDATE + HARD BRZE REBIND  ·  INTEGRATED','V35  ·  FINAL RC + 0.1 OFFSET + MOVABLE OVERLAY + UC MEMORY  ·  INTEGRATED'),
    ('OverlayHotkeyControllerV34.Attach(this);','OverlayHotkeyControllerV35.Attach(this);')
]:
    if old not in s:
        raise SystemExit('V35 main marker missing: '+old)
    s=s.replace(old,new,1)

# Persistent Unit Changer state lives under LOCALAPPDATA and stores ONLY UI choices.
# No BRZE addresses, process IDs, handles, caves or transient runtime data are persisted.
class_marker='internal sealed class MainForm : Form\n{'
persist_model=r'''internal sealed class MainForm : Form
{
    sealed class UnitChangerSavedState
    {
        public int Profile { get; set; }
        public bool[][] Enabled { get; set; }=Array.Empty<bool[]>();
        public uint[][] Outputs { get; set; }=Array.Empty<uint[]>();
    }

    static readonly string UnitChangerMemoryPath=System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BRZE-Trainer","unit-changer-v1.json");
    bool unitChangerMemoryLoading;
'''
if class_marker not in s:
    raise SystemExit('V35 MainForm class marker missing')
s=s.replace(class_marker,persist_model,1)

# Load after all Unit Changer controls and profile arrays are initialized.
old='SelectProfile(0);SetUnitFilter(-1);return box;'
new='SelectProfile(0);LoadUnitChangerMemory();SetUnitFilter(-1);return box;'
if old not in s:
    raise SystemExit('V35 Unit Changer initial profile marker missing')
s=s.replace(old,new,1)

# Persist user-selected building profile. Programmatic SelectProfile() calls are protected
# by loadingProfile and therefore cannot overwrite the saved state during restore.
old='profileSelect.SelectedIndexChanged+=(_,_)=>{if(!loadingProfile&&profileSelect.SelectedIndex>=0)SelectProfile(profileSelect.SelectedIndex);};'
new='profileSelect.SelectedIndexChanged+=(_,_)=>{if(!loadingProfile&&profileSelect.SelectedIndex>=0){SelectProfile(profileSelect.SelectedIndex);SaveUnitChangerMemory();}};'
if old not in s:
    raise SystemExit('V35 profile selection persistence marker missing')
s=s.replace(old,new,1)

# Save output-unit edits and slot enable/disable immediately, so even an abrupt X/close
# leaves the last completed UI change on disk.
old='profileOut[editProfile,i]=u.Type;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;\n    }\n\n    void SlotToggleChanged(int i)'
new='profileOut[editProfile,i]=u.Type;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;SaveUnitChangerMemory();\n    }\n\n    void SlotToggleChanged(int i)'
if old not in s:
    raise SystemExit('V35 UnitSelectionChanged persistence marker missing')
s=s.replace(old,new,1)

old='profileOn[editProfile,i]=slotOn[i].Checked;profileOut[editProfile,i]=u.Type;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;\n    }\n\n    void ActivateAllSlots()'
new='profileOn[editProfile,i]=slotOn[i].Checked;profileOut[editProfile,i]=u.Type;UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;SaveUnitChangerMemory();\n    }\n\n    void ActivateAllSlots()'
if old not in s:
    raise SystemExit('V35 SlotToggleChanged persistence marker missing')
s=s.replace(old,new,1)

old='UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 ON");\n    }'
new='UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 ON");SaveUnitChangerMemory();\n    }'
if old not in s:
    raise SystemExit('V35 ActivateAllSlots persistence marker missing')
s=s.replace(old,new,1)

old='UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 OFF");\n    }'
new='UpdateUnitLabels();nextStatusUtc=DateTime.MinValue;var p=Profiles[editProfile];SetNotice($"UNIT CHANGER — {p.Clan} / {p.Name}: S1–S9 OFF");SaveUnitChangerMemory();\n    }'
if old not in s:
    raise SystemExit('V35 DeactivateAllSlots persistence marker missing')
s=s.replace(old,new,1)

# Add save/load helpers just before UpdateUnitLabels(). JSON uses jagged arrays because
# System.Text.Json does not serialize multidimensional arrays directly.
marker='    void UpdateUnitLabels()\n'
memory_helpers=r'''    void SaveUnitChangerMemory()
    {
        if(unitChangerMemoryLoading)return;
        try
        {
            var st=new UnitChangerSavedState
            {
                Profile=Math.Clamp(editProfile,0,Profiles.Length-1),
                Enabled=new bool[Profiles.Length][],
                Outputs=new uint[Profiles.Length][]
            };
            for(int p=0;p<Profiles.Length;p++)
            {
                st.Enabled[p]=new bool[9];
                st.Outputs[p]=new uint[9];
                for(int i=0;i<9;i++){st.Enabled[p][i]=profileOn[p,i];st.Outputs[p][i]=profileOut[p,i];}
            }
            string? dir=System.IO.Path.GetDirectoryName(UnitChangerMemoryPath);
            if(!string.IsNullOrWhiteSpace(dir))System.IO.Directory.CreateDirectory(dir);
            string tmp=UnitChangerMemoryPath+".tmp";
            System.IO.File.WriteAllText(tmp,System.Text.Json.JsonSerializer.Serialize(st));
            System.IO.File.Move(tmp,UnitChangerMemoryPath,true);
        }
        catch
        {
            // Persistence must never interfere with gameplay/trainer runtime.
        }
    }

    void LoadUnitChangerMemory()
    {
        if(unitChangerMemoryLoading)return;
        unitChangerMemoryLoading=true;
        try
        {
            if(!System.IO.File.Exists(UnitChangerMemoryPath))return;
            var st=System.Text.Json.JsonSerializer.Deserialize<UnitChangerSavedState>(
                System.IO.File.ReadAllText(UnitChangerMemoryPath));
            if(st==null)return;
            int pc=Math.Min(Profiles.Length,Math.Min(st.Enabled?.Length??0,st.Outputs?.Length??0));
            for(int p=0;p<pc;p++)
            {
                var en=st.Enabled[p];var outs=st.Outputs[p];
                if(en==null||outs==null)continue;
                int n=Math.Min(9,Math.Min(en.Length,outs.Length));
                for(int i=0;i<n;i++)
                {
                    uint type=outs[i];
                    bool valid=Units.Any(u=>!u.Header&&u.Type==type);
                    profileOn[p,i]=en[i];
                    if(valid)profileOut[p,i]=type;
                }
            }
            SelectProfile(Math.Clamp(st.Profile,0,Profiles.Length-1));
        }
        catch
        {
            // Corrupt/old settings fall back to built-in defaults without blocking trainer start.
        }
        finally{unitChangerMemoryLoading=false;}
    }

'''
if marker not in s:
    raise SystemExit('V35 Unit Changer memory helper insertion marker missing')
s=s.replace(marker,memory_helpers+marker,1)

# Last-chance save on normal close too. Immediate event saves above cover abrupt user close.
old='FormClosed+=(_,_)=>StopAll();'
new='FormClosed+=(_,_)=>{SaveUnitChangerMemory();StopAll();};'
if old not in s:
    raise SystemExit('V35 FormClosed persistence marker missing')
s=s.replace(old,new,1)

ui.write_text(s,encoding='utf-8')

# Compile a UI-only copy of the V30 feature panels. The original proven V30 panel
# source remains byte-identical in the repo/build workspace.
panel_src=Path('IntegratedFeaturePanelsV30.cs').read_text(encoding='utf-8')
old='readonly NumericUpDown offset=new(){Minimum=2,Maximum=40,Increment=1,DecimalPlaces=1,Value=8,Width=74,Height=30,BackColor=Color.FromArgb(32,40,52),ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};'
new='readonly NumericUpDown offset=new(){Minimum=0.1m,Maximum=64m,Increment=0.1m,DecimalPlaces=1,Value=0.1m,Width=74,Height=30,BackColor=Color.FromArgb(32,40,52),ForeColor=Color.White,TextAlign=HorizontalAlignment.Center};'
if old not in panel_src:
    raise SystemExit('V35 main COPY offset marker missing')
panel_src=panel_src.replace(old,new,1)
Path('IntegratedFeaturePanelsV35.cs').write_text(panel_src,encoding='utf-8')

# V34 already contains the 91% premium overlay, 0.1 overlay COPY controls, Unit Changer,
# global Alt+W, and hard BRZE rebind. Layer draggable-header behavior on that exact source.
ov=Path('OverlayHotkeyControllerV34.cs').read_text(encoding='utf-8')
ov=ov.replace('V34','V35')
ov=ov.replace('const int HOTKEY_ID=0x3477;','const int HOTKEY_ID=0x3577;',1)

# Default/reset COPY offset is 0.1 in-game too.
old='float copyOffset=8f;'
new='float copyOffset=0.1f;'
if old not in ov:
    raise SystemExit('V35 overlay default COPY offset marker missing')
ov=ov.replace(old,new,1)

old='var om=FlatButton("−0.1",46,Off);var op=FlatButton("+0.1",46,Off);var orst=FlatButton("8.0",48,Off);'
new='var om=FlatButton("−0.1",46,Off);var op=FlatButton("+0.1",46,Off);var orst=FlatButton("0.1",48,Off);'
if old not in ov:
    raise SystemExit('V35 overlay COPY reset button marker missing')
ov=ov.replace(old,new,1)

old='orst.Click+=(_,_)=>{copyOffset=8f;UpdateOffset();};'
new='orst.Click+=(_,_)=>{copyOffset=0.1f;UpdateOffset();};'
if old not in ov:
    raise SystemExit('V35 overlay COPY reset handler marker missing')
ov=ov.replace(old,new,1)

# Preserve user-chosen overlay position across hide/show. Initial show still docks top-right.
old='''        var game=GameWindow();
        var b=OverlayBounds(overlay,game);
        overlay.Location=b.Location;
        overlay.SyncNow();'''
new='''        var game=GameWindow();
        var b=overlay.UserMoved?new Rectangle(overlay.Location,overlay.Size):OverlayBounds(overlay,game);
        overlay.Location=b.Location;
        overlay.SyncNow();'''
if old not in ov:
    raise SystemExit('V35 overlay Show position marker missing')
ov=ov.replace(old,new,1)

# Add drag state beside existing overlay state fields.
old='''    bool ucBridgeReady,ucSyncing;

    static readonly Color Bg='''
new='''    bool ucBridgeReady,ucSyncing;
    bool dragging;
    Point dragCursorStart,dragWindowStart;
    Control? dragCapture;
    public bool UserMoved { get; private set; }

    static readonly Color Bg='''
if old not in ov:
    raise SystemExit('V35 drag state insertion marker missing')
ov=ov.replace(old,new,1)

# Insert focus-safe manual drag helpers before rounded-region drawing.
marker='''    void UpdateRoundedRegion()
    {'''
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
old='''        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        head.Controls.Add(new Label{Text="BATTLE REALMS  //  LIVE CONTROL DECK",Location=new Point(0,2),Size=new Size(470,29),ForeColor=TextColor,Font=new Font("Segoe UI Semibold",15.2f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft});'''
new='''        var head=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent};
        var dragTitle=new Label{Text="BATTLE REALMS  //  LIVE CONTROL DECK",Location=new Point(0,2),Size=new Size(470,29),ForeColor=TextColor,Font=new Font("Segoe UI Semibold",15.2f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};
        EnableHeaderDrag(head);EnableHeaderDrag(dragTitle);head.Controls.Add(dragTitle);'''
if old not in ov:
    raise SystemExit('V35 header title marker missing')
ov=ov.replace(old,new,1)

old='''        head.Controls.Add(new Label{Text=$"V35  ·  {mode}  ·  91% GLASS  ·  GAME FOCUS LOCKED",Location=new Point(2,34),Size=new Size(490,20),ForeColor=Dim,Font=new Font("Segoe UI",8.4f),TextAlign=ContentAlignment.MiddleLeft});'''
new='''        var dragSub=new Label{Text=$"V35  ·  {mode}  ·  91% GLASS  ·  DRAG HEADER  ·  FOCUS LOCKED",Location=new Point(2,34),Size=new Size(490,20),ForeColor=Dim,Font=new Font("Segoe UI",8.4f),TextAlign=ContentAlignment.MiddleLeft};
        EnableHeaderDrag(dragSub);head.Controls.Add(dragSub);'''
if old not in ov:
    raise SystemExit('V35 header subtitle marker missing')
ov=ov.replace(old,new,1)

Path('OverlayHotkeyControllerV35.cs').write_text(ov,encoding='utf-8')
print('V35 generated: COPY default 0.1 in both UIs + draggable 91% overlay + persistent Unit Changer UI memory')
