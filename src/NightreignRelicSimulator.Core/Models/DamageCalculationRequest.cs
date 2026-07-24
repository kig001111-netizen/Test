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

    /// <summary>
    /// 段階効果の Level 上書き（キー: EffectId、値: Level）を取得または設定します。
    /// </summary>
    public IReadOnlyDictionary<int, int>? LevelOverrides { get; init; }

    /// <summary>
    /// Level 解決に使う効果マスタ（段階効果の全 Level）を取得または設定します。
    /// </summary>
    public IReadOnlyList<Effect>? EffectCatalog { get; init; }
}
