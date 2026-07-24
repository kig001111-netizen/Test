using NightreignRelicSimulator.App.Ui;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// Effect マスタ管理画面です。
/// </summary>
public sealed class EffectManageForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = UiFactory.CreateTextBox(240);
    private readonly ComboBox _categoryBox = UiFactory.CreateComboBox(140);
    private readonly Label _countLabel = UiFactory.CreateMutedLabel("0 件");

    private List<Effect> _allItems = [];
    private List<Effect> _items = [];

    public EffectManageForm()
    {
        Text = "Effect管理";
        UiFactory.ApplyFormChrome(this);

        var toolbar = UiFactory.CreateToolbar();
        _searchBox.PlaceholderText = "名称検索";
        _searchBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = UiHelper.RunAsync(ApplyFilterAsync, this);
            }
        };

        toolbar.Controls.Add(UiFactory.CreateMutedLabel("カテゴリ"));
        toolbar.Controls.Add(_categoryBox);
        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("検索", ApplyFilterAsync, this));
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("再読込", LoadAsync, this));
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("追加", () => EditAsync(isNew: true), this, primary: true));
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("編集", () => EditAsync(isNew: false), this));
        toolbar.Controls.Add(UiFactory.CreateAsyncButton("削除", DeleteAsync, this));

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            Padding = new Padding(16, 8, 16, 8),
            BackColor = UiTheme.SurfaceAlt
        };
        _countLabel.Dock = DockStyle.Left;
        footer.Controls.Add(_countLabel);

        UiFactory.ConfigureGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += async (_, _) => await UiHelper.RunAsync(() => EditAsync(isNew: false), this);

        _categoryBox.SelectedIndexChanged += (_, _) =>
        {
            if (_categoryBox.SelectedIndex >= 0)
            {
                ApplyFilterLocal();
            }
        };

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(toolbar);

        Shown += async (_, _) => await UiHelper.RunAsync(LoadAsync, this);
    }

    private async Task LoadAsync()
    {
        _allItems = (await AppServices.Effects.GetAllAsync().ConfigureAwait(true)).ToList();
        RefreshCategoryOptions();
        ApplyFilterLocal();
    }

    private async Task ApplyFilterAsync()
    {
        var keyword = _searchBox.Text.Trim();
        _allItems = string.IsNullOrEmpty(keyword)
            ? (await AppServices.Effects.GetAllAsync().ConfigureAwait(true)).ToList()
            : (await AppServices.Effects.SearchByNameAsync(keyword).ConfigureAwait(true)).ToList();
        RefreshCategoryOptions(preserveSelection: true);
        ApplyFilterLocal();
    }

    private void RefreshCategoryOptions(bool preserveSelection = false)
    {
        var previous = _categoryBox.SelectedItem as string;
        var categories = _allItems
            .Select(e => e.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var options = new List<string> { "(すべて)" };
        options.AddRange(categories);

        _categoryBox.BeginUpdate();
        _categoryBox.DataSource = null;
        _categoryBox.DataSource = options;
        _categoryBox.EndUpdate();

        if (preserveSelection && previous is not null && options.Contains(previous))
        {
            _categoryBox.SelectedItem = previous;
        }
        else
        {
            _categoryBox.SelectedIndex = 0;
        }
    }

    private void ApplyFilterLocal()
    {
        var category = _categoryBox.SelectedItem as string;
        _items = string.IsNullOrEmpty(category) || category == "(すべて)"
            ? _allItems.ToList()
            : _allItems.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
        BindGrid(_items);
    }

    private void BindGrid(IReadOnlyList<Effect> items)
    {
        _grid.DataSource = null;
        _grid.DataSource = items.Select(e => new EffectRow(e)).ToList();
        _countLabel.Text = $"{items.Count} 件表示 / 全 {_allItems.Count} 件";
    }

    private Effect? GetSelectedEffect()
    {
        if (_grid.CurrentRow?.DataBoundItem is not EffectRow row)
        {
            return null;
        }

        return _items.FirstOrDefault(e => e.Id == row.Id);
    }

    private async Task EditAsync(bool isNew)
    {
        Effect editing;
        if (isNew)
        {
            editing = new Effect
            {
                EffectId = 0,
                Name = string.Empty,
                Category = _categoryBox.SelectedItem as string is { } c && c != "(すべて)" ? c : string.Empty,
                CanStack = true,
                Value = 1.0m,
                Level = 1,
                Description = string.Empty,
                DisplayOrder = 0
            };
        }
        else
        {
            var selected = GetSelectedEffect();
            if (selected is null)
            {
                UiHelper.ShowInfo(this, "編集する行を選択してください。");
                return;
            }

            editing = await AppServices.Effects.GetByIdAsync(selected.Id).ConfigureAwait(true)
                      ?? selected;
        }

        using var dialog = new EffectEditDialog(editing, isNew);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        if (isNew)
        {
            await AppServices.Effects.CreateAsync(dialog.Effect).ConfigureAwait(true);
        }
        else
        {
            await AppServices.Effects.UpdateAsync(dialog.Effect).ConfigureAwait(true);
        }

        await LoadAsync().ConfigureAwait(true);
    }

    private async Task DeleteAsync()
    {
        var selected = GetSelectedEffect();
        if (selected is null)
        {
            UiHelper.ShowInfo(this, "削除する行を選択してください。");
            return;
        }

        if (!UiHelper.Confirm(this, $"EffectId={selected.EffectId} / {selected.Name} を削除しますか？"))
        {
            return;
        }

        await AppServices.Effects.DeleteAsync(selected.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private sealed class EffectRow
    {
        public EffectRow(Effect e)
        {
            Id = e.Id;
            EffectId = e.EffectId;
            Name = e.Name;
            Category = e.Category;
            CanStack = e.CanStack;
            Value = e.Value;
            Level = e.Level;
            Description = e.Description;
        }

        public int Id { get; }
        public int EffectId { get; }
        public string Name { get; }
        public string Category { get; }
        public bool CanStack { get; }
        public decimal Value { get; }
        public int Level { get; }
        public string Description { get; }
    }
}

/// <summary>
/// Effect 追加・編集ダイアログです。
/// </summary>
internal sealed class EffectEditDialog : Form
{
    private readonly NumericUpDown _effectId = UiFactory.CreateNumeric(0, 999999);
    private readonly TextBox _name = UiFactory.CreateTextBox(280);
    private readonly TextBox _category = UiFactory.CreateTextBox(280);
    private readonly CheckBox _canStack = new() { Text = "CanStack（重複可）", AutoSize = true, ForeColor = UiTheme.TextPrimary };
    private readonly NumericUpDown _value = UiFactory.CreateNumeric(0.01m, 100m, decimalPlaces: 4);
    private readonly NumericUpDown _level = UiFactory.CreateNumeric(1, 99);
    private readonly TextBox _description = new()
    {
        Width = 280,
        Height = 60,
        Multiline = true,
        BackColor = UiTheme.SurfaceAlt,
        ForeColor = UiTheme.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle
    };
    private readonly NumericUpDown _displayOrder = UiFactory.CreateNumeric(0, 99999);

    public Effect Effect { get; private set; }

    public EffectEditDialog(Effect source, bool isNew)
    {
        Effect = source;
        Text = isNew ? "Effect追加" : "Effect編集";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 400);
        UiFactory.ApplyFormChrome(this);

        _effectId.Value = Math.Max(0, source.EffectId);
        _name.Text = source.Name;
        _category.Text = source.Category;
        _canStack.Checked = source.CanStack;
        _value.Increment = 0.01m;
        _value.Value = ClampDecimal(source.Value);
        _level.Value = Math.Max(1, source.Level);
        _description.Text = source.Description;
        _displayOrder.Value = Math.Max(0, source.DisplayOrder);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(16),
            BackColor = UiTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "EffectId", _effectId);
        AddRow(layout, 1, "Name", _name);
        AddRow(layout, 2, "Category", _category);
        layout.Controls.Add(_canStack, 1, 3);
        AddRow(layout, 4, "Value", _value);
        AddRow(layout, 5, "Level", _level);
        AddRow(layout, 6, "Description", _description);
        AddRow(layout, 7, "DisplayOrder", _displayOrder);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 52,
            Padding = new Padding(12),
            BackColor = UiTheme.SurfaceAlt
        };
        var ok = UiFactory.CreateButton("OK", 100, primary: true);
        var cancel = UiFactory.CreateButton("キャンセル", 100);
        cancel.DialogResult = DialogResult.Cancel;
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                UiHelper.ShowInfo(this, "Name は必須です。");
                return;
            }

            Effect = new Effect
            {
                Id = source.Id,
                EffectId = (int)_effectId.Value,
                Name = _name.Text.Trim(),
                Category = _category.Text.Trim(),
                CanStack = _canStack.Checked,
                Value = _value.Value,
                Level = (int)_level.Value,
                Description = _description.Text.Trim(),
                DisplayOrder = (int)_displayOrder.Value
            };
            DialogResult = DialogResult.OK;
            Close();
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
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

    private static decimal ClampDecimal(decimal value)
    {
        if (value < 0.01m)
        {
            return 0.01m;
        }

        return value > 100m ? 100m : value;
    }
}
