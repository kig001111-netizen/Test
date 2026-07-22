using System.Data.Common;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// 遺物効果スロットの永続化を担当する Repository 契約です。
/// </summary>
public interface IRelicEffectRepository
{
    /// <summary>
    /// 指定遺物の効果スロットを取得します。
    /// </summary>
    Task<IReadOnlyList<RelicEffect>> GetByRelicIdAsync(
        DbConnection connection,
        int relicId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定効果を参照しているスロットを取得します。
    /// </summary>
    Task<IReadOnlyList<RelicEffect>> GetByEffectIdAsync(
        DbConnection connection,
        int effectId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物 ID とスロット番号で取得します。存在しない場合は null を返します。
    /// </summary>
    Task<RelicEffect?> GetByRelicIdAndSlotAsync(
        DbConnection connection,
        int relicId,
        int slotNumber,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果スロットを追加します。
    /// </summary>
    Task InsertAsync(
        DbConnection connection,
        RelicEffect relicEffect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果スロットを更新します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> UpdateAsync(
        DbConnection connection,
        RelicEffect relicEffect,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 効果スロットを削除します。対象が存在しない場合は false を返します。
    /// </summary>
    Task<bool> DeleteAsync(
        DbConnection connection,
        int relicId,
        int slotNumber,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定遺物の効果スロットをすべて削除します。
    /// </summary>
    Task<int> DeleteByRelicIdAsync(
        DbConnection connection,
        int relicId,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
