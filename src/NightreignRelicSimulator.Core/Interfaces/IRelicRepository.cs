using System.Data.Common;
using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// 遺物の永続化を担当する Repository 契約です。
/// </summary>
public interface IRelicRepository
{
    /// <summary>
    /// すべての遺物を取得します。
    /// </summary>
    Task<IReadOnlyList<Relic>> GetAllAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ID で遺物を取得します。存在しない場合は null を返します。
    /// </summary>
    Task<Relic?> GetByIdAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 名称の部分一致で遺物を検索します。
    /// </summary>
    Task<IReadOnlyList<Relic>> SearchByNameAsync(
        DbConnection connection,
        string keyword,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 色で遺物を取得します。
    /// </summary>
    Task<IReadOnlyList<Relic>> GetByColorAsync(
        DbConnection connection,
        RelicColor color,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物を追加し、採番された ID を返します。
    /// </summary>
    Task<int> InsertAsync(
        DbConnection connection,
        Relic relic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物を更新します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> UpdateAsync(
        DbConnection connection,
        Relic relic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物を削除します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> DeleteAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
