namespace NightreignRelicSimulator.Core.Constants;

/// <summary>
/// アプリケーション全体で共有する定数を定義します。
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// アプリケーション名。
    /// </summary>
    public const string ApplicationName = "Nightreign Relic Simulator";

    /// <summary>
    /// 1つの遺物が保持できる効果の最大数。
    /// </summary>
    public const int EffectsPerRelic = 3;

    /// <summary>
    /// 1ビルドを構成する遺物の最大数。
    /// </summary>
    public const int RelicsPerBuild = 6;

    /// <summary>
    /// 倍率計算の初期値（乗算の単位元）。
    /// </summary>
    public const decimal BaseMultiplier = 1.0m;
}
