using NightreignRelicSimulator.App.Ui;
using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Services.Calculation;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// 火力計算画面です。効果×遺物6列のチェックマトリクスで動的に計算します。
/// </summary>
public sealed class DamageCalculatorForm : Form
{
    private readonly NumericUpDown _weaponAttackBox = UiFactory.CreateNumeric(0, 999999);
    private readonly TextBox _buildNameBox = UiFactory.CreateTextBox(180);
    private readonly TextBox _characterBox = UiFactory.CreateTextBox(120);
    private readonly TextBox _weaponBox = UiFactory.CreateTextBox(120);
    private readonly DataGridView _matrix = new();
    private readonly FlowLayoutPanel _stagedPanel = new();
    private readonly Label _baseAttackLabel = new();
    private readonly Label _totalMultiplierLabel = new();
    private readonly Label _finalAttackLabel = new();
    private readonly Label _effectCountLabel = UiFactory.CreateMutedLabel("適用 0 / 無効 0");
    private readonly Label _hintLabel = UiFactory.CreateMutedLabel("編集中: 新規");
    private readonly ListBox _appliedList = new();
    private readonly ListBox _ignoredList = new();
    private readonly TextBox _logBox = new();

    private List<Effect> _catalog = [];
    private List<Effect> _matrixEffects = [];
    private int? _editingBuildId;
    private bool _suppressMatrixEvents;
    private readonly Dictionary<int, ComboBox> _stagedLevelBoxes = new();

    public DamageCalculatorForm()
    {
        Text = "火力計算";
        UiFactory.ApplyFormChrome(this);

        _editingBuildId = UiSessionState.SelectedBuildId;
        _weaponAttackBox.Value = ClampAttack(UiSessionState.WeaponAttack);
        _weaponAttackBox.ValueChanged += (_, _) =>
        {
            UiSessionState.WeaponAttack = _weaponAttackBox.Value;
            Recalculate();
        };

        var toolbar = UiFactory.CreateToolbar();
        toolbar.Height = 56;
        toolbar.Controls.Add(UiFactory.CreateMutedLabel("武器表示火力"));
        toolbar.Controls.Add(_weaponAttackBox);
        toolbar.Controls.Add(UiFactory.CreateMutedLabel("ビルド名"));
        toolbar.Controls.Add(_buildNameBox);
        toolbar.Controls.Add(UiFactory.CreateMutedLabel("キャラ"));
        toolbar.Controls.Add(_characterBox);
        toolbar.Controls.Add(UiFactory.CreateMutedLabel("武器"));
        toolbar.Controls.Add(_weaponBox);
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("ビルド保存", SaveBuildAsync, this, 110, primary: true));
        var clearBtn = UiFactory.CreateButton("クリア", 90);
        clearBtn.Click += (_, _) => ClearMatrix();
        toolbar.Controls.Add(clearBtn);

        ConfigureMatrix();
        var stagedHost = BuildStagedHost();
        var summary = BuildSummaryPanel();

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 360,
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

