namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 火力計算結果を表します。計算時のみ生成され、SQLite には保存しません。
/// </summary>
public sealed class CalculationResult
{
    /// <summary>
    /// 武器表示火力（計算入力）を取得または設定します。
    /// </summary>
    public decimal BaseAttack { get; set; }

    /// <summary>
    /// 適用倍率の総乗算値を取得または設定します。
    /// </summary>
    public decimal TotalMultiplier { get; set; }

    /// <summary>
    /// 最終火力を取得または設定します。
    /// </summary>
    public decimal FinalAttack { get; set; }

    /// <summary>
    /// 適用された効果の一覧を取得または設定します。
    /// </summary>
    public required IReadOnlyList<Effect> AppliedEffects { get; set; }

    /// <summary>
    /// 重複判定などにより無効化された効果の一覧を取得または設定します。
    /// </summary>
    public required IReadOnlyList<Effect> IgnoredEffects { get; set; }

    /// <summary>
    /// 計算ログの一覧を取得または設定します。
    /// </summary>
    public IReadOnlyList<CalculationLog> Logs { get; set; } = Array.Empty<CalculationLog>();
}
