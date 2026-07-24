using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Core.Interfaces;

/// <summary>
/// 火力計算マトリクス（効果×遺物6列）とビルドの同期を提供します。
/// </summary>
public interface IBuildMatrixService
{
    /// <summary>
    /// ビルドをマトリクス形式で読み込みます。存在しない場合は null です。
    /// </summary>
    Task<BuildMatrixDetail?> LoadAsync(int buildId, CancellationToken cancellationToken = default);

    /// <summary>
    /// マトリクス内容でビルドを保存し、ビルド専用遺物を同期します。
    /// </summary>
    Task<int> SaveAsync(BuildMatrixUpsertRequest request, CancellationToken cancellationToken = default);
}
