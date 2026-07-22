using System.Data;
using System.Data.Common;
using NightreignRelicSimulator.Core.Exceptions;

namespace NightreignRelicSimulator.Data.Repositories;

/// <summary>
/// Repository 実装向けの共通ヘルパーです。
/// </summary>
internal static class RepositoryCommandHelper
{
    /// <summary>
    /// SQL ファイルを読み込み、コマンドを生成します。
    /// </summary>
    public static DbCommand CreateCommand(
        DbConnection connection,
        DbTransaction? transaction,
        string sqlRelativePath)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.State != ConnectionState.Open)
        {
            throw new DatabaseException("SQLite 接続が Open 状態ではありません。");
        }

        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = Sqlite.SqlScriptLoader.Load(sqlRelativePath);
        return command;
    }

    /// <summary>
    /// パラメータを追加します。
    /// </summary>
    public static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// 影響行数が 1 以上かどうかを返します。
    /// </summary>
    public static bool Affected(int rows) => rows > 0;

    /// <summary>
    /// 直近の INSERT で採番された行 ID を取得します。
    /// </summary>
    public static async Task<int> GetLastInsertRowIdAsync(
        DbConnection connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, transaction, "Common/SelectLastInsertRowId.sql");
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result);
    }
}
