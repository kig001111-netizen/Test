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

/// <summary>
/// マトリクス形式のビルド保存リクエストです。
/// </summary>
/// <remarks>
/// <see cref="EffectIdsByRelic"/> は長さ 6。各要素は Effect 行 Id の配列（0〜3件）。
/// </remarks>
public sealed class BuildMatrixUpsertRequest
{
    public int? Id { get; init; }
    public required string Name { get; init; }
    public required string CharacterName { get; init; }
    public required string WeaponName { get; init; }

    /// <summary>
    /// 遺物列 1〜6 ごとの Effect 行 Id 一覧（各列最大 3）。
    /// </summary>
    public required IReadOnlyList<IReadOnlyList<int>> EffectIdsByRelic { get; init; }
}

/// <summary>
/// マトリクス形式のビルド詳細です。
/// </summary>
public sealed class BuildMatrixDetail
{
    public required Build Build { get; init; }

    /// <summary>
    /// 遺物列 1〜6 ごとの Effect 行 Id 一覧（各列 0〜3件）。空列は空配列。
    /// </summary>
    public required IReadOnlyList<IReadOnlyList<int>> EffectIdsByRelic { get; init; }
}
