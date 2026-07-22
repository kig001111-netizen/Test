using System.Data.Common;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Data.Repositories;

/// <summary>
/// <see cref="IRelicEffectRepository"/> の SQLite 実装です。
/// </summary>
public sealed class RelicEffectRepository : IRelicEffectRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RelicEffect>> GetByRelicIdAsync(
        DbConnection connection,
        int relicId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "RelicEffect/SelectByRelicId.sql");
        RepositoryCommandHelper.AddParameter(command, "$relicId", relicId);
        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelicEffect>> GetByEffectIdAsync(
        DbConnection connection,
        int effectId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "RelicEffect/SelectByEffectId.sql");
        RepositoryCommandHelper.AddParameter(command, "$effectId", effectId);
        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RelicEffect?> GetByRelicIdAndSlotAsync(
        DbConnection connection,
        int relicId,
        int slotNumber,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "RelicEffect/SelectByRelicIdAndSlot.sql");
        RepositoryCommandHelper.AddParameter(command, "$relicId", relicId);
        RepositoryCommandHelper.AddParameter(command, "$slotNumber", slotNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ModelDataReader.ReadRelicEffect(reader);
    }

    /// <inheritdoc />
    public async Task InsertAsync(
        DbConnection connection,
        RelicEffect relicEffect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relicEffect);

        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "RelicEffect/Insert.sql");
        RepositoryCommandHelper.AddParameter(command, "$relicId", relicEffect.RelicId);
        RepositoryCommandHelper.AddParameter(command, "$slotNumber", relicEffect.SlotNumber);
        RepositoryCommandHelper.AddParameter(command, "$effectId", relicEffect.EffectId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        DbConnection connection,
        RelicEffect relicEffect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relicEffect);

        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "RelicEffect/Update.sql");
        RepositoryCommandHelper.AddParameter(command, "$relicId", relicEffect.RelicId);
        RepositoryCommandHelper.AddParameter(command, "$slotNumber", relicEffect.SlotNumber);
        RepositoryCommandHelper.AddParameter(command, "$effectId", relicEffect.EffectId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        DbConnection connection,
        int relicId,
        int slotNumber,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(connection, transaction, "RelicEffect/Delete.sql");
        RepositoryCommandHelper.AddParameter(command, "$relicId", relicId);
        RepositoryCommandHelper.AddParameter(command, "$slotNumber", slotNumber);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return RepositoryCommandHelper.Affected(rows);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByRelicIdAsync(
        DbConnection connection,
        int relicId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = RepositoryCommandHelper.CreateCommand(
            connection,
            transaction,
            "RelicEffect/DeleteByRelicId.sql");
        RepositoryCommandHelper.AddParameter(command, "$relicId", relicId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<RelicEffect>> ReadListAsync(
        DbCommand command,
        CancellationToken cancellationToken)
    {
        var list = new List<RelicEffect>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(ModelDataReader.ReadRelicEffect(reader));
        }

        return list;
    }
}
