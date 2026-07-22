namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 火力計算の1ステップ分のログを表します。SQLite には保存しません。
/// </summary>
public sealed class CalculationLog
{
    /// <summary>
    /// 計算ステップ番号を取得または設定します。
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// ステップの説明を取得または設定します。
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// このステップで適用した倍率を取得または設定します。適用がない場合は null です。
    /// </summary>
    public decimal? Multiplier { get; set; }

    /// <summary>
    /// このステップ後の攻撃力を取得または設定します。
    /// </summary>
    public decimal CurrentAttack { get; set; }
}
