using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// 遺物管理画面です。
/// </summary>
public sealed class RelicManageForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _filterNameBox = new() { Width = 160, PlaceholderText = "名前検索" };
    private readonly ComboBox _filterColorBox = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };

    private readonly TextBox _nameBox = new() { Width = 180 };
    private readonly ComboBox _colorBox = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _memoBox = new() { Width = 220 };
    private readonly ComboBox[] _effectBoxes = new ComboBox[AppConstants.EffectsPerRelic];
    private readonly Label _editingLabel = new() { AutoSize = true, Text = "新規登録" };

    private List<RelicListItem> _allItems = [];
    private List<RelicListItem> _items = [];
    private List<Effect> _effects = [];
    private int? _editingRelicId;

    public RelicManageForm()
    {
        Text = "遺物管理";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 640);
        MinimizeBox = false;

        for (var i = 0; i < _effectBoxes.Length; i++)
        {
            _effectBoxes[i] = new ComboBox
            {
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        _colorBox.DataSource = Enum.GetValues<RelicColor>();

        var colorFilterOptions = new List<object> { "(すべて)" };
        colorFilterOptions.AddRange(Enum.GetValues<RelicColor>().Cast<object>());
        _filterColorBox.DataSource = colorFilterOptions;

        var editor = BuildEditorPanel();
        editor.Dock = DockStyle.Top;
        editor.Height = 220;

        var filterBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8),
            WrapContents = false
        };
        filterBar.Controls.Add(new Label { Text = "色", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        filterBar.Controls.Add(_filterColorBox);
        filterBar.Controls.Add(_filterNameBox);
        filterBar.Controls.Add(CreateButton("検索", ApplyFilterAsync));
        filterBar.Controls.Add(CreateButton("再読込", LoadAsync));
        filterBar.Controls.Add(CreateButton("選択を編集", LoadSelectedToEditorAsync));
        filterBar.Controls.Add(CreateButton("削除", DeleteAsync));

        _filterNameBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = UiHelper.RunAsync(ApplyFilterAsync, this);
            }
        };
        _filterColorBox.SelectedIndexChanged += (_, _) => ApplyFilterLocal();

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        _grid.CellDoubleClick += async (_, _) => await UiHelper.RunAsync(LoadSelectedToEditorAsync, this);

        Controls.Add(_grid);
        Controls.Add(filterBar);
        Controls.Add(editor);

        Shown += async (_, _) => await UiHelper.RunAsync(InitializeAsync, this);
    }

    private Panel BuildEditorPanel()
    {
        var panel = new Panel { Padding = new Padding(8) };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(_editingLabel, 1, 0);
        AddRow(layout, 1, "名前", _nameBox);
        AddRow(layout, 2, "色", _colorBox);
        AddRow(layout, 3, "メモ", _memoBox);
        for (var i = 0; i < _effectBoxes.Length; i++)
        {
            AddRow(layout, 4 + i, $"Effect{i + 1}", _effectBoxes[i]);
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.Add(CreateButton("新規クリア", () =>
        {
            ClearEditor();
            return Task.CompletedTask;
        }));
        buttons.Controls.Add(CreateButton("登録/更新", SaveAsync));

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
        _effects = (await AppServices.Effects.GetAllAsync().ConfigureAwait(true)).ToList();
        foreach (var box in _effectBoxes)
        {
            var options = new List<EffectOption> { new(0, "(なし)") };
            options.AddRange(_effects.Select(e => new EffectOption(e.Id, $"{e.EffectId}: {e.Name} (Lv{e.Level})")));
            box.DisplayMember = nameof(EffectOption.Text);
            box.ValueMember = nameof(EffectOption.RowId);
            box.DataSource = options.ToList();
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        var relics = await AppServices.Relics.GetAllAsync().ConfigureAwait(true);
        _allItems = [];
        foreach (var relic in relics)
        {
            var detail = await AppServices.Relics.GetDetailAsync(relic.Id).ConfigureAwait(true);
            _allItems.Add(new RelicListItem(
                relic.Id,
                relic.Name,
                relic.Color,
                detail?.Slots.Count ?? 0));
        }

        ApplyFilterLocal();
    }

    private async Task ApplyFilterAsync()
    {
        var keyword = _filterNameBox.Text.Trim();
        IReadOnlyList<Relic> relics = string.IsNullOrEmpty(keyword)
            ? await AppServices.Relics.GetAllAsync().ConfigureAwait(true)
            : await AppServices.Relics.SearchByNameAsync(keyword).ConfigureAwait(true);

        _allItems = [];
        foreach (var relic in relics)
        {
            var detail = await AppServices.Relics.GetDetailAsync(relic.Id).ConfigureAwait(true);
            _allItems.Add(new RelicListItem(
                relic.Id,
                relic.Name,
                relic.Color,
                detail?.Slots.Count ?? 0));
        }

        ApplyFilterLocal();
    }

    private void ApplyFilterLocal()
    {
        var colorFilter = _filterColorBox.SelectedItem;
        _items = colorFilter is RelicColor color
            ? _allItems.Where(r => r.Color == color).ToList()
            : _allItems.ToList();

        _grid.DataSource = null;
        _grid.DataSource = _items
            .Select(r => new { r.Id, r.Name, Color = r.Color.ToString(), r.EffectCount })
            .ToList();
    }

    private async Task LoadSelectedToEditorAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            MessageBox.Show(this, "編集する遺物を選択してください。", Text);
            return;
        }

        var detail = await AppServices.Relics.GetDetailAsync(id).ConfigureAwait(true);
        if (detail is null)
        {
            MessageBox.Show(this, "遺物が見つかりません。", Text);
            return;
        }

        _editingRelicId = detail.Relic.Id;
        _editingLabel.Text = $"編集中 Id={detail.Relic.Id}";
        _nameBox.Text = detail.Relic.Name;
        _colorBox.SelectedItem = detail.Relic.Color;
        _memoBox.Text = detail.Relic.Memo;

        for (var i = 0; i < _effectBoxes.Length; i++)
        {
            var slotNumber = i + 1;
            var slot = detail.Slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
            _effectBoxes[i].SelectedValue = slot?.Effect.Id ?? 0;
        }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            MessageBox.Show(this, "名前は必須です。", Text);
            return;
        }

        var effectIds = new int?[AppConstants.EffectsPerRelic];
        for (var i = 0; i < _effectBoxes.Length; i++)
        {
            var selectedId = _effectBoxes[i].SelectedValue is int id ? id : 0;
            effectIds[i] = selectedId > 0 ? selectedId : null;
        }

        var request = new RelicUpsertRequest
        {
            Id = _editingRelicId,
            Name = _nameBox.Text.Trim(),
            Color = _colorBox.SelectedItem is RelicColor color ? color : RelicColor.None,
            Memo = _memoBox.Text.Trim(),
            EffectIdsBySlot = effectIds
        };

        if (_editingRelicId is null)
        {
            await AppServices.Relics.RegisterAsync(request).ConfigureAwait(true);
        }
        else
        {
            await AppServices.Relics.UpdateAsync(request).ConfigureAwait(true);
        }

        ClearEditor();
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task DeleteAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            MessageBox.Show(this, "削除する遺物を選択してください。", Text);
            return;
        }

        var name = _grid.CurrentRow.Cells["Name"]?.Value?.ToString() ?? id.ToString();
        if (MessageBox.Show(this, $"「{name}」を削除しますか？", Text, MessageBoxButtons.YesNo) != DialogResult.Yes)
        {
            return;
        }

        await AppServices.Relics.DeleteAsync(id).ConfigureAwait(true);
        ClearEditor();
        await LoadAsync().ConfigureAwait(true);
    }

    private void ClearEditor()
    {
        _editingRelicId = null;
        _editingLabel.Text = "新規登録";
        _nameBox.Clear();
        _memoBox.Clear();
        _colorBox.SelectedItem = RelicColor.None;
        foreach (var box in _effectBoxes)
        {
            if (box.Items.Count > 0)
            {
                box.SelectedIndex = 0;
            }
        }
    }

    private sealed record RelicListItem(int Id, string Name, RelicColor Color, int EffectCount);

    private sealed class EffectOption
    {
        public EffectOption(int rowId, string text)
        {
            RowId = rowId;
            Text = text;
        }

        public int RowId { get; }
        public string Text { get; }
    }
}
