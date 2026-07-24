namespace NightreignRelicSimulator.App.Ui;

/// <summary>
/// テーマ適用済みコントロールの生成ヘルパーです。
/// </summary>
internal static class UiFactory
{
    public static void ApplyFormChrome(Form form)
    {
        form.BackColor = UiTheme.Background;
        form.ForeColor = UiTheme.TextPrimary;
        form.Font = UiTheme.BodyFont;
    }

    public static Panel CreateSurfacePanel(DockStyle dock = DockStyle.Fill, int padding = 12)
    {
        return new Panel
        {
            Dock = dock,
            BackColor = UiTheme.Surface,
            Padding = new Padding(padding)
        };
    }

    public static Label CreateMutedLabel(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            ForeColor = UiTheme.TextMuted,
            Font = UiTheme.BodyFont
        };

    public static Label CreateHeading(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.HeadingFont
        };

    public static Button CreateButton(string text, int width = 96, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            Font = UiTheme.BodyFont,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };

        button.FlatAppearance.BorderSize = 1;
        if (primary)
        {
            button.BackColor = UiTheme.Accent;
            button.ForeColor = Color.FromArgb(20, 16, 10);
            button.FlatAppearance.BorderColor = UiTheme.Accent;
        }
        else
        {
            button.BackColor = UiTheme.SurfaceAlt;
            button.ForeColor = UiTheme.TextPrimary;
            button.FlatAppearance.BorderColor = UiTheme.Border;
        }

        return button;
    }

    public static Button CreateAsyncButton(string text, Func<Task> action, Control owner, int width = 96, bool primary = false)
    {
        var button = CreateButton(text, width, primary);
        button.Click += async (_, _) => await UiHelper.RunAsync(action, owner);
        return button;
    }

    public static TextBox CreateTextBox(int width = 200)
    {
        return new TextBox
        {
            Width = width,
            Height = 28,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = UiTheme.BodyFont
        };
    }

    public static ComboBox CreateComboBox(int width = 160)
    {
        return new ComboBox
        {
            Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.BodyFont
        };
    }

    public static NumericUpDown CreateNumeric(decimal min, decimal max, int decimalPlaces = 0, int width = 120)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimalPlaces,
            Width = width,
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.BodyFont,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    public static void ConfigureGrid(DataGridView grid)
    {
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = UiTheme.Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = UiTheme.Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = UiTheme.SurfaceAlt,
            ForeColor = UiTheme.TextMuted,
            Font = UiTheme.BodyFont,
            SelectionBackColor = UiTheme.SurfaceAlt,
            SelectionForeColor = UiTheme.TextMuted,
            Padding = new Padding(6, 4, 6, 4)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.BodyFont,
            SelectionBackColor = UiTheme.AccentSoft,
            SelectionForeColor = UiTheme.TextPrimary,
            Padding = new Padding(6, 2, 6, 2)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(24, 24, 28),
            ForeColor = UiTheme.TextPrimary,
            SelectionBackColor = UiTheme.AccentSoft,
            SelectionForeColor = UiTheme.TextPrimary
        };
        grid.ColumnHeadersHeight = 36;
        grid.RowTemplate.Height = 30;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
    }

    public static FlowLayoutPanel CreateToolbar()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            WrapContents = false,
            BackColor = UiTheme.SurfaceAlt
        };
    }

    public static void StyleNavButton(Button button, bool selected)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Height = 44;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(16, 0, 0, 0);
        button.Font = UiTheme.BodyFont;
        button.Cursor = Cursors.Hand;

        if (selected)
        {
            button.BackColor = UiTheme.NavSelected;
            button.ForeColor = UiTheme.Accent;
        }
        else
        {
            button.BackColor = UiTheme.Background;
            button.ForeColor = UiTheme.TextPrimary;
        }
    }
}
