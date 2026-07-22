namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 重複判定の結果を表します。
/// </summary>
public sealed class DuplicateCheckResult
{
    /// <summary>
    /// 採用された効果を取得します。
    /// </summary>
    public required IReadOnlyList<Effect> AppliedEffects { get; init; }

    /// <summary>
    /// 無効化された効果を取得します。
    /// </summary>
    public required IReadOnlyList<Effect> IgnoredEffects { get; init; }
}
