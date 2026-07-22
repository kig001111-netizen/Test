using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Data.Repositories;
using NightreignRelicSimulator.Services.Common;
using Microsoft.Data.Sqlite;

namespace NightreignRelicSimulator.Services.Relics;

/// <summary>
/// 遺物の業務処理を実装します。
/// </summary>
public sealed class RelicService : IRelicService
{
    private readonly IRelicRepository _relicRepository;
    private readonly IRelicEffectRepository _relicEffectRepository;
    private readonly IEffectRepository _effectRepository;
    private readonly IBuildRelicRepository _buildRelicRepository;

    /// <summary>
    /// <see cref="RelicService"/> の新しいインスタンスを初期化します。
    /// </summary>
    public RelicService()
        : this(
            new RelicRepository(),
            new RelicEffectRepository(),
            new EffectRepository(),
            new BuildRelicRepository())
    {
    }

    /// <summary>
    /// テスト用コンストラクタです。
    /// </summary>
    public RelicService(
        IRelicRepository relicRepository,
        IRelicEffectRepository relicEffectRepository,
        IEffectRepository effectRepository,
        IBuildRelicRepository buildRelicRepository)
    {
        _relicRepository = relicRepository ?? throw new ArgumentNullException(nameof(relicRepository));
        _relicEffectRepository = relicEffectRepository ?? throw new ArgumentNullException(nameof(relicEffectRepository));
        _effectRepository = effectRepository ?? throw new ArgumentNullException(nameof(effectRepository));
        _buildRelicRepository = buildRelicRepository ?? throw new ArgumentNullException(nameof(buildRelicRepository));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Relic>> GetAllAsync(CancellationToken cancellationToken = default) =>
        SqliteSession.ExecuteAsync(
            (connection, token) => _relicRepository.GetAllAsync(connection, cancellationToken: token),
            cancellationToken);

    /// <inheritdoc />
    public Task<RelicDetail?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        SqliteSession.ExecuteAsync(
            async (connection, token) =>
            {
                var relic = await _relicRepository.GetByIdAsync(connection, id, cancellationToken: token)
                    .ConfigureAwait(false);
                if (relic is null)
                {
                    return null;
                }

                return await BuildDetailAsync(connection, relic, token).ConfigureAwait(false);
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Relic>> SearchByNameAsync(string keyword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        return SqliteSession.ExecuteAsync(
            (connection, token) => _relicRepository.SearchByNameAsync(connection, keyword, cancellationToken: token),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Relic>> GetByColorAsync(RelicColor color, CancellationToken cancellationToken = default) =>
        SqliteSession.ExecuteAsync(
            (connection, token) => _relicRepository.GetByColorAsync(connection, color, cancellationToken: token),
            cancellationToken);

    /// <inheritdoc />
    public async Task<int> RegisterAsync(RelicUpsertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateUpsertRequest(request, requireId: false);
        return await ExecuteUpsertWithTransactionAsync(request, isUpdate: false, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RelicUpsertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateUpsertRequest(request, requireId: true);
        await ExecuteUpsertWithTransactionAsync(request, isUpdate: true, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ServiceException("削除対象の遺物 ID が不正です。");
        }

        return SqliteSession.ExecuteInTransactionAsync(
            async (connection, transaction, token) =>
            {
                var usages = await _buildRelicRepository
                    .GetByRelicIdAsync(connection, id, transaction, token)
                    .ConfigureAwait(false);
                if (usages.Count > 0)
                {
                    throw new ServiceException(
                        $"遺物 ID={id} はビルドで装備中のため削除できません。参照数={usages.Count}");
                }

                await _relicEffectRepository
                    .DeleteByRelicIdAsync(connection, id, transaction, token)
                    .ConfigureAwait(false);

                var deleted = await _relicRepository
                    .DeleteAsync(connection, id, transaction, token)
                    .ConfigureAwait(false);
                if (!deleted)
                {
                    throw new ServiceException($"遺物 ID={id} が見つかりません。");
                }
            },
            cancellationToken);
    }

    private async Task<int> ExecuteUpsertWithTransactionAsync(
        RelicUpsertRequest request,
        bool isUpdate,
        CancellationToken cancellationToken)
    {
        try
        {
            return await SqliteSession.ExecuteInTransactionAsync(
                async (connection, transaction, token) =>
                {
                    await EnsureEffectsExistAsync(connection, transaction, request.EffectIdsBySlot, token)
                        .ConfigureAwait(false);

                    var now = DateTimeOffset.UtcNow;
                    int relicId;

                    if (isUpdate)
                    {
                        relicId = request.Id!.Value;
                        var existing = await _relicRepository
                            .GetByIdAsync(connection, relicId, transaction, token)
                            .ConfigureAwait(false);
                        if (existing is null)
                        {
                            throw new ServiceException($"遺物 ID={relicId} が見つかりません。");
                        }

                        existing.Name = request.Name.Trim();
                        existing.Color = request.Color;
                        existing.Memo = request.Memo?.Trim() ?? string.Empty;
                        existing.UpdatedAt = now;

                        var updated = await _relicRepository
                            .UpdateAsync(connection, existing, transaction, token)
                            .ConfigureAwait(false);
                        if (!updated)
                        {
                            throw new ServiceException($"遺物 ID={relicId} の更新に失敗しました。");
                        }

                        await _relicEffectRepository
                            .DeleteByRelicIdAsync(connection, relicId, transaction, token)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var relic = new Relic
                        {
                            Name = request.Name.Trim(),
                            Color = request.Color,
                            Memo = request.Memo?.Trim() ?? string.Empty,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        relicId = await _relicRepository
                            .InsertAsync(connection, relic, transaction, token)
                            .ConfigureAwait(false);
                    }

                    for (var index = 0; index < AppConstants.EffectsPerRelic; index++)
                    {
                        var effectId = request.EffectIdsBySlot[index];
                        if (effectId is null)
                        {
                            continue;
                        }

                        await _relicEffectRepository.InsertAsync(
                            connection,
                            new RelicEffect
                            {
                                RelicId = relicId,
                                SlotNumber = index + 1,
                                EffectId = effectId.Value
                            },
                            transaction,
                            token).ConfigureAwait(false);
                    }

                    return relicId;
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ServiceException(isUpdate ? "遺物の更新に失敗しました。" : "遺物の登録に失敗しました。", ex);
        }
    }

    private async Task EnsureEffectsExistAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<int?> effectIdsBySlot,
        CancellationToken cancellationToken)
    {
        foreach (var effectId in effectIdsBySlot)
        {
            if (effectId is null)
            {
                continue;
            }

            var effect = await _effectRepository
                .GetByIdAsync(connection, effectId.Value, transaction, cancellationToken)
                .ConfigureAwait(false);
            if (effect is null)
            {
                throw new ServiceException($"効果 ID={effectId.Value} が存在しません。");
            }
        }
    }

    private async Task<RelicDetail> BuildDetailAsync(
        SqliteConnection connection,
        Relic relic,
        CancellationToken cancellationToken)
    {
        var slots = await _relicEffectRepository
            .GetByRelicIdAsync(connection, relic.Id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var details = new List<RelicEffectSlot>(slots.Count);
        foreach (var slot in slots.OrderBy(s => s.SlotNumber))
        {
            var effect = await _effectRepository
                .GetByIdAsync(connection, slot.EffectId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (effect is null)
            {
                continue;
            }

            details.Add(new RelicEffectSlot
            {
                SlotNumber = slot.SlotNumber,
                Effect = effect
            });
        }

        return new RelicDetail
        {
            Relic = relic,
            Slots = details
        };
    }

    private static void ValidateUpsertRequest(RelicUpsertRequest request, bool requireId)
    {
        if (requireId)
        {
            if (request.Id is null or <= 0)
            {
                throw new ServiceException("更新対象の遺物 ID が不正です。");
            }
        }
        else if (request.Id is not null)
        {
            throw new ServiceException("新規登録時に遺物 ID を指定できません。");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ServiceException("遺物名は必須です。");
        }

        if (request.EffectIdsBySlot is null || request.EffectIdsBySlot.Count != AppConstants.EffectsPerRelic)
        {
            throw new ServiceException($"効果スロットは {AppConstants.EffectsPerRelic} 件で指定してください。");
        }
    }
}
