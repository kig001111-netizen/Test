using System.Data.Common;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Data.Repositories;

/// <summary>
/// <see cref="IEffectRepository"/> の SQLite 実装です。
/// </summary>
public sealed class EffectRepository : IEffectRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Effect>> GetAllAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Effect/SelectAll.sql");
        return await ReadEffectsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Effect?> GetByIdAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Effect/SelectById.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", id);
        return await ReadSingleEffectAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Effect>> SearchByNameAsync(
        DbConnection connection,
        string keyword,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Effect/SearchByName.sql");
        RepositoryCommandHelper.AddParameter(command, "$keyword", keyword);
        return await ReadEffectsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Effect>> GetByCategoryAsync(
        DbConnection connection,
        string category,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Effect/SelectByCategory.sql");
        RepositoryCommandHelper.AddParameter(command, "$category", category);
        return await ReadEffectsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> InsertAsync(
        DbConnection connection,
        Effect effect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        await using (var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Effect/Insert.sql"))
        {
            BindEffect(command, effect, includeId: false);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return await RepositoryCommandHelper.GetLastInsertRowIdAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        DbConnection connection,
        Effect effect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Effect/Update.sql");
        BindEffect(command, effect, includeId: true);
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
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "Effect/Delete.sql");
        RepositoryCommandHelper.AddParameter(command, "$id", id);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    private static void BindEffect(DbCommand command, Effect effect, bool includeId)
    {
        if (includeId)
        {
            RepositoryCommandHelper.AddParameter(command, "$id", effect.Id);
        }

        RepositoryCommandHelper.AddParameter(command, "$effectId", effect.EffectId);
        RepositoryCommandHelper.AddParameter(command, "$name", effect.Name);
        RepositoryCommandHelper.AddParameter(command, "$category", effect.Category);
        RepositoryCommandHelper.AddParameter(command, "$canStack", effect.CanStack ? 1 : 0);
        RepositoryCommandHelper.AddParameter(command, "$value", effect.Value);
        RepositoryCommandHelper.AddParameter(command, "$level", effect.Level);
        RepositoryCommandHelper.AddParameter(command, "$description", effect.Description);
        RepositoryCommandHelper.AddParameter(command, "$displayOrder", effect.DisplayOrder);
    }

    private static async Task<IReadOnlyList<Effect>> ReadEffectsAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var list = new List<Effect>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(ModelDataReader.ReadEffect(reader));
        }

        return list;
    }

    private static async Task<Effect?> ReadSingleEffectAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ModelDataReader.ReadEffect(reader);
    }
}
