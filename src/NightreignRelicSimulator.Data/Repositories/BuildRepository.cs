using System.Data.Common;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Data.Repositories;

/// <summary>
/// <see cref="IBuildRepository"/> の SQLite 実装です。
/// </summary>
public sealed class BuildRepository : IBuildRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Build>> GetAllAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Build/SelectAll.sql");
        return await ReadBuildsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Build?> GetByIdAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Build/SelectById.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", id);
        return await ReadSingleBuildAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Build>> SearchByNameAsync(
        DbConnection connection,
        string keyword,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Build/SearchByName.sql");
        RepositoryCommandHelper.AddParameter(command, "$keyword", keyword);
        return await ReadBuildsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Build>> GetByCharacterNameAsync(
        DbConnection connection,
        string characterName,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characterName);
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "Build/SelectByCharacterName.sql");
        RepositoryCommandHelper.AddParameter(command, "$characterName", characterName);
        return await ReadBuildsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> InsertAsync(
        DbConnection connection,
        Build build,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(build);

        await using (var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Build/Insert.sql"))
        {
            RepositoryCommandHelper.AddParameter(command, "$name", build.Name);
            RepositoryCommandHelper.AddParameter(command, "$characterName", build.CharacterName);
            RepositoryCommandHelper.AddParameter(command, "$weaponName", build.WeaponName);
            RepositoryCommandHelper.AddParameter(command, "$createdAt", ModelDataReader.FormatDateTimeOffset(build.CreatedAt));
            RepositoryCommandHelper.AddParameter(command, "$updatedAt", ModelDataReader.FormatDateTimeOffset(build.UpdatedAt));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return await RepositoryCommandHelper.GetLastInsertRowIdAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        DbConnection connection,
        Build build,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(build);

        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Build/Update.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", build.Id);
        RepositoryCommandHelper.AddParameter(command, "$name", build.Name);
        RepositoryCommandHelper.AddParameter(command, "$characterName", build.CharacterName);
        RepositoryCommandHelper.AddParameter(command, "$weaponName", build.WeaponName);
        RepositoryCommandHelper.AddParameter(command, "$updatedAt", ModelDataReader.FormatDateTimeOffset(build.UpdatedAt));
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
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Build/Delete.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", id);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    private static async Task<IReadOnlyList<Build>> ReadBuildsAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var list = new List<Build>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(ModelDataReader.ReadBuild(reader));
        }

        return list;
    }

    private static async Task<Build?> ReadSingleBuildAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ModelDataReader.ReadBuild(reader);
    }
}
