namespace NightreignRelicSimulator.Core.Models;

/// <summary>
/// データベースのスキーマバージョン情報を表します。
/// </summary>
public sealed class DatabaseInfo
{
    /// <summary>
    /// スキーマバージョンを取得または設定します。
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 初期化日時（UTC・ISO 8601）を取得または設定します。
    /// </summary>
    public string InitializedAt { get; set; } = string.Empty;
}
