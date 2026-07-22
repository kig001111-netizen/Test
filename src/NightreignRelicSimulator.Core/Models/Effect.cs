namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// 効果マスタを表します。計算ロジックは持ちません。
/// </summary>
/// <remarks>
/// <see cref="Id"/> は DB 行の主キー（RelicEffect 参照用）です。
/// 業務上の効果識別子は <see cref="EffectId"/> です（段階効果は Level と組で一意）。
/// </remarks>
public sealed class Effect : IEquatable<Effect>
{
    /// <summary>
    /// DB 行 ID（主キー）を取得または設定します。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 効果 ID を取得または設定します。+値違いは別 ID、段階効果は同一 ID で Level を分けます。
    /// </summary>
    public int EffectId { get; set; }

    /// <summary>
    /// 効果名を取得または設定します。
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// UI 分類用カテゴリを取得または設定します。
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// 重複適用可能かどうかを取得または設定します。
    /// </summary>
    public bool CanStack { get; set; }

    /// <summary>
    /// 倍率（例: 1.04）を取得または設定します。百分率では保持しません。
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// 効果レベルを取得または設定します。通常は 1。段階効果のみ 2 以上を持ちます。
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// 説明を取得または設定します。
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// UI 表示順を取得または設定します。計算順には使用しません。
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <inheritdoc />
    public bool Equals(Effect? other)
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
    public override bool Equals(object? obj) => Equals(obj as Effect);

    /// <inheritdoc />
    public override int GetHashCode() => Id.GetHashCode();
}
