using Microsoft.Data.Sqlite;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Data.Sqlite;

namespace NightreignRelicSimulator.Services.Common;

/// <summary>
/// Service 向けの SQLite セッション（接続・トランザクション）ヘルパーです。
/// </summary>
internal static class SqliteSession
{
    public static async Task<T> ExecuteAsync<T>(
        Func<SqliteConnection, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ServiceException("データベース操作に失敗しました。", ex);
        }
    }

    public static async Task ExecuteAsync(
        Func<SqliteConnection, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(async (connection, token) =>
        {
            await action(connection, token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<T> ExecuteInTransactionAsync<T>(
        Func<SqliteConnection, SqliteTransaction, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var result = await action(connection, transaction, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (ServiceException)
        {
            await TryRollbackAsync(transaction, cancellationToken).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await TryRollbackAsync(transaction, cancellationToken).ConfigureAwait(false);
            throw new ServiceException("データベース操作に失敗しました。", ex);
        }
    }

    public static async Task ExecuteInTransactionAsync(
        Func<SqliteConnection, SqliteTransaction, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await ExecuteInTransactionAsync(async (connection, transaction, token) =>
        {
            await action(connection, transaction, token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task TryRollbackAsync(SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignore rollback failures
        }
    }
}
