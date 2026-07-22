using Microsoft.Data.Sqlite;
using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Data.Repositories;
using NightreignRelicSimulator.Services.Common;

namespace NightreignRelicSimulator.Services.Builds;

/// <summary>
/// ビルドの業務処理を実装します。
/// </summary>
public sealed class BuildService : IBuildService
{
    private readonly IBuildRepository _buildRepository;
    private readonly IBuildRelicRepository _buildRelicRepository;
    private readonly IRelicRepository _relicRepository;

    /// <summary>
    /// <see cref="BuildService"/> の新しいインスタンスを初期化します。
    /// </summary>
    public BuildService()
        : this(new BuildRepository(), new BuildRelicRepository(), new RelicRepository())
    {
    }

    /// <summary>
    /// テスト用コンストラクタです。
    /// </summary>
    public BuildService(
        IBuildRepository buildRepository,
        IBuildRelicRepository buildRelicRepository,
        IRelicRepository relicRepository)
    {
        _buildRepository = buildRepository ?? throw new ArgumentNullException(nameof(buildRepository));
        _buildRelicRepository = buildRelicRepository ?? throw new ArgumentNullException(nameof(buildRelicRepository));
        _relicRepository = relicRepository ?? throw new ArgumentNullException(nameof(relicRepository));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Build>> GetAllAsync(CancellationToken cancellationToken = default) =>
        SqliteSession.ExecuteAsync(
            (connection, token) => _buildRepository.GetAllAsync(connection, cancellationToken: token),
            cancellationToken);

    /// <inheritdoc />
    public Task<BuildDetail?> LoadAsync(int id, CancellationToken cancellationToken = default) =>
        SqliteSession.ExecuteAsync(
            async (connection, token) =>
            {
                var build = await _buildRepository.GetByIdAsync(connection, id, cancellationToken: token)
                    .ConfigureAwait(false);
                if (build is null)
                {
                    return null;
                }

                return await BuildDetailAsync(connection, build, token).ConfigureAwait(false);
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Build>> SearchByNameAsync(string keyword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        return SqliteSession.ExecuteAsync(
            (connection, token) => _buildRepository.SearchByNameAsync(connection, keyword, cancellationToken: token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Build>> GetByCharacterNameAsync(
        string characterName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(characterName);
        return SqliteSession.ExecuteAsync(
            (connection, token) =>
                _buildRepository.GetByCharacterNameAsync(connection, characterName, cancellationToken: token),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveAsync(BuildUpsertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateSaveRequest(request);

        try
        {
            return await SqliteSession.ExecuteInTransactionAsync(
                async (connection, transaction, token) =>
                {
                    await EnsureRelicsExistAsync(connection, transaction, request.RelicIdsByPosition, token)
                        .ConfigureAwait(false);

                    var now = DateTimeOffset.UtcNow;
                    int buildId;

                    if (request.Id is null)
                    {
                        var build = new Build
                        {
                            Name = request.Name.Trim(),
                            CharacterName = request.CharacterName?.Trim() ?? string.Empty,
                            WeaponName = request.WeaponName?.Trim() ?? string.Empty,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        buildId = await _buildRepository
                            .InsertAsync(connection, build, transaction, token)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        buildId = request.Id.Value;
                        var existing = await _buildRepository
                            .GetByIdAsync(connection, buildId, transaction, token)
                            .ConfigureAwait(false);
                        if (existing is null)
                        {
                            throw new ServiceException($"ビルド ID={buildId} が見つかりません。");
                        }

                        existing.Name = request.Name.Trim();
                        existing.CharacterName = request.CharacterName?.Trim() ?? string.Empty;
                        existing.WeaponName = request.WeaponName?.Trim() ?? string.Empty;
                        existing.UpdatedAt = now;

                        var updated = await _buildRepository
                            .UpdateAsync(connection, existing, transaction, token)
                            .ConfigureAwait(false);
                        if (!updated)
                        {
                            throw new ServiceException($"ビルド ID={buildId} の更新に失敗しました。");
                        }

                        await _buildRelicRepository
                            .DeleteByBuildIdAsync(connection, buildId, transaction, token)
                            .ConfigureAwait(false);
                    }

                    for (var index = 0; index < AppConstants.RelicsPerBuild; index++)
                    {
                        var relicId = request.RelicIdsByPosition[index];
                        if (relicId is null)
                        {
                            continue;
                        }

                        await _buildRelicRepository.InsertAsync(
                            connection,
                            new BuildRelic
                            {
                                BuildId = buildId,
                                Position = index + 1,
                                RelicId = relicId.Value
                            },
                            transaction,
                            token).ConfigureAwait(false);
                    }

                    return buildId;
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ServiceException("ビルドの保存に失敗しました。", ex);
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ServiceException("削除対象のビルド ID が不正です。");
        }

        return SqliteSession.ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                await _buildRelicRepository
                    .DeleteByBuildIdAsync(connection, id, transaction, token)
                    .ConfigureAwait(false);

                var deleted = await _buildRepository
                    .DeleteAsync(connection, id, transaction, token)
                    .ConfigureAwait(false);
                if (!deleted)
                {
                    throw new ServiceException($"ビルド ID={id} が見つかりません。");
                }
            },
            cancellationToken);
    }

    private async Task EnsureRelicsExistAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<int?> relicIdsByPosition,
        CancellationToken cancellationToken)
    {
        foreach (var relicId in relicIdsByPosition)
        {
            if (relicId is null)
            {
                continue;
            }

            var relic = await _relicRepository
                .GetByIdAsync(connection, relicId.Value, transaction, cancellationToken)
                .ConfigureAwait(false);
            if (relic is null)
            {
                throw new ServiceException($"遺物 ID={relicId.Value} が存在しません。");
            }
        }
    }

    private async Task<BuildDetail> BuildDetailAsync(
        SqliteConnection connection,
        Build build,
        CancellationToken cancellationToken)
    {
        var slots = await _buildRelicRepository
            .GetByBuildIdAsync(connection, build.Id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var details = new List<BuildRelicSlot>(slots.Count);
        foreach (var slot in slots.OrderBy(s => s.Position))
        {
            var relic = await _relicRepository
                .GetByIdAsync(connection, slot.RelicId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (relic is null)
            {
                continue;
            }

            details.Add(new BuildRelicSlot
            {
                Position = slot.Position,
                Relic = relic
            });
        }

        return new BuildDetail
        {
            Build = build,
            Slots = details
        };
    }

    private static void ValidateSaveRequest(BuildUpsertRequest request)
    {
        if (request.Id is <= 0)
        {
            throw new ServiceException("更新対象のビルド ID が不正です。");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ServiceException("ビルド名は必須です。");
        }

        if (request.RelicIdsByPosition is null || request.RelicIdsByPosition.Count != AppConstants.RelicsPerBuild)
        {
            throw new ServiceException($"装備スロットは {AppConstants.RelicsPerBuild} 件で指定してください。");
        }
    }
}
