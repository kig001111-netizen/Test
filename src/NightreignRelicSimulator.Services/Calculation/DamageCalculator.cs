using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Services.Calculation;

/// <summary>
/// 遺物効果に基づく火力計算を行います。Relic は扱いません（Effect 一覧のみ入力）。
/// </summary>
/// <remarks>
/// 処理順: 重複判定 → 条件判定 → Level に応じた Value 取得 → 乗算 → 武器表示火力へ適用。
/// Excel の計算結果を唯一の正解とします。
/// </remarks>
public sealed class DamageCalculator
{
    private readonly DuplicateChecker _duplicateChecker;

    /// <summary>
    /// <see cref="DamageCalculator"/> の新しいインスタンスを初期化します。
    /// </summary>
    public DamageCalculator()
        : this(new DuplicateChecker())
    {
    }

    /// <summary>
    /// テスト用コンストラクタです。
    /// </summary>
    public DamageCalculator(DuplicateChecker duplicateChecker)
    {
        _duplicateChecker = duplicateChecker ?? throw new ArgumentNullException(nameof(duplicateChecker));
    }

    /// <summary>
    /// 火力を計算します。
    /// </summary>
    /// <param name="request">武器表示火力と効果一覧。</param>
    /// <returns>計算結果（非永続）。</returns>
    public CalculationResult Calculate(DamageCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Effects);

        if (request.WeaponAttack < 0m)
        {
            throw new ServiceException("武器表示火力は 0 以上を指定してください。");
        }

        var logs = new List<CalculationLog>();
        var step = 0;

        // ① Effect 一覧（入力済み）+ 段階 Level 解決
        var effects = request.Effects;
        if (request.EffectCatalog is { Count: > 0 })
        {
            effects = StagedEffectResolver.ApplyLevelOverrides(
                request.Effects,
                request.EffectCatalog,
                request.LevelOverrides);
        }

        logs.Add(CreateLog(++step, $"効果入力件数={effects.Count}", null, request.WeaponAttack));

        // ② DuplicateChecker
        var duplicateResult = _duplicateChecker.Check(effects);
        logs.Add(CreateLog(
            ++step,
            $"重複判定: 採用={duplicateResult.AppliedEffects.Count}, 無効={duplicateResult.IgnoredEffects.Count}",
            null,
            request.WeaponAttack));

        // ③ 条件判定（現段階は全件通過。将来 Excel 条件をここに追加）
        var conditioned = ApplyConditions(duplicateResult.AppliedEffects);
        logs.Add(CreateLog(++step, $"条件判定後件数={conditioned.Count}", null, request.WeaponAttack));

        // ④ Level に応じた Value 取得 + ⑤ 乗算
        var totalMultiplier = AppConstants.BaseMultiplier;
        var currentAttack = request.WeaponAttack;
        var resolvedEffects = new List<Effect>(conditioned.Count);

        foreach (var effect in conditioned)
        {
            var value = ResolveValueForLevel(effect);
            totalMultiplier *= value;
            currentAttack = request.WeaponAttack * totalMultiplier;
            resolvedEffects.Add(effect);
            logs.Add(CreateLog(
                ++step,
                $"適用: EffectId={effect.EffectId} Lv{effect.Level} {effect.Name}",
                value,
                currentAttack));
        }

        // ⑥ 武器表示火力へ適用
        var finalAttack = request.WeaponAttack * totalMultiplier;
        logs.Add(CreateLog(++step, "最終火力算出", totalMultiplier, finalAttack));

        // ⑦ CalculationResult
        return new CalculationResult
        {
            BaseAttack = request.WeaponAttack,
            TotalMultiplier = totalMultiplier,
            FinalAttack = finalAttack,
            AppliedEffects = resolvedEffects,
            IgnoredEffects = duplicateResult.IgnoredEffects,
            Logs = logs
        };
    }

    /// <summary>
    /// 条件判定を行います。現段階では入力をそのまま返します。
    /// </summary>
    private static IReadOnlyList<Effect> ApplyConditions(IReadOnlyList<Effect> effects) => effects;

    /// <summary>
    /// Level に対応する倍率を取得します。マスタ行の <see cref="Effect.Value"/> を使用します。
    /// </summary>
    private static decimal ResolveValueForLevel(Effect effect)
    {
        if (effect.Value <= 0m)
        {
            throw new ServiceException(
                $"EffectId={effect.EffectId} Level={effect.Level} の Value が不正です。");
        }

        return effect.Value;
    }

    private static CalculationLog CreateLog(int step, string description, decimal? multiplier, decimal currentAttack)
    {
        return new CalculationLog
        {
            Step = step,
            Description = description,
            Multiplier = multiplier,
            CurrentAttack = currentAttack
        };
    }
}
