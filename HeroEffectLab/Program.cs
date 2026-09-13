using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BRZEHeroEffectLab;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class GearChoice
{
    public int Slot { get; init; }
    public uint BattleGear { get; init; }
    public uint RootAbility { get; init; }
    public uint TargetAbility { get; init; }
    public uint ApplicationOfEffect { get; init; }
    public uint ProximityEffect { get; init; }
    public bool CanApply => TargetAbility != uint.MaxValue && TargetAbility != 0xFFFFu;
    public override string ToString() => $"BG{Slot} gear:0x{BattleGear:X} root:0x{RootAbility:X} target:0x{TargetAbility:X} appEffect:0x{ApplicationOfEffect:X} prox:{ProximityEffect}";
}

internal sealed class SourceCapture
{
    public bool Ok { get; init; }
    public uint Unit { get; init; }
    public uint UnitType { get; init; }
    public string Message { get; init; } = "";
    public System.Collections.Generic.List<GearChoice> Choices { get; init; } = new();
}

internal sealed class MainForm : Form
{
    readonly ComboBox grayChoice = new();
    readonly ComboBox issylChoice = new();
    readonly Label grayInfo = new();
    readonly Label issylInfo = new();
    readonly Label game = new();
    readonly Label queue = new();
    readonly Label last = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };

    public MainForm()
    {
        Text = "BRZE Hero Effect Lab — Grayback + Issyl Native Target Ability";
        ClientSize = new Size(1120, 650);
        MinimumSize = new Size(1050, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(9, 13, 20);
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 172));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 172));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var head = Card();
        head.Controls.Add(new Label { Text = "HERO EFFECT LAB // NATIVE TARGET ABILITY", Font = new Font("Segoe UI Semibold", 18f), AutoSize = true, Location = new Point(16, 9) });
        head.Controls.Add(new Label { Text = "Capture the hero's real BattleGear chain, then apply its target-side ability to selected units. No raw speed/damage writes.", ForeColor = Color.FromArgb(160, 180, 204), AutoSize = true, Location = new Point(18, 47) });
        root.Controls.Add(head, 0, 0);

        var g = Card(); g.Controls.Add(Title("GRAYBACK // WOLF'S HOWL", 10));
        var captureGray = Btn("CAPTURE SELECTED GRAYBACK", 18, 40, 235); var applyGray = Btn("APPLY GRAYBACK TO SELECTED", 270, 40, 250);
        SetupCombo(grayChoice, 18, 84); SetupInfo(grayInfo, 18, 120); g.Controls.Add(captureGray); g.Controls.Add(applyGray); g.Controls.Add(grayChoice); g.Controls.Add(grayInfo); root.Controls.Add(g, 0, 1);

        var i = Card(); i.Controls.Add(Title("ISSYL // HASTE", 10));
        var captureIssyl = Btn("CAPTURE SELECTED ISSYL", 18, 40, 235); var applyIssyl = Btn("APPLY ISSYL TO SELECTED", 270, 40, 250);
        SetupCombo(issylChoice, 18, 84); SetupInfo(issylInfo, 18, 120); i.Controls.Add(captureIssyl); i.Controls.Add(applyIssyl); i.Controls.Add(issylChoice); i.Controls.Add(issylInfo); root.Controls.Add(i, 0, 2);

        var actions = Card(); var both = Btn("APPLY BOTH TO SELECTED", 18, 27, 235); var refresh = Btn("RESET / REATTACH LAB", 270, 27, 210);
        actions.Controls.Add(both); actions.Controls.Add(refresh); actions.Controls.Add(new Label { Text = "Close the main trainer / Clone Lab while this experimental lab is armed.", ForeColor = Color.FromArgb(236, 183, 84), AutoSize = true, Location = new Point(510, 36) }); root.Controls.Add(actions, 0, 3);

        var status = Card(); status.Controls.Add(Title("RUNTIME MONITOR", 10)); game.Location = new Point(18, 38); game.AutoSize = true; game.ForeColor = Color.FromArgb(97, 224, 179); queue.Location = new Point(18, 65); queue.AutoSize = true; last.Location = new Point(18, 92); last.Size = new Size(1035, 78); last.AutoSize = false; last.ForeColor = Color.FromArgb(165, 182, 202); status.Controls.Add(game); status.Controls.Add(queue); status.Controls.Add(last); root.Controls.Add(status, 0, 4);

        captureGray.Click += (_, _) => Capture(grayChoice, grayInfo, "GRAYBACK", new uint[] { 89, 136, 137, 138 });
        captureIssyl.Click += (_, _) => Capture(issylChoice, issylInfo, "ISSYL", new uint[] { 90 });
        applyGray.Click += (_, _) => ApplyOne(grayChoice, "GRAYBACK");
        applyIssyl.Click += (_, _) => ApplyOne(issylChoice, "ISSYL");
        both.Click += (_, _) => ApplyBoth();
        refresh.Click += (_, _) => { EffectCore.ResetRuntime(); last.Text = "Runtime detached; captured IDs remain selected in the UI."; };
        timer.Tick += (_, _) => { var s = EffectCore.Snapshot(); game.Text = s.Game; queue.Text = s.Queue; };
        timer.Start(); FormClosed += (_, _) => EffectCore.ResetRuntime();
    }

    void Capture(ComboBox box, Label info, string label, uint[] expected)
    {
        var r = EffectCore.CaptureSelectedSource(label, expected); box.Items.Clear(); foreach (var c in r.Choices) box.Items.Add(c);
        int first = r.Choices.FindIndex(x => x.CanApply); if (box.Items.Count > 0) box.SelectedIndex = first >= 0 ? first : 0; info.Text = r.Message; last.Text = r.Message;
    }
    void ApplyOne(ComboBox box, string label)
    {
        if (box.SelectedItem is not GearChoice c || !c.CanApply) { last.Text = $"{label}: capture a source hero and choose a valid target ability first."; return; }
        last.Text = EffectCore.QueueSelected(c.TargetAbility, uint.MaxValue, label);
    }
    void ApplyBoth()
    {
        if (grayChoice.SelectedItem is not GearChoice g || !g.CanApply || issylChoice.SelectedItem is not GearChoice i || !i.CanApply) { last.Text = "APPLY BOTH needs valid captured Grayback and Issyl entries first."; return; }
        last.Text = EffectCore.QueueSelected(g.TargetAbility, i.TargetAbility, "GRAYBACK + ISSYL");
    }

    static void SetupCombo(ComboBox c, int x, int y) { c.DropDownStyle = ComboBoxStyle.DropDownList; c.Location = new Point(x, y); c.Size = new Size(1035, 28); }
    static void SetupInfo(Label l, int x, int y) { l.Location = new Point(x, y); l.Size = new Size(1035, 40); l.AutoSize = false; l.ForeColor = Color.FromArgb(165, 182, 202); }
    static Panel Card() => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 24, 34), Padding = new Padding(10), Margin = new Padding(0, 0, 0, 8) };
    static Label Title(string text, int y) => new() { Text = text, Font = new Font("Segoe UI Semibold", 11f), ForeColor = Color.FromArgb(97, 224, 179), AutoSize = true, Location = new Point(16, y) };
    static Button Btn(string text, int x, int y, int w) { var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(38, 52, 69), ForeColor = Color.White }; b.FlatAppearance.BorderColor = Color.FromArgb(70, 92, 118); return b; }
}

internal readonly record struct RuntimeSnapshot(string Game, string Queue);
