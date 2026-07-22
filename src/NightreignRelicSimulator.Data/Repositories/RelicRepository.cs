using System.Data.Common;
using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Data.Repositories;

/// <summary>
/// <see cref="IRelicRepository"/> の SQLite 実装です。
/// </summary>
public sealed class RelicRepository : IRelicRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Relic>> GetAllAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Relic/SelectAll.sql");
        return await ReadRelicsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Relic?> GetByIdAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Relic/SelectById.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", id);
        return await ReadSingleRelicAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Relic>> SearchByNameAsync(
        DbConnection connection,
        string keyword,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Relic/SearchByName.sql");
        RepositoryCommandHelper.AddParameter(command, "$keyword", keyword);
        return await ReadRelicsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Relic>> GetByColorAsync(
        DbConnection connection,
        RelicColor color,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Relic/SelectByColor.sql");
        RepositoryCommandHelper.AddParameter(command, "$color", (int)color);
        return await ReadRelicsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> InsertAsync(
        DbConnection connection,
        Relic relic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relic);

        await using (var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Relic/Insert.sql"))
        {
            RepositoryCommandHelper.AddParameter(command, "$name", relic.Name);
            RepositoryCommandHelper.AddParameter(command, "$color", (int)relic.Color);
            RepositoryCommandHelper.AddParameter(command, "$memo", relic.Memo);
            RepositoryCommandHelper.AddParameter(command, "$createdAt", ModelDataReader.FormatDateTimeOffset(relic.CreatedAt));
            RepositoryCommandHelper.AddParameter(command, "$updatedAt", ModelDataReader.FormatDateTimeOffset(relic.UpdatedAt));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return await RepositoryCommandHelper.GetLastInsertRowIdAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        DbConnection connection,
        Relic relic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relic);

        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Relic/Update.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", relic.Id);
        RepositoryCommandHelper.AddParameter(command, "$name", relic.Name);
        RepositoryCommandHelper.AddParameter(command, "$color", (int)relic.Color);
        RepositoryCommandHelper.AddParameter(command, "$memo", relic.Memo);
        RepositoryCommandHelper.AddParameter(command, "$updatedAt", ModelDataReader.FormatDateTimeOffset(relic.UpdatedAt));
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Relic/Delete.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", id);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    private static async Task<IReadOnlyList<Relic>> ReadRelicsAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var list = new List<Relic>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(ModelDataReader.ReadRelic(reader));
        }

        return list;
    }

    private static async Task<Relic?> ReadSingleRelicAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ModelDataReader.ReadRelic(reader);
    }
}
