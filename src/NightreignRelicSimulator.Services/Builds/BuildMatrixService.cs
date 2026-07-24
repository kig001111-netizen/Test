using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Services.Calculation;
using NightreignRelicSimulator.Services.Effects;
using NightreignRelicSimulator.Services.Relics;

namespace NightreignRelicSimulator.Services.Builds;

/// <summary>
/// 火力計算マトリクスとビルド／専用遺物の同期を実装します。
/// </summary>
public sealed class BuildMatrixService : IBuildMatrixService
{
    private readonly IBuildService _builds;
    private readonly IRelicService _relics;
    private readonly IEffectService _effects;

    public BuildMatrixService()
        : this(new BuildService(), new RelicService(), new EffectService())
    {
    }

    public BuildMatrixService(IBuildService builds, IRelicService relics, IEffectService effects)
    {
        _builds = builds ?? throw new ArgumentNullException(nameof(builds));
        _relics = relics ?? throw new ArgumentNullException(nameof(relics));
        _effects = effects ?? throw new ArgumentNullException(nameof(effects));
    }

    /// <inheritdoc />
    public async Task<BuildMatrixDetail?> LoadAsync(int buildId, CancellationToken cancellationToken = default)
    {
        var detail = await _builds.LoadAsync(buildId, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return null;
        }

        var catalog = await _effects.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var collapsed = StagedEffectResolver.CollapseForRelicSelection(catalog)
            .ToDictionary(e => e.EffectId, e => e.Id);

        var columns = new List<IReadOnlyList<int>>(AppConstants.RelicsPerBuild);
        for (var position = 1; position <= AppConstants.RelicsPerBuild; position++)
        {
            var slot = detail.Slots.FirstOrDefault(s => s.Position == position);
            if (slot is null)
            {
                columns.Add(Array.Empty<int>());
                continue;
            }

            var relicDetail = await _relics.GetDetailAsync(slot.Relic.Id, cancellationToken).ConfigureAwait(false);
            if (relicDetail is null)
            {
                columns.Add(Array.Empty<int>());
                continue;
            }

            var ids = relicDetail.Slots
                .OrderBy(s => s.SlotNumber)
                .Select(s => collapsed.TryGetValue(s.Effect.EffectId, out var rowId) ? rowId : s.Effect.Id)
                .Take(AppConstants.EffectsPerRelic)
                .ToList();
            columns.Add(ids);
        }

        return new BuildMatrixDetail
        {
            Build = detail.Build,
            EffectIdsByRelic = columns
        };
    }

    /// <inheritdoc />
    public async Task<int> SaveAsync(BuildMatrixUpsertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var previousRelicIds = new HashSet<int>();
        var byPosition = new Dictionary<int, int>();
        if (request.Id is int existingId)
        {
            var previous = await _builds.LoadAsync(existingId, cancellationToken).ConfigureAwait(false);
            if (previous is null)
            {
                throw new ServiceException($"ビルド ID={existingId} が見つかりません。");
            }

            foreach (var slot in previous.Slots)
            {
                previousRelicIds.Add(slot.Relic.Id);
                byPosition[slot.Position] = slot.Relic.Id;
            }
        }

        var name = request.Name.Trim();
        var relicIdsByPosition = new int?[AppConstants.RelicsPerBuild];
        var usedRelicIds = new HashSet<int>();

        for (var index = 0; index < AppConstants.RelicsPerBuild; index++)
        {
            var effectIds = request.EffectIdsByRelic[index];
            if (effectIds.Count == 0)
            {
                continue;
            }

            var slots = new int?[AppConstants.EffectsPerRelic];
            for (var i = 0; i < effectIds.Count; i++)
            {
                slots[i] = effectIds[i];
            }

            var position = index + 1;
            int relicId;
            if (byPosition.TryGetValue(position, out var existingRelicId))
            {
                await _relics.UpdateAsync(
                    new RelicUpsertRequest
                    {
                        Id = existingRelicId,
                        Name = $"{name} #{position}",
                        Color = RelicColor.None,
                        Memo = "matrix",
                        EffectIdsBySlot = slots
                    },
                    cancellationToken).ConfigureAwait(false);
                relicId = existingRelicId;
            }
            else
            {
                relicId = await _relics.RegisterAsync(
                    new RelicUpsertRequest
                    {
                        Name = $"{name} #{position}",
                        Color = RelicColor.None,
                        Memo = "matrix",
                        EffectIdsBySlot = slots
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            relicIdsByPosition[index] = relicId;
            usedRelicIds.Add(relicId);
        }

        // 先に未使用 Position の BuildRelic を外すため Save。削除は Save 後に行う
        var buildId = await _builds.SaveAsync(
            new BuildUpsertRequest
            {
                Id = request.Id,
                Name = name,
                CharacterName = request.CharacterName?.Trim() ?? string.Empty,
                WeaponName = request.WeaponName?.Trim() ?? string.Empty,
                RelicIdsByPosition = relicIdsByPosition
            },
            cancellationToken).ConfigureAwait(false);

        foreach (var oldId in previousRelicIds.Where(id => !usedRelicIds.Contains(id)))
        {
            try
            {
                await _relics.DeleteAsync(oldId, cancellationToken).ConfigureAwait(false);
            }
            catch (ServiceException)
            {
                // 参照残存などは無視
            }
        }

        return buildId;
    }

    private static void Validate(BuildMatrixUpsertRequest request)
    {
        if (request.Id is <= 0)
        {
            throw new ServiceException("更新対象のビルド ID が不正です。");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ServiceException("ビルド名は必須です。");
        }

        if (request.EffectIdsByRelic is null || request.EffectIdsByRelic.Count != AppConstants.RelicsPerBuild)
        {
            throw new ServiceException($"遺物列は {AppConstants.RelicsPerBuild} 件で指定してください。");
        }

        for (var i = 0; i < request.EffectIdsByRelic.Count; i++)
        {
            var column = request.EffectIdsByRelic[i]
                         ?? throw new ServiceException($"遺物{i + 1} の効果が不正です。");
            if (column.Count > AppConstants.EffectsPerRelic)
            {
                throw new ServiceException(
                    $"遺物{i + 1} に設定できる効果は最大 {AppConstants.EffectsPerRelic} 件です。");
            }

            if (column.Any(id => id <= 0))
            {
                throw new ServiceException($"遺物{i + 1} に不正な効果 ID が含まれています。");
            }

            if (column.Distinct().Count() != column.Count)
            {
                throw new ServiceException($"遺物{i + 1} に同じ効果が重複しています。");
            }
        }
    }
}
