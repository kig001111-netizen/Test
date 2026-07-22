namespace NightreignRelicSimulator.App;

/// <summary>
/// 画面間で共有する一時状態です（DB 非永続）。
/// </summary>
internal static class UiSessionState
{
    /// <summary>直近の武器表示火力。</summary>
    public static decimal WeaponAttack { get; set; } = 1000m;

    /// <summary>直近に選択・編集したビルド Id。</summary>
    public static int? SelectedBuildId { get; set; }
}
