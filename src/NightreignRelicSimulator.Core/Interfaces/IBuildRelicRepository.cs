using System.Data.Common;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// ビルド装備スロットの永続化を担当する Repository 契約です。
/// </summary>
public interface IBuildRelicRepository
{
    /// <summary>
    /// 指定ビルドの装備スロットを取得します。
    /// </summary>
    Task<IReadOnlyList<BuildRelic>> GetByBuildIdAsync(
        DbConnection connection,
        int buildId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定遺物を装備しているスロットを取得します。
    /// </summary>
    Task<IReadOnlyList<BuildRelic>> GetByRelicIdAsync(
        DbConnection connection,
        int relicId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ビルド ID と装備位置で取得します。存在しない場合は null を返します。
    /// </summary>
    Task<BuildRelic?> GetByBuildIdAndPositionAsync(
        DbConnection connection,
        int buildId,
        int position,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 装備スロットを追加します。
    /// </summary>
    Task InsertAsync(
        DbConnection connection,
        BuildRelic buildRelic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 装備スロットを更新します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> UpdateAsync(
        DbConnection connection,
        BuildRelic buildRelic,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 装備スロットを削除します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> DeleteAsync(
        DbConnection connection,
        int buildId,
        int position,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定ビルドの装備スロットをすべて削除します。
    /// </summary>
    Task<int> DeleteByBuildIdAsync(
        DbConnection connection,
        int buildId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
