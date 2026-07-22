using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Exceptions;

namespace NightreignRelicSimulator.App.Forms;

/// <summary>
/// アプリケーションのホーム画面です。各管理画面への遷移のみを担当します。
/// </summary>
public sealed class MainForm : Form
{
    /// <summary>
    /// <see cref="MainForm"/> の新しいインスタンスを初期化します。
    /// </summary>
    public MainForm()
    {
        Text = AppConstants.ApplicationName;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(480, 360);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var title = new Label
        {
            Text = AppConstants.ApplicationName,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(24, 24)
        };

        var hint = new Label
        {
            Text = "武器表示火力は画面間で引き継がれます（DB未保存）。",
            AutoSize = true,
            Location = new Point(24, 60)
        };

        var panel = new FlowLayoutPanel
        {
            Location = new Point(24, 100),
            Size = new Size(420, 220),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        panel.Controls.Add(CreateNavButton("Effect管理", () => new EffectManageForm()));
        panel.Controls.Add(CreateNavButton("遺物管理", () => new RelicManageForm()));
        panel.Controls.Add(CreateNavButton("ビルド管理", () => new BuildManageForm()));
        panel.Controls.Add(CreateNavButton("火力計算", () => new DamageCalculatorForm()));

        Controls.Add(title);
        Controls.Add(hint);
        Controls.Add(panel);
    }

    private static Button CreateNavButton(string text, Func<Form> formFactory)
    {
        var button = new Button
        {
            Text = text,
            Width = 400,
            Height = 40,
            Margin = new Padding(0, 0, 0, 8)
        };

        button.Click += (_, _) =>
        {
            try
            {
                using var form = formFactory();
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(ex);
            }
        };

        return button;
    }
}

/// <summary>
/// WinForms 向けの共通 UI ヘルパーです。
/// </summary>
internal static class UiHelper
{
    public static void ShowError(Exception ex)
    {
        var message = ex is ServiceException or AggregateException
            ? GetMessage(ex)
            : ex.Message;

        MessageBox.Show(
            message,
            AppConstants.ApplicationName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    public static async Task RunAsync(Func<Task> action, Control owner)
    {
        try
        {
            owner.Enabled = false;
            await action().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            owner.Enabled = true;
        }
    }

    private static string GetMessage(Exception ex)
    {
        if (ex is AggregateException aggregate)
        {
            return aggregate.Flatten().InnerExceptions.FirstOrDefault()?.Message ?? ex.Message;
        }

        return ex.Message;
    }
}
