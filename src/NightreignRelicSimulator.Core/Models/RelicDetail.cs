using NightreignRelicSimulator.Core.Enums;

namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 遺物の登録・更新リクエストを表します。
/// </summary>
public sealed class RelicUpsertRequest
{
    /// <summary>
    /// 更新時の遺物 ID。新規登録時は null です。
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// 遺物名を取得します。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 遺物の色を取得します。
    /// </summary>
    public RelicColor Color { get; init; }

    /// <summary>
    /// メモを取得します。
    /// </summary>
    public required string Memo { get; init; }

    /// <summary>
    /// スロット 1〜3 に割り当てる効果 ID を取得します。未設定スロットは null です。
    /// </summary>
    public required IReadOnlyList<int?> EffectIdsBySlot { get; init; }
}

/// <summary>
/// 遺物とその効果スロット詳細を表します。
/// </summary>
public sealed class RelicDetail
{
    /// <summary>
    /// 遺物本体を取得します。
    /// </summary>
    public required Relic Relic { get; init; }

    /// <summary>
    /// スロットに割り当てられた効果（未設定スロットは含まない）を取得します。
    /// </summary>
    public required IReadOnlyList<RelicEffectSlot> Slots { get; init; }
}

/// <summary>
/// 遺物効果スロットと効果マスタの組を表します。
/// </summary>
public sealed class RelicEffectSlot
{
    /// <summary>
    /// スロット番号（1〜3）を取得します。
    /// </summary>
    public int SlotNumber { get; init; }

    /// <summary>
    /// 効果を取得します。
    /// </summary>
    public required Effect Effect { get; init; }
}
