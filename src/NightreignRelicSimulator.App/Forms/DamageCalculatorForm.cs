using NightreignRelicSimulator.App.Ui;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// 火力計算画面です。最終火力 = 武器表示火力 × 全倍率。
/// </summary>
public sealed class DamageCalculatorForm : Form
{
    private readonly NumericUpDown _weaponAttackBox = UiFactory.CreateNumeric(0, 999999);
    private readonly ComboBox _buildBox = UiFactory.CreateComboBox(320);
    private readonly Label _baseAttackLabel = new();
    private readonly Label _totalMultiplierLabel = new();
    private readonly Label _finalAttackLabel = new();
    private readonly Label _effectCountLabel = UiFactory.CreateMutedLabel("適用 0 / 無効 0");
    private readonly ListBox _appliedList = new();
    private readonly ListBox _ignoredList = new();
    private readonly TextBox _logBox = new();

    private List<Build> _builds = [];

    public DamageCalculatorForm()
    {
        Text = "火力計算";
        UiFactory.ApplyFormChrome(this);

        _weaponAttackBox.Value = ClampAttack(UiSessionState.WeaponAttack);
        _weaponAttackBox.ValueChanged += (_, _) => UiSessionState.WeaponAttack = _weaponAttackBox.Value;

        var toolbar = UiFactory.CreateToolbar();
        toolbar.Height = 56;
        toolbar.Controls.Add(UiFactory.CreateMutedLabel("武器表示火力"));
        toolbar.Controls.Add(_weaponAttackBox);
        toolbar.Controls.Add(UiFactory.CreateMutedLabel("ビルド"));
        toolbar.Controls.Add(_buildBox);
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("計算", CalculateAsync, this, 100, primary: true));
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("ビルド再読込", LoadBuildsAsync, this, 120));

        var summary = BuildSummaryPanel();

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280,
            BackColor = UiTheme.Border
        };

        var lists = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 480,
            BackColor = UiTheme.Border
        };
        lists.Panel1.Controls.Add(CreateListPanel("適用された Effect", _appliedList));
        lists.Panel2.Controls.Add(CreateListPanel("適用されなかった Effect", _ignoredList));

        StyleListBox(_appliedList);
        StyleListBox(_ignoredList);
        StyleLogBox(_logBox);

        split.Panel1.Controls.Add(lists);
        split.Panel2.Controls.Add(CreateListPanel("計算ログ", _logBox));

        Controls.Add(split);
        Controls.Add(summary);
        Controls.Add(toolbar);

        Shown += async (_, _) => await UiHelper.RunAsync(LoadBuildsAsync, this);
        FormClosing += (_, _) =>
        {
            UiSessionState.WeaponAttack = _weaponAttackBox.Value;
            if (_buildBox.SelectedValue is int buildId)
            {
                UiSessionState.SelectedBuildId = buildId;
            }
        };
    }

    private Panel BuildSummaryPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20, 16, 20, 12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(UiFactory.CreateMutedLabel("武器表示火力"), 0, 0);
        layout.Controls.Add(UiFactory.CreateMutedLabel("総倍率"), 1, 0);
        layout.Controls.Add(UiFactory.CreateMutedLabel("最終火力"), 2, 0);

        StyleResultLabel(_baseAttackLabel, "-");
        StyleResultLabel(_totalMultiplierLabel, "-");
        StyleResultLabel(_finalAttackLabel, "-");
        _finalAttackLabel.ForeColor = UiTheme.Accent;

        layout.Controls.Add(_baseAttackLabel, 0, 1);
        layout.Controls.Add(_totalMultiplierLabel, 1, 1);
        layout.Controls.Add(_finalAttackLabel, 2, 1);

        _effectCountLabel.Dock = DockStyle.Bottom;
        panel.Controls.Add(layout);
        panel.Controls.Add(_effectCountLabel);
        return panel;
    }

    private static void StyleResultLabel(Label label, string text)
    {
        label.Text = text;
        label.Dock = DockStyle.Fill;
        label.Font = UiTheme.ResultFont;
        label.ForeColor = UiTheme.TextPrimary;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static Panel CreateListPanel(string title, Control content)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            Padding = new Padding(12)
        };
        var label = UiFactory.CreateHeading(title);
        label.Dock = DockStyle.Top;
        label.Height = 28;
        content.Dock = DockStyle.Fill;
        panel.Controls.Add(content);
        panel.Controls.Add(label);
        return panel;
    }

    private static void StyleListBox(ListBox list)
    {
        list.BackColor = UiTheme.SurfaceAlt;
        list.ForeColor = UiTheme.TextPrimary;
        list.BorderStyle = BorderStyle.None;
        list.Font = UiTheme.BodyFont;
        list.IntegralHeight = false;
    }

    private static void StyleLogBox(TextBox box)
    {
        box.Multiline = true;
        box.ReadOnly = true;
        box.ScrollBars = ScrollBars.Vertical;
        box.BackColor = UiTheme.SurfaceAlt;
        box.ForeColor = UiTheme.TextPrimary;
        box.BorderStyle = BorderStyle.None;
        box.Font = UiTheme.MonoFont;
    }

    private async Task LoadBuildsAsync()
    {
        _builds = (await AppServices.Builds.GetAllAsync().ConfigureAwait(true)).ToList();
        var options = _builds
            .Select(b => new BuildOption(b.Id, $"{b.Id}: {b.Name}"))
            .ToList();
        _buildBox.DisplayMember = nameof(BuildOption.Text);
        _buildBox.ValueMember = nameof(BuildOption.Id);
        _buildBox.DataSource = options;

        if (UiSessionState.SelectedBuildId is int selectedId &&
            options.Any(o => o.Id == selectedId))
        {
            _buildBox.SelectedValue = selectedId;
        }
    }

    private async Task CalculateAsync()
    {
        if (_buildBox.SelectedValue is not int buildId)
        {
            UiHelper.ShowInfo(this, "ビルドを選択してください。");
            return;
        }

        UiSessionState.SelectedBuildId = buildId;
        UiSessionState.WeaponAttack = _weaponAttackBox.Value;

        var detail = await AppServices.Builds.LoadAsync(buildId).ConfigureAwait(true);
        if (detail is null)
        {
            UiHelper.ShowInfo(this, "ビルドが見つかりません。");
            return;
        }

        var effects = new List<Effect>();
        foreach (var slot in detail.Slots.OrderBy(s => s.Position))
        {
            var relicDetail = await AppServices.Relics.GetDetailAsync(slot.Relic.Id).ConfigureAwait(true);
            if (relicDetail is null)
            {
                continue;
            }

            foreach (var effectSlot in relicDetail.Slots.OrderBy(s => s.SlotNumber))
            {
                effects.Add(effectSlot.Effect);
            }
        }

        var result = AppServices.DamageCalculator.Calculate(new DamageCalculationRequest
        {
            WeaponAttack = _weaponAttackBox.Value,
            Effects = effects
        });

        _baseAttackLabel.Text = $"{result.BaseAttack:0.####}";
        _totalMultiplierLabel.Text = $"× {result.TotalMultiplier:0.########}";
        _finalAttackLabel.Text = $"{result.FinalAttack:0.####}";
        _effectCountLabel.Text =
            $"入力効果 {effects.Count}  /  適用 {result.AppliedEffects.Count}  /  無効 {result.IgnoredEffects.Count}";

        _appliedList.DataSource = result.AppliedEffects
            .Select(e => $"[{e.Category}] EffectId={e.EffectId} Lv{e.Level}  {e.Name}  ×{e.Value}")
            .ToList();
        _ignoredList.DataSource = result.IgnoredEffects
            .Select(e => $"[{e.Category}] EffectId={e.EffectId} Lv{e.Level}  {e.Name}  ×{e.Value}")
            .ToList();
        _logBox.Text = string.Join(
            Environment.NewLine,
            result.Logs.Select(l =>
                $"[{l.Step}] {l.Description}" +
                (l.Multiplier is null ? string.Empty : $"  (×{l.Multiplier})") +
                $"  → {l.CurrentAttack:0.####}"));
    }

    private static decimal ClampAttack(decimal value)
    {
        if (value < 0m)
        {
            return 0m;
        }

        return value > 999999m ? 999999m : value;
    }

    private sealed class BuildOption
    {
        public BuildOption(int id, string text)
        {
            Id = id;
            Text = text;
        }

        public int Id { get; }
        public string Text { get; }
    }
}
