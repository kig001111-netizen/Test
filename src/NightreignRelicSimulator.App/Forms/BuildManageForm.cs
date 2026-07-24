using NightreignRelicSimulator.App.Ui;
using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// ビルド管理画面です。
/// </summary>
public sealed class BuildManageForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _filterNameBox = UiFactory.CreateTextBox(180);
    private readonly TextBox _nameBox = UiFactory.CreateTextBox(220);
    private readonly TextBox _characterBox = UiFactory.CreateTextBox(180);
    private readonly TextBox _weaponBox = UiFactory.CreateTextBox(180);
    private readonly NumericUpDown _weaponAttackBox = UiFactory.CreateNumeric(0, 999999);
    private readonly ComboBox[] _relicBoxes = new ComboBox[AppConstants.RelicsPerBuild];
    private readonly Label _editingLabel = UiFactory.CreateHeading("新規保存");
    private readonly Label _countLabel = UiFactory.CreateMutedLabel("0 件");

    private List<Build> _allBuilds = [];
    private List<Relic> _relics = [];
    private int? _editingBuildId;

    public BuildManageForm()
    {
        Text = "ビルド管理";
        UiFactory.ApplyFormChrome(this);

        for (var i = 0; i < _relicBoxes.Length; i++)
        {
            _relicBoxes[i] = UiFactory.CreateComboBox(260);
        }

        _weaponAttackBox.Value = ClampAttack(UiSessionState.WeaponAttack);
        _weaponAttackBox.ValueChanged += (_, _) => UiSessionState.WeaponAttack = _weaponAttackBox.Value;
        _filterNameBox.PlaceholderText = "ビルド名検索";

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 420,
            BackColor = UiTheme.Border,
            Panel1MinSize = 280,
            Panel2MinSize = 360
        };
        split.Panel1.Controls.Add(BuildListPanel());
        split.Panel2.Controls.Add(BuildEditorPanel());

        Controls.Add(split);
        Shown += async (_, _) => await UiHelper.RunAsync(InitializeAsync, this);
        FormClosing += (_, _) =>
        {
            UiSessionState.WeaponAttack = _weaponAttackBox.Value;
            UiSessionState.SelectedBuildId = _editingBuildId;
        };
    }

    private Panel BuildListPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
        var toolbar = UiFactory.CreateToolbar();
        toolbar.Controls.Add(_filterNameBox);
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("検索", ApplyFilterAsync, this));
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("再読込", LoadAsync, this));

        _filterNameBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = UiHelper.RunAsync(ApplyFilterAsync, this);
            }
        };

        UiFactory.ConfigureGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += async (_, _) => await UiHelper.RunAsync(LoadSelectedAsync, this);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = UiTheme.SurfaceAlt
        };
        footer.Controls.Add(_countLabel);

        panel.Controls.Add(_grid);
        panel.Controls.Add(footer);
        panel.Controls.Add(toolbar);
        return panel;
    }

    private Panel BuildEditorPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 12
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(_editingLabel, 1, 0);
        AddRow(layout, 1, "ビルド名", _nameBox);
        AddRow(layout, 2, "キャラ名", _characterBox);
        AddRow(layout, 3, "武器名", _weaponBox);
        AddRow(layout, 4, "武器表示火力", _weaponAttackBox);
        layout.Controls.Add(
            UiFactory.CreateMutedLabel("※火力は未保存。計算画面へ引き継ぎます"),
            1,
            5);

        for (var i = 0; i < _relicBoxes.Length; i++)
        {
            AddRow(layout, 6 + i, $"遺物{i + 1}", _relicBoxes[i]);
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(0, 12, 0, 0),
            WrapContents = false
        };
        buttons.Controls.Add(UiFactory.CreateAsyncButton("新規クリア", () =>
        {
            ClearEditor();
            return Task.CompletedTask;
        }, this, 110));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("選択を読込", LoadSelectedAsync, this, 110));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("保存/更新", SaveAsync, this, 110, primary: true));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("削除", DeleteAsync, this, 90));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("火力計算へ", OpenCalculatorAsync, this, 110, primary: true));

        panel.Controls.Add(buttons);
        panel.Controls.Add(layout);
        return panel;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = UiTheme.TextMuted,
            Anchor = AnchorStyles.Left
        }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private async Task InitializeAsync()
    {
        _relics = (await AppServices.Relics.GetAllAsync().ConfigureAwait(true)).ToList();
        foreach (var box in _relicBoxes)
        {
            var options = new List<RelicOption> { new(0, "(なし)") };
            options.AddRange(_relics.Select(r => new RelicOption(r.Id, $"{r.Id}: {r.Name}")));
            box.DisplayMember = nameof(RelicOption.Text);
            box.ValueMember = nameof(RelicOption.Id);
            box.DataSource = options.ToList();
        }

        await LoadAsync().ConfigureAwait(true);

        if (UiSessionState.SelectedBuildId is int buildId)
        {
            await TryLoadBuildAsync(buildId).ConfigureAwait(true);
        }
    }

    private async Task LoadAsync()
    {
        _allBuilds = (await AppServices.Builds.GetAllAsync().ConfigureAwait(true)).ToList();
        BindGrid(_allBuilds);
    }

    private async Task ApplyFilterAsync()
    {
        var keyword = _filterNameBox.Text.Trim();
        _allBuilds = string.IsNullOrEmpty(keyword)
            ? (await AppServices.Builds.GetAllAsync().ConfigureAwait(true)).ToList()
            : (await AppServices.Builds.SearchByNameAsync(keyword).ConfigureAwait(true)).ToList();
        BindGrid(_allBuilds);
    }

    private void BindGrid(IReadOnlyList<Build> builds)
    {
        _grid.DataSource = null;
        _grid.DataSource = builds.Select(b => new
        {
            b.Id,
            b.Name,
            Character = b.CharacterName,
            Weapon = b.WeaponName,
            Updated = b.UpdatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm")
        }).ToList();
        _countLabel.Text = $"{builds.Count} 件";
    }

    private async Task LoadSelectedAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            UiHelper.ShowInfo(this, "読込するビルドを選択してください。");
            return;
        }

        await TryLoadBuildAsync(id).ConfigureAwait(true);
    }

    private async Task TryLoadBuildAsync(int id)
    {
        var detail = await AppServices.Builds.LoadAsync(id).ConfigureAwait(true);
        if (detail is null)
        {
            UiHelper.ShowInfo(this, "ビルドが見つかりません。");
            return;
        }

        _editingBuildId = detail.Build.Id;
        UiSessionState.SelectedBuildId = detail.Build.Id;
        _editingLabel.Text = $"編集中  Id={detail.Build.Id}";
        _nameBox.Text = detail.Build.Name;
        _characterBox.Text = detail.Build.CharacterName;
        _weaponBox.Text = detail.Build.WeaponName;

        for (var i = 0; i < _relicBoxes.Length; i++)
        {
            var position = i + 1;
            var slot = detail.Slots.FirstOrDefault(s => s.Position == position);
            _relicBoxes[i].SelectedValue = slot?.Relic.Id ?? 0;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            UiHelper.ShowInfo(this, "ビルド名は必須です。");
            return;
        }

        var relicIds = new int?[AppConstants.RelicsPerBuild];
        for (var i = 0; i < _relicBoxes.Length; i++)
        {
            var selectedId = _relicBoxes[i].SelectedValue is int id ? id : 0;
            relicIds[i] = selectedId > 0 ? selectedId : null;
        }

        var request = new BuildUpsertRequest
        {
            Id = _editingBuildId,
            Name = _nameBox.Text.Trim(),
            CharacterName = _characterBox.Text.Trim(),
            WeaponName = _weaponBox.Text.Trim(),
            RelicIdsByPosition = relicIds
        };

        var savedId = await AppServices.Builds.SaveAsync(request).ConfigureAwait(true);
        _editingBuildId = savedId;
        UiSessionState.SelectedBuildId = savedId;
        UiSessionState.WeaponAttack = _weaponAttackBox.Value;
        _editingLabel.Text = $"編集中  Id={savedId}";
        await LoadAsync().ConfigureAwait(true);
        UiHelper.ShowInfo(this, $"保存しました。Id={savedId}");
    }

    private async Task DeleteAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            UiHelper.ShowInfo(this, "削除するビルドを選択してください。");
            return;
        }

        if (!UiHelper.Confirm(this, $"ビルド Id={id} を削除しますか？"))
        {
            return;
        }

        await AppServices.Builds.DeleteAsync(id).ConfigureAwait(true);
        if (UiSessionState.SelectedBuildId == id)
        {
            UiSessionState.SelectedBuildId = null;
        }

        ClearEditor();
        await LoadAsync().ConfigureAwait(true);
    }

    private Task OpenCalculatorAsync()
    {
        UiSessionState.WeaponAttack = _weaponAttackBox.Value;
        UiSessionState.SelectedBuildId = _editingBuildId;

        if (FindForm() is MainForm main)
        {
            main.OpenDamageCalculator();
            return Task.CompletedTask;
        }

        using var form = new DamageCalculatorForm { StartPosition = FormStartPosition.CenterParent };
        form.ShowDialog(FindForm());
        _weaponAttackBox.Value = ClampAttack(UiSessionState.WeaponAttack);
        return Task.CompletedTask;
    }

    private void ClearEditor()
    {
        _editingBuildId = null;
        _editingLabel.Text = "新規保存";
        _nameBox.Clear();
        _characterBox.Clear();
        _weaponBox.Clear();
        _weaponAttackBox.Value = ClampAttack(UiSessionState.WeaponAttack);
        foreach (var box in _relicBoxes)
        {
            if (box.Items.Count > 0)
            {
                box.SelectedIndex = 0;
            }
        }
    }

    private static decimal ClampAttack(decimal value)
    {
        if (value < 0m)
        {
            return 0m;
        }

        return value > 999999m ? 999999m : value;
    }

    private sealed class RelicOption
    {
        public RelicOption(int id, string text)
        {
            Id = id;
            Text = text;
        }

        public int Id { get; }
        public string Text { get; }
    }
}
