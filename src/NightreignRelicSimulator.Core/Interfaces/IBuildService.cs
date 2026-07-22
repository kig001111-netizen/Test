using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// ビルドの業務処理を提供します。
/// </summary>
public interface IBuildService
{
    /// <summary>
    /// ビルド一覧を取得します。
    /// </summary>
    Task<IReadOnlyList<Build>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ビルド詳細（装備スロット含む）を読み込みます。存在しない場合は null を返します。
    /// </summary>
    Task<BuildDetail?> LoadAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 名称でビルドを検索します。
    /// </summary>
    Task<IReadOnlyList<Build>> SearchByNameAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// キャラクター名でビルドを取得します。
    /// </summary>
    Task<IReadOnlyList<Build>> GetByCharacterNameAsync(string characterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// ビルドを保存します（新規または更新）。保存後のビルド ID を返します。
    /// </summary>
    Task<int> SaveAsync(BuildUpsertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// ビルドを削除します。
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
