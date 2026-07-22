using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Data.Sqlite;
using NightreignRelicSimulator.App.Forms;

namespace NightreignRelicSimulator.App;

/// <summary>
/// アプリケーションのエントリポイントを提供します。
/// </summary>
internal static class Program
{
    /// <summary>
    /// アプリケーションのメインエントリポイントです。
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            DatabaseInitializer.Initialize();
        }
        catch (DatabaseException ex)
        {
            MessageBox.Show(
                $"データベースの初期化に失敗しました。{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                AppConstants.ApplicationName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm());
    }
}
