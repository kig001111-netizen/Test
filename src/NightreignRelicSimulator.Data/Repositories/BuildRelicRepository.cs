using System.Data.Common;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Data.Repositories;

/// <summary>
/// <see cref="IBuildRelicRepository"/> の SQLite 実装です。
/// </summary>
public sealed class BuildRelicRepository : IBuildRelicRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BuildRelic>> GetByBuildIdAsync(
        DbConnection connection,
        int buildId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "BuildRelic/SelectByBuildId.sql");
        RepositoryCommandHelper.AddParameter(command, "$buildId", buildId);
        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BuildRelic>> GetByRelicIdAsync(
        DbConnection connection,
        int relicId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "BuildRelic/SelectByRelicId.sql");
        RepositoryCommandHelper.AddParameter(command, "$relicId", relicId);
        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BuildRelic?> GetByBuildIdAndPositionAsync(
        DbConnection connection,
        int buildId,
        int position,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "BuildRelic/SelectByBuildIdAndPosition.sql");
        RepositoryCommandHelper.AddParameter(command, "$buildId", buildId);
        RepositoryCommandHelper.AddParameter(command, "$position", position);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ModelDataReader.ReadBuildRelic(reader);
    }

    /// <inheritdoc />
    public async Task InsertAsync(
        DbConnection connection,
        BuildRelic buildRelic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buildRelic);

        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "BuildRelic/Insert.sql");
        RepositoryCommandHelper.AddParameter(command, "$buildId", buildRelic.BuildId);
        RepositoryCommandHelper.AddParameter(command, "$position", buildRelic.Position);
        RepositoryCommandHelper.AddParameter(command, "$relicId", buildRelic.RelicId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        DbConnection connection,
        BuildRelic buildRelic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buildRelic);

        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "BuildRelic/Update.sql");
        RepositoryCommandHelper.AddParameter(command, "$buildId", buildRelic.BuildId);
        RepositoryCommandHelper.AddParameter(command, "$position", buildRelic.Position);
        RepositoryCommandHelper.AddParameter(command, "$relicId", buildRelic.RelicId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        DbConnection connection,
        int buildId,
        int position,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "BuildRelic/Delete.sql");
        RepositoryCommandHelper.AddParameter(command, "$buildId", buildId);
        RepositoryCommandHelper.AddParameter(command, "$position", position);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByBuildIdAsync(
        DbConnection connection,
        int buildId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "BuildRelic/DeleteByBuildId.sql");
        RepositoryCommandHelper.AddParameter(command, "$buildId", buildId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<BuildRelic>> ReadListAsync(
        DbCommand command,
        CancellationToken cancellationToken)
    {
        var list = new List<BuildRelic>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(ModelDataReader.ReadBuildRelic(reader));
        }

        return list;
    }
}
