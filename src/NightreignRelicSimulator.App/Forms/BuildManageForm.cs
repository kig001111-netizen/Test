using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// ビルド管理画面です。
/// </summary>
/// <remarks>
/// 武器表示火力は DB 非永続のため、<see cref="UiSessionState"/> 経由で計算画面へ渡します。
/// </remarks>
public sealed class BuildManageForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _filterNameBox = new() { Width = 180, PlaceholderText = "ビルド名検索" };
    private readonly TextBox _nameBox = new() { Width = 200 };
    private readonly TextBox _characterBox = new() { Width = 160 };
    private readonly TextBox _weaponBox = new() { Width = 160 };
    private readonly NumericUpDown _weaponAttackBox = CreateAttackInput();
    private readonly ComboBox[] _relicBoxes = new ComboBox[AppConstants.RelicsPerBuild];
    private readonly Label _editingLabel = new() { AutoSize = true, Text = "新規保存" };

    private List<Build> _allBuilds = [];
    private List<Relic> _relics = [];
    private int? _editingBuildId;

    public BuildManageForm()
    {
        Text = "ビルド管理";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 680);
        MinimizeBox = false;

        for (var i = 0; i < _relicBoxes.Length; i++)
        {
            _relicBoxes[i] = new ComboBox
            {
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        _weaponAttackBox.Value = ClampAttack(UiSessionState.WeaponAttack);
        _weaponAttackBox.ValueChanged += (_, _) => UiSessionState.WeaponAttack = _weaponAttackBox.Value;

        var editor = BuildEditorPanel();
        editor.Dock = DockStyle.Top;
        editor.Height = 300;

        var listButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8),
            WrapContents = false
        };
        listButtons.Controls.Add(_filterNameBox);
        listButtons.Controls.Add(CreateButton("検索", ApplyFilterAsync));
        listButtons.Controls.Add(CreateButton("再読込", LoadAsync));
        listButtons.Controls.Add(CreateButton("選択を読込", LoadSelectedAsync));
        listButtons.Controls.Add(CreateButton("削除", DeleteAsync));
        listButtons.Controls.Add(CreateButton("火力計算へ", OpenCalculatorAsync));

        _filterNameBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = UiHelper.RunAsync(ApplyFilterAsync, this);
            }
        };

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        _grid.CellDoubleClick += async (_, _) => await UiHelper.RunAsync(LoadSelectedAsync, this);

        Controls.Add(_grid);
        Controls.Add(listButtons);
        Controls.Add(editor);

        Shown += async (_, _) => await UiHelper.RunAsync(InitializeAsync, this);
        FormClosing += (_, _) =>
        {
            UiSessionState.WeaponAttack = _weaponAttackBox.Value;
            UiSessionState.SelectedBuildId = _editingBuildId;
        };
    }

    private Panel BuildEditorPanel()
    {
        var panel = new Panel { Padding = new Padding(8) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
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
            new Label
            {
                Text = "※武器表示火力は未保存（計算画面へ引き継ぎ）",
                AutoSize = true,
                ForeColor = Color.DimGray
            },
            1,
            5);

        for (var i = 0; i < _relicBoxes.Length; i++)
        {
            AddRow(layout, 6 + i, $"遺物{i + 1}", _relicBoxes[i]);
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36
        };
        buttons.Controls.Add(CreateButton("新規クリア", () =>
        {
            ClearEditor();
            return Task.CompletedTask;
        }));
        buttons.Controls.Add(CreateButton("保存/更新", SaveAsync));

        panel.Controls.Add(layout);
        panel.Controls.Add(buttons);
        return panel;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private Button CreateButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 28 };
        button.Click += async (_, _) => await UiHelper.RunAsync(action, this);
        return button;
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
            b.CharacterName,
            b.WeaponName,
            UpdatedAt = b.UpdatedAt.LocalDateTime
        }).ToList();
    }

    private async Task LoadSelectedAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            MessageBox.Show(this, "読込するビルドを選択してください。", Text);
            return;
        }

        await TryLoadBuildAsync(id).ConfigureAwait(true);
    }

    private async Task TryLoadBuildAsync(int id)
    {
        var detail = await AppServices.Builds.LoadAsync(id).ConfigureAwait(true);
        if (detail is null)
        {
            MessageBox.Show(this, "ビルドが見つかりません。", Text);
            return;
        }

        _editingBuildId = detail.Build.Id;
        UiSessionState.SelectedBuildId = detail.Build.Id;
        _editingLabel.Text = $"編集中 Id={detail.Build.Id}";
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
            MessageBox.Show(this, "ビルド名は必須です。", Text);
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
        _editingLabel.Text = $"編集中 Id={savedId}";
        await LoadAsync().ConfigureAwait(true);
        MessageBox.Show(this, $"保存しました。Id={savedId}", Text);
    }

    private async Task DeleteAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            MessageBox.Show(this, "削除するビルドを選択してください。", Text);
            return;
        }

        if (MessageBox.Show(this, $"ビルド Id={id} を削除しますか？", Text, MessageBoxButtons.YesNo) != DialogResult.Yes)
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
        using var form = new DamageCalculatorForm();
        form.ShowDialog(this);
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

    private static NumericUpDown CreateAttackInput()
    {
        return new NumericUpDown
        {
            Minimum = 0,
            Maximum = 999999,
            DecimalPlaces = 0,
            Value = 1000,
            Width = 120
        };
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
