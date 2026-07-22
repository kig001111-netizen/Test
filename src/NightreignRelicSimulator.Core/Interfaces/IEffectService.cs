using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// 効果マスタの業務処理を提供します。
/// </summary>
public interface IEffectService
{
    /// <summary>
    /// 効果一覧を取得します。
    /// </summary>
    Task<IReadOnlyList<Effect>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ID で効果を取得します。存在しない場合は null を返します。
    /// </summary>
    Task<Effect?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 名称で効果を検索します。
    /// </summary>
    Task<IReadOnlyList<Effect>> SearchByNameAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// カテゴリで効果を取得します。
    /// </summary>
    Task<IReadOnlyList<Effect>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果を登録します。
    /// </summary>
    Task<int> CreateAsync(Effect effect, CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果を更新します。
    /// </summary>
    Task UpdateAsync(Effect effect, CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果を削除します。
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
