namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// ビルドの保存リクエストを表します。
/// </summary>
public sealed class BuildUpsertRequest
{
    /// <summary>
    /// 更新時のビルド ID。新規保存時は null です。
    /// </summary>
    public int? Id { get; init; }

    /// <summary>
    /// ビルド名を取得します。
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// キャラクター名を取得します。
    /// </summary>
    public required string CharacterName { get; init; }

    /// <summary>
    /// 武器名を取得します。
    /// </summary>
    public required string WeaponName { get; init; }

    /// <summary>
    /// 装備位置 1〜6 に割り当てる遺物 ID を取得します。未装備は null です。
    /// </summary>
    public required IReadOnlyList<int?> RelicIdsByPosition { get; init; }
}

/// <summary>
/// ビルドとその装備スロット詳細を表します。
/// </summary>
public sealed class BuildDetail
{
    /// <summary>
    /// ビルド本体を取得します。
    /// </summary>
    public required Build Build { get; init; }

    /// <summary>
    /// 装備スロット詳細（未装備位置は含まない）を取得します。
    /// </summary>
    public required IReadOnlyList<BuildRelicSlot> Slots { get; init; }
}

/// <summary>
/// ビルド装備位置と遺物の組を表します。
/// </summary>
public sealed class BuildRelicSlot
{
    /// <summary>
    /// 装備位置（1〜6）を取得します。
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// 装備中の遺物を取得します。
    /// </summary>
    public required Relic Relic { get; init; }
}
