from pathlib import Path

src = Path('Program.cs').read_text(encoding='utf-8')

def rep(old: str, new: str):
    global src
    if old not in src:
        raise SystemExit(f'MISSING EXPECTED SOURCE FRAGMENT:\n{old}')
    src = src.replace(old, new, 1)

rep('readonly CheckBox f5 = new() { Text = "F5 No Stamina Loss (selected only) — SAFE DELTA V62", AutoSize = true };',
    'readonly CheckBox f5 = new() { Text = "F5 Unlimited Stamina (selected only) — WAND READ-SITE", AutoSize = true };')
rep('readonly CheckBox f6 = new() { Text = "F6 No Damage (selected only) — SAFE DELTA V62", AutoSize = true };',
    'readonly CheckBox f6 = new() { Text = "F6 Unlimited HP (selected only) — HARD LOCK", AutoSize = true };')
rep('readonly CheckBox horses = new() { Text = "Maximum Horses / instant horse respawn", AutoSize = true };',
    'readonly CheckBox horses = new() { Text = "Unlimited Horses — Wand 6-slot Stable port", AutoSize = true };\n    readonly CheckBox fastPeasant = new() { Text = "Fast Peasant Spawn — FIXED 3.0 seconds (local player)", AutoSize = true };\n    readonly Button instantDeathBurst = new() { Text = "Instant Death BURST / ERASER — OFF", AutoSize = true };\n    readonly Button instantDeathSingle = new() { Text = "Instant Death SINGLE — kill hovered enemy now (PageDown)", AutoSize = true };\n    readonly CheckBox revealMap = new() { Text = "Reveal Map — disable Fog of War", AutoSize = true };\n    readonly CheckBox unlimitedWolves = new() { Text = "Unlimited Wolves V8 — select Wolves Den once (F10)", AutoSize = true };\n    bool deathBurstEnabled;')
rep('readonly Label status = new() { AutoSize = false, Height = 54, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft };',
    'readonly Label status = new() { AutoSize = false, Height = 245, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(FontFamily.GenericMonospace, 8.5f) };')
rep('Text = "BRZE Trainer 1.60 — Hook Test"; ClientSize = new Size(650, 520); StartPosition = FormStartPosition.CenterScreen;',
    'Text = "BRZE Trainer 1.60 — Death Modes + Reveal Map + Wolves Den Stock"; ClientSize = new Size(1000, 770); StartPosition = FormStartPosition.CenterScreen;')

rep('panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6, f7, legacyTower, pausePeasant, demolish, horses });',
    'panel.Controls.AddRange(new Control[] { f1, f2, f3, f4, f5, f6, f7, pausePeasant, horses, fastPeasant, instantDeathBurst, instantDeathSingle, revealMap, unlimitedWolves });')
rep('panel.Controls.Add(new Label { Text = "F10 Maximum Wolves — old trainer exact +0x250 remap", AutoSize = true });',
    'panel.Controls.Add(new Label { Text = "F10 = toggle Wolves Den stock 250 | PageDown = SINGLE instant death", AutoSize = true });')
rep('panel.Controls.Add(new Label { Text = "Delete = Instant Build/Repair/Research/BattleGear | PageDown = Instant Death", AutoSize = true });',
    'panel.Controls.Add(new Label { Text = "Delete = Instant Build/Repair/Research/BattleGear | BURST button = cursor eraser", AutoSize = true });')
rep('Native.Start(); timer.Tick += (_, _) => TickTrainer(); timer.Start(); FormClosed += (_, _) => Native.Stop();',
    'instantDeathBurst.Click += (_, _) => SetDeathBurst(!deathBurstEnabled); instantDeathSingle.Click += (_, _) => FireSingleDeath();\n        Native.Start(); timer.Tick += (_, _) => TickTrainer(); timer.Start(); FormClosed += (_, _) => { WolfCore.Stop(); RevealMapCore.Stop(); InstantDeathCore.Stop(); StaminaCore.Stop(); HorseCore.Stop(); HookCore.Stop(); SelectionCore.Stop(); Native.Stop(); };')

rep('    void SetAllImplemented(bool e) { f1.Checked=e; f2.Checked=e; f3.Checked=e; f4.Checked=e; f5.Checked=e; f6.Checked=e; f7.Checked=e; }',
'''    void SetAllImplemented(bool e) { f1.Checked=e; f2.Checked=e; f3.Checked=e; f4.Checked=e; f5.Checked=e; f6.Checked=e; f7.Checked=e; }
    void SetDeathBurst(bool enabled)
    {
        deathBurstEnabled=enabled;
        instantDeathBurst.Text=enabled ? "Instant Death BURST / ERASER — ON" : "Instant Death BURST / ERASER — OFF";
    }
    void FireSingleDeath()
    {
        SetDeathBurst(false);
        InstantDeathCore.TriggerSingle();
    }''')

