using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// Effect マスタ管理画面です。
/// </summary>
public sealed class EffectManageForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _categoryBox = new()
    {
        Width = 160,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private List<Effect> _allItems = [];
    private List<Effect> _items = [];

    public EffectManageForm()
    {
        Text = "Effect管理";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(960, 560);
        MinimizeBox = false;

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8),
            WrapContents = false
        };

        _searchBox.Width = 220;
        _searchBox.PlaceholderText = "名称検索";
        _searchBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = UiHelper.RunAsync(ApplyFilterAsync, this);
            }
        };

        var btnSearch = CreateButton("検索", ApplyFilterAsync);
        var btnReload = CreateButton("再読込", LoadAsync);
        var btnAdd = CreateButton("追加", () => EditAsync(isNew: true));
        var btnEdit = CreateButton("編集", () => EditAsync(isNew: false));
        var btnDelete = CreateButton("削除", DeleteAsync);

        top.Controls.Add(new Label { Text = "カテゴリ", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        top.Controls.Add(_categoryBox);
        top.Controls.Add(_searchBox);
        top.Controls.AddRange([btnSearch, btnReload, btnAdd, btnEdit, btnDelete]);

        _categoryBox.SelectedIndexChanged += (_, _) =>
        {
            if (_categoryBox.SelectedIndex >= 0)
            {
                ApplyFilterLocal();
            }
        };

        ConfigureGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += async (_, _) => await UiHelper.RunAsync(() => EditAsync(isNew: false), this);

        Controls.Add(_grid);
        Controls.Add(top);

        Shown += async (_, _) => await UiHelper.RunAsync(LoadAsync, this);
    }

    private Button CreateButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, Width = 80, Height = 28 };
        button.Click += async (_, _) => await UiHelper.RunAsync(action, this);
        return button;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false;
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
                MessageBox.Show(this, "編集する行を選択してください。", Text);
                return;
            }

            editing = await AppServices.Effects.GetByIdAsync(selected.Id).ConfigureAwait(true)
                      ?? selected;
        }

        using var dialog = new EffectEditDialog(editing, isNew);
        if (dialog.ShowDialog(this) != DialogResult.OK)
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
            MessageBox.Show(this, "削除する行を選択してください。", Text);
            return;
        }

        if (MessageBox.Show(
                this,
                $"EffectId={selected.EffectId} / {selected.Name} を削除しますか？",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
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
    private readonly NumericUpDown _effectId = CreateIntegerInput();
    private readonly TextBox _name = new() { Width = 280 };
    private readonly TextBox _category = new() { Width = 280 };
    private readonly CheckBox _canStack = new() { Text = "CanStack（重複可）", AutoSize = true };
    private readonly NumericUpDown _value = CreateDecimalInput();
    private readonly NumericUpDown _level = CreateIntegerInput(min: 1, max: 99);
    private readonly TextBox _description = new() { Width = 280, Height = 60, Multiline = true };
    private readonly NumericUpDown _displayOrder = CreateIntegerInput(min: 0, max: 99999);

    public Effect Effect { get; private set; }

    public EffectEditDialog(Effect source, bool isNew)
    {
        Effect = source;
        Text = isNew ? "Effect追加" : "Effect編集";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 360);

        _effectId.Value = Math.Max(0, source.EffectId);
        _name.Text = source.Name;
        _category.Text = source.Category;
        _canStack.Checked = source.CanStack;
        _value.Value = ClampDecimal(source.Value);
        _level.Value = Math.Max(1, source.Level);
        _description.Text = source.Description;
        _displayOrder.Value = Math.Max(0, source.DisplayOrder);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
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
            Height = 44,
            Padding = new Padding(8)
        };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.None, Width = 90 };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Width = 90 };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show(this, "Name は必須です。", Text);
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
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static NumericUpDown CreateIntegerInput(int min = 0, int max = 999999)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Width = 120,
            DecimalPlaces = 0
        };
    }

    private static NumericUpDown CreateDecimalInput()
    {
        return new NumericUpDown
        {
            Minimum = 0.01m,
            Maximum = 100m,
            DecimalPlaces = 4,
            Increment = 0.01m,
            Width = 120
        };
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
