using NightreignRelicSimulator.Core.Interfaces;
using NightreignRelicSimulator.Services.Builds;
using NightreignRelicSimulator.Services.Calculation;
using NightreignRelicSimulator.Services.Effects;
using NightreignRelicSimulator.Services.Relics;

namespace NightreignRelicSimulator.App;

/// <summary>
/// UI から利用する Service の構成ルートです。Repository は公開しません。
/// </summary>
internal static class AppServices
{
    /// <summary>効果マスタ Service。</summary>
    public static IEffectService Effects { get; } = new EffectService();

    /// <summary>遺物 Service。</summary>
    public static IRelicService Relics { get; } = new RelicService();

    /// <summary>ビルド Service。</summary>
    public static IBuildService Builds { get; } = new BuildService();

    /// <summary>マトリクス形式のビルド同期。</summary>
    public static IBuildMatrixService BuildMatrix { get; } = new BuildMatrixService();

    /// <summary>火力計算。</summary>
    public static DamageCalculator DamageCalculator { get; } = new DamageCalculator();
}
