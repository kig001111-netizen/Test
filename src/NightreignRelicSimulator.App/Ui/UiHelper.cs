using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Exceptions;

namespace NightreignRelicSimulator.App.Ui;

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

    public static void ShowInfo(IWin32Window owner, string message, string? title = null)
    {
        MessageBox.Show(
            owner,
            message,
            title ?? AppConstants.ApplicationName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    public static bool Confirm(IWin32Window owner, string message, string? title = null)
    {
        return MessageBox.Show(
            owner,
            message,
            title ?? AppConstants.ApplicationName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }

    public static async Task RunAsync(Func<Task> action, Control owner)
    {
        try
        {
            owner.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            await action().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
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
