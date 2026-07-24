using NightreignRelicSimulator.App.Ui;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Services.Calculation;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// 火力計算画面です。最終火力 = 武器表示火力 × 全倍率。
/// </summary>
/// <remarks>
/// 段階効果（複数 Level）は遺物では1件として登録し、ここで Level を指定します。
/// </remarks>
public sealed class DamageCalculatorForm : Form
{
    private readonly NumericUpDown _weaponAttackBox = UiFactory.CreateNumeric(0, 999999);
    private readonly ComboBox _buildBox = UiFactory.CreateComboBox(320);
    private readonly FlowLayoutPanel _stagedPanel = new();
    private readonly Label _baseAttackLabel = new();
    private readonly Label _totalMultiplierLabel = new();
    private readonly Label _finalAttackLabel = new();
    private readonly Label _effectCountLabel = UiFactory.CreateMutedLabel("適用 0 / 無効 0");
    private readonly ListBox _appliedList = new();
    private readonly ListBox _ignoredList = new();
    private readonly TextBox _logBox = new();

    private List<Build> _builds = [];
    private List<Effect> _catalog = [];
    private readonly Dictionary<int, ComboBox> _stagedLevelBoxes = new();

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

        var stagedHost = BuildStagedHost();
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
        Controls.Add(stagedHost);
        Controls.Add(toolbar);

        Shown += async (_, _) => await UiHelper.RunAsync(InitializeAsync, this);
        FormClosing += (_, _) =>
        {
            UiSessionState.WeaponAttack = _weaponAttackBox.Value;
            if (_buildBox.SelectedValue is int buildId)
            {
                UiSessionState.SelectedBuildId = buildId;
            }
        };
    }

    private Panel BuildStagedHost()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 0,
            Visible = false,
            BackColor = UiTheme.Surface,
            Padding = new Padding(16, 8, 16, 8)
        };

        var title = UiFactory.CreateHeading("段階効果レベル");
        title.Dock = DockStyle.Top;
        title.Height = 28;

        var hint = UiFactory.CreateMutedLabel("封牢・夜の侵入などは遺物では1効果として扱い、ここでレベルを変更します。");
        hint.Dock = DockStyle.Top;
        hint.Height = 22;

        _stagedPanel.Dock = DockStyle.Fill;
        _stagedPanel.AutoScroll = true;
        _stagedPanel.WrapContents = true;
        _stagedPanel.FlowDirection = FlowDirection.TopDown;

        panel.Controls.Add(_stagedPanel);
        panel.Controls.Add(hint);
        panel.Controls.Add(title);
        panel.Tag = "stagedHost";
        return panel;
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

    private async Task InitializeAsync()
    {
        _catalog = (await AppServices.Effects.GetAllAsync().ConfigureAwait(true)).ToList();
        await LoadBuildsAsync().ConfigureAwait(true);
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

        if (_catalog.Count == 0)
        {
            _catalog = (await AppServices.Effects.GetAllAsync().ConfigureAwait(true)).ToList();
        }

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

        RenderStagedControls(effects);

        var result = AppServices.DamageCalculator.Calculate(new DamageCalculationRequest
        {
            WeaponAttack = _weaponAttackBox.Value,
            Effects = effects,
            EffectCatalog = _catalog,
            LevelOverrides = new Dictionary<int, int>(UiSessionState.LevelOverrides)
        });

        ApplyResult(effects.Count, result);
    }

    private void ApplyResult(int inputCount, CalculationResult result)
    {
        _baseAttackLabel.Text = $"{result.BaseAttack:0.####}";
        _totalMultiplierLabel.Text = $"× {result.TotalMultiplier:0.########}";
        _finalAttackLabel.Text = $"{result.FinalAttack:0.####}";
        _effectCountLabel.Text =
            $"入力効果 {inputCount}  /  適用 {result.AppliedEffects.Count}  /  無効 {result.IgnoredEffects.Count}";

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

    private async Task RecalculateWithCurrentBuildAsync()
    {
        if (_buildBox.SelectedValue is not int buildId)
        {
            return;
        }

        var detail = await AppServices.Builds.LoadAsync(buildId).ConfigureAwait(true);
        if (detail is null)
        {
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
            Effects = effects,
            EffectCatalog = _catalog,
            LevelOverrides = new Dictionary<int, int>(UiSessionState.LevelOverrides)
        });
        ApplyResult(effects.Count, result);
    }

    private void RenderStagedControls(IReadOnlyList<Effect> equipped)
    {
        var host = Controls.OfType<Panel>().FirstOrDefault(p => Equals(p.Tag, "stagedHost"));
        if (host is null)
        {
            return;
        }

        var definitions = StagedEffectResolver.GetDefinitions(_catalog)
            .Where(d => equipped.Any(e => e.EffectId == d.EffectId))
            .ToList();

        var signature = string.Join(",", definitions.Select(d => d.EffectId));
        var previousSignature = host.Name;
        if (signature == previousSignature && _stagedLevelBoxes.Count == definitions.Count)
        {
            foreach (var def in definitions)
            {
                if (!_stagedLevelBoxes.TryGetValue(def.EffectId, out var combo))
                {
                    continue;
                }

                var selected = UiSessionState.LevelOverrides.TryGetValue(def.EffectId, out var saved)
                    ? saved
                    : equipped.First(e => e.EffectId == def.EffectId).Level;
                if (combo.SelectedValue is int current && current == selected)
                {
                    continue;
                }

                combo.SelectedValue = selected;
            }

            return;
        }

        host.Name = signature;
        _stagedPanel.SuspendLayout();
        _stagedPanel.Controls.Clear();
        _stagedLevelBoxes.Clear();

        if (definitions.Count == 0)
        {
            host.Visible = false;
            host.Height = 0;
            _stagedPanel.ResumeLayout();
            return;
        }

        foreach (var def in definitions)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 4, 12, 4)
            };

            var label = UiFactory.CreateMutedLabel($"{def.Name} (EffectId={def.EffectId})");
            label.AutoSize = true;
            label.Margin = new Padding(0, 8, 8, 0);

            var combo = UiFactory.CreateComboBox(160);
            var options = def.Levels
                .Select(l => new LevelOption(l.Level, $"Lv{l.Level} (×{l.Value})"))
                .ToList();
            combo.DisplayMember = nameof(LevelOption.Text);
            combo.ValueMember = nameof(LevelOption.Level);
            combo.DataSource = options;

            var selected = UiSessionState.LevelOverrides.TryGetValue(def.EffectId, out var saved)
                ? saved
                : equipped.First(e => e.EffectId == def.EffectId).Level;
            if (options.Any(o => o.Level == selected))
            {
                combo.SelectedValue = selected;
            }

            var effectId = def.EffectId;
            combo.SelectedIndexChanged += async (_, _) =>
            {
                if (combo.SelectedValue is not int level)
                {
                    return;
                }

                if (UiSessionState.LevelOverrides.TryGetValue(effectId, out var existing) && existing == level)
                {
                    return;
                }

                UiSessionState.LevelOverrides[effectId] = level;
                await UiHelper.RunAsync(RecalculateWithCurrentBuildAsync, this);
            };

            _stagedLevelBoxes[effectId] = combo;
            row.Controls.Add(label);
            row.Controls.Add(combo);
            _stagedPanel.Controls.Add(row);
        }

        host.Visible = true;
        host.Height = Math.Min(140, 70 + (definitions.Count * 40));
        _stagedPanel.ResumeLayout();
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

    private sealed class LevelOption
    {
        public LevelOption(int level, string text)
        {
            Level = level;
            Text = text;
        }

        public int Level { get; }
        public string Text { get; }
    }
}
