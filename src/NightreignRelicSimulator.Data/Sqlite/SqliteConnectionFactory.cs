using Microsoft.Data.Sqlite;
using NightreignRelicSimulator.Data.Sqlite;

namespace NightreignRelicSimulator.Data.Sqlite;

/// <summary>
/// Service 層向けに SQLite 接続を生成します。
/// </summary>
public static class SqliteConnectionFactory
{
    /// <summary>
    /// 外部キー有効化済みの接続を開いて返します。呼び出し側で破棄してください。
    /// </summary>
    public static async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys = ON;";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return connection;
    }
}
