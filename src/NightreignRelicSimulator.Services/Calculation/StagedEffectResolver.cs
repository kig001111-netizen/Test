using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Services.Calculation;

/// <summary>
/// 段階効果（同一 EffectId・複数 Level）の解決を行います。
/// </summary>
/// <remarks>
/// 封牢の囚・夜の侵入者・攻撃連続時など、遺物では1効果として扱い、
/// 計算時に Level を指定して倍率を切り替える用途向けです。
/// </remarks>
public static class StagedEffectResolver
{
    /// <summary>
    /// マスタ上で複数 Level を持つ EffectId の定義を取得します。
    /// </summary>
    public static IReadOnlyList<StagedEffectDefinition> GetDefinitions(IReadOnlyList<Effect> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return catalog
            .GroupBy(e => e.EffectId)
            .Where(g => g.Select(x => x.Level).Distinct().Count() > 1)
            .OrderBy(g => g.Min(x => x.DisplayOrder))
            .Select(g =>
            {
                var ordered = g.OrderBy(x => x.Level).ToList();
                return new StagedEffectDefinition
                {
                    EffectId = g.Key,
                    Name = ordered[0].Name,
                    Category = ordered[0].Category,
                    CanStack = ordered[0].CanStack,
                    Levels = ordered
                        .Select(x => new StagedEffectLevel
                        {
                            RowId = x.Id,
                            Level = x.Level,
                            Value = x.Value
                        })
                        .ToList()
                };
            })
            .ToList();
    }

    /// <summary>
    /// 遺物スロット用に、段階効果は代表行（最小 Level）のみ残した一覧を返します。
    /// </summary>
    public static IReadOnlyList<Effect> CollapseForRelicSelection(IReadOnlyList<Effect> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return catalog
            .GroupBy(e => e.EffectId)
            .Select(g => g.OrderBy(x => x.Level).ThenBy(x => x.Id).First())
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.EffectId)
            .ToList();
    }

    /// <summary>
    /// 装備中効果に Level 上書きを適用します。段階効果でないものはそのままです。
    /// </summary>
    public static IReadOnlyList<Effect> ApplyLevelOverrides(
        IReadOnlyList<Effect> equipped,
        IReadOnlyList<Effect> catalog,
        IReadOnlyDictionary<int, int>? levelOverrides)
    {
        ArgumentNullException.ThrowIfNull(equipped);
        ArgumentNullException.ThrowIfNull(catalog);

        var byEffectId = catalog
            .GroupBy(e => e.EffectId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Effect>)g.OrderBy(x => x.Level).ToList());

        var result = new List<Effect>(equipped.Count);
        foreach (var effect in equipped)
        {
            if (!byEffectId.TryGetValue(effect.EffectId, out var levels) || levels.Count <= 1)
            {
                result.Add(effect);
                continue;
            }

            var targetLevel = levelOverrides is not null
                              && levelOverrides.TryGetValue(effect.EffectId, out var overrideLevel)
                ? overrideLevel
                : effect.Level;

            var resolved = levels.FirstOrDefault(l => l.Level == targetLevel) ?? levels[0];
            result.Add(resolved);
        }

        return result;
    }
}

/// <summary>
/// 段階効果の定義です。
/// </summary>
public sealed class StagedEffectDefinition
{
    public int EffectId { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public bool CanStack { get; init; }
    public required IReadOnlyList<StagedEffectLevel> Levels { get; init; }
}

/// <summary>
/// 段階効果の1レベル分です。
/// </summary>
public sealed class StagedEffectLevel
{
    public int RowId { get; init; }
    public int Level { get; init; }
    public decimal Value { get; init; }
}
