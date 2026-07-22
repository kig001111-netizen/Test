using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Services.Calculation;

/// <summary>
/// 効果の重複判定を行います。
/// </summary>
/// <remarks>
/// CanStack=true: 全件採用。
/// CanStack=false: 同一 EffectId は先頭の1件のみ採用（EffectId が異なれば併用可）。
/// </remarks>
public sealed class DuplicateChecker
{
    /// <summary>
    /// 効果一覧に対して重複判定を実行します。
    /// </summary>
    /// <param name="effects">判定対象の効果一覧（入力順を維持）。</param>
    /// <returns>採用・無効化の結果。</returns>
    public DuplicateCheckResult Check(IReadOnlyList<Effect> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        var applied = new List<Effect>(effects.Count);
        var ignored = new List<Effect>();
        var seenNonStackableEffectIds = new HashSet<int>();

        foreach (var effect in effects)
        {
            if (effect.CanStack)
            {
                applied.Add(effect);
                continue;
            }

            if (seenNonStackableEffectIds.Add(effect.EffectId))
            {
                applied.Add(effect);
            }
            else
            {
                ignored.Add(effect);
            }
        }

        return new DuplicateCheckResult
        {
            AppliedEffects = applied,
            IgnoredEffects = ignored
        };
    }
}
