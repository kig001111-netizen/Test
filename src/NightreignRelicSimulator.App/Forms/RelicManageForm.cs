using NightreignRelicSimulator.App.Ui;
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
    private readonly TextBox _filterNameBox = UiFactory.CreateTextBox(160);
    private readonly ComboBox _filterColorBox = UiFactory.CreateComboBox(120);
    private readonly Label _countLabel = UiFactory.CreateMutedLabel("0 件");

    private readonly TextBox _nameBox = UiFactory.CreateTextBox(220);
    private readonly ComboBox _colorBox = UiFactory.CreateComboBox(140);
    private readonly TextBox _memoBox = UiFactory.CreateTextBox(220);
    private readonly ComboBox[] _effectBoxes = new ComboBox[AppConstants.EffectsPerRelic];
    private readonly Label _editingLabel = UiFactory.CreateHeading("新規登録");

    private List<RelicListItem> _allItems = [];
    private List<Effect> _effects = [];
    private int? _editingRelicId;

    public RelicManageForm()
    {
        Text = "遺物管理";
        UiFactory.ApplyFormChrome(this);

        for (var i = 0; i < _effectBoxes.Length; i++)
        {
            _effectBoxes[i] = UiFactory.CreateComboBox(280);
        }

        _colorBox.DataSource = Enum.GetValues<RelicColor>();
        var colorFilterOptions = new List<object> { "(すべて)" };
        colorFilterOptions.AddRange(Enum.GetValues<RelicColor>().Cast<object>());
        _filterColorBox.DataSource = colorFilterOptions;
        _filterNameBox.PlaceholderText = "名前検索";

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
    }

    private Panel BuildListPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };

        var toolbar = UiFactory.CreateToolbar();
        toolbar.Controls.Add(UiFactory.CreateMutedLabel("色"));
        toolbar.Controls.Add(_filterColorBox);
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
        _filterColorBox.SelectedIndexChanged += (_, _) => ApplyFilterLocal();

        UiFactory.ConfigureGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += async (_, _) => await UiHelper.RunAsync(LoadSelectedToEditorAsync, this);

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
            Height = 320,
            ColumnCount = 2,
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
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
        buttons.Controls.Add(UiFactory.CreateAsyncButton("選択を編集", LoadSelectedToEditorAsync, this, 110));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("登録/更新", SaveAsync, this, 110, primary: true));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("削除", DeleteAsync, this, 90));

        var hint = UiFactory.CreateMutedLabel("一覧をダブルクリックで編集できます。効果はマスタから選択します。");
        hint.Dock = DockStyle.Top;
        hint.Padding = new Padding(0, 8, 0, 0);

        panel.Controls.Add(hint);
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
        var items = colorFilter is RelicColor color
            ? _allItems.Where(r => r.Color == color).ToList()
            : _allItems.ToList();

        _grid.DataSource = null;
        _grid.DataSource = items
            .Select(r => new { r.Id, r.Name, Color = r.Color.ToString(), Effects = r.EffectCount })
            .ToList();
        _countLabel.Text = $"{items.Count} 件";
    }

    private async Task LoadSelectedToEditorAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            UiHelper.ShowInfo(this, "編集する遺物を選択してください。");
            return;
        }

        var detail = await AppServices.Relics.GetDetailAsync(id).ConfigureAwait(true);
        if (detail is null)
        {
            UiHelper.ShowInfo(this, "遺物が見つかりません。");
            return;
        }

        _editingRelicId = detail.Relic.Id;
        _editingLabel.Text = $"編集中  Id={detail.Relic.Id}";
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
            UiHelper.ShowInfo(this, "名前は必須です。");
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
            UiHelper.ShowInfo(this, "削除する遺物を選択してください。");
            return;
        }

        var name = _grid.CurrentRow.Cells["Name"]?.Value?.ToString() ?? id.ToString();
        if (!UiHelper.Confirm(this, $"「{name}」を削除しますか？"))
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