        split.Panel1.Controls.Add(_matrix);
        var bottom = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 160,
            BackColor = UiTheme.Border
        };
        bottom.Panel1.Controls.Add(lists);
        bottom.Panel2.Controls.Add(CreateListPanel("計算ログ", _logBox));
        split.Panel2.Controls.Add(bottom);

        Controls.Add(split);
        Controls.Add(summary);
        Controls.Add(stagedHost);
        Controls.Add(_hintLabel);
        Controls.Add(toolbar);
        _hintLabel.Dock = DockStyle.Top;
        _hintLabel.Height = 24;
        _hintLabel.Padding = new Padding(16, 4, 16, 0);

        Shown += async (_, _) => await UiHelper.RunAsync(InitializeAsync, this);
        FormClosing += (_, _) =>
        {
            UiSessionState.WeaponAttack = _weaponAttackBox.Value;
            UiSessionState.SelectedBuildId = _editingBuildId;
        };
    }

    private void ConfigureMatrix()
    {
        _matrix.Dock = DockStyle.Fill;
        _matrix.AllowUserToAddRows = false;
        _matrix.AllowUserToDeleteRows = false;
        _matrix.RowHeadersVisible = false;
        _matrix.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _matrix.BackgroundColor = UiTheme.Surface;
        _matrix.GridColor = UiTheme.Border;
        _matrix.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.SurfaceAlt;
        _matrix.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextPrimary;
        _matrix.DefaultCellStyle.BackColor = UiTheme.Surface;
        _matrix.DefaultCellStyle.ForeColor = UiTheme.TextPrimary;
        _matrix.EnableHeadersVisualStyles = false;
        _matrix.SelectionMode = DataGridViewSelectionMode.CellSelect;

        _matrix.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Effect",
            HeaderText = "効果",
            ReadOnly = true,
            Width = 280
        });
        for (var i = 1; i <= AppConstants.RelicsPerBuild; i++)
        {
            _matrix.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = $"R{i}",
                HeaderText = $"遺物{i}",
                Width = 70,
                TrueValue = true,
                FalseValue = false
            });
        }

        _matrix.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_matrix.IsCurrentCellDirty)
            {
                _matrix.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _matrix.CellValueChanged += MatrixOnCellValueChanged;
    }

    private void MatrixOnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressMatrixEvents || e.RowIndex < 0 || e.ColumnIndex < 1)
        {
            return;
        }

        if (_matrix.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is not true)
        {
            Recalculate();
            return;
        }

        var checkedInColumn = 0;
        for (var r = 0; r < _matrix.Rows.Count; r++)
        {
            if (_matrix.Rows[r].Cells[e.ColumnIndex].Value is true)
            {
                checkedInColumn++;
            }
        }

        if (checkedInColumn > AppConstants.EffectsPerRelic)
        {
            _suppressMatrixEvents = true;
            _matrix.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = false;
            _suppressMatrixEvents = false;
            UiHelper.ShowInfo(this, $"遺物{e.ColumnIndex} に設定できる効果は最大 {AppConstants.EffectsPerRelic} 件です。");
            return;
        }

        Recalculate();
    }

    private Panel BuildStagedHost()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 0,
            Visible = false,
            BackColor = UiTheme.Surface,
            Padding = new Padding(16, 8, 16, 8),
            Tag = "stagedHost"
        };
        var title = UiFactory.CreateHeading("段階効果レベル");
        title.Dock = DockStyle.Top;
        title.Height = 28;
        var hint = UiFactory.CreateMutedLabel("段階効果はマトリクスでは1行。ここで Level を変更します。");
        hint.Dock = DockStyle.Top;
        hint.Height = 22;
        _stagedPanel.Dock = DockStyle.Fill;
        _stagedPanel.AutoScroll = true;
        _stagedPanel.WrapContents = true;
        panel.Controls.Add(_stagedPanel);
        panel.Controls.Add(hint);
        panel.Controls.Add(title);
        return panel;
    }

    private Panel BuildSummaryPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20, 12, 20, 8)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
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
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(12) };
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
        _matrixEffects = StagedEffectResolver.CollapseForRelicSelection(_catalog).ToList();
        FillMatrixRows();

        if (_editingBuildId is int buildId)
        {
            await LoadBuildMatrixAsync(buildId).ConfigureAwait(true);
        }
        else
        {
            UpdateHint();
            Recalculate();
        }
    }

    private void FillMatrixRows()
    {
        _suppressMatrixEvents = true;
        _matrix.Rows.Clear();
        var staged = StagedEffectResolver.GetDefinitions(_catalog).Select(d => d.EffectId).ToHashSet();
        foreach (var effect in _matrixEffects)
        {
            var label = staged.Contains(effect.EffectId)
                ? $"{effect.EffectId}: {effect.Name}（段階）"
                : $"{effect.EffectId}: {effect.Name}";
            var values = new object[1 + AppConstants.RelicsPerBuild];
            values[0] = label;
            for (var i = 1; i <= AppConstants.RelicsPerBuild; i++)
            {
                values[i] = false;
            }

            var index = _matrix.Rows.Add(values);
            _matrix.Rows[index].Tag = effect;
        }

        _suppressMatrixEvents = false;
    }

    private async Task LoadBuildMatrixAsync(int buildId)
    {
        var detail = await AppServices.BuildMatrix.LoadAsync(buildId).ConfigureAwait(true);
        if (detail is null)
        {
            UiHelper.ShowInfo(this, "ビルドが見つかりません。");
            return;
        }

        _editingBuildId = detail.Build.Id;
        UiSessionState.SelectedBuildId = detail.Build.Id;
        _buildNameBox.Text = detail.Build.Name;
        _characterBox.Text = detail.Build.CharacterName;
        _weaponBox.Text = detail.Build.WeaponName;
        ApplyColumns(detail.EffectIdsByRelic);
        UpdateHint();
        Recalculate();
    }

    private void ApplyColumns(IReadOnlyList<IReadOnlyList<int>> columns)
    {
        _suppressMatrixEvents = true;
        for (var r = 0; r < _matrix.Rows.Count; r++)
        {
            if (_matrix.Rows[r].Tag is not Effect effect)
            {
                continue;
            }

            for (var c = 0; c < AppConstants.RelicsPerBuild; c++)
            {
                var on = c < columns.Count && columns[c].Contains(effect.Id);
                _matrix.Rows[r].Cells[c + 1].Value = on;
            }
        }

        _suppressMatrixEvents = false;
    }

    private IReadOnlyList<IReadOnlyList<int>> ReadColumns()
    {
        var columns = new List<IReadOnlyList<int>>(AppConstants.RelicsPerBuild);
        for (var c = 0; c < AppConstants.RelicsPerBuild; c++)
        {
            var ids = new List<int>();
            for (var r = 0; r < _matrix.Rows.Count; r++)
            {
                if (_matrix.Rows[r].Cells[c + 1].Value is true && _matrix.Rows[r].Tag is Effect effect)
                {
                    ids.Add(effect.Id);
                }
            }

            columns.Add(ids);
        }

        return columns;
    }

    private void ClearMatrix()
    {
        _editingBuildId = null;
        UiSessionState.SelectedBuildId = null;
        _buildNameBox.Clear();
        _characterBox.Clear();
        _weaponBox.Clear();
        _suppressMatrixEvents = true;
        for (var r = 0; r < _matrix.Rows.Count; r++)
        {
            for (var c = 1; c <= AppConstants.RelicsPerBuild; c++)
            {
                _matrix.Rows[r].Cells[c].Value = false;
            }
        }

        _suppressMatrixEvents = false;
        UpdateHint();
        Recalculate();
    }

    private void UpdateHint()
    {
        _hintLabel.Text = _editingBuildId is int id
            ? $"編集中: Build #{id}（チェックすると即計算）"
            : "編集中: 新規（チェックすると即計算）";
    }

    private void Recalculate()
    {
        var columns = ReadColumns();
        var effects = new List<Effect>();
        var byId = _catalog.ToDictionary(e => e.Id);
        foreach (var column in columns)
        {
            foreach (var id in column)
            {
                if (byId.TryGetValue(id, out var effect))
                {
                    effects.Add(effect);
                }
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

        _baseAttackLabel.Text = $"{result.BaseAttack:0.####}";
        _totalMultiplierLabel.Text = $"× {result.TotalMultiplier:0.########}";
        _finalAttackLabel.Text = $"{result.FinalAttack:0.####}";
        _effectCountLabel.Text =
            $"適用 {result.AppliedEffects.Count} / 無効 {result.IgnoredEffects.Count}";
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
        if (host.Name == signature && _stagedLevelBoxes.Count == definitions.Count)
        {
            return;
        }

        host.Name = signature;
        _stagedPanel.Controls.Clear();
        _stagedLevelBoxes.Clear();
        if (definitions.Count == 0)
        {
            host.Visible = false;
            host.Height = 0;
            return;
        }

        foreach (var def in definitions)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 12, 4) };
            var label = UiFactory.CreateMutedLabel($"{def.Name} (EffectId={def.EffectId})");
            label.AutoSize = true;
            label.Margin = new Padding(0, 8, 8, 0);
            var combo = UiFactory.CreateComboBox(160);
            var options = def.Levels.Select(l => new LevelOption(l.Level, $"Lv{l.Level} (×{l.Value})")).ToList();
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
            combo.SelectedIndexChanged += (_, _) =>
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
                Recalculate();
            };
            _stagedLevelBoxes[effectId] = combo;
            row.Controls.Add(label);
            row.Controls.Add(combo);
            _stagedPanel.Controls.Add(row);
        }

        host.Visible = true;
        host.Height = Math.Min(140, 70 + definitions.Count * 40);
    }

    private async Task SaveBuildAsync()
    {
        if (string.IsNullOrWhiteSpace(_buildNameBox.Text))
        {
            UiHelper.ShowInfo(this, "ビルド名を入力してください。");
            return;
        }

        var id = await AppServices.BuildMatrix.SaveAsync(
            new BuildMatrixUpsertRequest
            {
                Id = _editingBuildId,
                Name = _buildNameBox.Text.Trim(),
                CharacterName = _characterBox.Text.Trim(),
                WeaponName = _weaponBox.Text.Trim(),
                EffectIdsByRelic = ReadColumns()
            }).ConfigureAwait(true);

        _editingBuildId = id;
        UiSessionState.SelectedBuildId = id;
        UpdateHint();
        UiHelper.ShowInfo(this, $"保存しました。Id={id}");
    }

    /// <summary>
    /// ビルド管理からマトリクスを開きます。
    /// </summary>
    public Task OpenBuildAsync(int buildId) => LoadBuildMatrixAsync(buildId);

    private static decimal ClampAttack(decimal value) =>
        value < 0m ? 0m : value > 999999m ? 999999m : value;

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
