using NightreignRelicSimulator.App.Ui;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// 登録済みビルドの一覧・メタ編集・計算画面への読込を行う画面です。
/// </summary>
public sealed class BuildManageForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _filterNameBox = UiFactory.CreateTextBox(180);
    private readonly TextBox _nameBox = UiFactory.CreateTextBox(220);
    private readonly TextBox _characterBox = UiFactory.CreateTextBox(180);
    private readonly TextBox _weaponBox = UiFactory.CreateTextBox(180);
    private readonly Label _editingLabel = UiFactory.CreateHeading("ビルドを選択");
    private readonly Label _countLabel = UiFactory.CreateMutedLabel("0 件");

    private List<Build> _allBuilds = [];
    private int? _editingBuildId;

    public BuildManageForm()
    {
        Text = "ビルド管理";
        UiFactory.ApplyFormChrome(this);
        _filterNameBox.PlaceholderText = "ビルド名検索";

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 480,
            BackColor = UiTheme.Border,
            Panel1MinSize = 280,
            Panel2MinSize = 320
        };
        split.Panel1.Controls.Add(BuildListPanel());
        split.Panel2.Controls.Add(BuildEditorPanel());
        Controls.Add(split);
        Shown += async (_, _) => await UiHelper.RunAsync(LoadAsync, this);
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
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            _ = UiHelper.RunAsync(ApplyFilterAsync, this);
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
            Padding = new Padding(16)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(layout, 0, "名前", _nameBox);
        AddRow(layout, 1, "キャラ", _characterBox);
        AddRow(layout, 2, "武器", _weaponBox);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 12, 0, 0)
        };
        buttons.Controls.Add(UiFactory.CreateAsyncButton("メタ保存", SaveMetaAsync, this, 110, primary: true));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("計算で開く", OpenInCalcAsync, this, 120, primary: true));
        buttons.Controls.Add(UiFactory.CreateAsyncButton("削除", DeleteAsync, this, 90));

        var hint = UiFactory.CreateMutedLabel("マトリクス構成は火力計算画面で編集・保存します。");
        hint.Dock = DockStyle.Top;
        hint.Padding = new Padding(0, 8, 0, 0);
        _editingLabel.Dock = DockStyle.Top;
        panel.Controls.Add(hint);
        panel.Controls.Add(buttons);
        panel.Controls.Add(layout);
        panel.Controls.Add(_editingLabel);
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

    private async Task LoadAsync()
    {
        _allBuilds = (await AppServices.Builds.GetAllAsync().ConfigureAwait(true)).ToList();
        BindGrid(_allBuilds);
    }

    private async Task ApplyFilterAsync()
    {
        var keyword = _filterNameBox.Text.Trim();
        var list = string.IsNullOrEmpty(keyword)
            ? (await AppServices.Builds.GetAllAsync().ConfigureAwait(true)).ToList()
            : (await AppServices.Builds.SearchByNameAsync(keyword).ConfigureAwait(true)).ToList();
        _allBuilds = list.ToList();
        BindGrid(_allBuilds);
    }

    private void BindGrid(IReadOnlyList<Build> builds)
    {
        _grid.DataSource = null;
        _grid.DataSource = builds
            .Select(b => new { b.Id, b.Name, Character = b.CharacterName, Weapon = b.WeaponName })
            .ToList();
        _countLabel.Text = $"{builds.Count} 件";
    }

    private async Task LoadSelectedAsync()
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not int id)
        {
            UiHelper.ShowInfo(this, "ビルドを選択してください。");
            return;
        }

        var detail = await AppServices.Builds.LoadAsync(id).ConfigureAwait(true);
        if (detail is null)
        {
            UiHelper.ShowInfo(this, "ビルドが見つかりません。");
            return;
        }

        _editingBuildId = detail.Build.Id;
        _editingLabel.Text = $"編集中  Id={detail.Build.Id}";
        _nameBox.Text = detail.Build.Name;
        _characterBox.Text = detail.Build.CharacterName;
        _weaponBox.Text = detail.Build.WeaponName;
    }

    private async Task SaveMetaAsync()
    {
        if (_editingBuildId is not int id)
        {
            UiHelper.ShowInfo(this, "編集するビルドを選択してください。");
            return;
        }

        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            UiHelper.ShowInfo(this, "名前は必須です。");
            return;
        }

        var matrix = await AppServices.BuildMatrix.LoadAsync(id).ConfigureAwait(true);
        if (matrix is null)
        {
            UiHelper.ShowInfo(this, "ビルドが見つかりません。");
            return;
        }

        await AppServices.BuildMatrix.SaveAsync(
            new BuildMatrixUpsertRequest
            {
                Id = id,
                Name = _nameBox.Text.Trim(),
                CharacterName = _characterBox.Text.Trim(),
                WeaponName = _weaponBox.Text.Trim(),
                EffectIdsByRelic = matrix.EffectIdsByRelic
            }).ConfigureAwait(true);

        await LoadAsync().ConfigureAwait(true);
        UiHelper.ShowInfo(this, "保存しました。");
    }

    private Task OpenInCalcAsync()
    {
        var buildId = ResolveSelectedBuildId();
        if (buildId is null)
        {
            UiHelper.ShowInfo(this, "ビルドを選択してください。");
            return Task.CompletedTask;
        }

        UiSessionState.SelectedBuildId = buildId;
        var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
        main?.OpenDamageCalculatorWithBuild(buildId.Value);
        return Task.CompletedTask;
    }

    private async Task DeleteAsync()
    {
        var buildId = ResolveSelectedBuildId();
        if (buildId is null)
        {
            UiHelper.ShowInfo(this, "削除するビルドを選択してください。");
            return;
        }

        if (!UiHelper.Confirm(this, $"ビルド Id={buildId} を削除しますか？"))
        {
            return;
        }

        await AppServices.Builds.DeleteAsync(buildId.Value).ConfigureAwait(true);
        if (UiSessionState.SelectedBuildId == buildId)
        {
            UiSessionState.SelectedBuildId = null;
        }

        _editingBuildId = null;
        _editingLabel.Text = "ビルドを選択";
        _nameBox.Clear();
        _characterBox.Clear();
        _weaponBox.Clear();
        await LoadAsync().ConfigureAwait(true);
    }

    private int? ResolveSelectedBuildId()
    {
        if (_editingBuildId is int id)
        {
            return id;
        }

        if (_grid.CurrentRow?.Cells["Id"]?.Value is int gridId)
        {
            return gridId;
        }

        return null;
    }
}
