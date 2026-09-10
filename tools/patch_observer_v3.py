from pathlib import Path

p = Path('WeModHorseStaminaDiff.cs')
s = p.read_text(encoding='utf-8')

def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('MISSING EXPECTED FRAGMENT:\n' + old)
    s = s.replace(old, new, 1)

rep('readonly Button off1 = new(){ Text="1) Capture OFF baseline", Width=220, Height=38 };',
    'readonly Button off1 = new(){ Text="1) CAPTURE OFF BASELINE", Width=420, Height=44 };')
rep('readonly Button on = new(){ Text="2) Capture ON", Width=220, Height=38 };',
    'readonly Button on = new(){ Text="2) CAPTURE WEMOD ON + SAVE PARTIAL TXT", Width=420, Height=44 };')
rep('readonly Button off2 = new(){ Text="3) Capture OFF again + Analyze", Width=260, Height=38 };',
    'readonly Button off2 = new(){ Text="3) ANALYZE + SAVE FINAL TXT", Width=420, Height=48 };')
rep('readonly Label note = new(){ AutoSize=true, MaximumSize=new System.Drawing.Size(1080,0), Text="READ-ONLY. Test ONE WeMod cheat at a time. Horse: keep the same Stable selected. Stamina: keep the same unit selected. OFF baseline -> enable only that cheat -> ON -> disable it -> OFF again + Analyze." };',
    'readonly Label note = new(){ AutoSize=true, MaximumSize=new System.Drawing.Size(980,0), Text="READ-ONLY. Test ONE cheat at a time. HORSE: keep the same Stable selected. Step 1 with WeMod cheat OFF -> enable ONLY that cheat -> Step 2 -> disable it -> wait ~1 second -> Step 3. Step 2 already saves a PARTIAL TXT; Step 3 saves the FINAL reversible diff TXT to Desktop." };')
rep('Text="BRZE 1.60 — WeMod Horse/Stamina Forensic Observer v2 (READ ONLY)"; Width=1180; Height=760; StartPosition=FormStartPosition.CenterScreen;\n        mode.Items.AddRange(new object[]{"HORSE","STAMINA"}); mode.SelectedIndex=0;\n        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=110,Padding=new Padding(10),WrapContents=true};\n        top.Controls.Add(new Label{Text="Test:",AutoSize=true,Padding=new Padding(0,10,0,0)}); top.Controls.Add(mode); top.Controls.Add(off1); top.Controls.Add(on); top.Controls.Add(off2); top.Controls.Add(note);',
    'Text="BRZE 1.60 — WeMod Forensic Observer v3 — BIG BUTTONS (READ ONLY)"; Width=1080; Height=820; StartPosition=FormStartPosition.CenterScreen; MinimumSize=new System.Drawing.Size(900,700);\n        mode.Items.AddRange(new object[]{"HORSE","STAMINA"}); mode.SelectedIndex=0;\n        var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=255,Padding=new Padding(12),WrapContents=false,FlowDirection=FlowDirection.TopDown,AutoScroll=true};\n        var modeRow=new FlowLayoutPanel{AutoSize=true,Width=1000,Height=36,WrapContents=false};\n        modeRow.Controls.Add(new Label{Text="TEST MODE:",AutoSize=true,Padding=new Padding(0,8,8,0),Font=new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif,10f,System.Drawing.FontStyle.Bold)}); modeRow.Controls.Add(mode);\n        top.Controls.Add(modeRow); top.Controls.Add(off1); top.Controls.Add(on); top.Controls.Add(off2); top.Controls.Add(note);')

# Step 2 now saves a partial diff too, so user always gets a TXT after enabling the cheat.
rep('sb.AppendLine("Now disable the SAME WeMod cheat, wait ~1 second, then click Step 3. Step 3 filters reversible cheat patches.");return sb.ToString();',
    'sb.AppendLine("Now disable the SAME WeMod cheat, wait ~1 second, then click Step 3. Step 3 filters reversible cheat patches."); string text=sb.ToString(); string path=Save(mode+"-PARTIAL",text); return text+"\\r\\nPARTIAL TXT SAVED: "+path;')

p.write_text(s, encoding='utf-8')
print('Observer v3 UI patch applied')
