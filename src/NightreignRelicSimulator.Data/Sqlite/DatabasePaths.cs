using NightreignRelicSimulator.Core.Constants;

namespace NightreignRelicSimulator.Data.Sqlite;

/// <summary>
/// SQLite データベースのパスおよび接続文字列を提供します。
/// </summary>
public static class DatabasePaths
{
    /// <summary>
    /// データベースファイルを格納するディレクトリの絶対パスを取得します。
    /// </summary>
    public static string DatabaseDirectory
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, DatabaseConstants.ApplicationFolderName);
        }
    }

    /// <summary>
    /// データベースファイルの絶対パスを取得します。
    /// </summary>
    public static string DatabaseFilePath =>
        Path.Combine(DatabaseDirectory, DatabaseConstants.DatabaseFileName);

    /// <summary>
    /// ADO.NET 用の接続文字列を取得します。
    /// </summary>
    public static string ConnectionString =>
        $"Data Source={DatabaseFilePath};Mode=ReadWriteCreate;Cache=Shared";
}
