namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// ビルド（遺物 6 枠の構成）を表します。
/// </summary>
public sealed class Build : IEquatable<Build>
{
    /// <summary>
    /// ビルド ID を取得または設定します。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ビルド名を取得または設定します。
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// キャラクター名を取得または設定します。将来マスタ化までの識別用文字列です。
    /// </summary>
    public required string CharacterName { get; set; }

    /// <summary>
    /// 武器名を取得または設定します。将来マスタ化までの識別用文字列です。
    /// </summary>
    public required string WeaponName { get; set; }

    /// <summary>
    /// 作成日時（UTC）を取得または設定します。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新日時（UTC）を取得または設定します。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <inheritdoc />
    public bool Equals(Build? other)
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
    public override bool Equals(object? obj) => Equals(obj as Build);

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();
}
