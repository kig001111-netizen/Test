using System.Data.Common;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// 効果マスタの永続化を担当する Repository 契約です。
/// </summary>
public interface IEffectRepository
{
    /// <summary>
    /// すべての効果を表示順で取得します。
    /// </summary>
    Task<IReadOnlyList<Effect>> GetAllAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ID で効果を取得します。存在しない場合は null を返します。
    /// </summary>
    Task<Effect?> GetByIdAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 名称の部分一致で効果を検索します。
    /// </summary>
    Task<IReadOnlyList<Effect>> SearchByNameAsync(
        DbConnection connection,
        string keyword,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// カテゴリで効果を取得します。
    /// </summary>
    Task<IReadOnlyList<Effect>> GetByCategoryAsync(
        DbConnection connection,
        string category,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果を追加し、採番された ID を返します。
    /// </summary>
    Task<int> InsertAsync(
        DbConnection connection,
        Effect effect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果を更新します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> UpdateAsync(
        DbConnection connection,
        Effect effect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果を削除します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> DeleteAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
