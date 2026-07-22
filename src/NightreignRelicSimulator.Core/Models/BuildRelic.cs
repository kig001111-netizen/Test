namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// ビルドと遺物装備位置の対応を表します。
/// </summary>
public sealed class BuildRelic : IEquatable<BuildRelic>
{
    /// <summary>
    /// ビルド ID を取得または設定します。
    /// </summary>
    public int BuildId { get; set; }

    /// <summary>
    /// 装備位置（1〜6）を取得または設定します。
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// 遺物 ID を取得または設定します。同一ビルド内での重複装備を許容します。
    /// </summary>
    public int RelicId { get; set; }

    /// <inheritdoc />
    public bool Equals(BuildRelic? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return BuildId == other.BuildId && Position == other.Position;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as BuildRelic);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(BuildId, Position);
}
