using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// 遺物の業務処理を提供します。
/// </summary>
public interface IRelicService
{
    /// <summary>
    /// 遺物一覧を取得します。
    /// </summary>
    Task<IReadOnlyList<Relic>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物詳細（効果スロット含む）を取得します。存在しない場合は null を返します。
    /// </summary>
    Task<RelicDetail?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 名称で遺物を検索します。
    /// </summary>
    Task<IReadOnlyList<Relic>> SearchByNameAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 色で遺物を取得します。
    /// </summary>
    Task<IReadOnlyList<Relic>> GetByColorAsync(RelicColor color, CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物を登録します。
    /// </summary>
    Task<int> RegisterAsync(RelicUpsertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物を更新します。
    /// </summary>
    Task UpdateAsync(RelicUpsertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 遺物を削除します。
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
