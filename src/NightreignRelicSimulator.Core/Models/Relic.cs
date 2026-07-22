using NightreignRelicSimulator.Core.Enums;

namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 登録済み遺物を表します。
/// </summary>
public sealed class Relic : IEquatable<Relic>
{
    /// <summary>
    /// 遺物 ID を取得または設定します。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 遺物名を取得または設定します。
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 遺物の色を取得または設定します。
    /// </summary>
    public RelicColor Color { get; set; }

    /// <summary>
    /// メモを取得または設定します。
    /// </summary>
    public required string Memo { get; set; }

    /// <summary>
    /// 作成日時（UTC）を取得または設定します。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新日時（UTC）を取得または設定します。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <inheritdoc />
    public bool Equals(Relic? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Id == 0 || other.Id == 0)
        {
            return false;
        }

        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Relic);

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();
}
