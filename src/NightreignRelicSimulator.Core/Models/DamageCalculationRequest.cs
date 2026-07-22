namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 火力計算の入力を表します。
/// </summary>
public sealed class DamageCalculationRequest
{
    /// <summary>
    /// 武器表示火力を取得または設定します。
    /// </summary>
    public decimal WeaponAttack { get; init; }

    /// <summary>
    /// 装備などから集約した効果一覧を取得または設定します。
    /// </summary>
    public required IReadOnlyList<Effect> Effects { get; init; }
}
