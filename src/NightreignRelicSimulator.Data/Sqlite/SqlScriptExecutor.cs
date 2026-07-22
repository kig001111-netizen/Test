using Microsoft.Data.Sqlite;
using NightreignRelicSimulator.Core.Exceptions;

namespace NightreignRelicSimulator.Data.Sqlite;

/// <summary>
/// SQL スクリプトをステートメント単位で実行します。
/// </summary>
internal static class SqlScriptExecutor
{
    /// <summary>
    /// スクリプト内の各ステートメントを順に実行します。
    /// </summary>
    /// <param name="connection">開かれた接続。</param>
    /// <param name="transaction">トランザクション（任意）。</param>
    /// <param name="scriptName">ログ用スクリプト名。</param>
    /// <param name="script">SQL 本文。</param>
    public static void Execute(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string scriptName,
        string script)
    {
        foreach (var statement in SplitStatements(script))
        {
            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = statement;
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new DatabaseException(
                    $"SQL スクリプト '{scriptName}' の実行に失敗しました。Statement: {Truncate(statement, 120)}",
                    ex);
            }
        }
    }

    /// <summary>
    /// コメント行を除き、セミコロン区切りでステートメントへ分割します。
    /// </summary>
    internal static IEnumerable<string> SplitStatements(string script)
    {
        var buffer = new System.Text.StringBuilder();

        using var reader = new StringReader(script);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            buffer.AppendLine(line);

            if (!trimmed.EndsWith(';'))
            {
                continue;
            }

            var statement = buffer.ToString().Trim();
            buffer.Clear();

            if (statement.Length > 0)
            {
                yield return statement;
            }
        }

        var trailing = buffer.ToString().Trim();
        if (trailing.Length > 0)
        {
            yield return trailing;
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }
}
