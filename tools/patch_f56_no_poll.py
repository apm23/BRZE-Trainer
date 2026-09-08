from pathlib import Path
p=Path('Program.cs')
s=p.read_text(encoding='utf-8')
old='''    public static void Start(){ if(running)return; running=true; topupThread=new Thread(TopupLoop){IsBackground=true,Name="BRZE-Selected-Topup"}; topupThread.Start(); }
    public static void Stop(){ running=false; try{topupThread?.Join(300);}catch{} Detach(); }'''
new='''    // Old trainer PageUp did its HP/stamina work in injected game-side code instead of
    // repeatedly walking every selected unit through ReadProcessMemory/WriteProcessMemory.
    // Our central delta hooks are already event-driven, so external top-up polling is removed.
    public static void Start(){ running=true; }
    public static void Stop(){ running=false; Detach(); }'''
if old not in s:
    raise SystemExit('Start/Stop pattern not found')
s=s.replace(old,new)
a=s.index('    static void TopupLoop()')
b=s.index('    public static void InstantSelectedBuilding()',a)
s=s[:a]+'''    // Intentionally no external selected-unit HP/stamina walker.\n    // F5/F6 now cost work only when the game applies stamina/health deltas.\n\n'''+s[b:]
s=s.replace('F5 No Stamina Consumption (selected only) — HOOK TEST','F5 No Stamina Consumption (selected only) — EVENT HOOK')
s=s.replace('F6 No Damage (selected only) — HOOK TEST','F6 No Damage (selected only) — EVENT HOOK')
p.write_text(s,encoding='utf-8')