rep('Toggle(0x22,12,()=>Native.InstantDeathSelected());',
    'Toggle(0x22,12,()=>FireSingleDeath());')
rep('        Toggle(0x7A,14,()=>demolish.Checked=!demolish.Checked);\n', '')
rep('Toggle(0x79,10,()=>Native.MaxWolves());',
    'Toggle(0x79,10,()=>unlimitedWolves.Checked=!unlimitedWolves.Checked);')

rep('Native.SetHooks(f5.Checked, f6.Checked, f7.Checked);',
    'string hookStatus = HookCore.Tick(false, f6.Checked, f7.Checked);\n        string staminaStatus = StaminaCore.Tick(f5.Checked);')
rep('Native.ApplyLegacyRuntime(pausePeasant.Checked,demolish.Checked,horses.Checked,legacyTower.Checked);',
    'Native.ApplyLegacyRuntime(pausePeasant.Checked,false,false,false);\n        string horseStatus = HorseCore.Tick(horses.Checked);\n        string deathStatus = InstantDeathCore.Tick(deathBurstEnabled);\n        string mapStatus = RevealMapCore.Tick(revealMap.Checked);\n        string wolfStatus = WolfCore.Tick(unlimitedWolves.Checked);')
rep('status.Text = Native.Apply(f1.Checked,f2.Checked,f3.Checked,f4.Checked,f7.Checked);',
    'string selectionStatus = SelectionCore.Tick(fastPeasant.Checked);\n        status.Text = Native.Apply(f1.Checked,f2.Checked,f3.Checked,f4.Checked,f7.Checked) + "\\r\\n" + hookStatus + "\\r\\n" + staminaStatus + "\\r\\n" + horseStatus + "\\r\\n" + deathStatus + "\\r\\n" + mapStatus + "\\r\\n" + wolfStatus + "\\r\\n" + selectionStatus;')

rep('static int selectedLocked,trainingBuildings; static IntPtr cave=IntPtr.Zero; static long hpFlag,stFlag,trainFlag; static bool hooksInstalled; static uint horseOriginal; static bool horseSaved; static int remoteTrainState=-1; static string hookError="";',
    'static int selectedLocked,trainingBuildings; static IntPtr cave=IntPtr.Zero; static long hpFlag,stFlag,trainFlag; static bool hooksInstalled; static uint horseOriginal; static bool horseSaved; static int remoteTrainState=-1; static string hookError="";\n    static bool maxPopSaved; static uint originalMaxPop,originalMaxPopPlayer=0xFFFFFFFF;')
rep('h=IntPtr.Zero;p=null;cave=IntPtr.Zero;hooksInstalled=false;localUnits=Array.Empty<UnitInfo>();remoteTrainState=-1;hookError="";',
    'h=IntPtr.Zero;p=null;cave=IntPtr.Zero;hooksInstalled=false;localUnits=Array.Empty<UnitInfo>();remoteTrainState=-1;hookError="";maxPopSaved=false;originalMaxPop=0;originalMaxPopPlayer=0xFFFFFFFF;')

marker = '    public static string Apply(bool rice,bool water,bool yinYang,bool pop,bool instantTrain)\n'
if marker not in src:
    raise SystemExit('MISSING Apply marker')
method = '''    static void ApplyMaxPopulation(uint lid,bool enabled)
    {
        long a=moduleBase+RVA_MAX_UNITS+(long)lid*4;
        uint cur=R32(a);
        if((!maxPopSaved||originalMaxPopPlayer!=lid)&&cur!=0&&cur!=9_999_999u)
        {
            originalMaxPop=cur;originalMaxPopPlayer=lid;maxPopSaved=true;
        }
        if(enabled)W32(a,9_999_999u);
        else if(maxPopSaved&&originalMaxPopPlayer==lid)W32(a,originalMaxPop);
    }

'''
src = src.replace(marker, method + marker, 1)
rep('if(rice)W32(player+OFF_RICE,50000);if(water)W32(player+OFF_WATER,50000);if(yinYang){W32(player+OFF_YIN,10);W32(player+OFF_YANG,10);}if(pop)W32(moduleBase+RVA_MAX_UNITS+lid*4,9_999_999);',
    'if(rice)W32(player+OFF_RICE,50000);if(water)W32(player+OFF_WATER,50000);if(yinYang){W32(player+OFF_YIN,10);W32(player+OFF_YANG,10);}ApplyMaxPopulation(lid,pop);')

Path('MergedProgram.cs').write_text(src, encoding='utf-8')
print('MergedProgram.cs V8 generated OK')
