using System.Data.Common;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// ビルドの永続化を担当する Repository 契約です。
/// </summary>
public interface IBuildRepository
{
    /// <summary>
    /// すべてのビルドを取得します。
    /// </summary>
    Task<IReadOnlyList<Build>> GetAllAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ID でビルドを取得します。存在しない場合は null を返します。
    /// </summary>
    Task<Build?> GetByIdAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 名称の部分一致でビルドを検索します。
    /// </summary>
    Task<IReadOnlyList<Build>> SearchByNameAsync(
        DbConnection connection,
        string keyword,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// キャラクター名でビルドを取得します。
    /// </summary>
    Task<IReadOnlyList<Build>> GetByCharacterNameAsync(
        DbConnection connection,
        string characterName,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ビルドを追加し、採番された ID を返します。
    /// </summary>
    Task<int> InsertAsync(
        DbConnection connection,
        Build build,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ビルドを更新します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> UpdateAsync(
        DbConnection connection,
        Build build,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ビルドを削除します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> DeleteAsync(
        DbConnection connection,
        int id,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
