using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// 火力計算画面です。Build / Relic Service から Effect を集め、DamageCalculator に渡します。
/// </summary>
/// <remarks>
/// 計算式: 最終火力 = 武器表示火力 × 全倍率（Excel の効果数値が倍率）。
/// </remarks>
public sealed class DamageCalculatorForm : Form
{
    private readonly NumericUpDown _weaponAttackBox = new()
    {
        Minimum = 0,
        Maximum = 999999,
        DecimalPlaces = 0,
        Value = 1000,
        Width = 120
    };

    private readonly ComboBox _buildBox = new()
    {
        Width = 320,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly Label _baseAttackLabel = new() { AutoSize = true, Text = "武器表示火力: -" };
    private readonly Label _totalMultiplierLabel = new() { AutoSize = true, Text = "総倍率: -" };
    private readonly Label _finalAttackLabel = new() { AutoSize = true, Text = "最終火力: -" };
    private readonly ListBox _appliedList = new() { Dock = DockStyle.Fill };
    private readonly ListBox _ignoredList = new() { Dock = DockStyle.Fill };
    private readonly TextBox _logBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical
    };

    private List<Build> _builds = [];

    public DamageCalculatorForm()
    {
        Text = "火力計算";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 640);
        MinimizeBox = false;

        _weaponAttackBox.Value = ClampAttack(UiSessionState.WeaponAttack);
        _weaponAttackBox.ValueChanged += (_, _) => UiSessionState.WeaponAttack = _weaponAttackBox.Value;

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(8),
            WrapContents = false
        };

        var calcButton = new Button { Text = "計算", Width = 100, Height = 28 };
        calcButton.Click += async (_, _) => await UiHelper.RunAsync(CalculateAsync, this);
        var reloadButton = new Button { Text = "ビルド再読込", Width = 120, Height = 28 };
        reloadButton.Click += async (_, _) => await UiHelper.RunAsync(LoadBuildsAsync, this);

        top.Controls.Add(new Label { Text = "武器表示火力", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        top.Controls.Add(_weaponAttackBox);
        top.Controls.Add(new Label { Text = "ビルド", AutoSize = true, Padding = new Padding(12, 6, 0, 0) });
        top.Controls.Add(_buildBox);
        top.Controls.Add(calcButton);
        top.Controls.Add(reloadButton);

        var summary = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.TopDown
        };
        summary.Controls.Add(_baseAttackLabel);
        summary.Controls.Add(_totalMultiplierLabel);
        summary.Controls.Add(_finalAttackLabel);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 280
        };

        var lists = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 440
        };

        var appliedPanel = CreateLabeledPanel("適用されたEffect", _appliedList);
        var ignoredPanel = CreateLabeledPanel("適用されなかったEffect", _ignoredList);
        lists.Panel1.Controls.Add(appliedPanel);
        lists.Panel2.Controls.Add(ignoredPanel);

        var logPanel = CreateLabeledPanel("計算ログ", _logBox);
        split.Panel1.Controls.Add(lists);
        split.Panel2.Controls.Add(logPanel);

        Controls.Add(split);
        Controls.Add(summary);
        Controls.Add(top);

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

    private static Panel CreateLabeledPanel(string title, Control content)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        content.Dock = DockStyle.Fill;
        panel.Controls.Add(content);
        panel.Controls.Add(label);
        return panel;
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
            MessageBox.Show(this, "ビルドを選択してください。", Text);
            return;
        }

        UiSessionState.SelectedBuildId = buildId;
        UiSessionState.WeaponAttack = _weaponAttackBox.Value;

        var detail = await AppServices.Builds.LoadAsync(buildId).ConfigureAwait(true);
        if (detail is null)
        {
            MessageBox.Show(this, "ビルドが見つかりません。", Text);
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

        _baseAttackLabel.Text = $"武器表示火力: {result.BaseAttack:0.####}";
        _totalMultiplierLabel.Text = $"総倍率: {result.TotalMultiplier:0.########}";
        _finalAttackLabel.Text = $"最終火力: {result.FinalAttack:0.####}";

        _appliedList.DataSource = result.AppliedEffects
            .Select(e => $"EffectId={e.EffectId} Lv{e.Level} {e.Name} x{e.Value}")
            .ToList();
        _ignoredList.DataSource = result.IgnoredEffects
            .Select(e => $"EffectId={e.EffectId} Lv{e.Level} {e.Name} x{e.Value}")
            .ToList();
        _logBox.Text = string.Join(
            Environment.NewLine,
            result.Logs.Select(l =>
                $"[{l.Step}] {l.Description}" +
                (l.Multiplier is null ? string.Empty : $" (x{l.Multiplier})") +
                $" → {l.CurrentAttack:0.####}"));
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
