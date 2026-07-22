namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 遺物と効果スロットの対応を表します。
/// </summary>
public sealed class RelicEffect : IEquatable<RelicEffect>
{
    /// <summary>
    /// 遺物 ID を取得または設定します。
    /// </summary>
    public int RelicId { get; set; }

    /// <summary>
    /// 効果 ID を取得または設定します。
    /// </summary>
    public int EffectId { get; set; }

    /// <summary>
    /// スロット番号（1〜3）を取得または設定します。
    /// </summary>
    public int SlotNumber { get; set; }

    /// <inheritdoc />
    public bool Equals(RelicEffect? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return RelicId == other.RelicId && SlotNumber == other.SlotNumber;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as RelicEffect);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(RelicId, SlotNumber);
}
