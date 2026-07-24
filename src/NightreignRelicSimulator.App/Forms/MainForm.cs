using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.App.Ui;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// アプリケーションのシェル画面です。サイドナビで各機能画面を切り替えます。
/// </summary>
public sealed class MainForm : Form
{
    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
    private readonly Label _screenTitle = new();
    private readonly Label _sessionLabel = new();
    private readonly Dictionary<string, Button> _navButtons = new(StringComparer.Ordinal);
    private Form? _currentChild;
    private string _currentKey = string.Empty;

    public MainForm()
    {
        Text = AppConstants.ApplicationName;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        ClientSize = new Size(1280, 800);
        UiFactory.ApplyFormChrome(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildNavPanel(), 0, 0);
        root.Controls.Add(BuildMainColumn(), 1, 0);
        Controls.Add(root);

        Shown += (_, _) => OpenDamageCalculator();
        Activated += (_, _) => RefreshSessionLabel();
    }

    /// <summary>
    /// 火力計算画面へ切り替えます。
    /// </summary>
    public void OpenDamageCalculator() =>
        Navigate("calc", "火力計算", () => new DamageCalculatorForm());

    private Panel BuildNavPanel()
    {
        var nav = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 0, 1, 0)
        };

        var brand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = UiTheme.Surface,
            Padding = new Padding(16, 18, 16, 12)
        };
        var brandTitle = new Label
        {
            Text = "NIGHTREIGN",
            Dock = DockStyle.Top,
            Height = 28,
            Font = UiTheme.HeadingFont,
            ForeColor = UiTheme.Accent
        };
        var brandSub = new Label
        {
            Text = "Relic Simulator",
            Dock = DockStyle.Top,
            Height = 22,
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.TextMuted
        };
        brand.Controls.Add(brandSub);
        brand.Controls.Add(brandTitle);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8, 12, 8, 8),
            BackColor = UiTheme.Background
        };

        buttons.Controls.Add(CreateNav("calc", "火力計算", () => new DamageCalculatorForm()));
        buttons.Controls.Add(CreateNav("build", "ビルド管理", () => new BuildManageForm()));
        buttons.Controls.Add(CreateNav("relic", "遺物管理", () => new RelicManageForm()));
        buttons.Controls.Add(CreateNav("effect", "Effect管理", () => new EffectManageForm()));

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(12),
            BackColor = UiTheme.Surface
        };
        _sessionLabel.Dock = DockStyle.Fill;
        _sessionLabel.ForeColor = UiTheme.TextMuted;
        _sessionLabel.Font = UiTheme.BodyFont;
        footer.Controls.Add(_sessionLabel);

        nav.Controls.Add(buttons);
        nav.Controls.Add(footer);
        nav.Controls.Add(brand);
        return nav;
    }

    private Panel BuildMainColumn()
    {
        var column = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20, 16, 20, 12)
        };
        _screenTitle.Text = "火力計算";
        _screenTitle.Font = UiTheme.TitleFont;
        _screenTitle.ForeColor = UiTheme.TextPrimary;
        _screenTitle.AutoSize = true;
        header.Controls.Add(_screenTitle);

        var border = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = UiTheme.Border
        };

        column.Controls.Add(_contentHost);
        column.Controls.Add(border);
        column.Controls.Add(header);
        return column;
    }

    private Button CreateNav(string key, string title, Func<Form> factory)
    {
        var button = new Button
        {
            Text = "  " + title,
            Width = 196,
            Margin = new Padding(0, 0, 0, 4)
        };
        UiFactory.StyleNavButton(button, selected: false);
        button.Click += (_, _) => Navigate(key, title, factory);
        _navButtons[key] = button;
        return button;
    }

    private void Navigate(string key, string title, Func<Form> factory)
    {
        if (_currentKey == key && _currentChild is { IsDisposed: false })
        {
            RefreshSessionLabel();
            return;
        }

        try
        {
            _contentHost.SuspendLayout();
            _contentHost.Controls.Clear();
            _currentChild?.Dispose();

            var child = factory();
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            child.BackColor = UiTheme.Background;
            UiFactory.ApplyFormChrome(child);

            _contentHost.Controls.Add(child);
            child.Show();
            _currentChild = child;
            _currentKey = key;
            _screenTitle.Text = title;

            foreach (var (navKey, button) in _navButtons)
            {
                UiFactory.StyleNavButton(button, selected: navKey == key);
            }

            RefreshSessionLabel();
        }
        catch (Exception ex)
        {
            UiHelper.ShowError(ex);
        }
        finally
        {
            _contentHost.ResumeLayout();
        }
    }

    private void RefreshSessionLabel()
    {
        var build = UiSessionState.SelectedBuildId is int id ? $"Build #{id}" : "Build未選択";
        _sessionLabel.Text = $"火力 {UiSessionState.WeaponAttack:0}\n{build}";
    }
}
